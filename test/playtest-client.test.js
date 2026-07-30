import test from "node:test";
import assert from "node:assert/strict";
import { createPlaytestClient } from "../src/playtest-client.js";

class MemoryStorage {
  constructor() {
    this.data = new Map();
    this.operations = [];
  }

  get size() {
    return this.data.size;
  }

  getItem(key) {
    this.operations.push(["get", key]);
    return this.data.has(key) ? this.data.get(key) : null;
  }

  setItem(key, value) {
    this.operations.push(["set", key]);
    this.data.set(key, String(value));
  }

  removeItem(key) {
    this.operations.push(["remove", key]);
    this.data.delete(key);
  }

  values() {
    return this.data.values();
  }
}

function makeUuidFactory() {
  let sequence = 0;
  return () => {
    sequence += 1;
    return `00000000-0000-4000-8000-${String(sequence).padStart(12, "0")}`;
  };
}

function createHarness({
  config = { cohort: "TG1", source: "telegram", build: "TG1-01" },
  responseStatus = 200,
  responseStatuses = [],
  storage = new MemoryStorage(),
} = {}) {
  const requests = [];
  const randomUUID = makeUuidFactory();
  const fetchImpl = async (url, init) => {
    const body = init?.body ? JSON.parse(init.body) : null;
    requests.push({ url: String(url), init, body });
    const status = responseStatuses.length > 0
      ? responseStatuses.shift()
      : responseStatus;
    const accepted = body?.events?.map((event) => event.eventId) || [];
    return {
      ok: status >= 200 && status < 300,
      status,
      async json() {
        return { accepted };
      },
    };
  };
  const client = createPlaytestClient({
    config,
    endpoint: "https://collector.example",
    storage,
    fetchImpl,
    randomUUID,
    now: () => new Date(2026, 6, 30, 12, 0),
  });

  return { client, requests, storage };
}

test("inactive mode creates no keys or requests", async () => {
  const harness = createHarness({ config: null });

  assert.equal(harness.client.status().mode, "inactive");
  assert.equal(harness.storage.size, 0);
  assert.equal(harness.storage.operations.length, 0);
  assert.equal(harness.requests.length, 0);
});

test("blocked storage keeps the game in honest unavailable mode", () => {
  const storage = {
    getItem() {
      throw new Error("blocked");
    },
    setItem() {
      throw new Error("blocked");
    },
    removeItem() {
      throw new Error("blocked");
    },
  };
  const harness = createHarness({ storage });

  assert.equal(harness.client.status().mode, "unavailable");
  assert.equal(harness.client.join(), false);
  assert.equal(harness.requests.length, 0);
});

test("private choice stores no participant identity", () => {
  const harness = createHarness();

  assert.equal(harness.client.status().mode, "undecided");
  harness.client.choosePrivate();

  assert.equal(harness.client.status().mode, "private");
  assert.equal(harness.client.status().participantId, null);
  assert.equal(
    [...harness.storage.values()].some((value) => value.includes("participantId")),
    false,
  );
});

test("join queues once and removes only acknowledged events", async () => {
  const harness = createHarness();

  assert.equal(harness.client.join(), true);
  const result = await harness.client.recordStarted("orchard", "external");

  assert.equal(result.queued, true);
  assert.equal(harness.requests.length, 1);
  assert.equal(harness.requests[0].url, "https://collector.example/v1/events");
  assert.equal(harness.requests[0].body.events[0].type, "round_started");
  assert.equal(harness.client.status().pending, 0);
  assert.equal(harness.client.status().participantId.includes("-"), true);
});

test("round finish stores only a bounded semantic summary", async () => {
  const harness = createHarness();
  harness.client.join();

  await harness.client.recordFinished("cloud", {
    outcome: "completed",
    durationSeconds: 81.2,
    progressPercent: 100,
    badges: 8,
    jumps: 5,
    obstaclesHit: 1,
    obstaclesCleared: 1,
  });

  const event = harness.requests[0].body.events[0];
  assert.equal(event.type, "round_finished");
  assert.equal(event.durationSeconds, 81);
  assert.equal(event.routeId, "cloud");
  assert.equal("coordinates" in event, false);
});

test("network failure keeps the earliest fifty events without breaking play", async () => {
  const harness = createHarness({ responseStatus: 503 });
  harness.client.join();

  for (let index = 0; index < 60; index += 1) {
    await harness.client.recordRetry("orchard");
  }

  assert.equal(harness.client.status().pending, 50);
  assert.equal(harness.client.status().dropped, 10);
  assert.equal(harness.requests.length, 60);
});

test("a later successful flush acknowledges an existing outbox", async () => {
  const harness = createHarness({ responseStatuses: [503, 200] });
  harness.client.join();

  await harness.client.recordRetry("orchard");
  assert.equal(harness.client.status().pending, 1);
  const result = await harness.client.flush();

  assert.deepEqual(result, { delivered: true, accepted: 1 });
  assert.equal(harness.client.status().pending, 0);
});

test("a long offline outbox reconnects in server-sized batches", async () => {
  const batches = [];
  let online = false;
  const client = createPlaytestClient({
    config: { cohort: "TG1", source: "telegram", build: "TG1-01" },
    endpoint: "https://collector.example",
    storage: new MemoryStorage(),
    randomUUID: makeUuidFactory(),
    now: () => new Date(2026, 6, 30, 12, 0),
    fetchImpl: async (url, init) => {
      const body = JSON.parse(init.body);
      if (!online) return { ok: false, status: 503 };
      batches.push(body.events);
      return {
        ok: true,
        async json() {
          return { accepted: body.events.map((event) => event.eventId) };
        },
      };
    },
  });
  client.join();

  for (let index = 0; index < 25; index += 1) {
    await client.recordRetry("orchard");
  }
  assert.equal(client.status().pending, 25);

  online = true;
  const result = await client.flush();

  assert.deepEqual(batches.map((batch) => batch.length), [20, 5]);
  assert.deepEqual(result, { delivered: true, accepted: 25 });
  assert.equal(client.status().pending, 0);
});

test("an event queued during delivery is flushed before the shared flush settles", async () => {
  const storage = new MemoryStorage();
  const requests = [];
  let releaseFirst;
  const firstDelivery = new Promise((resolve) => {
    releaseFirst = resolve;
  });
  const client = createPlaytestClient({
    config: { cohort: "TG1", source: "telegram", build: "TG1-01" },
    endpoint: "https://collector.example",
    storage,
    randomUUID: makeUuidFactory(),
    now: () => new Date(2026, 6, 30, 12, 0),
    fetchImpl: async (url, init) => {
      const body = JSON.parse(init.body);
      requests.push({ url, body });
      if (requests.length === 1) await firstDelivery;
      return {
        ok: true,
        async json() {
          return { accepted: body.events.map((event) => event.eventId) };
        },
      };
    },
  });
  client.join();

  const retryPromise = client.recordRetry("orchard");
  await Promise.resolve();
  const startPromise = client.recordStarted("orchard", "external");
  releaseFirst();
  await Promise.all([retryPromise, startPromise]);

  assert.deepEqual(
    requests.map((request) => request.body.events[0].type),
    ["retry_clicked", "round_started"],
  );
  assert.equal(client.status().pending, 0);
});

test("delete removes server and local telemetry state only after success", async () => {
  const harness = createHarness();
  harness.client.join();
  const participantId = harness.client.status().participantId;

  const result = await harness.client.deleteData();

  assert.deepEqual(result, { deleted: true });
  assert.equal(harness.requests.at(-1).url, "https://collector.example/v1/delete");
  assert.deepEqual(harness.requests.at(-1).body, {
    version: 1,
    cohort: "TG1",
    participantId,
  });
  assert.equal(harness.client.status().participantId, null);
  assert.equal(harness.client.status().mode, "undecided");
  assert.equal(harness.storage.size, 0);
});

test("failed deletion preserves local identity and queued data", async () => {
  const harness = createHarness({ responseStatus: 503 });
  harness.client.join();
  await harness.client.recordRetry("orchard");
  const participantId = harness.client.status().participantId;

  const result = await harness.client.deleteData();

  assert.deepEqual(result, { deleted: false });
  assert.equal(harness.client.status().participantId, participantId);
  assert.equal(harness.client.status().pending, 1);
});

test("corrupt scoped state returns to an undecided clean mode", () => {
  const storage = new MemoryStorage();
  storage.setItem("wobble-stack-playtest-choice:v1:TG1-01:TG1", "joined");
  storage.setItem("wobble-stack-playtest:v1:TG1-01:TG1", "{broken");

  const harness = createHarness({ storage });

  assert.equal(harness.client.status().mode, "undecided");
  assert.equal(harness.client.status().participantId, null);
  assert.equal(harness.storage.size, 0);
});

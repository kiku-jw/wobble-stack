import assert from "node:assert/strict";
import test from "node:test";

import worker, { handleRequest } from "../src/index.js";
import * as workerEntry from "../src/index.js";

const GAME_ORIGIN = "https://kiku-jw.github.io";
const participantId = "11111111-1111-4111-8111-111111111111";
const eventId = "22222222-2222-4222-8222-222222222222";
const started = {
  version: 1,
  participantId,
  cohort: "TG1",
  build: "TG1-01",
  source: "telegram",
  localDay: "2026-07-30",
  clientSequence: 1,
  routeId: "orchard",
  eventId,
  type: "round_started",
  launchContext: "telegram",
};

class FakeStatement {
  constructor(database, sql) {
    this.database = database;
    this.sql = sql.replace(/\s+/g, " ").trim();
    this.params = [];
  }

  bind(...params) {
    this.params = params;
    return this;
  }

  async run() {
    this.database.runs.push(this);
    return { success: true };
  }
}

class FakeDatabase {
  constructor() {
    this.prepared = [];
    this.batches = [];
    this.runs = [];
  }

  prepare(sql) {
    const statement = new FakeStatement(this, sql);
    this.prepared.push(statement);
    return statement;
  }

  async batch(statements) {
    this.batches.push(statements);
    return statements.map(() => ({ success: true }));
  }
}

function createEnv({ rateLimitSuccess = true } = {}) {
  const calls = [];
  return {
    GAME_ORIGIN,
    ACTIVE_BUILDS: "TG1-01",
    ACTIVE_COHORTS: "TG1,TG1-PILOT",
    ACTIVE_SOURCES: "telegram,website,pilot",
    DB: new FakeDatabase(),
    EVENTS_RATE_LIMITER: {
      calls,
      async limit(input) {
        calls.push(input);
        return { success: rateLimitSuccess };
      },
    },
  };
}

function request(path, {
  method = "POST",
  origin = GAME_ORIGIN,
  body,
  headers = {},
} = {}) {
  return new Request(`https://collector.example${path}`, {
    method,
    headers: {
      ...(origin ? { Origin: origin } : {}),
      ...(body === undefined ? {} : { "Content-Type": "application/json" }),
      ...headers,
    },
    body: body === undefined ? undefined : body,
  });
}

test("entry module exposes only Worker-compatible named handlers", () => {
  for (const [name, value] of Object.entries(workerEntry)) {
    if (name === "default") continue;
    assert.equal(typeof value, "function", `${name} must be a function`);
  }
});

test("health returns only a readiness result", async () => {
  const response = await handleRequest(
    request("/health", { method: "GET", origin: null }),
    createEnv(),
  );

  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { ok: true });
  assert.equal(response.headers.get("Access-Control-Allow-Origin"), null);
});

test("disallowed origins are rejected before any write", async () => {
  const env = createEnv();
  const response = await handleRequest(
    request("/v1/events", {
      origin: "https://attacker.example",
      body: JSON.stringify({ events: [started] }),
    }),
    env,
  );

  assert.equal(response.status, 403);
  assert.equal(env.DB.batches.length, 0);
  assert.equal(env.EVENTS_RATE_LIMITER.calls.length, 0);
  assert.equal(response.headers.get("Access-Control-Allow-Origin"), null);
});

test("preflight exposes the exact game-origin CORS policy", async () => {
  const response = await handleRequest(
    request("/v1/events", { method: "OPTIONS" }),
    createEnv(),
  );

  assert.equal(response.status, 204);
  assert.equal(
    response.headers.get("Access-Control-Allow-Origin"),
    GAME_ORIGIN,
  );
  assert.equal(
    response.headers.get("Access-Control-Allow-Methods"),
    "POST, OPTIONS",
  );
  assert.equal(
    response.headers.get("Access-Control-Allow-Headers"),
    "Content-Type",
  );
  assert.equal(response.headers.get("Vary"), "Origin");
});

test("invalid and oversized bodies fail without echoing input", async () => {
  const invalid = await handleRequest(
    request("/v1/events", { body: "{\"secret\":\"do-not-echo\"" }),
    createEnv(),
  );
  assert.equal(invalid.status, 400);
  assert.equal((await invalid.text()).includes("do-not-echo"), false);

  const oversizedBody = JSON.stringify({ filler: "x".repeat(5000) });
  const oversized = await handleRequest(
    request("/v1/events", { body: oversizedBody }),
    createEnv(),
  );
  assert.equal(oversized.status, 413);
  assert.equal((await oversized.text()).includes("xxxx"), false);
});

test("rate limiting happens before D1 writes", async () => {
  const env = createEnv({ rateLimitSuccess: false });
  const response = await handleRequest(
    request("/v1/events", {
      body: JSON.stringify({ events: [started] }),
    }),
    env,
  );

  assert.equal(response.status, 429);
  assert.equal(env.DB.batches.length, 0);
  assert.deepEqual(env.EVENTS_RATE_LIMITER.calls, [{
    key: `TG1:${participantId}`,
  }]);
});

test("accepted events use bound statements and acknowledge event IDs", async () => {
  const env = createEnv();
  const response = await handleRequest(
    request("/v1/events", {
      body: JSON.stringify({ events: [started] }),
    }),
    env,
  );

  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { accepted: [eventId] });
  assert.equal(env.DB.batches.length, 1);
  assert.equal(env.DB.batches[0].length, 2);
  assert.match(env.DB.batches[0][0].sql, /INSERT OR IGNORE INTO events/);
  assert.equal(env.DB.batches[0][0].sql.includes(participantId), false);
  assert.equal(env.DB.batches[0][0].params[1], participantId);
  assert.match(env.DB.batches[0][1].sql, /INSERT INTO cohort_members/);
  assert.deepEqual(env.DB.batches[0][1].params, [
    "TG1",
    participantId,
    "TG1",
  ]);
});

test("delete removes events and anonymizes the occupied cohort slot", async () => {
  const env = createEnv();
  const response = await handleRequest(
    request("/v1/delete", {
      body: JSON.stringify({
        version: 1,
        cohort: "TG1",
        participantId,
      }),
    }),
    env,
  );

  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), { deleted: true });
  assert.equal(env.DB.batches[0].length, 2);
  assert.match(env.DB.batches[0][0].sql, /DELETE FROM events/);
  assert.match(env.DB.batches[0][1].sql, /participant_id = NULL/);
  assert.deepEqual(env.DB.batches[0][1].params, ["TG1", participantId]);
});

test("unknown routes return 404 without touching bindings", async () => {
  const env = createEnv();
  const response = await handleRequest(
    request("/private-report", { method: "GET", origin: null }),
    env,
  );

  assert.equal(response.status, 404);
  assert.equal(env.DB.prepared.length, 0);
  assert.equal(env.EVENTS_RATE_LIMITER.calls.length, 0);
});

test("scheduled retention deletes raw events older than 30 days", async () => {
  const env = createEnv();
  let retentionPromise;
  worker.scheduled({}, env, {
    waitUntil(promise) {
      retentionPromise = promise;
    },
  });
  await retentionPromise;

  assert.equal(env.DB.runs.length, 1);
  assert.match(env.DB.runs[0].sql, /2592000/);
});

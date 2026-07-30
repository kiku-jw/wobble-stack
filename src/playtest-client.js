import {
  PLAYTEST_VERSION,
  createFinishedEvent,
  createRetryEvent,
  createStartedEvent,
  getLocalDay,
} from "./playtest-logic.js";

const STATE_PREFIX = "wobble-stack-playtest:v1";
const CHOICE_PREFIX = "wobble-stack-playtest-choice:v1";
const MAX_PENDING_EVENTS = 50;
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function normalizeEndpoint(value) {
  try {
    const url = new URL(value);
    const localHttp =
      url.protocol === "http:" &&
      (url.hostname === "127.0.0.1" || url.hostname === "localhost");
    if (url.protocol !== "https:" && !localHttp) return null;
    url.search = "";
    url.hash = "";
    return url.href.replace(/\/$/, "");
  } catch {
    return null;
  }
}

function isStoredState(value) {
  return Boolean(
    value &&
    value.version === PLAYTEST_VERSION &&
    UUID_PATTERN.test(String(value.participantId)) &&
    Number.isInteger(value.sequence) &&
    value.sequence >= 0 &&
    Number.isInteger(value.dropped) &&
    value.dropped >= 0 &&
    Array.isArray(value.pending) &&
    value.pending.length <= MAX_PENDING_EVENTS,
  );
}

export function createPlaytestClient({
  config,
  endpoint,
  storage,
  fetchImpl,
  randomUUID,
  now,
}) {
  const normalizedEndpoint = normalizeEndpoint(endpoint);
  const active = Boolean(config && normalizedEndpoint);

  if (!active) {
    return createInactiveClient();
  }

  const stateKey = `${STATE_PREFIX}:${config.build}:${config.cohort}`;
  const choiceKey = `${CHOICE_PREFIX}:${config.build}:${config.cohort}`;
  let mode = "undecided";
  let state = null;
  let storageAvailable = true;
  let flushPromise = null;

  function removeLocalState() {
    try {
      storage.removeItem(stateKey);
      storage.removeItem(choiceKey);
    } catch {
      storageAvailable = false;
    }
  }

  try {
    const choice = storage.getItem(choiceKey);
    if (choice === "private") {
      mode = "private";
    } else if (choice === "joined") {
      const parsed = JSON.parse(storage.getItem(stateKey) || "null");
      if (isStoredState(parsed)) {
        state = parsed;
        mode = "joined";
      } else {
        removeLocalState();
      }
    } else if (storage.getItem(stateKey) !== null) {
      storage.removeItem(stateKey);
    }
  } catch {
    removeLocalState();
    mode = storageAvailable ? "undecided" : "unavailable";
  }

  function persistJoinedState() {
    try {
      storage.setItem(stateKey, JSON.stringify(state));
      storage.setItem(choiceKey, "joined");
      return true;
    } catch {
      removeLocalState();
      state = null;
      mode = "unavailable";
      storageAvailable = false;
      return false;
    }
  }

  function status() {
    return {
      mode,
      participantId: state?.participantId || null,
      pending: state?.pending.length || 0,
      dropped: state?.dropped || 0,
      storageAvailable,
    };
  }

  function join() {
    if (mode === "joined") return true;
    if (mode !== "undecided") return false;

    let participantId;
    try {
      participantId = randomUUID();
    } catch {
      mode = "unavailable";
      return false;
    }
    if (!UUID_PATTERN.test(String(participantId))) {
      mode = "unavailable";
      return false;
    }

    state = {
      version: PLAYTEST_VERSION,
      participantId,
      sequence: 0,
      dropped: 0,
      pending: [],
    };
    mode = "joined";
    return persistJoinedState();
  }

  function choosePrivate() {
    if (mode !== "undecided") return mode === "private";

    try {
      storage.setItem(choiceKey, "private");
      mode = "private";
      return true;
    } catch {
      storageAvailable = false;
      mode = "unavailable";
      return false;
    }
  }

  function eventBase(routeId) {
    state.sequence += 1;
    return {
      participantId: state.participantId,
      cohort: config.cohort,
      source: config.source,
      localDay: getLocalDay(now()),
      clientSequence: state.sequence,
      routeId,
    };
  }

  async function record(factory) {
    if (mode !== "joined" || !state) return { queued: false };

    if (state.pending.length >= MAX_PENDING_EVENTS) {
      state.dropped += 1;
      persistJoinedState();
      await flush();
      return { queued: false };
    }

    let event;
    try {
      event = factory();
    } catch {
      return { queued: false };
    }

    state.pending.push(event);
    if (!persistJoinedState()) return { queued: false };
    await flush();
    return { queued: true };
  }

  async function recordStarted(routeId, launchContext) {
    return record(() => createStartedEvent(
      { ...eventBase(routeId), launchContext },
      randomUUID,
    ));
  }

  async function recordFinished(routeId, summary) {
    return record(() => createFinishedEvent(
      { ...eventBase(routeId), ...summary },
      randomUUID,
    ));
  }

  async function recordRetry(routeId) {
    return record(() => createRetryEvent(eventBase(routeId), randomUUID));
  }

  async function flushPending() {
    if (mode !== "joined" || !state || state.pending.length === 0) {
      return { delivered: false, accepted: 0 };
    }

    const snapshot = [...state.pending];
    try {
      const response = await fetchImpl(`${normalizedEndpoint}/v1/events`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ events: snapshot }),
      });
      if (!response.ok) return { delivered: false, accepted: 0 };

      const payload = await response.json();
      const snapshotIds = new Set(snapshot.map((event) => event.eventId));
      const acceptedIds = new Set(
        Array.isArray(payload?.accepted)
          ? payload.accepted.filter((id) => snapshotIds.has(id))
          : [],
      );
      if (acceptedIds.size === 0) {
        return { delivered: false, accepted: 0 };
      }

      state.pending = state.pending.filter(
        (event) => !acceptedIds.has(event.eventId),
      );
      if (!persistJoinedState()) {
        return { delivered: false, accepted: 0 };
      }
      return { delivered: true, accepted: acceptedIds.size };
    } catch {
      return { delivered: false, accepted: 0 };
    }
  }

  async function flushAll() {
    let accepted = 0;
    let delivered = false;

    while (mode === "joined" && state?.pending.length > 0) {
      const result = await flushPending();
      if (!result.delivered) break;
      delivered = true;
      accepted += result.accepted;
    }

    return { delivered, accepted };
  }

  function flush() {
    if (flushPromise) return flushPromise;
    flushPromise = flushAll().finally(() => {
      flushPromise = null;
    });
    return flushPromise;
  }

  async function deleteData() {
    if (mode === "private") {
      removeLocalState();
      mode = "undecided";
      return { deleted: true };
    }
    if (mode !== "joined" || !state) return { deleted: false };

    try {
      if (flushPromise) await flushPromise;
      const response = await fetchImpl(`${normalizedEndpoint}/v1/delete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          version: PLAYTEST_VERSION,
          cohort: config.cohort,
          participantId: state.participantId,
        }),
      });
      if (!response.ok) return { deleted: false };

      removeLocalState();
      state = null;
      mode = "undecided";
      return { deleted: true };
    } catch {
      return { deleted: false };
    }
  }

  return {
    status,
    join,
    choosePrivate,
    recordStarted,
    recordFinished,
    recordRetry,
    flush,
    deleteData,
  };
}

function createInactiveClient() {
  const noEvent = async () => ({ queued: false });
  return {
    status: () => ({
      mode: "inactive",
      participantId: null,
      pending: 0,
      dropped: 0,
      storageAvailable: true,
    }),
    join: () => false,
    choosePrivate: () => false,
    recordStarted: noEvent,
    recordFinished: noEvent,
    recordRetry: noEvent,
    flush: async () => ({ delivered: false, accepted: 0 }),
    deleteData: async () => ({ deleted: false }),
  };
}

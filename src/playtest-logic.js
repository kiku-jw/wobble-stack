export const PLAYTEST_VERSION = 1;
export const PLAYTEST_BUILD = "TG1-01";

const PLAYTEST_COHORTS = new Set(["TG1", "TG1-PILOT"]);
const PLAYTEST_SOURCES = new Set(["telegram", "website", "pilot"]);
const ROUTE_IDS = new Set(["orchard", "cloud", "windmill"]);
const OUTCOMES = new Set(["loss", "completed"]);
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function boundedInteger(value, minimum, maximum) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) return minimum;
  return Math.min(maximum, Math.max(minimum, Math.round(numeric)));
}

function requireUuid(value, label) {
  if (!UUID_PATTERN.test(String(value))) {
    throw new TypeError(`Invalid ${label}`);
  }
  return String(value);
}

function requireBase(base) {
  if (!base || !PLAYTEST_COHORTS.has(base.cohort)) {
    throw new TypeError("Invalid playtest cohort");
  }
  if (!PLAYTEST_SOURCES.has(base.source)) {
    throw new TypeError("Invalid playtest source");
  }
  if (!ROUTE_IDS.has(base.routeId)) {
    throw new TypeError("Invalid route");
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(String(base.localDay))) {
    throw new TypeError("Invalid local day");
  }

  return {
    version: PLAYTEST_VERSION,
    participantId: requireUuid(base.participantId, "participant ID"),
    cohort: base.cohort,
    build: PLAYTEST_BUILD,
    source: base.source,
    localDay: base.localDay,
    clientSequence: boundedInteger(base.clientSequence, 1, 1000000),
    routeId: base.routeId,
  };
}

function defaultRandomUuid() {
  if (typeof globalThis.crypto?.randomUUID !== "function") {
    throw new TypeError("Secure random UUID is unavailable");
  }
  return globalThis.crypto.randomUUID();
}

function createEventId(randomUuid) {
  return requireUuid(randomUuid(), "event ID");
}

export function parsePlaytestConfig(search) {
  const params = new URLSearchParams(search);
  const cohort = params.get("playtest") || "";
  const source = params.get("source") || "";

  return PLAYTEST_COHORTS.has(cohort) && PLAYTEST_SOURCES.has(source)
    ? { cohort, source, build: PLAYTEST_BUILD }
    : null;
}

export function getLocalDay(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function createStartedEvent(base, randomUuid = defaultRandomUuid) {
  return {
    ...requireBase(base),
    eventId: createEventId(randomUuid),
    type: "round_started",
    launchContext: base.launchContext === "telegram" ? "telegram" : "external",
  };
}

export function createFinishedEvent(base, randomUuid = defaultRandomUuid) {
  if (!OUTCOMES.has(base?.outcome)) {
    throw new TypeError("Invalid outcome");
  }

  return {
    ...requireBase(base),
    eventId: createEventId(randomUuid),
    type: "round_finished",
    outcome: base.outcome,
    durationSeconds: boundedInteger(base.durationSeconds, 0, 1800),
    progressPercent: boundedInteger(base.progressPercent, 0, 100),
    badges: boundedInteger(base.badges, 0, 20),
    jumps: boundedInteger(base.jumps, 0, 1000),
    obstaclesHit: boundedInteger(base.obstaclesHit, 0, 20),
    obstaclesCleared: boundedInteger(base.obstaclesCleared, 0, 20),
  };
}

export function createRetryEvent(base, randomUuid = defaultRandomUuid) {
  return {
    ...requireBase(base),
    eventId: createEventId(randomUuid),
    type: "retry_clicked",
  };
}

export function isRetryReason(reason) {
  return reason === "retry";
}

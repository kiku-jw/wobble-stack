const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const LOCAL_DAY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const ROUTE_IDS = new Set(["orchard", "cloud", "windmill"]);
const EVENT_TYPES = new Set([
  "round_started",
  "round_finished",
  "retry_clicked",
]);
const OUTCOMES = new Set(["loss", "completed"]);
const LAUNCH_CONTEXTS = new Set(["telegram", "external"]);

const COMMON_FIELDS = [
  "version",
  "participantId",
  "cohort",
  "build",
  "source",
  "localDay",
  "clientSequence",
  "routeId",
  "eventId",
  "type",
];

const EVENT_FIELDS = {
  round_started: [...COMMON_FIELDS, "launchContext"],
  round_finished: [
    ...COMMON_FIELDS,
    "outcome",
    "durationSeconds",
    "progressPercent",
    "badges",
    "jumps",
    "obstaclesHit",
    "obstaclesCleared",
  ],
  retry_clicked: COMMON_FIELDS,
};

function isPlainObject(value) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  const prototype = Object.getPrototypeOf(value);
  return prototype === Object.prototype || prototype === null;
}

function requireExactFields(value, fields, label) {
  if (!isPlainObject(value)) {
    throw new TypeError(`Invalid ${label}`);
  }

  const allowed = new Set(fields);
  for (const key of Object.keys(value)) {
    if (!allowed.has(key)) {
      throw new TypeError(`Unknown ${label} field`);
    }
  }
  for (const key of fields) {
    if (!Object.hasOwn(value, key)) {
      throw new TypeError(`Missing ${key}`);
    }
  }
}

function parseAllowList(value) {
  return new Set(
    String(value || "")
      .split(",")
      .map((entry) => entry.trim())
      .filter(Boolean),
  );
}

function requireAllowed(value, allowed, label) {
  if (typeof value !== "string" || !allowed.has(value)) {
    throw new TypeError(`Invalid ${label}`);
  }
  return value;
}

function requireUuid(value, label) {
  if (typeof value !== "string" || !UUID_PATTERN.test(value)) {
    throw new TypeError(`Invalid ${label}`);
  }
  return value;
}

function requireInteger(value, minimum, maximum, label) {
  if (!Number.isInteger(value) || value < minimum || value > maximum) {
    throw new TypeError(`Invalid ${label}`);
  }
  return value;
}

function requireLocalDay(value) {
  if (typeof value !== "string" || !LOCAL_DAY_PATTERN.test(value)) {
    throw new TypeError("Invalid localDay");
  }

  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    throw new TypeError("Invalid localDay");
  }
  return value;
}

function validateEvent(input, allowLists) {
  if (!isPlainObject(input) || !EVENT_TYPES.has(input.type)) {
    throw new TypeError("Invalid event type");
  }

  requireExactFields(input, EVENT_FIELDS[input.type], "event");
  const event = {
    version: requireInteger(input.version, 1, 1, "version"),
    participantId: requireUuid(input.participantId, "participantId"),
    cohort: requireAllowed(input.cohort, allowLists.cohorts, "cohort"),
    build: requireAllowed(input.build, allowLists.builds, "build"),
    source: requireAllowed(input.source, allowLists.sources, "source"),
    localDay: requireLocalDay(input.localDay),
    clientSequence: requireInteger(
      input.clientSequence,
      1,
      1_000_000,
      "clientSequence",
    ),
    routeId: requireAllowed(input.routeId, ROUTE_IDS, "routeId"),
    eventId: requireUuid(input.eventId, "eventId"),
    type: input.type,
  };

  if (input.type === "round_started") {
    event.launchContext = requireAllowed(
      input.launchContext,
      LAUNCH_CONTEXTS,
      "launchContext",
    );
  } else if (input.type === "round_finished") {
    event.outcome = requireAllowed(input.outcome, OUTCOMES, "outcome");
    event.durationSeconds = requireInteger(
      input.durationSeconds,
      0,
      1800,
      "durationSeconds",
    );
    event.progressPercent = requireInteger(
      input.progressPercent,
      0,
      100,
      "progressPercent",
    );
    event.badges = requireInteger(input.badges, 0, 20, "badges");
    event.jumps = requireInteger(input.jumps, 0, 1000, "jumps");
    event.obstaclesHit = requireInteger(
      input.obstaclesHit,
      0,
      20,
      "obstaclesHit",
    );
    event.obstaclesCleared = requireInteger(
      input.obstaclesCleared,
      0,
      20,
      "obstaclesCleared",
    );
  }

  return event;
}

export function validateBatch(input, env) {
  requireExactFields(input, ["events"], "batch");
  if (
    !Array.isArray(input.events) ||
    input.events.length < 1 ||
    input.events.length > 20
  ) {
    throw new TypeError("Invalid events");
  }

  const allowLists = {
    builds: parseAllowList(env.ACTIVE_BUILDS),
    cohorts: parseAllowList(env.ACTIVE_COHORTS),
    sources: parseAllowList(env.ACTIVE_SOURCES),
  };
  const events = input.events.map((event) => validateEvent(event, allowLists));
  const first = events[0];

  for (const event of events.slice(1)) {
    if (
      event.participantId !== first.participantId ||
      event.cohort !== first.cohort ||
      event.build !== first.build ||
      event.source !== first.source
    ) {
      throw new TypeError("Mixed event identity");
    }
  }

  return events;
}

export function validateDeletion(input, env) {
  requireExactFields(
    input,
    ["version", "cohort", "participantId"],
    "deletion",
  );
  const cohorts = parseAllowList(env.ACTIVE_COHORTS);
  return {
    version: requireInteger(input.version, 1, 1, "version"),
    cohort: requireAllowed(input.cohort, cohorts, "cohort"),
    participantId: requireUuid(input.participantId, "participantId"),
  };
}

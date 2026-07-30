import assert from "node:assert/strict";
import test from "node:test";

import {
  validateBatch,
  validateDeletion,
} from "../src/validation.js";

const env = {
  ACTIVE_BUILDS: "TG1-01",
  ACTIVE_COHORTS: "TG1,TG1-PILOT",
  ACTIVE_SOURCES: "telegram,website,pilot",
};

const started = {
  version: 1,
  participantId: "11111111-1111-4111-8111-111111111111",
  cohort: "TG1",
  build: "TG1-01",
  source: "telegram",
  localDay: "2026-07-30",
  clientSequence: 1,
  routeId: "orchard",
  eventId: "22222222-2222-4222-8222-222222222222",
  type: "round_started",
  launchContext: "telegram",
};

const finished = {
  ...started,
  clientSequence: 2,
  eventId: "33333333-3333-4333-8333-333333333333",
  type: "round_finished",
  outcome: "loss",
  durationSeconds: 42,
  progressPercent: 58,
  badges: 2,
  jumps: 4,
  obstaclesHit: 1,
  obstaclesCleared: 2,
};
delete finished.launchContext;

const retry = {
  ...started,
  clientSequence: 3,
  eventId: "44444444-4444-4444-8444-444444444444",
  type: "retry_clicked",
};
delete retry.launchContext;

test("validator accepts the three exact semantic event shapes", () => {
  const input = { events: [started, finished, retry] };
  const result = validateBatch(input, env);

  assert.equal(result.length, 3);
  assert.notEqual(result, input.events);
  assert.notEqual(result[0], started);
  assert.deepEqual(result, input.events);
});

test("validator rejects unknown fields and forbidden identifiers", () => {
  assert.throws(
    () => validateBatch({
      events: [{ ...started, telegramId: "123" }],
    }, env),
    /field/i,
  );
  assert.throws(
    () => validateBatch({
      events: [{ ...started, participantId: "telegram-user" }],
    }, env),
    /participantId/,
  );
  assert.throws(
    () => validateDeletion({
      version: 1,
      cohort: "TG1",
      participantId: started.participantId,
      reason: "anything",
    }, env),
    /field/i,
  );
});

test("validator rejects oversized, unknown, and out-of-range values", () => {
  assert.throws(() => validateBatch({ events: [] }, env), /events/i);
  assert.throws(
    () => validateBatch({
      events: [{ ...finished, durationSeconds: 1801 }],
    }, env),
    /durationSeconds/,
  );
  assert.throws(
    () => validateBatch({
      events: [{ ...started, localDay: "2026-02-31" }],
    }, env),
    /localDay/,
  );
  assert.throws(
    () => validateBatch({
      events: [{ ...started, cohort: "UNKNOWN" }],
    }, env),
    /cohort/,
  );
});

test("validator rejects mixed participant batches", () => {
  assert.throws(
    () => validateBatch({
      events: [
        started,
        {
          ...retry,
          participantId: "55555555-5555-4555-8555-555555555555",
        },
      ],
    }, env),
    /mixed/i,
  );
});

test("deletion validation returns only the exact anonymous identity", () => {
  assert.deepEqual(validateDeletion({
    version: 1,
    cohort: "TG1",
    participantId: started.participantId,
  }, env), {
    version: 1,
    cohort: "TG1",
    participantId: started.participantId,
  });
});

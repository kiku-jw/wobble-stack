import test from "node:test";
import assert from "node:assert/strict";
import {
  PLAYTEST_BUILD,
  createFinishedEvent,
  createRetryEvent,
  createStartedEvent,
  getLocalDay,
  isRetryReason,
  parsePlaytestConfig,
} from "../src/playtest-logic.js";

const base = {
  participantId: "018f47d4-6f5c-4f67-91cf-93e12bf2b6f1",
  cohort: "TG1",
  source: "telegram",
  launchContext: "external",
  localDay: "2026-07-30",
  clientSequence: 1,
  routeId: "orchard",
};

function uuid(sequence) {
  return `00000000-0000-4000-8000-${String(sequence).padStart(12, "0")}`;
}

test("playtest activation is explicit and allow-listed", () => {
  assert.equal(parsePlaytestConfig(""), null);
  assert.equal(parsePlaytestConfig("?playtest=BAD&source=telegram"), null);
  assert.equal(parsePlaytestConfig("?playtest=TG1&source=unknown"), null);
  assert.deepEqual(
    parsePlaytestConfig("?from=telegram&playtest=TG1&source=telegram"),
    { cohort: "TG1", source: "telegram", build: PLAYTEST_BUILD },
  );
  assert.deepEqual(
    parsePlaytestConfig("?playtest=TG1-PILOT&source=pilot"),
    { cohort: "TG1-PILOT", source: "pilot", build: PLAYTEST_BUILD },
  );
});

test("local day contains no time or timezone", () => {
  assert.equal(getLocalDay(new Date(2026, 6, 30, 23, 59)), "2026-07-30");
});

test("round summaries are bounded and semantic", () => {
  const started = createStartedEvent(base, () => uuid(1));
  const finished = createFinishedEvent({
    ...base,
    clientSequence: 2,
    durationSeconds: 999999,
    progressPercent: 42.7,
    badges: 99,
    jumps: 12,
    obstaclesHit: 3,
    obstaclesCleared: 2,
    outcome: "loss",
  }, () => uuid(2));
  const retry = createRetryEvent(
    { ...base, clientSequence: 3 },
    () => uuid(3),
  );

  assert.equal(started.type, "round_started");
  assert.equal(started.launchContext, "external");
  assert.equal(finished.durationSeconds, 1800);
  assert.equal(finished.progressPercent, 43);
  assert.equal(finished.badges, 20);
  assert.equal(retry.type, "retry_clicked");
  assert.equal("coordinates" in finished, false);
});

test("event construction rejects invalid identifiers and event values", () => {
  assert.throws(
    () => createStartedEvent({ ...base, participantId: "friend-7" }, () => uuid(1)),
    /participant/i,
  );
  assert.throws(
    () => createStartedEvent(base, () => "event-start"),
    /event/i,
  );
  assert.throws(
    () => createFinishedEvent({ ...base, outcome: "quit" }, () => uuid(2)),
    /outcome/i,
  );
});

test("only loss-result Retry is a retry event", () => {
  assert.equal(isRetryReason("retry"), true);
  for (const reason of ["start", "replay", "next", "debug", "route"]) {
    assert.equal(isRetryReason(reason), false);
  }
});

import test from "node:test";
import assert from "node:assert/strict";
import {
  DIFFICULTY_PROFILES,
  clamp,
  createSeededRandom,
  formatTime,
  getFailureTimeScale,
  getGustEnvelope,
  getGustTiming,
  getRequiredCounterAngle,
  layoutStack,
  shouldShowFailureResults,
} from "../src/game-logic.js";

test("seeded random repeats the same run", () => {
  const first = createSeededRandom(42);
  const second = createSeededRandom(42);

  assert.deepEqual([first(), first(), first()], [second(), second(), second()]);
});

test("each gust samples independent timing and force within its profile", () => {
  for (const profile of Object.values(DIFFICULTY_PROFILES)) {
    const low = getGustTiming(() => 0, profile);
    const high = getGustTiming(() => 1, profile);

    assert.equal(low.force, profile.forceMin);
    assert.equal(high.force, profile.forceMax);
    assert.equal(low.restSeconds, profile.restMin);
    assert.equal(high.restSeconds, profile.restMax);
    assert.equal(low.durationSeconds, profile.durationMin);
    assert.equal(high.durationSeconds, profile.durationMax);
  }
});

test("difficulty force ranges do not overlap", () => {
  assert.ok(DIFFICULTY_PROFILES.gentle.forceMax < DIFFICULTY_PROFILES.normal.forceMin);
  assert.ok(DIFFICULTY_PROFILES.normal.forceMax < DIFFICULTY_PROFILES.wild.forceMin);
});

test("normal wind is meaningful and stays within the platform counter-angle", () => {
  const profile = DIFFICULTY_PROFILES.normal;
  const minimumAngle = getRequiredCounterAngle(profile.forceMin, 0.00105);
  const maximumAngle = getRequiredCounterAngle(profile.forceMax, 0.00105);

  assert.ok(minimumAngle > 0.06);
  assert.ok(maximumAngle < 0.46);
});

test("a gust eases in, holds, and eases out instead of hitting instantly", () => {
  assert.equal(getGustEnvelope(0), 0);
  assert.ok(getGustEnvelope(0.1) < getGustEnvelope(0.4));
  assert.equal(getGustEnvelope(0.5), 1);
  assert.ok(getGustEnvelope(0.9) < getGustEnvelope(0.7));
  assert.equal(getGustEnvelope(1), 0);
});

test("stack layout supports three through five touching creatures", () => {
  const specs = [78, 54, 56, 62, 50].map((proxyHeight, index) => ({
    kind: String(index),
    proxyHeight,
  }));

  for (const count of [3, 4, 5]) {
    const stack = layoutStack(specs, 665, count);
    assert.equal(stack.length, count);
    assert.equal(stack[0].y + stack[0].proxyHeight / 2, 665);

    for (let index = 1; index < stack.length; index += 1) {
      const lower = stack[index - 1];
      const upper = stack[index];
      assert.equal(upper.y + upper.proxyHeight / 2, lower.y - lower.proxyHeight / 2);
    }
  }
});

test("failure results wait for an impact reaction or the hard timeout", () => {
  assert.equal(shouldShowFailureResults(800, null, 900, 2600), false);
  assert.equal(shouldShowFailureResults(2600, null, 900, 2600), true);
  assert.equal(shouldShowFailureResults(999, 100, 900, 2600), false);
  assert.equal(shouldShowFailureResults(1000, 100, 900, 2600), true);
  assert.equal(shouldShowFailureResults(900, 0, 900, 2600), true);
});

test("failure time scale changes only during the ground-impact window", () => {
  assert.equal(getFailureTimeScale(80, null, 0.18), 1);
  assert.equal(getFailureTimeScale(100, 460, 0.18), 0.18);
  assert.equal(getFailureTimeScale(459, 460, 0.18), 0.18);
  assert.equal(getFailureTimeScale(460, 460, 0.18), 1);
});

test("display helpers keep values bounded and readable", () => {
  assert.equal(clamp(8, 0, 5), 5);
  assert.equal(clamp(-2, 0, 5), 0);
  assert.equal(formatTime(-3), "0.0");
  assert.equal(formatTime(12.34), "12.3");
});

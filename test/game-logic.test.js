import test from "node:test";
import assert from "node:assert/strict";
import {
  DIFFICULTY_PROFILES,
  clamp,
  createSeededRandom,
  formatTime,
  getGustEnvelope,
  getGustTiming,
  getRequiredCounterAngle,
  layoutStack,
} from "../src/game-logic.js";

test("seeded random repeats the same run", () => {
  const first = createSeededRandom(42);
  const second = createSeededRandom(42);

  assert.deepEqual([first(), first(), first()], [second(), second(), second()]);
});

test("every difficulty ramps wind gradually through the run", () => {
  const fixedRandom = () => 0.5;

  for (const profile of Object.values(DIFFICULTY_PROFILES)) {
    const early = getGustTiming(0, fixedRandom, profile);
    const late = getGustTiming(200, fixedRandom, profile);

    assert.ok(late.force > early.force);
    assert.ok(late.restSeconds < early.restSeconds);
    assert.ok(late.warningSeconds < early.warningSeconds);
    assert.equal(early.pressure, 0);
    assert.equal(late.pressure, 1);
  }
});

test("difficulty profiles have a clear force hierarchy", () => {
  const fixedRandom = () => 0;
  const gentle = getGustTiming(45, fixedRandom, DIFFICULTY_PROFILES.gentle);
  const normal = getGustTiming(45, fixedRandom, DIFFICULTY_PROFILES.normal);
  const wild = getGustTiming(45, fixedRandom, DIFFICULTY_PROFILES.wild);

  assert.ok(gentle.force < normal.force);
  assert.ok(normal.force < wild.force);
});

test("normal wind begins modestly and stays within the platform counter-angle", () => {
  const profile = DIFFICULTY_PROFILES.normal;
  const earlyAngle = getRequiredCounterAngle(profile.forceStart, 0.00105);
  const lateAngle = getRequiredCounterAngle(profile.forceEnd, 0.00105);

  assert.ok(earlyAngle < 0.12);
  assert.ok(lateAngle < 0.46);
});

test("a gust eases in, holds, and eases out instead of hitting instantly", () => {
  assert.equal(getGustEnvelope(0), 0);
  assert.ok(getGustEnvelope(0.1) < getGustEnvelope(0.4));
  assert.equal(getGustEnvelope(0.5), 1);
  assert.ok(getGustEnvelope(0.9) < getGustEnvelope(0.7));
  assert.equal(getGustEnvelope(1), 0);
});

test("stack layout supports one through five touching creatures", () => {
  const specs = [78, 54, 56, 62, 50].map((proxyHeight, index) => ({
    kind: String(index),
    proxyHeight,
  }));

  for (const count of [1, 3, 5]) {
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

test("display helpers keep values bounded and readable", () => {
  assert.equal(clamp(8, 0, 5), 5);
  assert.equal(clamp(-2, 0, 5), 0);
  assert.equal(formatTime(-3), "0.0");
  assert.equal(formatTime(12.34), "12.3");
});

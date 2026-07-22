import test from "node:test";
import assert from "node:assert/strict";
import { clamp, createSeededRandom, formatTime, getGustTiming } from "../src/game-logic.js";

test("seeded random repeats the same run", () => {
  const first = createSeededRandom(42);
  const second = createSeededRandom(42);

  assert.deepEqual([first(), first(), first()], [second(), second(), second()]);
});

test("gusts become stronger and closer together", () => {
  const fixedRandom = () => 0.5;
  const early = getGustTiming(2, fixedRandom);
  const late = getGustTiming(60, fixedRandom);

  assert.ok(late.force > early.force);
  assert.ok(late.restSeconds < early.restSeconds);
  assert.ok(late.warningSeconds < early.warningSeconds);
});

test("display helpers keep values bounded and readable", () => {
  assert.equal(clamp(8, 0, 5), 5);
  assert.equal(clamp(-2, 0, 5), 0);
  assert.equal(formatTime(-3), "0.0");
  assert.equal(formatTime(12.34), "12.3");
});

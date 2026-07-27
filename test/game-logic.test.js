import test from "node:test";
import assert from "node:assert/strict";
import {
  WIND_PROFILE,
  clamp,
  createSeededRandom,
  formatTime,
  getEffectiveGustAcceleration,
  getFailureTimeScale,
  getGustEnvelope,
  getGustTiming,
  getRequiredCounterAngle,
  getWindTravelSpeed,
  layoutStack,
  shouldShowFailureResults,
} from "../src/game-logic.js";
import {
  ROUTES,
  createShuffledOrder,
  getBadgeScreenY,
  getCappedPointerSupportOffset,
  getCounterSupportOffset,
  getRoute,
  getRouteCompletion,
  getSupportAngle,
  getWorldScreenX,
} from "../src/game-content.js";

test("seeded random repeats the same run", () => {
  const first = createSeededRandom(42);
  const second = createSeededRandom(42);

  assert.deepEqual([first(), first(), first()], [second(), second(), second()]);
});

test("one profile spans soft through strong independently sampled gusts", () => {
  const low = getGustTiming(() => 0);
  const high = getGustTiming(() => 1);
  const midpointSamples = [0.5, 0.5, 0.5];
  const ordinary = getGustTiming(() => midpointSamples.shift());

  assert.equal(low.force, WIND_PROFILE.forceMin);
  assert.equal(high.force, WIND_PROFILE.forceMax);
  assert.equal(low.restSeconds, WIND_PROFILE.restMin);
  assert.equal(high.restSeconds, WIND_PROFILE.restMax);
  assert.equal(low.durationSeconds, WIND_PROFILE.durationMin);
  assert.equal(high.durationSeconds, WIND_PROFILE.durationMax);
  assert.ok(ordinary.force < (WIND_PROFILE.forceMin + WIND_PROFILE.forceMax) / 2);
  assert.ok(WIND_PROFILE.forceMax > WIND_PROFILE.forceMin * 2);
});

test("the single wind range stays within the platform counter-angle", () => {
  const minimumAngle = getRequiredCounterAngle(WIND_PROFILE.forceMin, 0.00105);
  const maximumAngle = getRequiredCounterAngle(WIND_PROFILE.forceMax, 0.00105);

  assert.ok(minimumAngle > 0.05);
  assert.ok(maximumAngle < 0.46);
});

test("counter-tilt changes the effective gust acceleration for the whole stack", () => {
  const force = 0.00009;
  const gravityScale = 0.00105;
  const authority = 0.72;
  const counterAngle = getRequiredCounterAngle(force, gravityScale);
  const neutral = getEffectiveGustAcceleration(force, -1, 1, 0, gravityScale, authority);
  const correct = getEffectiveGustAcceleration(
    force,
    -1,
    1,
    counterAngle,
    gravityScale,
    authority,
  );
  const wrong = getEffectiveGustAcceleration(
    force,
    -1,
    1,
    -counterAngle,
    gravityScale,
    authority,
  );
  const rightwardCorrect = getEffectiveGustAcceleration(
    force,
    1,
    1,
    -counterAngle,
    gravityScale,
    authority,
  );

  assert.equal(neutral, -force);
  assert.ok(Math.abs(correct) < Math.abs(neutral) * 0.3);
  assert.ok(Math.abs(wrong) > Math.abs(neutral) * 1.7);
  assert.ok(Math.abs(rightwardCorrect + correct) < Number.EPSILON);
  assert.equal(getEffectiveGustAcceleration(force, -1, 0, counterAngle, gravityScale, authority), 0);
});

test("a gust eases in, holds, and eases out instead of hitting instantly", () => {
  assert.equal(getGustEnvelope(0), 0);
  assert.ok(getGustEnvelope(0.1) < getGustEnvelope(0.4));
  assert.equal(getGustEnvelope(0.5), 1);
  assert.ok(getGustEnvelope(0.9) < getGustEnvelope(0.7));
  assert.equal(getGustEnvelope(1), 0);
});

test("wind streak travel speed increases with visual intensity", () => {
  const speeds = [0, 0.05, 0.4, 0.8, 1].map(getWindTravelSpeed);

  assert.equal(speeds[0], 0);
  for (let index = 1; index < speeds.length; index += 1) {
    assert.ok(speeds[index] > speeds[index - 1]);
  }
  assert.equal(getWindTravelSpeed(-1), 0);
  assert.equal(getWindTravelSpeed(2), getWindTravelSpeed(1));
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

test("the browser journey uses the three authored iPhone routes", () => {
  assert.equal(ROUTES.length, 3);
  assert.deepEqual(ROUTES.map((route) => route.badgeOffsets.length), [7, 8, 9]);
  assert.deepEqual(ROUTES.map((route) => route.bumpDistances.length), [1, 2, 3]);
  assert.deepEqual(ROUTES[0].joinStops.map((stop) => stop.character), ["rabbit", "jelly"]);
  assert.equal(ROUTES[0].initialCreatures, 3);
  assert.equal(ROUTES[2].finishDistance, 58);
  assert.equal(getRoute(-4).id, "orchard");
  assert.equal(getRoute(50).id, "windmill");
});

test("route objects move left while the friends travel right", () => {
  const before = getWorldScreenX(12, 2);
  const after = getWorldScreenX(12, 4);

  assert.ok(after < before);
  assert.equal(before - after, 68);
  assert.equal(getRouteCompletion(19, 38), 0.5);
  assert.equal(getRouteCompletion(90, 38), 1);
  assert.ok(getBadgeScreenY(2) < getBadgeScreenY(-2));
});

test("rolling the wheel under the falling side counter-tilts the board", () => {
  assert.equal(getSupportAngle(0, 42, 0.34), 0);
  assert.ok(getSupportAngle(42, 42, 0.34) < 0);
  assert.ok(getSupportAngle(-42, 42, 0.34) > 0);
  assert.equal(getSupportAngle(100, 42, 0.34), -0.34);
});

test("keyboard support accounts for both the board slope and explicit counter force", () => {
  const support = getCounterSupportOffset(0.0001, 1, 0.00105, 0.8, 44, 0.23);
  const mirrored = getCounterSupportOffset(0.0001, -1, 0.00105, 0.8, 44, 0.23);

  assert.ok(support > 8);
  assert.ok(support < 20);
  assert.equal(mirrored, -support);
  assert.equal(getCounterSupportOffset(0.0001, 1, 0, 0.8, 44, 0.23), 0);
});

test("pointer control caps a full correct swipe at the strongest useful counter-angle", () => {
  const parameters = [1, 1, 0.000135, 0.00105, 0.8, 44, 0.23];
  const ideal = getCounterSupportOffset(0.000135, 1, 0.00105, 0.8, 44, 0.23);
  const partialSwipe = getCappedPointerSupportOffset(0.35, ...parameters);
  const fullSwipe = getCappedPointerSupportOffset(1, ...parameters);

  assert.equal(partialSwipe, ideal * 0.35);
  assert.equal(fullSwipe, ideal);
  assert.ok(fullSwipe < 20);
});

test("pointer control follows the gust envelope and keeps wrong-way input dangerous", () => {
  const earlyGust = getCappedPointerSupportOffset(
    0.4,
    1,
    0.25,
    WIND_PROFILE.forceMax,
    0.00105,
    0.8,
    44,
    0.23,
  );
  const fullGust = getCappedPointerSupportOffset(
    0.4,
    1,
    1,
    WIND_PROFILE.forceMax,
    0.00105,
    0.8,
    44,
    0.23,
  );
  const wrongWay = getCappedPointerSupportOffset(
    -1,
    1,
    1,
    WIND_PROFILE.forceMax,
    0.00105,
    0.8,
    44,
    0.23,
  );
  const betweenGusts = getCappedPointerSupportOffset(
    1,
    0,
    0,
    WIND_PROFILE.forceMax,
    0.00105,
    0.8,
    44,
    0.23,
  );

  assert.ok(earlyGust > 0);
  assert.ok(fullGust > earlyGust);
  assert.equal(wrongWay, -9.68);
  assert.equal(betweenGusts, 9.68);
});

test("music shuffle exhausts every track and avoids a boundary repeat", () => {
  const tracks = ["a", "b", "c", "d"];
  const firstOrder = createShuffledOrder(tracks, () => 0);
  const nextOrder = createShuffledOrder(tracks, () => 0, firstOrder.at(-1));

  assert.deepEqual([...firstOrder].sort(), tracks);
  assert.equal(new Set(firstOrder).size, tracks.length);
  assert.deepEqual([...nextOrder].sort(), tracks);
  assert.notEqual(nextOrder[0], firstOrder.at(-1));
  assert.deepEqual(tracks, ["a", "b", "c", "d"]);
});

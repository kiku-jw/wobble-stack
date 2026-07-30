import test from "node:test";
import assert from "node:assert/strict";
import * as gameLogic from "../src/game-logic.js";
import {
  WIND_PROFILE,
  clamp,
  createSeededRandom,
  formatTime,
  getAudioFadeGain,
  getDirectSupportOffset,
  getEffectiveGustAcceleration,
  getFailureTimeScale,
  getGustEnvelope,
  getGustTiming,
  getJumpArcHeight,
  getObstacleHitResponse,
  getRequiredCounterAngle,
  getStackWindScale,
  getWindTravelSpeed,
  isJumpKey,
  isObstacleCleared,
  isShortTap,
  isTelegramContext,
  layoutStack,
  shouldShowFailureResults,
} from "../src/game-logic.js";
import {
  ROUTES,
  createShuffledOrder,
  getBadgeScreenY,
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

test("music fade enters gently and reaches the selected volume", () => {
  assert.equal(getAudioFadeGain(0, 1100), 0);
  assert.equal(getAudioFadeGain(550, 1100), 0.5);
  assert.equal(getAudioFadeGain(1100, 1100), 1);
  assert.equal(getAudioFadeGain(2000, 1100), 1);
  assert.equal(getAudioFadeGain(0, 0), 1);
});

test("taller stacks receive normalized wind without changing the gust visuals", () => {
  assert.equal(getStackWindScale(3), 1);
  assert.equal(getStackWindScale(4), 0.88);
  assert.equal(getStackWindScale(5), 0.76);
  assert.equal(getStackWindScale(12), 0.76);
  assert.equal(getStackWindScale(-2), 1);
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

test("pointer control directly uses most of the visible support range", () => {
  assert.equal(getDirectSupportOffset(0, 44), 0);
  assert.equal(getDirectSupportOffset(1, 44), 36.96);
  assert.equal(getDirectSupportOffset(-1, 44), -36.96);
  assert.equal(getDirectSupportOffset(0.1, 44), 3.696);
  assert.equal(getDirectSupportOffset(2, 44), 36.96);
  assert.equal(getDirectSupportOffset(1, 0), 0);
});

test("tap classification rejects drags, holds, and excessive peak travel", () => {
  assert.equal(isShortTap(140, 4), true);
  assert.equal(isShortTap(260, 12), true);
  assert.equal(isShortTap(261, 4), false);
  assert.equal(isShortTap(140, 13), false);
  assert.equal(isShortTap(-1, 0), false);
});

test("jump keys fire once per physical key press", () => {
  assert.equal(isJumpKey("ArrowUp"), true);
  assert.equal(isJumpKey(" "), true);
  assert.equal(isJumpKey("W"), true);
  assert.equal(isJumpKey("w", true), false);
  assert.equal(isJumpKey("ArrowUp", true), false);
  assert.equal(isJumpKey("ArrowLeft"), false);
});

test("jump arc is bounded and obstacle clearance is explicit", () => {
  assert.equal(getJumpArcHeight(0, 0.72, 54), 0);
  assert.equal(getJumpArcHeight(0.36, 0.72, 54), 54);
  assert.equal(getJumpArcHeight(0.72, 0.72, 54), 0);
  assert.equal(getJumpArcHeight(2, 0.72, 54), 0);
  assert.equal(getJumpArcHeight(0.2, 0, 54), 0);
  assert.equal(isObstacleCleared(18, 18), true);
  assert.equal(isObstacleCleared(17.99, 18), false);
});

test("obstacle hits alternate direction and disturb taller stacks more", () => {
  assert.deepEqual(getObstacleHitResponse(0, 0, 0), {
    direction: 1,
    platformKick: 0.15,
    velocityX: 0.9,
    velocityY: -1.55,
    angularVelocity: 0.022,
  });

  const laterHit = getObstacleHitResponse(1, 2, 4);
  assert.equal(laterHit.direction, -1);
  assert.ok(Math.abs(laterHit.platformKick + 0.174) < 1e-12);
  assert.ok(Math.abs(laterHit.velocityX + 1.38) < 1e-12);
  assert.ok(Math.abs(laterHit.velocityY + 2.03) < 1e-12);
  assert.ok(Math.abs(laterHit.angularVelocity + 0.038) < 1e-12);
});

test("Telegram context detection stays explicit and dependency-free", () => {
  assert.equal(isTelegramContext({ source: "telegram" }), true);
  assert.equal(isTelegramContext({ source: "tg" }), true);
  assert.equal(isTelegramContext({ hasWebApp: true }), true);
  assert.equal(isTelegramContext({ userAgent: "Mozilla Telegram/12.0" }), true);
  assert.equal(isTelegramContext({ userAgent: "Mozilla/5.0 Safari/605.1" }), false);
});

test("external handoff keeps the playtest query without retriggering Telegram", () => {
  assert.equal(
    gameLogic.buildExternalGameUrl?.(
      "https://kiku-jw.github.io/wobble-stack/?from=telegram&playtest=TG1&source=telegram&debug=1#game",
    ),
    "https://kiku-jw.github.io/wobble-stack/?playtest=TG1&source=telegram&debug=1",
  );
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

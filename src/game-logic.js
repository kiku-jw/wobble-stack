export const WIND_PROFILE = Object.freeze({
  forceMin: 0.000055,
  forceMax: 0.000135,
  restMin: 2.2,
  restMax: 3.8,
  durationMin: 3.8,
  durationMax: 5.4,
});

export function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

export function createSeededRandom(seed) {
  let state = seed >>> 0;

  return () => {
    state += 0x6d2b79f5;
    let value = state;
    value = Math.imul(value ^ (value >>> 15), value | 1);
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };
}

function sampleRange(min, max, sample) {
  return min + (max - min) * sample;
}

export function getGustTiming(random, profile = WIND_PROFILE) {
  const restSeconds = sampleRange(profile.restMin, profile.restMax, random());
  const durationSeconds = sampleRange(profile.durationMin, profile.durationMax, random());
  const forceSample = random();

  return {
    restSeconds,
    durationSeconds,
    force: sampleRange(profile.forceMin, profile.forceMax, forceSample * forceSample),
  };
}

function smoothstep(value) {
  const bounded = clamp(value, 0, 1);
  return bounded * bounded * (3 - 2 * bounded);
}

export function getGustEnvelope(progress) {
  const bounded = clamp(progress, 0, 1);

  if (bounded < 0.38) return smoothstep(bounded / 0.38);
  if (bounded <= 0.75) return 1;
  return smoothstep((1 - bounded) / 0.25);
}

export function getWindTravelSpeed(intensity) {
  const bounded = clamp(intensity, 0, 1);
  return bounded === 0 ? 0 : 55 + bounded * 315;
}

export function getAudioFadeGain(elapsedMs, durationMs) {
  if (durationMs <= 0) return 1;
  const progress = clamp(elapsedMs / durationMs, 0, 1);
  return progress * progress * (3 - 2 * progress);
}

export function getStackWindScale(creatureCount) {
  const extraFriends = clamp(Math.round(Number(creatureCount) || 3), 3, 5) - 3;
  return 1 - extraFriends * 0.12;
}

export function getDirectSupportOffset(control, maxSupportOffset, authority = 0.84) {
  const boundedControl = clamp(Number(control) || 0, -1, 1);
  const boundedOffset = Math.max(0, Number(maxSupportOffset) || 0);
  const boundedAuthority = clamp(Number(authority) || 0, 0, 1);
  return boundedControl * boundedOffset * boundedAuthority;
}

export function isShortTap(
  durationMs,
  maximumTravel,
  maximumDurationMs = 260,
  maximumTravelPixels = 12,
) {
  return (
    Number(durationMs) >= 0 &&
    Number(durationMs) <= maximumDurationMs &&
    Number(maximumTravel) >= 0 &&
    Number(maximumTravel) <= maximumTravelPixels
  );
}

export function isJumpKey(key, repeat = false) {
  const normalizedKey = String(key).toLowerCase();
  return !repeat && (key === "ArrowUp" || key === " " || normalizedKey === "w");
}

export function getJumpArcHeight(elapsedSeconds, durationSeconds, maximumHeight) {
  if (durationSeconds <= 0 || maximumHeight <= 0) return 0;
  const progress = clamp(elapsedSeconds / durationSeconds, 0, 1);
  if (progress === 0 || progress === 1) return 0;
  return Math.sin(progress * Math.PI) * maximumHeight;
}

export function isObstacleCleared(jumpHeight, minimumClearance) {
  return Number(jumpHeight) >= Math.max(0, Number(minimumClearance) || 0);
}

export function getObstacleHitResponse(obstacleIndex, routeIndex, creatureIndex) {
  const obstacle = Math.max(0, Math.round(Number(obstacleIndex) || 0));
  const route = Math.max(0, Math.round(Number(routeIndex) || 0));
  const creature = Math.max(0, Math.round(Number(creatureIndex) || 0));
  const direction = obstacle % 2 === 0 ? 1 : -1;

  return {
    direction,
    platformKick: direction * (0.15 + route * 0.012),
    velocityX: direction * (0.9 + creature * 0.12),
    velocityY: -1.55 - creature * 0.12,
    angularVelocity: direction * (0.022 + creature * 0.004),
  };
}

export function isTelegramContext({
  userAgent = "",
  source = "",
  hasWebApp = false,
} = {}) {
  const normalizedSource = String(source).trim().toLowerCase();
  const normalizedUserAgent = String(userAgent).toLowerCase();
  return (
    Boolean(hasWebApp) ||
    normalizedSource === "telegram" ||
    normalizedSource === "tg" ||
    normalizedUserAgent.includes("telegram") ||
    normalizedUserAgent.includes("tgwebview")
  );
}

export function getRequiredCounterAngle(force, gravityScale) {
  return Math.atan(force / gravityScale);
}

export function getEffectiveGustAcceleration(
  force,
  direction,
  envelope,
  platformAngle,
  gravityScale,
  counterAuthority,
) {
  const activeEnvelope = clamp(envelope, 0, 1);
  if (activeEnvelope === 0) return 0;

  const windAcceleration = Math.max(0, force) * Math.sign(direction);
  const platformAcceleration = Math.tan(platformAngle) * gravityScale * counterAuthority;
  return (windAcceleration + platformAcceleration) * activeEnvelope;
}

export function layoutStack(specs, platformTop, count) {
  const selected = specs.slice(0, clamp(Math.round(count), 1, specs.length));
  let bottom = platformTop;

  return selected.map((spec) => {
    const y = bottom - spec.proxyHeight / 2;
    bottom -= spec.proxyHeight;
    return { ...spec, y };
  });
}

export function shouldShowFailureResults(elapsedMs, firstImpactAtMs, impactHoldMs, timeoutMs) {
  const impactReactionWasVisible =
    firstImpactAtMs !== null && elapsedMs - firstImpactAtMs >= impactHoldMs;
  return impactReactionWasVisible || elapsedMs >= timeoutMs;
}

export function getFailureTimeScale(elapsedMs, impactSlowMoEndsAtMs, slowMoScale) {
  return impactSlowMoEndsAtMs !== null && elapsedMs < impactSlowMoEndsAtMs ? slowMoScale : 1;
}

export function formatTime(seconds) {
  return Math.max(0, seconds).toFixed(1);
}

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

export const DIFFICULTY_PROFILES = Object.freeze({
  gentle: Object.freeze({
    label: "Gentle",
    note: "Light gusts · room to recover",
    forceMin: 0.000025,
    forceMax: 0.00005,
    restMin: 3.2,
    restMax: 5,
    durationMin: 3.6,
    durationMax: 4.6,
  }),
  normal: Object.freeze({
    label: "Normal",
    note: "Random gusts · real pressure",
    forceMin: 0.000065,
    forceMax: 0.000105,
    restMin: 2.2,
    restMax: 3.8,
    durationMin: 3.8,
    durationMax: 5,
  }),
  wild: Object.freeze({
    label: "Wild",
    note: "Heavy gusts · little recovery",
    forceMin: 0.00011,
    forceMax: 0.000135,
    restMin: 1.3,
    restMax: 2.6,
    durationMin: 4,
    durationMax: 5.4,
  }),
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

function sampleRange(min, max, random) {
  return min + (max - min) * random();
}

export function getGustTiming(random, profile = DIFFICULTY_PROFILES.normal) {
  return {
    restSeconds: sampleRange(profile.restMin, profile.restMax, random),
    durationSeconds: sampleRange(profile.durationMin, profile.durationMax, random),
    force: sampleRange(profile.forceMin, profile.forceMax, random),
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

export const DIFFICULTY_PROFILES = Object.freeze({
  gentle: Object.freeze({
    label: "Gentle",
    note: "Slow build · always counterable",
    initialCalmSeconds: 7,
    rampSeconds: 90,
    forceStart: 0.00005,
    forceEnd: 0.00028,
    restStart: 5.5,
    restEnd: 3.8,
    restJitter: 1.2,
    warningStart: 1.2,
    warningEnd: 0.8,
    durationStart: 1.45,
    durationEnd: 1.65,
  }),
  normal: Object.freeze({
    label: "Normal",
    note: "Balanced · learns, then bites",
    initialCalmSeconds: 6,
    rampSeconds: 75,
    forceStart: 0.000065,
    forceEnd: 0.00042,
    restStart: 5,
    restEnd: 3,
    restJitter: 1.3,
    warningStart: 1.1,
    warningEnd: 0.65,
    durationStart: 1.35,
    durationEnd: 1.55,
  }),
  wild: Object.freeze({
    label: "Wild",
    note: "Fast ramp · late wind can win",
    initialCalmSeconds: 4.5,
    rampSeconds: 55,
    forceStart: 0.000085,
    forceEnd: 0.00058,
    restStart: 4.6,
    restEnd: 2.2,
    restJitter: 1.1,
    warningStart: 0.95,
    warningEnd: 0.5,
    durationStart: 1.2,
    durationEnd: 1.5,
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

function interpolate(start, end, progress) {
  return start + (end - start) * progress;
}

export function getDifficultyPressure(elapsedSeconds, profile) {
  return clamp((elapsedSeconds - profile.initialCalmSeconds) / profile.rampSeconds, 0, 1);
}

export function getGustTiming(elapsedSeconds, random, profile = DIFFICULTY_PROFILES.normal) {
  const pressure = getDifficultyPressure(elapsedSeconds, profile);
  const restSeconds =
    interpolate(profile.restStart, profile.restEnd, pressure) + random() * profile.restJitter;
  const warningSeconds = interpolate(profile.warningStart, profile.warningEnd, pressure);
  const durationSeconds = interpolate(profile.durationStart, profile.durationEnd, pressure);
  const force = interpolate(profile.forceStart, profile.forceEnd, pressure);

  return { pressure, restSeconds, warningSeconds, durationSeconds, force };
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

export function getRequiredCounterAngle(force, gravityScale) {
  return Math.atan(force / gravityScale);
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

export function formatTime(seconds) {
  return Math.max(0, seconds).toFixed(1);
}

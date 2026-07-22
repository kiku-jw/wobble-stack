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

export function getGustTiming(elapsedSeconds, random) {
  const pressure = clamp(elapsedSeconds / 45, 0, 1);
  const restSeconds = 4.2 - pressure * 2 + random() * 1.8;
  const warningSeconds = Math.max(0.48, 0.9 - pressure * 0.28);
  const durationSeconds = 0.55 + pressure * 0.28;
  const force = 0.00042 + pressure * 0.00055;

  return { restSeconds, warningSeconds, durationSeconds, force };
}

export function formatTime(seconds) {
  return Math.max(0, seconds).toFixed(1);
}

import { clamp } from "./game-logic.js";

export const ROUTES = Object.freeze([
  Object.freeze({
    id: "orchard",
    title: "ORCHARD ROAD",
    subtitle: "FIND RABBIT AND JELLY",
    finishDistance: 38,
    initialCreatures: 3,
    badgeOffsets: Object.freeze([
      Object.freeze({ distance: 5, height: -2.3 }),
      Object.freeze({ distance: 10, height: -0.45 }),
      Object.freeze({ distance: 15, height: 0.7 }),
      Object.freeze({ distance: 20, height: -1.65 }),
      Object.freeze({ distance: 25, height: 1.55 }),
      Object.freeze({ distance: 31, height: -0.15 }),
      Object.freeze({ distance: 36, height: 1.3 }),
    ]),
    joinStops: Object.freeze([
      Object.freeze({ distance: 12.5, character: "rabbit" }),
      Object.freeze({ distance: 25, character: "jelly" }),
    ]),
    bumpDistances: Object.freeze([19]),
  }),
  Object.freeze({
    id: "cloud",
    title: "CLOUD BRIDGE",
    subtitle: "KEEP ALL FIVE TOGETHER",
    finishDistance: 48,
    initialCreatures: 5,
    badgeOffsets: Object.freeze([
      Object.freeze({ distance: 6, height: -0.9 }),
      Object.freeze({ distance: 12, height: 1.65 }),
      Object.freeze({ distance: 18, height: -2 }),
      Object.freeze({ distance: 24, height: 0.2 }),
      Object.freeze({ distance: 30, height: 2.05 }),
      Object.freeze({ distance: 36, height: -1.4 }),
      Object.freeze({ distance: 42, height: 0.75 }),
      Object.freeze({ distance: 46, height: 1.75 }),
    ]),
    joinStops: Object.freeze([]),
    bumpDistances: Object.freeze([15, 34]),
  }),
  Object.freeze({
    id: "windmill",
    title: "WINDMILL HILL",
    subtitle: "MAKE THE SUNSET FESTIVAL",
    finishDistance: 58,
    initialCreatures: 5,
    badgeOffsets: Object.freeze([
      Object.freeze({ distance: 6, height: -1.8 }),
      Object.freeze({ distance: 12, height: 0.3 }),
      Object.freeze({ distance: 18, height: 1.9 }),
      Object.freeze({ distance: 24, height: -0.8 }),
      Object.freeze({ distance: 30, height: 1.35 }),
      Object.freeze({ distance: 37, height: -1.9 }),
      Object.freeze({ distance: 44, height: 0.15 }),
      Object.freeze({ distance: 51, height: 2.2 }),
      Object.freeze({ distance: 56, height: 0.85 }),
    ]),
    joinStops: Object.freeze([]),
    bumpDistances: Object.freeze([12, 29, 46]),
  }),
]);

export function getRoute(routeIndex) {
  return ROUTES[clamp(Math.round(routeIndex), 0, ROUTES.length - 1)];
}

export function getWorldScreenX(distance, progress, centerX = 195, pixelsPerUnit = 34) {
  return centerX + (distance - progress) * pixelsPerUnit;
}

export function getBadgeScreenY(heightOffset) {
  return 532 - heightOffset * 32;
}

export function getSupportAngle(supportOffset, maxSupportOffset, maxPlatformAngle) {
  if (maxSupportOffset <= 0 || maxPlatformAngle <= 0) return 0;
  const normalizedOffset = clamp(supportOffset / maxSupportOffset, -1, 1);
  if (normalizedOffset === 0) return 0;
  return -normalizedOffset * maxPlatformAngle;
}

export function getRouteCompletion(progress, finishDistance) {
  if (finishDistance <= 0) return 1;
  return clamp(progress / finishDistance, 0, 1);
}

export function getCounterSupportOffset(
  force,
  direction,
  gravityScale,
  counterAuthority,
  maxSupportOffset,
  maxPlatformAngle,
) {
  if (gravityScale <= 0 || maxSupportOffset <= 0 || maxPlatformAngle <= 0) return 0;
  const combinedAuthority = 1 + Math.max(0, counterAuthority);
  const requiredAngle = Math.atan(Math.max(0, force) / (gravityScale * combinedAuthority));
  const normalizedAngle = clamp(requiredAngle / maxPlatformAngle, 0, 0.8);
  return Math.sign(direction) * normalizedAngle * maxSupportOffset;
}

export function createShuffledOrder(items, random = Math.random, previousItem = null) {
  const order = [...items];

  for (let index = order.length - 1; index > 0; index -= 1) {
    const sample = clamp(Number(random()) || 0, 0, 0.999999);
    const swapIndex = Math.floor(sample * (index + 1));
    const item = order[index];
    order[index] = order[swapIndex];
    order[swapIndex] = item;
  }

  if (order.length > 1 && order[0] === previousItem) {
    const swapIndex = order.findIndex((item) => item !== previousItem);
    if (swapIndex > 0) {
      const item = order[0];
      order[0] = order[swapIndex];
      order[swapIndex] = item;
    }
  }

  return order;
}

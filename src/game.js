import Matter from "matter-js";
import "./style.css";
import {
  WIND_PROFILE,
  clamp,
  createSeededRandom,
  getEffectiveGustAcceleration,
  getFailureTimeScale,
  getGustEnvelope,
  getGustTiming,
  getWindTravelSpeed,
  layoutStack,
  shouldShowFailureResults,
} from "./game-logic.js";
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
} from "./game-content.js";
import { getCharacterArt, loadGameArt } from "./game-art.js";

const { Bodies, Body, Composite, Constraint, Engine, Events } = Matter;

const WIDTH = 390;
const HEIGHT = 844;
const CENTER_X = WIDTH / 2;
const FIXED_STEP = 1000 / 60;
const PLATFORM_Y = 665;
const PLATFORM_TOP = 652;
const ROAD_SURFACE_Y = 758;
const WHEEL_Y = 718;
const GRAVITY_SCALE = 0.00105;
const MAX_PLATFORM_ANGLE = 0.23;
const MAX_SUPPORT_OFFSET = 44;
const COUNTER_TILT_AUTHORITY = 0.8;
const FAIL_Y = 731;
const PIXELS_PER_UNIT = 34;
const JOURNEY_SPEED = 1.46;
const IMPACT_SLOWMO_TIME_SCALE = 0.18;
const IMPACT_SLOWMO_DURATION_MS = 360;
const FAILURE_TIMEOUT_MS = 2700;
const FAILURE_IMPACT_HOLD_MS = 980;
const REDUCED_IMPACT_SLOWMO_TIME_SCALE = 0.86;
const REDUCED_IMPACT_SLOWMO_DURATION_MS = 100;
const REDUCED_FAILURE_TIMEOUT_MS = 1150;
const REDUCED_FAILURE_IMPACT_HOLD_MS = 180;
const PROGRESS_KEY = "wobble-stack-journey-v1";
const MUSIC_VOLUME_KEY = "wobble-stack-music-volume-v1";
const DEFAULT_MUSIC_VOLUME = 0.5;
const MUSIC_TRACKS = Object.freeze([
  "assets/music/sunny-clay-parade-a.mp3",
  "assets/music/sunny-clay-parade-b.mp3",
  "assets/music/sunny-clay-parade-c.mp3",
  "assets/music/bouncy-clay-parade.mp3",
].map((path) => new URL(path, document.baseURI).href));
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

const canvas = document.querySelector("#game-canvas");
const context = canvas.getContext("2d");
const hud = document.querySelector(".hud");
const badgeValue = document.querySelector("#badge-value");
const routeName = document.querySelector("#route-name");
const progressFill = document.querySelector("#progress-fill");
const pauseButton = document.querySelector("#pause-button");
const loadingOverlay = document.querySelector("#loading-overlay");
const loadingFill = document.querySelector("#loading-fill");
const startOverlay = document.querySelector("#start-overlay");
const startButton = document.querySelector("#start-button");
const routeList = document.querySelector("#route-list");
const resetProgressButton = document.querySelector("#reset-progress-button");
const resultOverlay = document.querySelector("#result-overlay");
const resultKicker = document.querySelector("#result-kicker");
const resultTitle = document.querySelector("#result-title");
const resultBadges = document.querySelector("#result-badges");
const retryButton = document.querySelector("#retry-button");
const resultRoutesButton = document.querySelector("#result-routes-button");
const finishOverlay = document.querySelector("#finish-overlay");
const finishBadges = document.querySelector("#finish-badges");
const nextRouteButton = document.querySelector("#next-route-button");
const replayButton = document.querySelector("#replay-button");
const finishRoutesButton = document.querySelector("#finish-routes-button");
const pauseOverlay = document.querySelector("#pause-overlay");
const resumeButton = document.querySelector("#resume-button");
const pauseRoutesButton = document.querySelector("#pause-routes-button");
const musicVolumeInputs = document.querySelectorAll("[data-music-volume]");
const musicVolumeOutputs = document.querySelectorAll("[data-music-volume-output]");
const thumbCue = document.querySelector("#thumb-cue");
const journeyMessage = document.querySelector("#journey-message");
const liveStatus = document.querySelector("#live-status");

let art = {};
let engine;
let platform;
let catchFloor;
let creatures = [];
let stackLinks = [];
let state = "loading";
let selectedRouteIndex = 0;
let currentRoute = getRoute(0);
let journeyProgress = 0;
let collectedBadges = new Set();
let joinedStops = new Set();
let triggeredBumps = new Set();
let journeyPause = 0;
let runSeconds = 0;
let runCount = 0;
let random = createSeededRandom(1);
let gust = null;
let windTravel = 0;
let supportOffset = 0;
let supportTarget = 0;
let pointerControl = 0;
let pointerStartX = 0;
let pointerStartControl = 0;
let pointerActive = false;
let keyboardDirection = 0;
let bumpKick = 0;
let accumulator = 0;
let lastFrameTime = performance.now();
let failElapsed = 0;
let firstImpactAt = null;
let impactSlowMoEndsAt = null;
let dangerWasHigh = false;
let saveFlash = 0;
let shake = 0;
let particles = [];
let impactEffects = [];
let messageSeconds = 0;
let resetArmed = false;
let resetArmTimer = null;
let finishTimer = null;
let progressData = readProgress();
let musicVolume = readMusicVolume();
let musicQueue = [];
let currentMusicTrack = null;
let musicActivated = false;
const musicPlayer = new Audio();

const creatureSpecs = [
  {
    kind: "pear",
    x: CENTER_X,
    proxyWidth: 66,
    proxyHeight: 94,
    drawWidth: 78,
    drawHeight: 118,
    drawOffsetY: -10,
    panicThreshold: 0.72,
    phase: 0.2,
  },
  {
    kind: "cube",
    x: CENTER_X,
    proxyWidth: 68,
    proxyHeight: 60,
    drawWidth: 82,
    drawHeight: 76,
    drawOffsetY: -4,
    panicThreshold: 0.54,
    phase: 1.8,
  },
  {
    kind: "bird",
    x: CENTER_X,
    proxyWidth: 61,
    proxyHeight: 70,
    drawWidth: 78,
    drawHeight: 94,
    drawOffsetY: -7,
    panicThreshold: 0.42,
    phase: 3.1,
  },
  {
    kind: "rabbit",
    x: CENTER_X,
    proxyWidth: 60,
    proxyHeight: 79,
    drawWidth: 78,
    drawHeight: 112,
    drawOffsetY: -14,
    panicThreshold: 0.34,
    phase: 4.4,
  },
  {
    kind: "jelly",
    x: CENTER_X,
    proxyWidth: 68,
    proxyHeight: 56,
    drawWidth: 90,
    drawHeight: 79,
    drawOffsetY: -8,
    panicThreshold: 0.62,
    phase: 5.7,
  },
];

configureMusic();
setupCanvas();
bindControls();
requestAnimationFrame(frame);

loadGameArt((progress) => {
  loadingFill.style.width = `${Math.round(progress * 100)}%`;
}).then((images) => {
  art = images;
  selectedRouteIndex = clamp(progressData.selectedRoute, 0, progressData.unlockedRoute);
  currentRoute = getRoute(selectedRouteIndex);
  resetJourney();
  state = "ready";
  loadingOverlay.hidden = true;
  startOverlay.hidden = false;
  renderRoutePicker();
  syncInterface();
  liveStatus.textContent = "Choose a road and start the journey.";
}).catch((error) => {
  console.error(error);
  const loadingMessage = loadingOverlay.querySelector("p");
  loadingMessage.textContent = "The friends could not arrive. Reload to try again.";
  liveStatus.textContent = "Game art failed to load.";
});

function setupCanvas() {
  const pixelRatio = Math.min(window.devicePixelRatio || 1, 2);
  canvas.width = WIDTH * pixelRatio;
  canvas.height = HEIGHT * pixelRatio;
  context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
}

function bindControls() {
  startButton.addEventListener("click", startRun);
  retryButton.addEventListener("click", startRun);
  replayButton.addEventListener("click", startRun);
  resultRoutesButton.addEventListener("click", showRouteSelect);
  finishRoutesButton.addEventListener("click", showRouteSelect);
  pauseRoutesButton.addEventListener("click", showRouteSelect);
  pauseButton.addEventListener("click", pauseRun);
  resumeButton.addEventListener("click", resumeRun);
  nextRouteButton.addEventListener("click", startNextRoute);
  resetProgressButton.addEventListener("click", handleResetProgress);
  musicVolumeInputs.forEach((input) => {
    input.addEventListener("input", () => setMusicVolume(input.value));
  });

  canvas.addEventListener("pointerdown", (event) => {
    if (state !== "playing") return;
    pointerActive = true;
    pointerStartX = event.clientX;
    pointerStartControl = pointerControl;
    canvas.classList.add("is-grabbing");
    canvas.setPointerCapture(event.pointerId);
    canvas.focus({ preventScroll: true });
    thumbCue.classList.remove("is-visible");
  });

  canvas.addEventListener("pointermove", (event) => {
    if (!pointerActive || state !== "playing") return;
    updatePointerControl(event);
  });

  canvas.addEventListener("pointerup", releasePointer);
  canvas.addEventListener("pointercancel", releasePointer);

  window.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      if (state === "playing") pauseRun();
      else if (state === "paused") resumeRun();
      return;
    }

    if (state !== "playing") return;
    const key = event.key.toLowerCase();

    if (event.key === "ArrowLeft" || key === "a") {
      keyboardDirection = -1;
      thumbCue.classList.remove("is-visible");
      event.preventDefault();
    }

    if (event.key === "ArrowRight" || key === "d") {
      keyboardDirection = 1;
      thumbCue.classList.remove("is-visible");
      event.preventDefault();
    }
  });

  window.addEventListener("keyup", (event) => {
    const key = event.key.toLowerCase();
    if (event.key === "ArrowLeft" || event.key === "ArrowRight" || key === "a" || key === "d") {
      keyboardDirection = 0;
    }
  });

  document.addEventListener("visibilitychange", () => {
    if (!document.hidden) return;
    if (state === "playing") pauseRun();
    else pauseMusic();
  });
}

function configureMusic() {
  musicPlayer.preload = "metadata";
  musicPlayer.volume = musicVolume;
  musicPlayer.addEventListener("ended", playNextMusicTrack);
  syncMusicVolumeControls();
}

function startMusic() {
  musicActivated = true;
  if (!currentMusicTrack) selectNextMusicTrack();
  musicPlayer.play().catch(() => {
    // A later Play or Resume gesture retries when the browser blocks autoplay.
  });
}

function pauseMusic() {
  musicPlayer.pause();
}

function playNextMusicTrack() {
  if (!selectNextMusicTrack() || !musicActivated || document.hidden) return;
  musicPlayer.play().catch(() => {
    // Keep the playlist queued for the next explicit user gesture.
  });
}

function selectNextMusicTrack() {
  if (musicQueue.length === 0) {
    musicQueue = createShuffledOrder(MUSIC_TRACKS, Math.random, currentMusicTrack);
  }

  const nextTrack = musicQueue.shift();
  if (!nextTrack) return false;
  currentMusicTrack = nextTrack;
  musicPlayer.src = nextTrack;
  return true;
}

function setMusicVolume(value) {
  const percentage = clamp(Math.round(Number(value) || 0), 0, 100);
  musicVolume = percentage / 100;
  musicPlayer.volume = musicVolume;
  syncMusicVolumeControls();
  writeMusicVolume();
}

function syncMusicVolumeControls() {
  const percentage = Math.round(musicVolume * 100);

  musicVolumeInputs.forEach((input) => {
    input.value = String(percentage);
    input.setAttribute("aria-valuetext", `${percentage} percent`);
  });

  musicVolumeOutputs.forEach((output) => {
    output.value = `${percentage}%`;
    output.textContent = `${percentage}%`;
  });
}

function renderRoutePicker() {
  routeList.replaceChildren();

  ROUTES.forEach((route, index) => {
    const button = document.createElement("button");
    const title = document.createElement("strong");
    const status = document.createElement("span");
    const unlocked = index <= progressData.unlockedRoute;
    const best = progressData.bestBadges[index];

    button.type = "button";
    button.className = "route-card";
    button.setAttribute("aria-pressed", index === selectedRouteIndex ? "true" : "false");
    button.disabled = !unlocked;
    button.classList.toggle("is-selected", index === selectedRouteIndex);
    title.textContent = route.title;
    status.textContent = unlocked
      ? best > 0
        ? `★ ${best} / ${route.badgeOffsets.length}`
        : "READY"
      : "LOCKED";
    button.append(title, status);

    button.addEventListener("click", () => {
      if (!unlocked) return;
      selectedRouteIndex = index;
      progressData.selectedRoute = index;
      currentRoute = getRoute(index);
      writeProgress();
      resetJourney();
      renderRoutePicker();
      syncInterface();
      liveStatus.textContent = `${route.title} selected.`;
    });

    routeList.append(button);
  });

  startButton.textContent = selectedRouteIndex === 0
    ? "Start the journey"
    : `Ride ${currentRoute.title.toLowerCase()}`;
}

function resetJourney() {
  currentRoute = getRoute(selectedRouteIndex);
  journeyProgress = 0;
  collectedBadges = new Set();
  joinedStops = new Set();
  triggeredBumps = new Set();
  journeyPause = 0;
  supportOffset = 0;
  supportTarget = 0;
  pointerControl = 0;
  pointerActive = false;
  keyboardDirection = 0;
  bumpKick = 0;
  runSeconds = 0;
  failElapsed = 0;
  firstImpactAt = null;
  impactSlowMoEndsAt = null;
  accumulator = 0;
  gust = null;
  windTravel = 0;
  dangerWasHigh = false;
  saveFlash = 0;
  shake = 0;
  particles = [];
  impactEffects = [];
  messageSeconds = 0;
  clearFinishTimer();
  resetPhysics();
}

function resetPhysics() {
  engine = Engine.create({ enableSleeping: false });
  engine.gravity.y = 1;
  engine.gravity.scale = GRAVITY_SCALE;

  platform = Bodies.rectangle(CENTER_X, PLATFORM_Y, 304, 24, {
    label: "platform",
    isStatic: true,
    friction: 1,
    frictionStatic: 1.65,
    restitution: 0.02,
    chamfer: { radius: 10 },
  });

  catchFloor = Bodies.rectangle(CENTER_X, ROAD_SURFACE_Y + 16, 560, 32, {
    label: "catch-floor",
    isStatic: true,
    friction: 0.86,
    restitution: 0.08,
    render: { visible: false },
  });

  const activeSpecs = layoutStack(
    creatureSpecs,
    PLATFORM_TOP,
    currentRoute.initialCreatures,
  );
  creatures = activeSpecs.map((spec) => createCreature(spec));
  stackLinks = [];

  for (let index = 1; index < creatures.length; index += 1) {
    stackLinks.push(...createFriendshipLinks(creatures[index - 1], creatures[index]));
  }

  Composite.add(engine.world, [
    platform,
    catchFloor,
    ...creatures.map((creature) => creature.body),
    ...stackLinks,
  ]);
  Events.on(engine, "collisionStart", handleCollisionStart);
}

function createCreature(spec) {
  const chamferRadius = spec.kind === "cube" ? 13 : spec.kind === "pear" ? 24 : 20;
  const body = Bodies.rectangle(spec.x, spec.y, spec.proxyWidth, spec.proxyHeight, {
    label: `creature-${spec.kind}`,
    density: 0.00245,
    friction: 0.96,
    frictionStatic: 1.55,
    frictionAir: 0.08,
    restitution: 0.025,
    slop: 0.018,
    chamfer: { radius: chamferRadius },
  });

  return {
    ...spec,
    body,
    panicLevel: 0,
    impactElapsed: null,
  };
}

function createFriendshipLinks(lowerCreature, upperCreature) {
  const jointWidth = Math.min(lowerCreature.proxyWidth, upperCreature.proxyWidth) * 0.19;

  return [-1, 1].map((side) => Constraint.create({
    label: "stack-friendship",
    bodyA: lowerCreature.body,
    pointA: {
      x: side * jointWidth,
      y: -lowerCreature.proxyHeight / 2,
    },
    bodyB: upperCreature.body,
    pointB: {
      x: side * jointWidth,
      y: upperCreature.proxyHeight / 2,
    },
    length: 0,
    stiffness: 0.017,
    damping: 0.09,
    render: { visible: false },
  }));
}

function startRun() {
  startMusic();
  resetJourney();
  runCount += 1;
  random = createSeededRandom(7907 + selectedRouteIndex * 2003 + runCount * 101);
  state = "playing";
  loadingOverlay.hidden = true;
  startOverlay.hidden = true;
  resultOverlay.hidden = true;
  finishOverlay.hidden = true;
  pauseOverlay.hidden = true;
  engine.timing.timeScale = 1;
  scheduleGust(3.5);
  syncInterface();
  liveStatus.textContent = `${currentRoute.title} started with ${creatures.length} friends.`;

  if (runCount === 1) {
    thumbCue.classList.add("is-visible");
    window.setTimeout(() => thumbCue.classList.remove("is-visible"), 3000);
  }

  canvas.focus({ preventScroll: true });
}

function pauseRun() {
  if (state !== "playing") return;
  state = "paused";
  pointerActive = false;
  pointerControl = 0;
  keyboardDirection = 0;
  pauseMusic();
  canvas.classList.remove("is-grabbing");
  pauseOverlay.hidden = false;
  thumbCue.classList.remove("is-visible");
  liveStatus.textContent = "Journey paused.";
  syncInterface();
  resumeButton.focus({ preventScroll: true });
}

function resumeRun() {
  if (state !== "paused") return;
  startMusic();
  state = "playing";
  pauseOverlay.hidden = true;
  liveStatus.textContent = "Journey resumed.";
  syncInterface();
  canvas.focus({ preventScroll: true });
}

function showRouteSelect() {
  pauseMusic();
  state = "ready";
  hideOutcomeOverlays();
  startOverlay.hidden = false;
  resetJourney();
  renderRoutePicker();
  syncInterface();
  liveStatus.textContent = "Choose a road.";
  startButton.focus({ preventScroll: true });
}

function hideOutcomeOverlays() {
  resultOverlay.hidden = true;
  finishOverlay.hidden = true;
  pauseOverlay.hidden = true;
  clearFinishTimer();
}

function startNextRoute() {
  const nextIndex = selectedRouteIndex < ROUTES.length - 1 ? selectedRouteIndex + 1 : 0;
  selectedRouteIndex = Math.min(nextIndex, progressData.unlockedRoute);
  currentRoute = getRoute(selectedRouteIndex);
  progressData.selectedRoute = selectedRouteIndex;
  writeProgress();
  startRun();
}

function handleResetProgress() {
  if (!resetArmed) {
    resetArmed = true;
    resetProgressButton.textContent = "Tap again to reset";
    window.clearTimeout(resetArmTimer);
    resetArmTimer = window.setTimeout(disarmReset, 3000);
    return;
  }

  progressData = createDefaultProgress();
  selectedRouteIndex = 0;
  currentRoute = getRoute(0);
  writeProgress();
  disarmReset();
  resetJourney();
  renderRoutePicker();
  syncInterface();
  liveStatus.textContent = "Journey progress reset.";
}

function disarmReset() {
  resetArmed = false;
  resetProgressButton.textContent = "Reset progress";
}

function releasePointer(event) {
  if (!pointerActive) return;
  pointerActive = false;
  pointerControl = 0;
  canvas.classList.remove("is-grabbing");
  if (canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
}

function updatePointerControl(event) {
  const bounds = canvas.getBoundingClientRect();
  const travel = (event.clientX - pointerStartX) / Math.max(1, bounds.width * 0.32);
  pointerControl = clamp(pointerStartControl + travel, -1, 1);
}

function frame(now) {
  const deltaMs = Math.min(50, Math.max(0, now - lastFrameTime));
  const deltaSeconds = deltaMs / 1000;
  lastFrameTime = now;

  if (state === "playing") {
    runSeconds += deltaSeconds;
    updateJourney(deltaSeconds);
    updateGustPhase();
    accumulator += deltaMs;

    while (accumulator >= FIXED_STEP && state === "playing") {
      updateVehicleControl();
      applyGustForce();
      Engine.update(engine, FIXED_STEP);
      enforcePlatformLimits();
      accumulator -= FIXED_STEP;
    }

    updateBadgeCollection();
    updateDangerFeedback();

    if (hasStackCollapsed()) beginFailure();
    else if (journeyProgress >= currentRoute.finishDistance) completeRoute();
  } else if (state === "failing") {
    failElapsed += deltaMs;
    accumulator += deltaMs;
    updateFailureTimeScale();

    while (accumulator >= FIXED_STEP) {
      Engine.update(engine, FIXED_STEP);
      enforcePlatformLimits();
      accumulator -= FIXED_STEP;
    }

    if (shouldShowResults()) showResults();
  }

  updateExpressions(deltaSeconds);
  updateParticles(deltaSeconds);
  updateImpactEffects(deltaSeconds);
  updateWindTravel(deltaSeconds);
  updateJourneyMessage(deltaSeconds);
  saveFlash = Math.max(0, saveFlash - deltaSeconds);
  shake = Math.max(0, shake - deltaMs * 0.022);
  syncInterface();
  draw(now / 1000);
  requestAnimationFrame(frame);
}

function updateJourney(deltaSeconds) {
  if (journeyPause > 0) {
    journeyPause = Math.max(0, journeyPause - deltaSeconds);
    return;
  }

  const previousProgress = journeyProgress;
  journeyProgress = Math.min(
    currentRoute.finishDistance,
    journeyProgress + JOURNEY_SPEED * deltaSeconds,
  );

  currentRoute.joinStops.forEach((stop, index) => {
    if (
      !joinedStops.has(index) &&
      previousProgress < stop.distance &&
      journeyProgress >= stop.distance
    ) {
      joinedStops.add(index);
      journeyProgress = stop.distance;
      journeyPause = 1.15;
      addFriend(stop.character);
    }
  });

  currentRoute.bumpDistances.forEach((distance, index) => {
    if (
      !triggeredBumps.has(index) &&
      previousProgress < distance &&
      journeyProgress >= distance
    ) {
      triggeredBumps.add(index);
      triggerBump(index);
    }
  });
}

function addFriend(character) {
  const spec = creatureSpecs.find((candidate) => candidate.kind === character);
  const topCreature = creatures[creatures.length - 1];
  if (!spec || !topCreature || creatures.some((creature) => creature.kind === character)) return;

  const y =
    topCreature.body.position.y -
    topCreature.proxyHeight / 2 -
    spec.proxyHeight / 2 -
    2;
  const creature = createCreature({ ...spec, x: CENTER_X, y });
  const links = createFriendshipLinks(topCreature, creature);
  creatures.push(creature);
  stackLinks.push(...links);
  Composite.add(engine.world, [creature.body, ...links]);
  Body.setVelocity(creature.body, { x: 0, y: 0.45 });
  burst(CENTER_X, y, "#fff0a6", reducedMotion.matches ? 7 : 22, "star");
  setJourneyMessage(`${character.toUpperCase()} FOUND!`, 1.8);
  liveStatus.textContent = `${character} joined the stack.`;
}

function triggerBump(index) {
  const direction = index % 2 === 0 ? 1 : -1;
  bumpKick = direction * (0.055 + selectedRouteIndex * 0.008);

  for (const creature of creatures) {
    Body.applyForce(creature.body, creature.body.position, {
      x: direction * 0.000013 * creature.body.mass,
      y: -0.000042 * creature.body.mass,
    });
  }

  burst(CENTER_X, ROAD_SURFACE_Y - 4, "#f7d29b", reducedMotion.matches ? 5 : 14, "dust");
  shake = reducedMotion.matches ? 0 : 3.4;
  setJourneyMessage("BUMP!", 0.8);
}

function updateVehicleControl() {
  if (keyboardDirection !== 0) {
    supportTarget = getKeyboardSupportTarget();
  } else if (pointerActive) {
    supportTarget = getCappedPointerSupportOffset(
      pointerControl,
      gust?.direction || 0,
      getActiveGustEnvelope(),
      WIND_PROFILE.forceMax,
      GRAVITY_SCALE,
      COUNTER_TILT_AUTHORITY,
      MAX_SUPPORT_OFFSET,
      MAX_PLATFORM_ANGLE,
    );
  } else {
    supportTarget = 0;
  }

  const supportError = supportTarget - supportOffset;
  supportOffset += clamp(supportError * 0.17, -2.7, 2.7);
  bumpKick *= 0.972;

  const targetAngle =
    getSupportAngle(supportOffset, MAX_SUPPORT_OFFSET, MAX_PLATFORM_ANGLE) +
    bumpKick;
  const angleError = targetAngle - platform.angle;
  const angleStep = clamp(angleError * 0.1, -0.0065, 0.0065);

  Body.setAngle(
    platform,
    clamp(platform.angle + angleStep, -MAX_PLATFORM_ANGLE, MAX_PLATFORM_ANGLE),
  );
  Body.setAngularVelocity(platform, 0);
}

function getKeyboardSupportTarget() {
  if (!gust || gust.phase !== "active" || keyboardDirection !== gust.direction) {
    return keyboardDirection * 25;
  }

  const support = getCounterSupportOffset(
    gust.force,
    gust.direction,
    GRAVITY_SCALE,
    COUNTER_TILT_AUTHORITY,
    MAX_SUPPORT_OFFSET,
    MAX_PLATFORM_ANGLE,
  );
  const minimumMagnitude = 0.045 / MAX_PLATFORM_ANGLE * MAX_SUPPORT_OFFSET;
  return Math.sign(support) * Math.max(Math.abs(support), minimumMagnitude);
}

function enforcePlatformLimits() {
  const boundedAngle = clamp(platform.angle, -MAX_PLATFORM_ANGLE, MAX_PLATFORM_ANGLE);
  if (boundedAngle !== platform.angle) {
    Body.setAngle(platform, boundedAngle);
    Body.setAngularVelocity(platform, 0);
  }
}

function scheduleGust(minimumRest = 0) {
  const timing = getGustTiming(random);
  const restSeconds = Math.max(minimumRest, timing.restSeconds);
  gust = {
    phase: "waiting",
    direction: random() > 0.5 ? 1 : -1,
    startsAt: runSeconds + restSeconds,
    endsAt: runSeconds + restSeconds + timing.durationSeconds,
    force: timing.force,
  };
}

function updateGustPhase() {
  if (!gust) return;

  if (gust.phase === "waiting" && runSeconds >= gust.startsAt) {
    gust.phase = "active";
    setJourneyMessage("HOLD TOGETHER!", 1.05);
    liveStatus.textContent = `A gust is pushing ${gust.direction > 0 ? "right" : "left"}.`;
  }

  if (gust.phase === "active" && runSeconds >= gust.endsAt) {
    scheduleGust();
  }
}

function applyGustForce() {
  if (!gust || gust.phase !== "active") return;
  const envelope = getActiveGustEnvelope();
  const horizontalAcceleration = getEffectiveGustAcceleration(
    gust.force,
    gust.direction,
    envelope,
    platform.angle,
    GRAVITY_SCALE,
    COUNTER_TILT_AUTHORITY,
  );

  for (const creature of creatures) {
    Body.applyForce(creature.body, creature.body.position, {
      x: horizontalAcceleration * creature.body.mass,
      y: -0.000011 * creature.body.mass * envelope,
    });
  }
}

function getActiveGustEnvelope() {
  if (!gust || gust.phase !== "active") return 0;
  const duration = Math.max(0.01, gust.endsAt - gust.startsAt);
  const progress = clamp((runSeconds - gust.startsAt) / duration, 0, 1);
  return getGustEnvelope(progress);
}

function getWindVisualIntensity() {
  if (!gust) return 0;

  if (gust.phase === "waiting") {
    const warningSeconds = 1.45;
    const timeUntil = gust.startsAt - runSeconds;
    if (timeUntil > warningSeconds) return 0;
    return clamp((warningSeconds - timeUntil) / warningSeconds, 0, 1) * 0.18;
  }

  const forceRatio = clamp(
    (gust.force - WIND_PROFILE.forceMin) / (WIND_PROFILE.forceMax - WIND_PROFILE.forceMin),
    0,
    1,
  );
  return getActiveGustEnvelope() * (0.44 + forceRatio * 0.56);
}

function updateWindTravel(deltaSeconds) {
  if (state !== "playing") return;
  const speed = getWindTravelSpeed(getWindVisualIntensity());
  windTravel = (windTravel + speed * deltaSeconds) % 540;
}

function updateBadgeCollection() {
  currentRoute.badgeOffsets.forEach((badge, index) => {
    if (collectedBadges.has(index)) return;
    const x = getWorldScreenX(badge.distance, journeyProgress, CENTER_X, PIXELS_PER_UNIT);
    if (x < CENTER_X - 34 || x > CENTER_X + 34) return;

    const y = getBadgeScreenY(badge.height);
    const reached = creatures.some((creature) => {
      const horizontalDistance = Math.abs(creature.body.position.x - x);
      const verticalDistance = Math.abs(creature.body.position.y - y);
      return horizontalDistance < 62 && verticalDistance < 83;
    });

    if (!reached) return;
    collectedBadges.add(index);
    burst(x, y, "#ffe06b", reducedMotion.matches ? 7 : 20, "star");
    setJourneyMessage("BADGE RESCUED!", 0.95);
    liveStatus.textContent =
      `${collectedBadges.size} of ${currentRoute.badgeOffsets.length} badges rescued.`;
  });
}

function getDangerLevel() {
  if (!platform || creatures.length === 0) return 0;
  const drift = Math.max(
    ...creatures.map((creature) => Math.abs(creature.body.position.x - CENTER_X)),
  );
  const speed = Math.max(
    ...creatures.map((creature) => Math.abs(creature.body.velocity.x)),
  );
  return clamp(
    Math.max(
      drift / 112,
      Math.abs(platform.angle) / MAX_PLATFORM_ANGLE,
      speed / 5.4,
    ),
    0,
    1,
  );
}

function updateDangerFeedback() {
  const danger = getDangerLevel();
  if (danger > 0.68) dangerWasHigh = true;

  if (dangerWasHigh && danger < 0.28) {
    dangerWasHigh = false;
    saveFlash = 0.8;
    burst(CENTER_X, 280, "#fff2a8", reducedMotion.matches ? 5 : 18, "star");
    liveStatus.textContent = "Nice save.";
  }

  if (danger > 0.8 && !reducedMotion.matches) {
    shake = Math.max(shake, Math.min(2.8, danger * 2));
  }
}

function updateExpressions(deltaSeconds) {
  const danger = state === "failing" ? 1 : getDangerLevel();
  const gustNerves = getWindVisualIntensity() * 0.2;

  for (const creature of creatures) {
    const target = creature.impactElapsed !== null
      ? 1
      : clamp(
        (danger + gustNerves - creature.panicThreshold) /
          Math.max(0.08, 1 - creature.panicThreshold),
        0,
        1,
      );
    const speed = target > creature.panicLevel ? 4.2 : 2.2;
    creature.panicLevel += (target - creature.panicLevel) * clamp(deltaSeconds * speed, 0, 1);
  }
}

function hasStackCollapsed() {
  const leftPlayArea = creatures.some(
    (creature) =>
      creature.body.position.y > FAIL_Y ||
      creature.body.position.x < -58 ||
      creature.body.position.x > WIDTH + 58,
  );
  const lostVerticalOrder = creatures
    .slice(1)
    .some((creature, index) => {
      const lower = creatures[index];
      const expectedGap = (lower.proxyHeight + creature.proxyHeight) * 0.5;
      const actualGap = lower.body.position.y - creature.body.position.y;
      return actualGap < expectedGap * 0.2;
    });
  const tornApart = creatures
    .slice(1)
    .some((creature, index) => {
      const lower = creatures[index];
      const dx = creature.body.position.x - lower.body.position.x;
      const dy = creature.body.position.y - lower.body.position.y;
      return Math.hypot(dx, dy) > 126;
    });
  return leftPlayArea || lostVerticalOrder || tornApart;
}

function beginFailure() {
  if (state !== "playing") return;
  state = "failing";
  failElapsed = 0;
  firstImpactAt = null;
  impactSlowMoEndsAt = null;
  pointerActive = false;
  pointerControl = 0;
  keyboardDirection = 0;
  gust = null;
  thumbCue.classList.remove("is-visible");
  canvas.classList.remove("is-grabbing");

  for (const link of stackLinks) Composite.remove(engine.world, link);
  stackLinks = [];

  const centerIndex = (creatures.length - 1) / 2;
  creatures.forEach((creature, index) => {
    const direction = Math.sign(creature.body.position.x - CENTER_X) || Math.sign(index - centerIndex) || 1;
    Body.setVelocity(creature.body, {
      x: creature.body.velocity.x + direction * (0.35 + Math.abs(index - centerIndex) * 0.14),
      y: creature.body.velocity.y - 0.45,
    });
    Body.setAngularVelocity(creature.body, direction * (0.018 + index * 0.004));
  });

  engine.timing.timeScale = 1;
  shake = reducedMotion.matches ? 0 : 8;
  burst(CENTER_X, PLATFORM_TOP - 8, "#fff2b3", reducedMotion.matches ? 5 : 15, "star");
  liveStatus.textContent = "The friends are falling.";
  syncInterface();
}

function handleCollisionStart(event) {
  if (state !== "failing") return;

  for (const pair of event.pairs) {
    const creatureBody = pair.bodyA.label === "catch-floor"
      ? pair.bodyB
      : pair.bodyB.label === "catch-floor"
        ? pair.bodyA
        : null;
    const creature = creatures.find((candidate) => candidate.body === creatureBody);
    if (!creature || creature.impactElapsed !== null) continue;

    creature.impactElapsed = failElapsed;
    const isFirstImpact = firstImpactAt === null;

    if (isFirstImpact) {
      firstImpactAt = failElapsed;
      const duration = reducedMotion.matches
        ? REDUCED_IMPACT_SLOWMO_DURATION_MS
        : IMPACT_SLOWMO_DURATION_MS;
      impactSlowMoEndsAt = failElapsed + duration;
      engine.timing.timeScale = reducedMotion.matches
        ? REDUCED_IMPACT_SLOWMO_TIME_SCALE
        : IMPACT_SLOWMO_TIME_SCALE;
      liveStatus.textContent = "Impact. The friends look dazed.";
    }

    const effectX = creature.body.position.x;
    const effectY = Math.min(ROAD_SURFACE_Y - 8, creature.body.position.y + creature.proxyHeight * 0.42);
    impactEffects.push({ x: effectX, y: effectY, life: 0.78, maxLife: 0.78 });
    burst(effectX, effectY, "#f8d49d", reducedMotion.matches ? 4 : 11, "dust");
    shake = reducedMotion.matches ? 0 : Math.max(shake, 4.5);
  }
}

function updateFailureTimeScale() {
  const slowMoScale = reducedMotion.matches
    ? REDUCED_IMPACT_SLOWMO_TIME_SCALE
    : IMPACT_SLOWMO_TIME_SCALE;
  engine.timing.timeScale = getFailureTimeScale(
    failElapsed,
    impactSlowMoEndsAt,
    slowMoScale,
  );
}

function shouldShowResults() {
  const timeout = reducedMotion.matches ? REDUCED_FAILURE_TIMEOUT_MS : FAILURE_TIMEOUT_MS;
  const impactHold = reducedMotion.matches
    ? REDUCED_FAILURE_IMPACT_HOLD_MS
    : FAILURE_IMPACT_HOLD_MS;
  return shouldShowFailureResults(failElapsed, firstImpactAt, impactHold, timeout);
}

function showResults() {
  if (state !== "failing") return;
  state = "results";
  engine.timing.timeScale = 1;
  const best = progressData.bestBadges[selectedRouteIndex];
  const newBest = collectedBadges.size > best;

  if (newBest) {
    progressData.bestBadges[selectedRouteIndex] = collectedBadges.size;
    writeProgress();
  }

  resultKicker.textContent = newBest ? "NEW BADGE BEST!" : pickFailureLine();
  resultTitle.textContent = collectedBadges.size === 0 ? "A spectacular pile." : "So close.";
  resultBadges.textContent =
    `${collectedBadges.size} of ${currentRoute.badgeOffsets.length} badges rescued`;
  resultOverlay.hidden = false;
  liveStatus.textContent =
    `Run over with ${collectedBadges.size} of ${currentRoute.badgeOffsets.length} badges.`;
  renderRoutePicker();
  syncInterface();
  retryButton.focus({ preventScroll: true });
}

function completeRoute() {
  if (state !== "playing") return;
  state = "finished";
  pointerActive = false;
  pointerControl = 0;
  keyboardDirection = 0;
  gust = null;
  journeyProgress = currentRoute.finishDistance;
  const badgeCount = collectedBadges.size;
  progressData.bestBadges[selectedRouteIndex] = Math.max(
    progressData.bestBadges[selectedRouteIndex],
    badgeCount,
  );

  if (selectedRouteIndex < ROUTES.length - 1) {
    progressData.unlockedRoute = Math.max(
      progressData.unlockedRoute,
      selectedRouteIndex + 1,
    );
  }

  progressData.selectedRoute = selectedRouteIndex;
  writeProgress();
  burst(CENTER_X, 230, "#fff0a4", reducedMotion.matches ? 10 : 34, "star");
  setJourneyMessage("FESTIVAL REACHED!", 1.2);
  liveStatus.textContent = `${currentRoute.title} completed with ${badgeCount} badges.`;
  nextRouteButton.textContent =
    selectedRouteIndex < ROUTES.length - 1 ? "Next road" : "Back to Orchard";
  finishBadges.textContent =
    `${badgeCount} of ${currentRoute.badgeOffsets.length} badges rescued`;
  clearFinishTimer();
  finishTimer = window.setTimeout(() => {
    finishOverlay.hidden = false;
    renderRoutePicker();
    syncInterface();
    nextRouteButton.focus({ preventScroll: true });
  }, reducedMotion.matches ? 120 : 720);
}

function pickFailureLine() {
  const lines = [
    "EVERYONE BECAME A PILE",
    "GRAVITY HAD NOTES",
    "THE ROAD FOUGHT BACK",
    "THE WIND IS VERY SORRY",
  ];
  return lines[Math.floor(random() * lines.length)];
}

function clearFinishTimer() {
  if (finishTimer !== null) {
    window.clearTimeout(finishTimer);
    finishTimer = null;
  }
}

function setJourneyMessage(message, seconds) {
  journeyMessage.textContent = message;
  journeyMessage.classList.add("is-visible");
  messageSeconds = seconds;
}

function updateJourneyMessage(deltaSeconds) {
  if (messageSeconds <= 0) return;
  messageSeconds = Math.max(0, messageSeconds - deltaSeconds);
  if (messageSeconds === 0) journeyMessage.classList.remove("is-visible");
}

function burst(x, y, color, count, shape = "circle") {
  for (let index = 0; index < count; index += 1) {
    const angle = random() * Math.PI * 2;
    const speed = 28 + random() * 78;
    const life = 0.48 + random() * 0.45;
    particles.push({
      x,
      y,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      radius: 2 + random() * 4,
      rotation: random() * Math.PI,
      spin: (random() - 0.5) * 8,
      color,
      shape,
      life,
      maxLife: life,
    });
  }
}

function updateParticles(deltaSeconds) {
  particles = particles.filter((particle) => {
    particle.x += particle.vx * deltaSeconds;
    particle.y += particle.vy * deltaSeconds;
    particle.vy += 92 * deltaSeconds;
    particle.rotation += particle.spin * deltaSeconds;
    particle.life -= deltaSeconds;
    return particle.life > 0;
  });
}

function updateImpactEffects(deltaSeconds) {
  impactEffects = impactEffects.filter((effect) => {
    effect.life -= deltaSeconds;
    return effect.life > 0;
  });
}

function draw(time) {
  context.save();
  const shakeX = reducedMotion.matches ? 0 : Math.sin(time * 91) * shake * 0.5;
  const shakeY = reducedMotion.matches ? 0 : Math.sin(time * 117 + 1.8) * shake * 0.5;
  context.translate(shakeX, shakeY);

  drawBackground(time);
  drawRouteDecor(time);
  drawRoad();
  drawRouteObjects(time);
  drawWind(time);
  drawVehicle(time);

  for (const creature of creatures) drawCreature(creature, time);

  drawImpactEffects();
  drawParticles();
  if (saveFlash > 0) drawSaveFlash();
  context.restore();
}

function drawBackground(time) {
  if (art.sky) {
    drawImageCover(art.sky, 0, 0, WIDTH, HEIGHT);
  } else {
    const sky = context.createLinearGradient(0, 0, 0, HEIGHT);
    sky.addColorStop(0, "#f4a07d");
    sky.addColorStop(0.62, "#f6c983");
    sky.addColorStop(1, "#c86f65");
    context.fillStyle = sky;
    context.fillRect(0, 0, WIDTH, HEIGHT);
  }

  if (selectedRouteIndex === 1) {
    context.fillStyle = "rgba(132, 203, 218, 0.09)";
    context.fillRect(0, 0, WIDTH, HEIGHT);
  } else if (selectedRouteIndex === 2) {
    const sunset = context.createLinearGradient(0, 0, 0, HEIGHT);
    sunset.addColorStop(0, "rgba(223, 99, 95, 0.14)");
    sunset.addColorStop(0.7, "rgba(164, 79, 92, 0.08)");
    sunset.addColorStop(1, "rgba(84, 45, 61, 0.18)");
    context.fillStyle = sunset;
    context.fillRect(0, 0, WIDTH, HEIGHT);
  }

  drawCloudSprite(art.cloudLeft, ((time * 4.2 + 35) % 540) - 90, 170, 0.56);
  drawCloudSprite(art.cloudMiddle, WIDTH - ((time * 3.1 + 20) % 560), 250, 0.44);
  drawCloudSprite(art.cloudRight, ((time * 2.4 + 180) % 620) - 130, 390, 0.28);

  const horizonHaze = context.createLinearGradient(0, 520, 0, ROAD_SURFACE_Y);
  horizonHaze.addColorStop(0, "rgba(255, 225, 183, 0)");
  horizonHaze.addColorStop(1, "rgba(255, 205, 159, 0.24)");
  context.fillStyle = horizonHaze;
  context.fillRect(0, 520, WIDTH, ROAD_SURFACE_Y - 520);
}

function drawImageCover(image, x, y, width, height) {
  const scale = Math.max(width / image.width, height / image.height);
  const drawWidth = image.width * scale;
  const drawHeight = image.height * scale;
  const drawX = x + (width - drawWidth) / 2;
  const drawY = y + (height - drawHeight) / 2;
  context.drawImage(image, drawX, drawY, drawWidth, drawHeight);
}

function drawCloudSprite(image, x, y, opacity) {
  if (!image) return;
  context.save();
  context.globalAlpha = opacity;
  context.drawImage(image, x, y, image.width * 0.54, image.height * 0.54);
  context.restore();
}

function drawRouteDecor(time) {
  const mesaDistances = [4, 18, 33, 49, 64];
  for (const [index, distance] of mesaDistances.entries()) {
    const x = getWorldScreenX(
      distance,
      journeyProgress * 0.44,
      CENTER_X,
      PIXELS_PER_UNIT * 0.58,
    );
    drawGroundedSprite(
      art.mesa,
      x,
      ROAD_SURFACE_Y + 3,
      150 + (index % 2) * 24,
      0.42,
    );
  }

  if (selectedRouteIndex === 0) {
    [3, 9, 17, 27, 34, 43].forEach((distance, index) => {
      const x = getWorldScreenX(distance, journeyProgress, CENTER_X, PIXELS_PER_UNIT);
      drawGroundedSprite(art.tree, x, ROAD_SURFACE_Y + 5, 92 + (index % 2) * 17, 0.95);
    });

    currentRoute.joinStops.forEach((stop) => {
      const x = getWorldScreenX(stop.distance, journeyProgress, CENTER_X, PIXELS_PER_UNIT);
      drawGroundedSprite(art.safeStop, x, ROAD_SURFACE_Y + 4, 78, 0.88);
    });
  }

  if (selectedRouteIndex === 1) {
    [8, 21, 37, 52].forEach((distance, index) => {
      const x = getWorldScreenX(distance, journeyProgress * 0.86, CENTER_X, PIXELS_PER_UNIT);
      const y = 365 + (index % 2) * 78 + Math.sin(time * 0.8 + index) * 5;
      const image = index % 2 === 0 ? art.cloudMiddle : art.cloudRight;
      if (!image) return;
      context.save();
      context.globalAlpha = 0.58;
      context.drawImage(image, x - 70, y, 140, 88);
      context.restore();
    });
  }

  const finishX = getWorldScreenX(
    currentRoute.finishDistance,
    journeyProgress,
    CENTER_X,
    PIXELS_PER_UNIT,
  );

  if (finishX > -220 && finishX < WIDTH + 220) {
    drawGroundedSprite(art.festivalArch, finishX - 42, ROAD_SURFACE_Y + 7, 126, 1);
    drawWindmill(finishX + 82, time);
  }
}

function drawGroundedSprite(image, centerX, bottomY, width, opacity) {
  if (!image) return;
  const height = width * image.height / image.width;
  context.save();
  context.globalAlpha = opacity;
  context.drawImage(image, centerX - width / 2, bottomY - height, width, height);
  context.restore();
}

function drawWindmill(x, time) {
  if (!art.windmillTower || !art.windmillRotor) return;
  const towerWidth = 106;
  const towerHeight = towerWidth * art.windmillTower.height / art.windmillTower.width;
  const towerBottom = ROAD_SURFACE_Y + 5;
  const towerTop = towerBottom - towerHeight;
  context.drawImage(
    art.windmillTower,
    x - towerWidth / 2,
    towerTop,
    towerWidth,
    towerHeight,
  );

  const rotorSize = 92;
  const rotorX = x + 4;
  const rotorY = towerTop + 38;
  context.save();
  context.translate(rotorX, rotorY);
  context.rotate(time * 0.62);
  context.drawImage(
    art.windmillRotor,
    -rotorSize / 2,
    -rotorSize / 2,
    rotorSize,
    rotorSize,
  );
  context.restore();
}

function drawRoad() {
  if (!art.road) {
    context.fillStyle = "#c97158";
    context.fillRect(0, ROAD_SURFACE_Y, WIDTH, HEIGHT - ROAD_SURFACE_Y);
    return;
  }

  const tileWidth = 632;
  const tileHeight = 300;
  const tileY = 629;
  const travel = journeyProgress * PIXELS_PER_UNIT;
  const offset = -(travel % tileWidth);

  for (let x = offset - tileWidth; x < WIDTH + tileWidth; x += tileWidth) {
    context.drawImage(art.road, x, tileY, tileWidth, tileHeight);
  }

  context.save();
  context.fillStyle = "rgba(118, 59, 50, 0.28)";
  for (let index = 0; index < 9; index += 1) {
    const x = ((index * 79 - travel * 0.94) % 560 + 560) % 560 - 80;
    const y = 786 + (index % 3) * 17;
    context.beginPath();
    context.ellipse(x, y, 6 + (index % 2) * 3, 2.4, -0.15, 0, Math.PI * 2);
    context.fill();
  }
  context.restore();
}

function drawRouteObjects(time) {
  currentRoute.bumpDistances.forEach((distance, index) => {
    if (triggeredBumps.has(index) && distance < journeyProgress - 2) return;
    const x = getWorldScreenX(distance, journeyProgress, CENTER_X, PIXELS_PER_UNIT);
    if (x < -70 || x > WIDTH + 70) return;
    if (art.bump) {
      context.drawImage(art.bump, x - 37, ROAD_SURFACE_Y - 24, 74, 43);
    }
  });

  currentRoute.badgeOffsets.forEach((badge, index) => {
    if (collectedBadges.has(index)) return;
    const x = getWorldScreenX(badge.distance, journeyProgress, CENTER_X, PIXELS_PER_UNIT);
    if (x < -50 || x > WIDTH + 50) return;
    const y = getBadgeScreenY(badge.height);
    drawBadge(x, y + Math.sin(time * 2.1 + index) * 5, time + index);
  });
}

function drawBadge(x, y, time) {
  context.save();
  context.translate(x, y);
  context.rotate(Math.sin(time * 1.6) * 0.11);
  const scale = 1 + Math.sin(time * 2.4) * 0.045;
  context.scale(scale, scale);
  context.shadowColor = "rgba(116, 69, 48, 0.34)";
  context.shadowBlur = 9;
  context.shadowOffsetY = 5;
  drawStarPath(0, 0, 18, 9, 5);
  context.fillStyle = "#ffd767";
  context.fill();
  context.shadowColor = "transparent";
  context.strokeStyle = "#ad6b47";
  context.lineWidth = 2.2;
  context.stroke();
  context.fillStyle = "rgba(255, 247, 184, 0.74)";
  context.beginPath();
  context.ellipse(-5, -7, 4, 2.2, -0.5, 0, Math.PI * 2);
  context.fill();
  context.restore();
}

function drawStarPath(x, y, outerRadius, innerRadius, points) {
  context.beginPath();
  for (let index = 0; index < points * 2; index += 1) {
    const radius = index % 2 === 0 ? outerRadius : innerRadius;
    const angle = -Math.PI / 2 + index * Math.PI / points;
    const pointX = x + Math.cos(angle) * radius;
    const pointY = y + Math.sin(angle) * radius;
    if (index === 0) context.moveTo(pointX, pointY);
    else context.lineTo(pointX, pointY);
  }
  context.closePath();
}

function drawWind(time) {
  const intensity = getWindVisualIntensity();
  if (intensity <= 0.005 || !gust) return;
  const direction = gust.direction;
  const lineCount = 4 + Math.floor(intensity * 9);
  const trailLength = 28 + intensity * 72;
  context.save();
  context.strokeStyle = `rgba(157, 225, 242, ${0.2 + intensity * 0.56})`;
  context.lineWidth = 2 + intensity * 2.7;
  context.lineCap = "round";
  context.shadowColor = `rgba(108, 203, 230, ${intensity * 0.34})`;
  context.shadowBlur = 5;

  for (let index = 0; index < lineCount; index += 1) {
    const phase = (windTravel + index * 73) % 540;
    const x = direction > 0 ? phase - 110 : WIDTH + 110 - phase;
    const y = 145 + index * (520 / lineCount) + Math.sin(time * 2.8 + index) * 13;
    context.beginPath();
    context.moveTo(x, y);
    context.bezierCurveTo(
      x + direction * trailLength * 0.35,
      y - 8,
      x + direction * trailLength * 0.72,
      y + 7,
      x + direction * trailLength,
      y - 3,
    );
    context.stroke();
  }

  context.restore();
}

function drawVehicle(time) {
  if (!platform) return;

  context.save();
  context.fillStyle = "rgba(78, 43, 48, 0.23)";
  context.beginPath();
  context.ellipse(CENTER_X + supportOffset, ROAD_SURFACE_Y + 5, 56, 10, 0, 0, Math.PI * 2);
  context.fill();
  context.restore();

  const wheelX = CENTER_X + supportOffset;
  if (art.wheel) {
    context.save();
    context.translate(wheelX, WHEEL_Y);
    context.rotate(journeyProgress * 0.8 + supportOffset * 0.028);
    context.shadowColor = "rgba(74, 39, 45, 0.32)";
    context.shadowBlur = 8;
    context.shadowOffsetY = 5;
    context.drawImage(art.wheel, -46, -42, 92, 84);
    context.restore();
  }

  context.save();
  context.translate(platform.position.x, platform.position.y);
  context.rotate(platform.angle);
  context.shadowColor = "rgba(74, 39, 45, 0.3)";
  context.shadowBlur = 10;
  context.shadowOffsetY = 7;
  if (art.beam) {
    context.drawImage(art.beam, -164, -25, 328, 50);
  } else {
    context.fillStyle = "#65aeb9";
    context.beginPath();
    context.roundRect(-152, -12, 304, 24, 12);
    context.fill();
  }
  context.restore();

  if (state === "ready") {
    context.save();
    context.globalAlpha = 0.24 + Math.sin(time * 2) * 0.04;
    context.fillStyle = "#fff3b4";
    context.beginPath();
    context.ellipse(wheelX, WHEEL_Y - 2, 16, 9, 0, 0, Math.PI * 2);
    context.fill();
    context.restore();
  }
}

function drawCreature(creature, time) {
  const { body, kind, drawWidth, drawHeight, drawOffsetY, phase } = creature;
  const impacted = creature.impactElapsed !== null;
  const calmArt = getCharacterArt(art, kind, "calm");
  const panicArt = getCharacterArt(art, kind, "panic");
  const impactArt = getCharacterArt(art, kind, "impact");
  const breathing = reducedMotion.matches ? 1 : 1 + Math.sin(time * 2.1 + phase) * 0.012;
  const velocitySquash = clamp(body.velocity.y * 0.006, -0.035, 0.045);

  context.save();
  context.translate(body.position.x, body.position.y);
  context.rotate(body.angle);
  context.scale(breathing - velocitySquash * 0.4, 1 / breathing + velocitySquash);
  context.shadowColor = "rgba(67, 35, 46, 0.25)";
  context.shadowBlur = 8;
  context.shadowOffsetY = 6;

  if (impacted && impactArt) {
    context.drawImage(
      impactArt,
      -drawWidth / 2,
      -drawHeight / 2 + drawOffsetY,
      drawWidth,
      drawHeight,
    );
  } else {
    if (calmArt) {
      context.drawImage(
        calmArt,
        -drawWidth / 2,
        -drawHeight / 2 + drawOffsetY,
        drawWidth,
        drawHeight,
      );
    }

    if (panicArt && creature.panicLevel > 0.02) {
      context.globalAlpha = clamp(creature.panicLevel, 0, 1);
      context.drawImage(
        panicArt,
        -drawWidth / 2,
        -drawHeight / 2 + drawOffsetY,
        drawWidth,
        drawHeight,
      );
      context.globalAlpha = 1;
    }
  }

  context.restore();
}

function drawImpactEffects() {
  for (const effect of impactEffects) {
    const progress = 1 - effect.life / effect.maxLife;
    const alpha = clamp(effect.life / effect.maxLife, 0, 1);
    context.save();
    context.globalAlpha = alpha;

    if (art.dust) {
      const width = 84 + progress * 28;
      const height = width * art.dust.height / art.dust.width;
      context.drawImage(art.dust, effect.x - width / 2, effect.y - height * 0.58, width, height);
    }

    if (art.impactStars) {
      const width = 92 + progress * 18;
      const height = width * art.impactStars.height / art.impactStars.width;
      context.drawImage(
        art.impactStars,
        effect.x - width / 2,
        effect.y - 74 - progress * 12,
        width,
        height,
      );
    }

    context.restore();
  }
}

function drawParticles() {
  for (const particle of particles) {
    context.save();
    context.globalAlpha = clamp(particle.life / particle.maxLife, 0, 1);
    context.fillStyle = particle.color;
    context.translate(particle.x, particle.y);
    context.rotate(particle.rotation);

    if (particle.shape === "star") {
      drawStarPath(0, 0, particle.radius * 1.5, particle.radius * 0.72, 5);
      context.fill();
    } else if (particle.shape === "dust") {
      context.beginPath();
      context.ellipse(0, 0, particle.radius * 1.5, particle.radius, 0, 0, Math.PI * 2);
      context.fill();
    } else {
      context.beginPath();
      context.arc(0, 0, particle.radius, 0, Math.PI * 2);
      context.fill();
    }

    context.restore();
  }
}

function drawSaveFlash() {
  const progress = 1 - saveFlash / 0.8;
  context.save();
  context.translate(CENTER_X, 255);
  context.scale(1 + progress * 0.12, 1 + progress * 0.12);
  context.globalAlpha = clamp(saveFlash * 1.6, 0, 1);
  context.fillStyle = "#fff6c8";
  context.font = "900 19px 'Avenir Next', system-ui, sans-serif";
  context.textAlign = "center";
  context.shadowColor = "rgba(85, 45, 50, 0.44)";
  context.shadowBlur = 6;
  context.fillText("NICE SAVE!", 0, 0);
  context.restore();
}

function syncInterface() {
  if (!currentRoute) return;
  badgeValue.textContent = `${collectedBadges.size} / ${currentRoute.badgeOffsets.length}`;
  routeName.textContent = currentRoute.title;
  progressFill.style.width =
    `${Math.round(getRouteCompletion(journeyProgress, currentRoute.finishDistance) * 100)}%`;

  const showHud = state === "playing" || state === "paused" || state === "failing";
  hud.hidden = !showHud;
  pauseButton.disabled = state !== "playing";
}

function createDefaultProgress() {
  return {
    unlockedRoute: 0,
    selectedRoute: 0,
    bestBadges: ROUTES.map(() => 0),
  };
}

function readProgress() {
  const defaults = createDefaultProgress();

  try {
    const stored = JSON.parse(window.localStorage.getItem(PROGRESS_KEY) || "{}");
    if (!stored || typeof stored !== "object" || Array.isArray(stored)) return defaults;

    const unlockedRoute = clamp(
      Math.round(Number(stored.unlockedRoute) || 0),
      0,
      ROUTES.length - 1,
    );
    const selectedRoute = clamp(
      Math.round(Number(stored.selectedRoute) || 0),
      0,
      unlockedRoute,
    );
    const storedBadges = Array.isArray(stored.bestBadges) ? stored.bestBadges : [];
    const bestBadges = ROUTES.map((route, index) =>
      clamp(
        Math.round(Number(storedBadges[index]) || 0),
        0,
        route.badgeOffsets.length,
      ));

    return { unlockedRoute, selectedRoute, bestBadges };
  } catch {
    return defaults;
  }
}

function writeProgress() {
  try {
    window.localStorage.setItem(PROGRESS_KEY, JSON.stringify(progressData));
  } catch {
    // The full game remains playable when private browsing blocks storage.
  }
}

function readMusicVolume() {
  try {
    const storedVolume = window.localStorage.getItem(MUSIC_VOLUME_KEY);
    if (storedVolume === null) return DEFAULT_MUSIC_VOLUME;
    const parsedVolume = Number(storedVolume);
    return Number.isFinite(parsedVolume)
      ? clamp(parsedVolume, 0, 1)
      : DEFAULT_MUSIC_VOLUME;
  } catch {
    return DEFAULT_MUSIC_VOLUME;
  }
}

function writeMusicVolume() {
  try {
    window.localStorage.setItem(MUSIC_VOLUME_KEY, String(musicVolume));
  } catch {
    // Music still works when private browsing blocks preference storage.
  }
}

if (new URLSearchParams(window.location.search).has("debug")) {
  window.__WOBBLE_DEBUG__ = {
    getState: () => state,
    getWorldState: () => ({
      routeIndex: selectedRouteIndex,
      routeId: currentRoute.id,
      progress: journeyProgress,
      completion: getRouteCompletion(journeyProgress, currentRoute.finishDistance),
      badges: [...collectedBadges],
      supportOffset,
      supportTarget,
      platformAngle: platform ? platform.angle : 0,
      creatureCount: creatures.length,
      creaturePositions: creatures.map((creature) => ({
        kind: creature.kind,
        x: creature.body.position.x,
        y: creature.body.position.y,
        panic: creature.panicLevel,
        impacted: creature.impactElapsed !== null,
      })),
    }),
    getGustState: () => gust
      ? {
        phase: gust.phase,
        direction: gust.direction,
        startsAt: gust.startsAt,
        endsAt: gust.endsAt,
        force: gust.force,
        envelope: getActiveGustEnvelope(),
        visualIntensity: getWindVisualIntensity(),
      }
      : null,
    getProgressData: () => ({
      unlockedRoute: progressData.unlockedRoute,
      selectedRoute: progressData.selectedRoute,
      bestBadges: [...progressData.bestBadges],
    }),
    getMusicState: () => ({
      volume: musicVolume,
      paused: musicPlayer.paused,
      currentTrack: currentMusicTrack
        ? new URL(currentMusicTrack).pathname.split("/").pop()
        : null,
      queuedTracks: musicQueue.map((track) => new URL(track).pathname.split("/").pop()),
    }),
    setMusicVolume: (percentage) => {
      setMusicVolume(percentage);
      return musicVolume;
    },
    nextMusicTrack: () => {
      playNextMusicTrack();
      return currentMusicTrack
        ? new URL(currentMusicTrack).pathname.split("/").pop()
        : null;
    },
    start: () => {
      if (state !== "ready" && state !== "results" && state !== "finished") return false;
      startRun();
      return true;
    },
    setRoute: (routeIndex) => {
      if (state !== "ready") return false;
      selectedRouteIndex = clamp(Math.round(routeIndex), 0, ROUTES.length - 1);
      progressData.unlockedRoute = Math.max(progressData.unlockedRoute, selectedRouteIndex);
      currentRoute = getRoute(selectedRouteIndex);
      resetJourney();
      renderRoutePicker();
      syncInterface();
      return true;
    },
    setControl: (value) => {
      if (state !== "playing") return false;
      pointerActive = true;
      pointerControl = clamp(Number(value) || 0, -1, 1);
      return true;
    },
    releaseControl: () => {
      pointerActive = false;
      pointerControl = 0;
      return true;
    },
    triggerGust: (direction = 1, force = WIND_PROFILE.forceMax) => {
      if (state !== "playing") return false;
      gust = {
        phase: "active",
        direction: Math.sign(direction) || 1,
        startsAt: runSeconds,
        endsAt: runSeconds + 4.8,
        force: clamp(force, WIND_PROFILE.forceMin, WIND_PROFILE.forceMax),
      };
      return true;
    },
    collapseNow: () => {
      if (state !== "playing") return false;
      beginFailure();
      const midpoint = (creatures.length - 1) / 2;
      creatures.forEach((creature, index) => {
        Body.setPosition(creature.body, {
          x: CENTER_X + (index - midpoint) * 42,
          y: 704 - index * 7,
        });
        Body.setVelocity(creature.body, {
          x: (index - midpoint) * 0.75,
          y: 2.8 + index * 0.22,
        });
        Body.setAngularVelocity(creature.body, (index - midpoint) * 0.028);
      });
      return true;
    },
    finishNow: () => {
      if (state !== "playing") return false;
      journeyProgress = currentRoute.finishDistance;
      completeRoute();
      return true;
    },
  };
}

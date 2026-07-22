import Matter from "matter-js";
import "./style.css";
import { clamp, createSeededRandom, formatTime, getGustTiming } from "./game-logic.js";

const { Bodies, Body, Composite, Constraint, Engine } = Matter;

const WIDTH = 390;
const HEIGHT = 844;
const CENTER_X = WIDTH / 2;
const FIXED_STEP = 1000 / 60;
const MAX_PLATFORM_ANGLE = 0.38;
const FAIL_Y = 744;
const BEST_SCORE_KEY = "wobble-stack-best-v1";
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

const canvas = document.querySelector("#game-canvas");
const context = canvas.getContext("2d");
const scoreValue = document.querySelector("#score-value");
const bestValue = document.querySelector("#best-value");
const pauseButton = document.querySelector("#pause-button");
const gustWarning = document.querySelector("#gust-warning");
const gustArrow = document.querySelector("#gust-arrow");
const startOverlay = document.querySelector("#start-overlay");
const startButton = document.querySelector("#start-button");
const resultOverlay = document.querySelector("#result-overlay");
const resultKicker = document.querySelector("#result-kicker");
const resultScore = document.querySelector("#result-score");
const resultBest = document.querySelector("#result-best");
const retryButton = document.querySelector("#retry-button");
const pauseOverlay = document.querySelector("#pause-overlay");
const resumeButton = document.querySelector("#resume-button");
const thumbCue = document.querySelector("#thumb-cue");
const liveStatus = document.querySelector("#live-status");

let engine;
let platform;
let platformPin;
let creatures = [];
let state = "ready";
let previousState = "ready";
let runSeconds = 0;
let bestSeconds = readBestScore();
let targetAngle = 0;
let pointerActive = false;
let keyboardDirection = 0;
let accumulator = 0;
let lastFrameTime = performance.now();
let failElapsed = 0;
let random = createSeededRandom(1);
let gust = null;
let dangerWasHigh = false;
let saveFlash = 0;
let shake = 0;
let particles = [];
let runCount = 0;

const creatureSpecs = [
  { kind: "pear", x: 195, y: 626, width: 82, height: 84, proxyWidth: 68, proxyHeight: 78, color: "#93bf67" },
  { kind: "cube", x: 195, y: 560, width: 70, height: 54, proxyWidth: 70, proxyHeight: 54, color: "#68a7c8" },
  { kind: "bird", x: 195, y: 505, width: 62, height: 62, proxyWidth: 56, proxyHeight: 56, color: "#ee9855" },
  { kind: "rabbit", x: 195, y: 446, width: 54, height: 68, proxyWidth: 52, proxyHeight: 62, color: "#aa75bd" },
  { kind: "jelly", x: 195, y: 390, width: 56, height: 56, proxyWidth: 50, proxyHeight: 50, color: "#65c8c3" },
];

setupCanvas();
resetPhysics();
syncScore();
syncControls();
requestAnimationFrame(frame);

startButton.addEventListener("click", startRun);
retryButton.addEventListener("click", startRun);
pauseButton.addEventListener("click", pauseRun);
resumeButton.addEventListener("click", resumeRun);

canvas.addEventListener("pointerdown", (event) => {
  if (state !== "playing") return;
  pointerActive = true;
  canvas.classList.add("is-grabbing");
  canvas.setPointerCapture(event.pointerId);
  updatePointerTarget(event);
  canvas.focus({ preventScroll: true });
  thumbCue.classList.remove("is-visible");
});

canvas.addEventListener("pointermove", (event) => {
  if (!pointerActive || state !== "playing") return;
  updatePointerTarget(event);
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

  if (event.key === "ArrowLeft" || event.key.toLowerCase() === "a") {
    keyboardDirection = -1;
    thumbCue.classList.remove("is-visible");
    event.preventDefault();
  }

  if (event.key === "ArrowRight" || event.key.toLowerCase() === "d") {
    keyboardDirection = 1;
    thumbCue.classList.remove("is-visible");
    event.preventDefault();
  }
});

window.addEventListener("keyup", (event) => {
  if (
    event.key === "ArrowLeft" ||
    event.key === "ArrowRight" ||
    event.key.toLowerCase() === "a" ||
    event.key.toLowerCase() === "d"
  ) {
    keyboardDirection = 0;
  }
});

document.addEventListener("visibilitychange", () => {
  if (document.hidden && state === "playing") pauseRun();
});

function setupCanvas() {
  const pixelRatio = Math.min(window.devicePixelRatio || 1, 2);
  canvas.width = WIDTH * pixelRatio;
  canvas.height = HEIGHT * pixelRatio;
  context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
}

function resetPhysics() {
  engine = Engine.create({ enableSleeping: false });
  engine.gravity.y = 1;
  engine.gravity.scale = 0.00105;

  platform = Bodies.rectangle(CENTER_X, 676, 286, 22, {
    label: "platform",
    density: 0.006,
    friction: 0.95,
    frictionAir: 0.08,
    restitution: 0.02,
    chamfer: { radius: 10 },
  });

  platformPin = Constraint.create({
    label: "platform-pin",
    pointA: { x: CENTER_X, y: 694 },
    bodyB: platform,
    pointB: { x: 0, y: 18 },
    length: 0,
    stiffness: 1,
    damping: 0.28,
  });

  creatures = creatureSpecs.map((spec) => createCreature(spec));

  const catchFloor = Bodies.rectangle(CENTER_X, 830, 520, 30, {
    label: "catch-floor",
    isStatic: true,
    friction: 0.8,
    restitution: 0.08,
    render: { visible: false },
  });

  Composite.add(engine.world, [platform, platformPin, catchFloor, ...creatures.map((item) => item.body)]);
}

function createCreature(spec) {
  const common = {
    label: `creature-${spec.kind}`,
    density: 0.0024,
    friction: 0.88,
    frictionStatic: 1.2,
    frictionAir: 0.012,
    restitution: 0.03,
    slop: 0.02,
  };

  let body;

  const chamferRadius = spec.kind === "cube" ? 14 : spec.kind === "pear" ? 23 : 20;
  body = Bodies.rectangle(spec.x, spec.y, spec.proxyWidth, spec.proxyHeight, {
    ...common,
    chamfer: { radius: chamferRadius },
  });

  return { ...spec, body };
}

function startRun() {
  resetPhysics();
  runCount += 1;
  random = createSeededRandom(7907 + runCount * 101);
  runSeconds = 0;
  targetAngle = 0;
  keyboardDirection = 0;
  pointerActive = false;
  failElapsed = 0;
  accumulator = 0;
  gust = null;
  dangerWasHigh = false;
  saveFlash = 0;
  shake = 0;
  particles = [];
  engine.timing.timeScale = 1;
  state = "playing";
  previousState = "playing";
  startOverlay.hidden = true;
  resultOverlay.hidden = true;
  pauseOverlay.hidden = true;
  liveStatus.textContent = "Game started. Keep the creatures balanced.";
  scheduleGust(4.2);
  syncScore();
  syncControls();

  if (runCount === 1) {
    thumbCue.classList.add("is-visible");
    window.setTimeout(() => thumbCue.classList.remove("is-visible"), 2400);
  }

  canvas.focus({ preventScroll: true });
}

function pauseRun() {
  if (state !== "playing") return;
  previousState = state;
  state = "paused";
  pointerActive = false;
  keyboardDirection = 0;
  canvas.classList.remove("is-grabbing");
  pauseOverlay.hidden = false;
  liveStatus.textContent = "Game paused.";
  syncControls();
  resumeButton.focus({ preventScroll: true });
}

function resumeRun() {
  if (state !== "paused") return;
  state = previousState === "playing" ? "playing" : previousState;
  pauseOverlay.hidden = true;
  liveStatus.textContent = "Game resumed.";
  syncControls();
  canvas.focus({ preventScroll: true });
}

function beginFailure() {
  if (state !== "playing") return;
  state = "failing";
  failElapsed = 0;
  pointerActive = false;
  keyboardDirection = 0;
  targetAngle = platform.angle;
  thumbCue.classList.remove("is-visible");
  hideGustWarning();
  engine.timing.timeScale = reducedMotion.matches ? 0.82 : 0.42;
  shake = reducedMotion.matches ? 0 : 10;
  burst(CENTER_X, 625, "#fff2b3", 12);
  liveStatus.textContent = "The stack fell.";
  syncControls();
}

function showResults() {
  if (state !== "failing") return;
  state = "results";
  engine.timing.timeScale = 1;
  const isNewBest = runSeconds > bestSeconds;

  if (isNewBest) {
    bestSeconds = runSeconds;
    writeBestScore(bestSeconds);
  }

  resultKicker.textContent = isNewBest ? "NEW BEST!" : pickFailureLine();
  resultScore.textContent = `${formatTime(runSeconds)}s`;
  resultBest.textContent = `Best ${formatTime(bestSeconds)}s`;
  resultOverlay.hidden = false;
  liveStatus.textContent = `Run over. You balanced for ${formatTime(runSeconds)} seconds.`;
  syncScore();
  syncControls();
  retryButton.focus({ preventScroll: true });
}

function pickFailureLine() {
  const lines = ["TOTAL DISASTER", "SO CLOSE", "EVERYONE PANICKED", "GRAVITY WINS"];
  return lines[Math.floor(random() * lines.length)];
}

function releasePointer(event) {
  if (!pointerActive) return;
  pointerActive = false;
  targetAngle = 0;
  canvas.classList.remove("is-grabbing");
  if (canvas.hasPointerCapture(event.pointerId)) canvas.releasePointerCapture(event.pointerId);
}

function updatePointerTarget(event) {
  const bounds = canvas.getBoundingClientRect();
  const normalizedX = clamp((event.clientX - bounds.left) / bounds.width, 0, 1);
  targetAngle = (normalizedX * 2 - 1) * MAX_PLATFORM_ANGLE;
}

function frame(now) {
  const deltaMs = Math.min(50, now - lastFrameTime);
  lastFrameTime = now;

  if (state === "playing") {
    runSeconds += deltaMs / 1000;
    accumulator += deltaMs;
    updateGustPhase();
    updateDangerFeedback();

    while (accumulator >= FIXED_STEP) {
      updatePlatformControl();
      applyGustForce();
      Engine.update(engine, FIXED_STEP);
      accumulator -= FIXED_STEP;
    }

    if (creatures.some(({ body }) => body.position.y > FAIL_Y || body.position.x < -55 || body.position.x > WIDTH + 55)) {
      beginFailure();
    }
  } else if (state === "failing") {
    failElapsed += deltaMs;
    accumulator += deltaMs;

    while (accumulator >= FIXED_STEP) {
      Engine.update(engine, FIXED_STEP);
      accumulator -= FIXED_STEP;
    }

    if (failElapsed >= 690) showResults();
  }

  updateParticles(deltaMs);
  saveFlash = Math.max(0, saveFlash - deltaMs / 1000);
  shake = Math.max(0, shake - deltaMs * 0.022);
  syncScore();
  draw(now / 1000);
  requestAnimationFrame(frame);
}

function updatePlatformControl() {
  const inputTarget = keyboardDirection === 0 ? targetAngle : keyboardDirection * MAX_PLATFORM_ANGLE;
  const neutralTarget = pointerActive || keyboardDirection !== 0 ? inputTarget : 0;
  const error = neutralTarget - platform.angle;
  const desiredVelocity = clamp(error * 0.17, -0.055, 0.055);
  const nextVelocity = platform.angularVelocity * 0.7 + desiredVelocity * 0.3;

  Body.setAngularVelocity(platform, nextVelocity);

  if (platform.angle < -MAX_PLATFORM_ANGLE - 0.02) {
    Body.setAngle(platform, -MAX_PLATFORM_ANGLE - 0.02);
    Body.setAngularVelocity(platform, Math.max(0, platform.angularVelocity));
  } else if (platform.angle > MAX_PLATFORM_ANGLE + 0.02) {
    Body.setAngle(platform, MAX_PLATFORM_ANGLE + 0.02);
    Body.setAngularVelocity(platform, Math.min(0, platform.angularVelocity));
  }
}

function scheduleGust(minimumRest = 0) {
  const timing = getGustTiming(runSeconds, random);
  const restSeconds = Math.max(minimumRest, timing.restSeconds);
  gust = {
    phase: "waiting",
    direction: random() > 0.5 ? 1 : -1,
    warningAt: runSeconds + restSeconds,
    startsAt: runSeconds + restSeconds + timing.warningSeconds,
    endsAt: runSeconds + restSeconds + timing.warningSeconds + timing.durationSeconds,
    force: timing.force,
  };
}

function updateGustPhase() {
  if (!gust) return;

  if (gust.phase === "waiting" && runSeconds >= gust.warningAt) {
    gust.phase = "warning";
    showGustWarning(gust.direction);
    liveStatus.textContent = `Wind warning from the ${gust.direction > 0 ? "left" : "right"}.`;
  }

  if (gust.phase === "warning" && runSeconds >= gust.startsAt) {
    gust.phase = "active";
    gustWarning.classList.add("is-active");
    shake = reducedMotion.matches ? 0 : 2.2;
  }

  if (gust.phase === "active" && runSeconds >= gust.endsAt) {
    hideGustWarning();
    scheduleGust();
  }
}

function applyGustForce() {
  if (!gust || gust.phase !== "active") return;
  const duration = Math.max(0.01, gust.endsAt - gust.startsAt);
  const progress = clamp((runSeconds - gust.startsAt) / duration, 0, 1);
  const pulse = Math.sin(progress * Math.PI);

  for (const { body } of creatures) {
    Body.applyForce(body, body.position, {
      x: gust.direction * gust.force * body.mass * pulse,
      y: -0.00004 * body.mass * pulse,
    });
  }
}

function updateDangerFeedback() {
  const maxDrift = Math.max(...creatures.map(({ body }) => Math.abs(body.position.x - CENTER_X)));
  const danger = Math.max(maxDrift / 116, Math.abs(platform.angle) / MAX_PLATFORM_ANGLE);

  if (danger > 0.67) dangerWasHigh = true;

  if (dangerWasHigh && danger < 0.28) {
    dangerWasHigh = false;
    saveFlash = 0.8;
    burst(CENTER_X, 275, "#fff2a8", reducedMotion.matches ? 5 : 18);
    liveStatus.textContent = "Nice save.";
  }

  if (danger > 0.78 && !reducedMotion.matches) {
    shake = Math.max(shake, Math.min(3, danger * 2));
  }
}

function showGustWarning(direction) {
  gustWarning.classList.toggle("from-right", direction < 0);
  gustArrow.textContent = "➜";
  gustWarning.classList.add("is-visible");
}

function hideGustWarning() {
  gustWarning.classList.remove("is-visible", "is-active", "from-right");
}

function burst(x, y, color, count) {
  for (let index = 0; index < count; index += 1) {
    const angle = random() * Math.PI * 2;
    const speed = 28 + random() * 74;
    particles.push({
      x,
      y,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      radius: 2 + random() * 4,
      color,
      life: 0.45 + random() * 0.45,
      maxLife: 0.9,
    });
  }
}

function updateParticles(deltaMs) {
  const deltaSeconds = deltaMs / 1000;

  particles = particles.filter((particle) => {
    particle.x += particle.vx * deltaSeconds;
    particle.y += particle.vy * deltaSeconds;
    particle.vy += 90 * deltaSeconds;
    particle.life -= deltaSeconds;
    return particle.life > 0;
  });
}

function draw(time) {
  context.save();
  const shakeX = reducedMotion.matches ? 0 : Math.sin(time * 91) * shake * 0.5;
  const shakeY = reducedMotion.matches ? 0 : Math.sin(time * 117 + 1.8) * shake * 0.5;
  context.translate(shakeX, shakeY);

  drawBackground(time);
  drawWind(time);
  drawPlatform();

  for (const creature of creatures) drawCreature(creature);
  drawParticles();

  if (saveFlash > 0) drawSaveFlash();
  context.restore();
}

function drawBackground(time) {
  const sky = context.createLinearGradient(0, 0, 0, HEIGHT);
  sky.addColorStop(0, "#f8b58f");
  sky.addColorStop(0.48, "#f39a78");
  sky.addColorStop(1, "#d96b65");
  context.fillStyle = sky;
  context.fillRect(-8, -8, WIDTH + 16, HEIGHT + 16);

  const sun = context.createRadialGradient(106, 153, 8, 106, 153, 92);
  sun.addColorStop(0, "rgba(255, 236, 185, 0.7)");
  sun.addColorStop(1, "rgba(255, 224, 172, 0)");
  context.fillStyle = sun;
  context.fillRect(10, 52, 200, 200);

  drawCloud(44 + Math.sin(time * 0.12) * 5, 216, 0.8, 0.22);
  drawCloud(304 + Math.sin(time * 0.1 + 2) * 6, 286, 0.56, 0.18);
  drawCloud(78 + Math.sin(time * 0.09 + 1) * 8, 456, 0.48, 0.12);
  drawIsland(44, 351, 0.66, -0.1);
  drawIsland(334, 418, 0.52, 0.14);
  drawIsland(60, 573, 0.38, 0.08);
  drawIsland(330, 585, 0.34, -0.08);

  const haze = context.createLinearGradient(0, 610, 0, HEIGHT);
  haze.addColorStop(0, "rgba(255, 210, 170, 0)");
  haze.addColorStop(1, "rgba(112, 58, 69, 0.26)");
  context.fillStyle = haze;
  context.fillRect(0, 610, WIDTH, HEIGHT - 610);
}

function drawCloud(x, y, scale, opacity) {
  context.save();
  context.translate(x, y);
  context.scale(scale, scale);
  context.fillStyle = `rgba(255, 234, 207, ${opacity})`;
  context.beginPath();
  context.arc(-25, 2, 22, 0, Math.PI * 2);
  context.arc(0, -10, 31, 0, Math.PI * 2);
  context.arc(29, 4, 20, 0, Math.PI * 2);
  context.roundRect(-46, 0, 94, 29, 15);
  context.fill();
  context.restore();
}

function drawIsland(x, y, scale, rotation) {
  context.save();
  context.translate(x, y);
  context.rotate(rotation);
  context.scale(scale, scale);
  context.globalAlpha = 0.34;
  context.fillStyle = "#8b5359";
  context.beginPath();
  context.moveTo(-53, -4);
  context.quadraticCurveTo(0, -22, 53, -4);
  context.quadraticCurveTo(25, 16, 6, 52);
  context.quadraticCurveTo(-13, 29, -53, -4);
  context.fill();
  context.fillStyle = "#dba26f";
  context.beginPath();
  context.ellipse(0, -6, 53, 15, 0, 0, Math.PI * 2);
  context.fill();
  context.restore();
}

function drawWind(time) {
  if (!gust || gust.phase !== "active") return;
  const direction = gust.direction;
  context.save();
  context.strokeStyle = "rgba(255, 239, 210, 0.34)";
  context.lineWidth = 3;
  context.lineCap = "round";

  for (let index = 0; index < 7; index += 1) {
    const phase = (time * 190 + index * 83) % 520;
    const x = direction > 0 ? phase - 80 : WIDTH + 80 - phase;
    const y = 210 + index * 58 + Math.sin(time * 3 + index) * 15;
    context.beginPath();
    context.moveTo(x, y);
    context.lineTo(x + direction * (34 + index * 2), y - 4);
    context.stroke();
  }

  context.restore();
}

function drawPlatform() {
  context.save();
  context.translate(CENTER_X, 696);
  context.fillStyle = "rgba(91, 46, 56, 0.22)";
  context.beginPath();
  context.ellipse(0, 93, 112, 22, 0, 0, Math.PI * 2);
  context.fill();

  const pedestal = context.createLinearGradient(-28, 0, 34, 82);
  pedestal.addColorStop(0, "#f2c071");
  pedestal.addColorStop(1, "#bd7158");
  context.fillStyle = pedestal;
  context.beginPath();
  context.moveTo(-19, 6);
  context.lineTo(19, 6);
  context.lineTo(67, 87);
  context.lineTo(-67, 87);
  context.closePath();
  context.fill();
  context.restore();

  context.save();
  context.translate(platform.position.x, platform.position.y);
  context.rotate(platform.angle);
  context.shadowColor = "rgba(76, 36, 48, 0.25)";
  context.shadowBlur = 12;
  context.shadowOffsetY = 8;
  context.fillStyle = "#f7c869";
  context.beginPath();
  context.roundRect(-143, -11, 286, 22, 10);
  context.fill();
  context.shadowColor = "transparent";
  context.fillStyle = "rgba(255, 246, 189, 0.46)";
  context.beginPath();
  context.roundRect(-132, -7, 264, 5, 3);
  context.fill();
  context.restore();

  context.fillStyle = "#9e5b50";
  context.beginPath();
  context.arc(CENTER_X, 694, 10, 0, Math.PI * 2);
  context.fill();
  context.fillStyle = "#f6d189";
  context.beginPath();
  context.arc(CENTER_X - 2, 691, 4, 0, Math.PI * 2);
  context.fill();
}

function drawCreature(creature) {
  const { body, kind, width, height, color } = creature;
  const danger = state === "failing" || Math.abs(body.angle) > 0.35 || Math.abs(body.velocity.x) > 2.6;
  const effort = state === "playing" && !danger && Math.abs(body.angle) > 0.15;

  context.save();
  context.translate(body.position.x, body.position.y);
  context.rotate(body.angle);
  context.shadowColor = "rgba(71, 34, 47, 0.22)";
  context.shadowBlur = 9;
  context.shadowOffsetY = 6;
  drawCreatureShape(kind, width, height, color);
  context.shadowColor = "transparent";
  drawFace(kind, width, height, danger, effort);
  context.restore();
}

function drawCreatureShape(kind, width, height, color) {
  context.fillStyle = color;

  if (kind === "pear") {
    context.beginPath();
    context.moveTo(0, -height * 0.48);
    context.bezierCurveTo(width * 0.14, -height * 0.32, width * 0.48, -height * 0.2, width * 0.49, height * 0.16);
    context.bezierCurveTo(width * 0.5, height * 0.48, width * 0.2, height * 0.52, 0, height * 0.48);
    context.bezierCurveTo(-width * 0.2, height * 0.52, -width * 0.5, height * 0.48, -width * 0.49, height * 0.16);
    context.bezierCurveTo(-width * 0.48, -height * 0.2, -width * 0.14, -height * 0.32, 0, -height * 0.48);
    context.fill();
  } else if (kind === "cube") {
    context.beginPath();
    context.roundRect(-width / 2, -height / 2, width, height, 16);
    context.fill();
  } else if (kind === "bird") {
    context.beginPath();
    context.arc(0, 0, width / 2, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = "#d97845";
    context.beginPath();
    context.moveTo(-5, -height / 2 + 4);
    context.lineTo(1, -height / 2 - 10);
    context.lineTo(8, -height / 2 + 5);
    context.fill();
    context.fillStyle = "#ffe0a0";
    context.beginPath();
    context.moveTo(width * 0.32, -2);
    context.lineTo(width * 0.52, 5);
    context.lineTo(width * 0.31, 12);
    context.closePath();
    context.fill();
  } else if (kind === "rabbit") {
    context.beginPath();
    context.roundRect(-width / 2, -height / 2 + 8, width, height - 8, 23);
    context.fill();
    context.beginPath();
    context.ellipse(-13, -height / 2 + 3, 10, 23, -0.18, 0, Math.PI * 2);
    context.ellipse(13, -height / 2 + 3, 10, 23, 0.18, 0, Math.PI * 2);
    context.fill();
  } else {
    context.beginPath();
    context.arc(0, 2, width / 2, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = "#f7ca58";
    context.beginPath();
    context.moveTo(-17, -height / 2 + 2);
    context.lineTo(-13, -height / 2 - 13);
    context.lineTo(-3, -height / 2 - 5);
    context.lineTo(4, -height / 2 - 16);
    context.lineTo(13, -height / 2 - 5);
    context.lineTo(18, -height / 2 - 13);
    context.lineTo(18, -height / 2 + 3);
    context.closePath();
    context.fill();
  }

  const highlight = context.createRadialGradient(-width * 0.18, -height * 0.2, 1, 0, 0, width * 0.55);
  highlight.addColorStop(0, "rgba(255, 255, 255, 0.27)");
  highlight.addColorStop(0.58, "rgba(255, 255, 255, 0)");
  context.fillStyle = highlight;
  context.beginPath();
  context.ellipse(-width * 0.08, -height * 0.06, width * 0.34, height * 0.36, -0.35, 0, Math.PI * 2);
  context.fill();
}

function drawFace(kind, width, height, danger, effort) {
  const faceY = kind === "pear" ? 5 : kind === "rabbit" ? 5 : 2;
  const eyeGap = width * 0.17;
  const eyeY = faceY - 7;
  context.fillStyle = "#fff8e9";

  if (danger) {
    context.beginPath();
    context.ellipse(-eyeGap, eyeY, 7, 9, 0, 0, Math.PI * 2);
    context.ellipse(eyeGap, eyeY, 7, 9, 0, 0, Math.PI * 2);
    context.fill();
  } else {
    context.beginPath();
    context.ellipse(-eyeGap, eyeY, 6, effort ? 4 : 7, 0, 0, Math.PI * 2);
    context.ellipse(eyeGap, eyeY, 6, effort ? 4 : 7, 0, 0, Math.PI * 2);
    context.fill();
  }

  context.fillStyle = "#3d2c37";
  const pupilOffset = clamp(platform.angle * 14, -3, 3);
  context.beginPath();
  context.arc(-eyeGap + pupilOffset, eyeY + 1, 2.4, 0, Math.PI * 2);
  context.arc(eyeGap + pupilOffset, eyeY + 1, 2.4, 0, Math.PI * 2);
  context.fill();

  if (danger) {
    context.beginPath();
    context.ellipse(0, faceY + 13, 7, 9, 0, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = "#e77a79";
    context.beginPath();
    context.ellipse(0, faceY + 16, 4, 3, 0, 0, Math.PI * 2);
    context.fill();
  } else {
    context.strokeStyle = "#3d2c37";
    context.lineWidth = 2.5;
    context.lineCap = "round";
    context.beginPath();
    if (effort) {
      context.moveTo(-6, faceY + 14);
      context.quadraticCurveTo(0, faceY + 10, 6, faceY + 14);
    } else {
      context.arc(0, faceY + 7, 7, 0.18, Math.PI - 0.18);
    }
    context.stroke();
  }
}

function drawParticles() {
  for (const particle of particles) {
    context.globalAlpha = clamp(particle.life / particle.maxLife, 0, 1);
    context.fillStyle = particle.color;
    context.beginPath();
    context.arc(particle.x, particle.y, particle.radius, 0, Math.PI * 2);
    context.fill();
  }
  context.globalAlpha = 1;
}

function drawSaveFlash() {
  const progress = 1 - saveFlash / 0.8;
  context.save();
  context.translate(CENTER_X, 245);
  context.scale(1 + progress * 0.12, 1 + progress * 0.12);
  context.globalAlpha = clamp(saveFlash * 1.6, 0, 1);
  context.fillStyle = "#fff6c8";
  context.font = "900 20px 'Avenir Next', system-ui, sans-serif";
  context.textAlign = "center";
  context.fillText("NICE SAVE!", 0, 0);
  context.restore();
}

function syncScore() {
  scoreValue.textContent = formatTime(runSeconds);
  bestValue.textContent = `BEST ${formatTime(bestSeconds)}`;
}

function syncControls() {
  const canPause = state === "playing";
  pauseButton.disabled = !canPause;
  pauseButton.style.opacity = canPause ? "1" : "0";
  pauseButton.style.pointerEvents = canPause ? "auto" : "none";
}

function readBestScore() {
  try {
    const value = Number.parseFloat(window.localStorage.getItem(BEST_SCORE_KEY) || "0");
    return Number.isFinite(value) ? value : 0;
  } catch {
    return 0;
  }
}

function writeBestScore(value) {
  try {
    window.localStorage.setItem(BEST_SCORE_KEY, String(value));
  } catch {
    // The game remains fully playable when storage is unavailable.
  }
}

if (new URLSearchParams(window.location.search).has("debug")) {
  window.__WOBBLE_DEBUG__ = {
    getState: () => state,
    getRunSeconds: () => runSeconds,
    getPlatformAngle: () => platform.angle,
    failNow: () => beginFailure(),
  };
}

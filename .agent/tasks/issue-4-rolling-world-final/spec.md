# Issue 4 — rolling-world final iPhone game

Status: Frozen for implementation
Frozen: 2026-07-25
Canonical issue: `kiku-jw/wobble-stack#4`

## Owner outcome

Turn the current static balance prototype into the finished portrait iPhone
game represented by the concept frames. The plank must visibly rest on a wheel
that rolls on physical ground. The player drives that wheel with one thumb;
the plank and creature tower react through physics rather than direct angle
control. The world travels with them, characters act like five distinct living
friends, and a collapse becomes a short comic physical event.

## Final game decision

Five friends are late for the sunset celebration at the windmill. Their cart
has broken down, so they travel on the remaining wooden plank and one star
wheel while collecting the golden festival badges scattered along the road.

- The first route begins with Pear, Cube, and Bird.
- Rabbit joins at the first safe stop; Jelly King joins at the second.
- The finished roster is exactly five characters. Version 1 favors personality
  depth over an expandable catalogue.
- A run is a rightward journey with a visible destination, not an endless
  stationary timer.
- Golden badges sit at different heights and are collected by creature contact.
  They provide per-route mastery and a reason to risk a lean.
- Three short handcrafted routes form the first release: Orchard Road,
  Cloud Bridge, and Windmill Hill.
- Failure is losing any friend to the ground. Success is bringing the complete
  active group to the route finish.

## Visual comparison receipt

At scope freeze, the build already matched the concepts in palette, toy
material, character identity, and portrait composition. It did not yet match
their game:

- the wheel and plank float above a background floor;
- the camera and environment never travel;
- input rotates a kinematic plank directly;
- each creature is one rigid sprite with only calm, panic, and impact swaps;
- contacts have large visual gaps and appendages cannot lag, brace, or grip;
- collapse reads as five rotating cards instead of articulated bodies;
- the ground receives no wheel dust, landing deformation, debris, or weight;
- cloud and windmill layers are baked into a static image;
- setup and result controls compete with the toy scene.

The target concept reads in the opposite order: grounded wheel and road,
physical diagonal tower, five individualized reactions, layered moving world,
then only score and pause.

## Acceptance criteria

### AC1 — Grounded rolling vehicle

- A dynamic circular wheel collides with a continuous visible terrain surface.
- The plank is supported above the wheel by a physical single-wheel joint and
  is never positioned or rotated directly during play.
- The wheel rotates consistently with travelled distance and never appears to
  float, penetrate, or leave the road on ordinary terrain.
- Horizontal thumb displacement from the touch-down point controls signed
  wheel motor torque. Returning near the touch-down point coasts/brakes;
  releasing the thumb does not snap the plank.
- The same contract works anywhere outside UI controls and does not require a
  screen edge.

### AC2 — Travel camera and readable balance

- Correctly moving the wheel under a leaning tower materially improves survival.
- Neutral input and deliberately moving away from the lean remain fallible
  under the strongest accepted gust.
- A follow camera uses a horizontal dead zone and velocity look-ahead without
  making the wheel or tower hard to read.
- A full route has enough horizontal space to establish travel and uses no
  teleporting stage reset.
- The first calm segment teaches roll, coast, and catch before the first
  consequential gust.

### AC3 — Dense articulated creature stack

- Physics colliders follow the visible torso silhouette and leave no obvious
  air gap between consecutive friends or the plank.
- Each character has an independently moving torso plus visible articulated
  appendages appropriate to its silhouette: feet and arms for all, ears for
  Rabbit, leaves for Pear, wings and crest for Bird, soft skirt/crown response
  for Jelly.
- Appendages use damped secondary motion driven by acceleration, wind, contact,
  and impacts; they do not repeat a fixed sine-only animation.
- Hands can visibly find nearby grip points on the plank or another character.
  Grips are weak, breakable, visibly represented, and cannot make neutral or
  wrong play immortal.
- Bodies continue to collide and tumble freely after a grip breaks.

### AC4 — Character-specific life and emotion

- Every character supports calm, anticipation, effort, panic, relief, impact,
  and dazed behavior with character-specific thresholds and timing.
- Eyes track nearby danger or the direction of travel, blink at irregular
  intervals, and briefly squeeze on hard contact.
- Pear braces and protects the base; Cube concentrates and reaches to stabilize;
  Bird flaps and overreacts; Rabbit clings with delayed ears; Jelly tries to
  remain regal until its crown lifts.
- Characters do not begin a run in a surprised face.
- At gameplay scale, at least three emotional states per character are
  distinguishable without reading UI.

### AC5 — Living travelling world

- Background, midground, road, and foreground move at distinct parallax rates.
- Clouds drift independently and loop without visible seams.
- Windmill blades rotate continuously while the tower travels toward them.
- Roadside props provide motion reference without obscuring the wheel, plank,
  creatures, stars, or safe-area controls.
- Wind remains a blue-tinted world effect whose direction and buildup are
  readable without text, arrows, meters, or difficulty labels.

### AC6 — Journey, stars, onboarding, and minimal UI

- The first route teaches the relative one-thumb drive gesture through play and
  a single temporary finger cue.
- Rabbit and Jelly join at authored safe stops, producing the full five-friend
  concept stack during the first route.
- Golden badges can be collected by creature trigger contact and are persisted
  as a best per-route total.
- Gameplay UI contains only the collected badge count and pause.
- Ready state uses one primary Play action; accessibility motion control lives
  inside Pause/Settings, not on the title composition.
- Failure shows one dominant Retry action. Route selection and progression
  never compete with the immediate retry path.

### AC7 — Comic saves, falls, and finish

- Near-saves produce secondary-motion overshoot, relief faces, a restrained
  camera impulse, sound, and haptic feedback.
- The first ground impact triggers impact-only slow motion, local dust,
  squash/rebound, facial impact response, and a small camera impulse.
- Subsequent bodies can land during the same collapse beat; results do not hide
  them before the fall is readable.
- Crown, stars, dust, and small toy debris follow distinct physical arcs.
- Reduced Motion removes camera shake and prolonged slow motion while retaining
  semantic wind, impact, and expression feedback.
- Route finish produces a short group recovery/celebration without a modal
  wall covering the characters.

### AC8 — Release proof

- Domain, EditMode, and PlayMode suites cover input mapping, wheel travel,
  board freedom, camera tracking, dense contacts, breakable grips, character
  reactions, star collection, route state, fallability, and interruption.
- Portrait captures cover title, calm travel, catch, gust, star pickup,
  five-friend stack, collapse impact, finish, and retry.
- The current public web prototype remains available but is not used as proof
  for native physics quality.
- A non-Development arm64 iPhone build compiles, signs, installs, and launches
  on the paired device.
- Final release claims require owner device approval of control clarity,
  physical feel, visual life, performance, and voluntary Retry.

## Constraints

- Preserve the user-owned five-character concept identity and warm clay visual
  language.
- No accounts, backend, analytics SDK, ads, IAP, multiplayer, live operations,
  or external runtime dependency for the first release.
- Generated source assets must be committed with an updated asset manifest.
- Do not use invisible positional auto-balance, direct plank rotation, frozen
  character rotation, permanent tethers, or unbreakable neighbor joints.
- Physics proof must compare human-repeatable constant gestures, not a scripted
  frame-perfect correction.
- Implement the lowest complete vertical slice first:
  `thumb -> wheel -> ground -> plank -> stack -> camera -> fall`.

## Execution gates

1. Replace the static stage with the rolling single-wheel vertical slice.
2. Tune the three-creature first segment until correct catch, neutral, and
   wrong-direction outcomes are distinguishable on the same seed.
3. Add camera travel, first route geometry, stars, and join stops.
4. Replace rigid character presentation with articulated rigs, grips,
   secondary motion, and personality reactions.
5. Split and animate the environment, then add complete fall/finish effects.
6. Remove obsolete setup UI and complete progression/persistence.
7. Run automated, visual, and physical-iPhone release gates.

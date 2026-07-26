# iPhone status

Updated: 2026-07-25

## Current

- Phase: M3 rolling-world native candidate, automated and visual gates passed.
- Canonical task: `kiku-jw/wobble-stack#4`.
- Next actor: Agent, exporting and installing the current source revision on
  the paired iPhone; owner feel approval follows.
- Blocker: none for local production. Subjective control remains a physical
  iPhone gate; App Store distribution remains a separate Apple Developer
  Program boundary.

## Verified native candidate

- The direct-angle kinematic beam is gone from native gameplay.
- A dynamic star wheel rolls on a continuous physical road and drives a dynamic
  plank through one native `WheelJoint2D`.
- Touch displacement is relative to the initial contact point, works away from
  the bezel, and controls wheel motion rather than plank angle.
- A smoothed camera follows horizontal vehicle travel with velocity look-ahead;
  wind rendering moves with the camera.
- Correct wheel travel completes the strongest deterministic gust in both
  directions with three and five creatures. Neutral and wrong travel still
  collapse.
- A delayed broad rescue gesture also completes the strongest matrix without
  tower-state feedback or per-frame scripted correction.
- The wheel stays grounded through a strong five-friend catch, and measured
  sprite fill now matches its circular collider at the road contact.
- A generated alpha clay terrain tile replaces the temporary flat-color road.
- Route distance comes from a physical-wheel odometer; counter-steering remains
  local balance input and cannot erase forward journey progress.
- Three finite routes provide `7/8/9` badges, parallax landmarks, four moving
  clouds, a turning windmill, friend stops, route unlocks, and a festival
  finish.
- Five articulated rigs provide character-specific blink, gaze, emotion,
  secondary appendage motion, weak visible grips, free collapse, impact
  reactions, dust, debris, crown flight, sound, haptics, and impact-only slow
  motion.
- Gameplay UI is limited to badge count and Pause. Difficulty and creature
  count selectors are removed; gust intensity varies continuously without a
  HUD label.
- Current start, travel, collapse, and finish captures are stored under the
  canonical proof bundle and reflected in the README.
- Unity batch compile, `13/13` EditMode, `25/25` PlayMode, Mac smoke build, and
  inspected portrait Metal captures pass.

## Remaining release gate

- Fresh non-Development iOS export, signed arm64 build, install, and launch.
- Physical-device performance, gesture readability, first-route pacing, Retry,
  and voluntary-Retry owner verdict.
- TestFlight and App Store distribution remain separate Apple account gates.

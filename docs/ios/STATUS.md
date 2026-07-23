# iPhone status

Updated: 2026-07-23

## Current

- Phase: M2 constant-hold control recovery; physical-feel retest waiting.
- Canonical task: `kiku-jw/wobble-stack#4`.
- Next actor: Owner, to test the launched constant-hold recovery build on the
  iPhone.
- Blocker: none for another local iteration. App Store distribution remains a
  separate Apple Developer Program gate.

## Verified inputs

- Public web prototype establishes the game loop and deterministic calibration harness.
- Same-seed web outcomes after the latest physics fix: neutral `9.00 s`, correct `11.78 s`, wrong `7.22 s`.
- Three concept frames establish gameplay, collapse, team, character, environment, and UI direction.
- Unity `6000.3.19f1` and `6000.5.2f1` are installed; `6000.3.19f1` includes iOS support.
- Xcode `26.6` is selected, licensed, and initialized with the iOS `26.5` platform runtime.
- A paired development iPhone and valid Apple Development signing identity are
  available for local device builds.

## Verified in-repository

- Complete Ready → Playing → Paused/Failing → Results → Retry state flow.
- Deterministic gust model with distinct Gentle, Normal, and Wild force bands.
- Constant outer-side input moves the beam during the teaching window and
  completes the strongest first gust across all three difficulties, both
  directions, and three/five-creature towers without a changing test angle.
- Three-to-five creature setup, local per-setup best scores, reduced motion, and safe-area UI.
- Rounded beam collision, compact flat-contact silhouettes, free rotation, and
  a jointless physical stack.
- Calm, wind/panic, and impact-expression atlases plus a 1024 px app icon.
- Impact-only slow motion, flying crown, dust/stars, procedural wind and feedback audio, and iOS haptic hooks.
- Maximum Wild wind collapses neutral and wrong input; a constant correct hold
  completes it.
- Unity batch compile, `10/10` EditMode tests, `12/12` PlayMode tests, Mac smoke
  build, and four Metal-rendered portrait captures.
- Unity exports a non-Development Xcode project. Xcode produced a valid signed
  arm64 app, and CoreDevice confirmed install and launch on the paired iPhone.

## Next verified outcome

Pass the owner playtest for constant-hold first-gust survival, understandable
beam/thumb correspondence, believable fallability, beam-end collisions,
expression readability, visible wind direction, clean presentation, and
voluntary Retry. Install and launch receipts prove delivery, not subjective
feel, performance, or App Store readiness.

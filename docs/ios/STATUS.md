# iPhone status

Updated: 2026-07-25

## Current

- Phase: M2 single-mode variable-intensity device retest.
- Canonical task: `kiku-jw/wobble-stack#4`.
- Next actor: Owner, to test the launched single-mode build on the iPhone.
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
- One deterministic continuous gust range produces mostly Normal-like wind plus
  occasional visibly soft and strong outliers; elapsed time does not raise it.
- Difficulty selection and the intensity HUD are absent. Creature count and
  reduced motion remain setup options.
- Early strong input and a delayed moderate input both complete the strongest
  possible gust in both directions and three/five-creature towers without a
  changing test angle.
- Correct tilt directly weakens gust acceleration for every creature and
  damps only motion currently traveling downwind. It never chooses the side or
  pulls a recovering body back.
- Three-to-five creature setup, local best scores by count, reduced motion, and
  safe-area UI. Former Normal score slots remain readable.
- Rounded beam collision, compact flat-contact silhouettes, free rotation, and
  a jointless physical stack.
- Calm, wind/panic, and impact-expression atlases plus a 1024 px app icon.
- Impact-only slow motion, flying crown, dust/stars, procedural wind and feedback audio, and iOS haptic hooks.
- Maximum wind collapses neutral and wrong input; a constant correct hold
  completes it.
- Unity batch compile, `12/12` EditMode tests, `13/13` PlayMode tests, Mac smoke
  build, and an inspected Metal gameplay capture pass for the replacement
  source.
- Unity exports a non-Development Xcode project. Xcode produced a valid signed
  arm64 app, and CoreDevice confirmed install and launch on the paired iPhone.

## Next verified outcome

Pass the owner playtest for the single-mode intensity mix: soft and strong
gusts should feel different without a meter while delayed imprecise correction,
believable fallability, beam-end collisions, expression readability, clean
presentation, and voluntary Retry remain intact. Install and launch receipts
prove delivery, not subjective feel, performance, or App Store readiness.

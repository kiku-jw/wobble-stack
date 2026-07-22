# iPhone status

Updated: 2026-07-22

## Current

- Phase: M1 verified; M2 physical-iPhone proof waiting.
- Canonical task: `kiku-jw/wobble-stack#4`.
- Next actor: Owner, to install the missing Apple/Unity toolchain.
- Blocker for device delivery: Unity iOS Build Support and full Xcode are not installed.

## Verified inputs

- Public web prototype establishes the game loop and deterministic calibration harness.
- Same-seed web outcomes after the latest physics fix: neutral `9.00 s`, correct `11.78 s`, wrong `7.22 s`.
- Three concept frames establish gameplay, collapse, team, character, environment, and UI direction.
- Unity `6000.3.19f1` and `6000.5.2f1` are installed; neither currently includes iOS support.

## Verified in-repository

- Complete Ready → Playing → Paused/Failing → Results → Retry state flow.
- Deterministic gust model with distinct Gentle, Normal, and Wild force bands.
- Whole-stack counter-tilt contract verified for both wind directions.
- Three-to-five creature setup, local per-setup best scores, reduced motion, and safe-area UI.
- Clay start, gameplay, collapse, impact-expression, results, and 1024 px app-icon art.
- Impact-only slow motion, flying crown, dust/stars, procedural wind and feedback audio, and iOS haptic hooks.
- Unity batch compile, `7/7` EditMode tests, `6/6` PlayMode tests, Mac smoke build, and four offscreen portrait captures.

## Next verified outcome

Install Unity iOS Build Support plus full Xcode, export the generated Xcode project, and pass the physical-iPhone device and fresh-player gates. The product is not release-ready until that happens.

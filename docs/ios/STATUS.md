# iPhone status

Updated: 2026-07-23

## Current

- Phase: M1 verified; M2 physical-iPhone proof waiting.
- Canonical task: `kiku-jw/wobble-stack#4`.
- Next actor: Owner, to connect a physical iPhone and select an Apple Personal or Developer Team in Xcode.
- Blocker for device delivery: no physical iPhone is connected and this Mac has no valid code-signing identity.

## Verified inputs

- Public web prototype establishes the game loop and deterministic calibration harness.
- Same-seed web outcomes after the latest physics fix: neutral `9.00 s`, correct `11.78 s`, wrong `7.22 s`.
- Three concept frames establish gameplay, collapse, team, character, environment, and UI direction.
- Unity `6000.3.19f1` and `6000.5.2f1` are installed; `6000.3.19f1` now includes iOS support.
- Xcode `26.6` is selected, licensed, and initialized with the iOS `26.5` platform runtime.

## Verified in-repository

- Complete Ready → Playing → Paused/Failing → Results → Retry state flow.
- Deterministic gust model with distinct Gentle, Normal, and Wild force bands.
- Whole-stack counter-tilt contract verified for both wind directions.
- Three-to-five creature setup, local per-setup best scores, reduced motion, and safe-area UI.
- Clay start, gameplay, collapse, impact-expression, results, and 1024 px app-icon art.
- Impact-only slow motion, flying crown, dust/stars, procedural wind and feedback audio, and iOS haptic hooks.
- Unity batch compile, `7/7` EditMode tests, `6/6` PlayMode tests, Mac smoke build, and four offscreen portrait captures.
- Unity exports the Xcode project, and Xcode compiles its Debug configuration for generic `iphoneos` as an unsigned arm64 app bundle.

## Next verified outcome

Sign and install a Development build on a physical iPhone, then pass the device and fresh-player gates. The unsigned compile proves the build chain, not device behavior or App Store readiness.

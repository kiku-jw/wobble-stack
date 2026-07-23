# iPhone test plan

## Automated foundation

- Batch project import and C# compile with Unity `6000.3.19f1`.
- EditMode tests for gust sampling, envelope bounds, counter-force sign and ordering, setup clamping, and state transitions.
- Scene smoke verifies required root objects, camera, portrait configuration, safe-area canvas, five creature definitions, and start overlay.
- Current receipt: `7/7` EditMode and `6/6` PlayMode tests pass on Unity `6000.3.19f1`.

## Deterministic physics matrix

For the same seed and Normal gust:

1. Neutral beam establishes the baseline.
2. Correct counter-angle survives the entire first gust and materially outlasts neutral.
3. Wrong angle fails sooner than neutral.
4. Repeat with the opposite gust direction.
5. Repeat with three and five creatures.

## Interaction matrix

- Touch drag maps the full safe beam range without moving the page or camera.
- Release returns the beam toward neutral.
- Pause freezes time, physics, particles, and score.
- App focus loss pauses safely.
- Retry rebuilds bodies, faces, crown, particles, wind seed, and score without loading a new scene.

## Presentation matrix

- Capture start, calm, building gust, near-save, airborne collapse, first impact, and results at iPhone portrait aspect.
- Compare each capture against `ART_DIRECTION.md`.
- Verify reduced motion keeps semantic feedback without camera shake or prolonged slow motion.
- Verify UI inside representative notch and Dynamic Island safe areas.

Current automated captures cover start, active counter-tilt, airborne collapse with separate crown and impact faces, and results. Near-save timing and actual notch rendering remain device checks.

## Device gate

Toolchain foundation complete: full Xcode is selected, the iOS platform runtime is available, Unity exports the Xcode project, and its unsigned Debug `iphoneos` configuration compiles successfully.

The remaining gate requires a connected physical iPhone and code signing:

- Development build on a physical iPhone.
- Stable 60 fps target on the selected baseline device.
- Touch latency, haptics, audio interruption, thermal behavior, safe areas, and background/foreground transitions.
- Fresh-player comprehension and voluntary Retry.

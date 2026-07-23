# iPhone test plan

## Automated foundation

- Batch project import and C# compile with Unity `6000.3.19f1`.
- EditMode tests for gust sampling, warning/preview timing, force direction,
  control coverage, setup clamping, and state transitions.
- PlayMode tests cover scene bootstrap, count controls, beam/collider alignment,
  compact contacts, expression progression, cyan wind direction, full first
  gusts, input ordering, and interruption-safe failure.
- Current receipt: `10/10` EditMode and `9/9` PlayMode tests pass on Unity
  `6000.3.19f1`.

## Deterministic physics matrix

For the same seed and Normal gust:

1. Neutral hold establishes the baseline.
2. Correct source-side hold survives the entire maximum first gust.
3. Correct hold produces less maximum drift and survives at least as long as
   neutral and wrong-side hold on the same Normal gust.
4. Repeat with the opposite gust direction.
5. Repeat with three and five creatures.

## Interaction matrix

- Holding either screen half selects that side outside a small center dead zone.
- Direction text names the wind source and the side to hold.
- Input before physical force is safe; the beam stays neutral during preview.
- Release returns the beam toward neutral.
- Pause freezes time, physics, particles, and score.
- App focus loss pauses safely.
- Retry rebuilds bodies, faces, crown, particles, wind seed, and score without loading a new scene.

## Presentation matrix

- Capture start, calm, building gust, near-save, airborne collapse, first impact, and results at iPhone portrait aspect.
- Compare each capture against `ART_DIRECTION.md`.
- Verify reduced motion keeps semantic feedback without camera shake or prolonged slow motion.
- Verify UI inside representative notch and Dynamic Island safe areas.

Current Metal captures cover calm start, active gust with source-side prompt and
cyan streaks, airborne/impact collapse with distinct faces and crown, and
results. Near-save timing and actual notch rendering remain device checks.

## Device gate

Toolchain and delivery proof are complete: Unity exports a non-Development Xcode
project; Xcode produces a valid signed arm64 app; CoreDevice confirms install
and launch on the paired iPhone.

The remaining owner gate is a second physical playtest:

- Correct source-side hold survives the first gust on Gentle, Normal, and Wild.
- The source-side instruction is understood without explanation.
- Characters sit close, trust the beam ends, and change expressions clearly.
- Cyan wind direction is visible before force; no development console appears.
- Stable 60 fps target on the selected baseline device.
- Touch latency, haptics, audio interruption, thermal behavior, safe areas, and background/foreground transitions.
- Fresh-player comprehension and voluntary Retry.

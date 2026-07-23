# iPhone test plan

## Automated foundation

- Batch project import and C# compile with Unity `6000.3.19f1`.
- EditMode tests for gust sampling, warning/preview timing, force direction,
  control coverage, setup clamping, and state transitions.
- PlayMode tests cover scene bootstrap, count controls, beam/collider alignment,
  compact contacts, expression progression, cyan wind direction, direct
  pre-gust beam motion, absence of hidden joints/locks, stable free warmup,
  full first gusts, bounded Wild collapse, input ordering, and
  interruption-safe failure.
- Current receipt: `10/10` EditMode and `12/12` PlayMode tests pass on Unity
  `6000.3.19f1`.

## Deterministic physics matrix

For the same seed and Normal gust:

1. Neutral angle establishes the baseline.
2. One constant hold at 22% or 78% of the screen width survives the entire
   maximum first gust; the test does not feed a changing angle.
3. Correct hold produces less downwind displacement and survives at least as
   long as neutral and wrong input on the same Normal gust.
4. Repeat with the opposite gust direction.
5. Repeat with three and five creatures.
6. On the maximum Wild gust, neutral and wrong input both collapse while
   correct input completes the gust.

## Interaction matrix

- Horizontal touch position maps continuously to signed authority outside a
  small center dead zone.
- The useful response saturates inside the outer quarter; the bezel is not a
  required target.
- The touched end rises, and the beam responds during the pre-gust teaching
  window.
- Direction text names which end to raise.
- Release returns the beam toward neutral.
- Pause freezes time, physics, particles, and score.
- App focus loss pauses safely.
- Retry rebuilds bodies, faces, crown, particles, wind seed, and score without loading a new scene.

## Presentation matrix

- Capture start, calm, building gust, near-save, airborne collapse, first impact, and results at iPhone portrait aspect.
- Compare each capture against `ART_DIRECTION.md`.
- Verify reduced motion keeps semantic feedback without camera shake or prolonged slow motion.
- Verify UI inside representative notch and Dynamic Island safe areas.

Current Metal captures cover calm start, active direct tilt with a single
direction prompt and cyan streaks, airborne/impact collapse with distinct faces
and crown, and results. Near-save timing and actual notch rendering remain
device checks.

## Device gate

Toolchain and delivery proof are complete: Unity exports a non-Development Xcode
project; Xcode produces a valid signed arm64 app; CoreDevice confirms install
and launch on the paired iPhone.

The remaining owner gate is another physical playtest:

- Dragging and holding the indicated outer side visibly raises that end before
  wind begins.
- One constant correct hold survives the first gust on Gentle, Normal, and
  Wild without chasing a changing angle.
- The touched-end instruction is understood without explanation.
- Neutral/wrong play still produces a believable collapse.
- Characters sit close, trust the beam ends, and change expressions clearly.
- Cyan wind direction is visible before force; no development console appears.
- Stable 60 fps target on the selected baseline device.
- Touch latency, haptics, audio interruption, thermal behavior, safe areas, and background/foreground transitions.
- Fresh-player comprehension and voluntary Retry.

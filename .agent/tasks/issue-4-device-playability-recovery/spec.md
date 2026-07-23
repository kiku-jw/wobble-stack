# Issue 4 — physical playability recovery

Status: Frozen with one bounded amendment
Frozen: 2026-07-23
Canonical issue: `kiku-jw/wobble-stack#4`

Amended after red/green calibration: a correct-tilt-only downwind velocity
damper is allowed. It may read horizontal velocity only to oppose motion in
the current wind direction; it may not choose the side, pull against recovery,
or stabilize neutral/wrong input.

## Rejected build

The fourth physical iPhone check rejected `989dee5`. Its constant-hold tests
started with a favorable input already active before force. On-device, the
owner still could not survive one gust normally with the beam.

## Acceptance criteria

### AC1 — Tilt changes the danger, not just the picture

- Correct beam tilt must immediately reduce the horizontal acceleration
  applied to every creature.
- Wrong tilt must increase it.
- The relationship must be deterministic and unit-tested independently from
  tower collision luck.

### AC2 — A late, imprecise human correction works

- The tower starts neutral.
- Physical wind is allowed to act for at least `0.35 s`.
- A constant same-direction touch no farther than `68%`/`32%` screen width is
  then applied.
- That correction completes the maximum first gust for Gentle, Normal, and
  Wild, both directions, with three and five creatures.
- The proof must not pre-hold, feed an exact angle, or change input every frame.

### AC3 — Failure remains real

- Neutral and wrong-direction input still collapse under maximum Wild wind.
- Correct input must outlast both on the same gust.
- No joints, grips, tethers, rotation locks, positional auto-balancing, or
  automatic side choice may be reintroduced.
- The bounded velocity damper must be zero for neutral tilt, wrong tilt,
  upwind recovery, and calm air.

### AC4 — Input remains legible

- Touching left raises the left end; touching right raises the right end.
- Release returns toward neutral.
- The cyan preview and end-to-raise hint remain.

### AC5 — Delivery is exact

- EditMode and PlayMode pass on Unity `6000.3.19f1`.
- The current source revision receives a non-Development iOS export, signed
  arm64 build, strict signature verification, install, and launch.
- Physical feel and voluntary Retry remain an owner gate.

## Constraints

- Keep the public web prototype unchanged.
- Add no package, service, analytics, progression, or meta system.
- Preserve current art, colliders, expressions, impact beat, and UI.
- Make the smallest local change that gives the beam real counter-authority.

## Verification plan

1. Add a domain assertion proving correct/wrong tilt changes effective wind.
2. Add a PlayMode probe that starts neutral and applies one imprecise input
   after `0.35 s` of active wind.
3. Confirm the new test fails on `989dee5`.
4. Implement the smallest input-gated counter-authority in the existing wind
   calculation without allowing it to reverse a recovering body.
5. Re-run full suites, Mac smoke/capture, iOS export/build/install/launch.
6. Perform a fresh adversarial review and hand the launched build to the owner.

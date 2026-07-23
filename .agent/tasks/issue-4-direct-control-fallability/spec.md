# Issue 4 — direct control and fallability recovery

Status: Frozen
Frozen: 2026-07-23
Canonical issue: `kiku-jw/wobble-stack#4`

Amended: 2026-07-23 after the third device check. AC1 permits
gust-strength-calibrated authority without tower feedback, and AC3 requires a
constant outer-side human input rather than an automation-only changing angle.

## Rejected build

The second physical iPhone playtest rejected `3130df5`: binary source-side
input made control less understandable, the beam did not visibly follow the
thumb, and hidden support constraints made the creatures effectively unable to
fall.

## Acceptance criteria

### AC1 — The thumb directly owns the beam

- Horizontal touch position maps continuously and monotonically to signed
  control amount for a fixed gust.
- Touching toward an end visibly raises that same end.
- The beam responds during the pre-gust rest, so the player can learn the
  control before danger.
- Known gust strength may calibrate the useful angle range, but there is no
  tower-state feedback controller or automatic choice of side.

### AC2 — The tower is a physical stack

- Dynamic creatures have free rotation.
- No joint, grip, tether, or other hidden constraint connects creatures to the
  beam or to one another.
- Collider silhouettes remain compact and stable enough to reach the first
  gust from a centered start.

### AC3 — Correct play helps, but failure remains real

- Dragging to the correct outer side and holding there completes the maximum
  first gust for Gentle, Normal, and Wild with three and five creatures in both
  directions. The proof may not feed an exact per-frame counter-angle.
- On a fixed strong stress gust, neutral and wrong-way input fail in bounded
  time; the automated contract may not allow every input to remain immortal.
- Correct input outlasts neutral and wrong-way input on the same stress gust.

### AC4 — The control is explained by visible effect

- Before the first gust, the hint tells the player to drag and states that the
  touched end rises.
- During preview, the hint names which end to raise.
- The cyan directional wind preview remains visible before physical force.

### AC5 — Prior presentation fixes remain intact

- Rounded beam collision still matches the visible beam.
- Calm, wind/panic, and impact/dazed faces remain distinct.
- The device build remains non-Development.

### AC6 — Verification remains honest

- EditMode, PlayMode, Metal portrait capture, iOS export, signed build, install,
  and launch pass.
- Physical feel and voluntary Retry remain an owner gate.

## Constraints

- Unity remains `6000.3.19f1`.
- Keep the web prototype unchanged.
- Add no runtime dependency, service, analytics, progression, or meta system.
- Keep the current art and the first playtest's beam/wind/face improvements.

## Verification plan

1. Replace binary input and the feedback controller with continuous signed
   touch control and force-calibrated authority.
2. Remove all stack joints and rotation locks.
3. Add tests for direct mapping, no hidden constraints, stable warmup,
   survivable correct first gusts, and bounded neutral/wrong failure.
4. Tune only friction, damping, angle, and gust forces needed to satisfy the
   matrix.
5. Rebuild captures and deliver another non-Development signed iPhone build.

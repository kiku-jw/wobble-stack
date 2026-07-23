# Evidence

Collected: 2026-07-23

| Criterion | Status | Proof |
| --- | --- | --- |
| AC1 | PASS | The red domain test measured the full `0.00009` acceleration under correct tilt. Final domain tests verify correct tilt reduces acceleration, wrong tilt increases it, and the bounded damper acts only on current downwind motion. |
| AC2 | PASS | `DelayedImpreciseCorrectionSurvivesFirstGustMatrix` starts neutral, permits `0.35 s` of physical wind, then applies one unchanged 32%/68% touch. It completes maximum Gentle/Normal/Wild gusts in both directions with three and five creatures. |
| AC3 | PASS | Maximum Wild neutral and wrong input still collapse, while correct input survives and outlasts both. The runtime remains jointless with free creature rotation and no positional auto-balancing. |
| AC4 | PASS | Existing PlayMode coverage confirms the touched end rises before force and release returns control to neutral. The inspected Metal capture retains the cyan wind, one direction prompt, and visible matching beam tilt. |
| AC5 | PASS WITH OWNER GATE | Final suites pass `12/12` EditMode and `13/13` PlayMode. Web `11/11`, production build, Mac smoke/capture, non-Development iOS export, signed arm64 Xcode build, strict signature verification, paired-device install, and launch pass. Physical feel remains unclaimed. |

## Final receipts

- Unity: `6000.3.19f1`; EditMode `12/12`; PlayMode `13/13`.
- Human-input coverage: early strong 22%/78% hold and delayed moderate 32%/68%
  hold both complete the maximum first-gust matrix.
- Fallability: maximum Wild neutral and wrong input fail; correct input
  completes and outlasts both.
- Public prototype: `11/11` tests and production build pass unchanged.
- Mac: player build succeeds; inspected Metal capture is `1179 × 2556`.
- iPhone: non-Development export, signed arm64 app, strict signature check,
  install, and active launch all succeed.

## Residual owner gate

Automation and delivery now cover ordinary timing and thumb placement. The
owner must still judge whether the launched build actually feels controllable,
clear, and worth another Retry on the physical iPhone.

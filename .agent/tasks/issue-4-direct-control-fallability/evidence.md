# Evidence

Collected: 2026-07-23
Unity: `6000.3.19f1`
Owner result: **REJECTED**. The constant-hold automation passed, but the fourth
physical iPhone check still could not survive a gust normally with the beam.
This is historical automation evidence for `989dee5`, not current playability
proof.

| Criterion | Status | Proof |
| --- | --- | --- |
| AC1 | PASS | EditMode verifies continuous symmetric signed touch control. PlayMode `HumanSideHoldMovesTheBeamBeforeWindStarts` verifies that a hold at 82% screen width visibly raises the right beam end before force begins. Product code contains no tower-feedback controller. |
| AC2 | PASS | PlayMode `DynamicTowerHasNoHiddenConstraintsAndReachesTheFirstGust` verifies five free-rotation bodies, zero `Joint2D` components, and a stable 3.5-second no-wind window. |
| AC3 | PASS | The full matrix holds one constant screen position at 22%/78% width and completes maximum first gusts for Gentle/Normal/Wild, both directions, and three/five creatures. Correct input reduces downwind displacement on Normal. `NeutralAndWrongInputCollapseUnderTheStrongestWildGust` verifies bounded real failure while the same constant correct hold completes. |
| AC4 | PASS | Runtime hint says the touched end rises, then names the end to raise. The Metal gameplay capture shows a visibly tilted beam, a single direction prompt, and cyan wind. |
| AC5 | PASS | Rounded beam/compact contacts and calm/wind/impact expressions remain covered by PlayMode tests and four inspected portrait captures. The iOS export remains `BuildOptions.None`. |
| AC6 | REJECTED BY OWNER | Final suites passed `10/10` EditMode and `12/12` PlayMode and delivery succeeded, but the physical playability gate failed. |

## Final receipts

- EditMode: `10` passed, `0` failed.
- PlayMode: `12` passed, `0` failed; the first-gust proof never changes control
  input after setup.
- Mac smoke: build succeeded; four Metal portrait states were inspected.
- iOS: non-Development export, signed arm64 build, strict signature check,
  paired-device install, and launch succeeded.

## Owner verdict

The device gate failed. The replacement task must prove delayed, imprecise
human correction rather than another favorable pre-held input.

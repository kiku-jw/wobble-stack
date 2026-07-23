# Evidence

Collected: 2026-07-23
Unity: `6000.3.19f1`
Owner result: **REJECTED**. The second physical playtest found the binary
control less understandable and the constrained stack effectively unable to
fall. This receipt remains the automation record for `3130df5`, not the current
product verdict.

| Criterion | Status | Proof |
| --- | --- | --- |
| AC1 | PASS | PlayMode `CorrectCounterTiltSurvivesWorstFirstGustAcrossDifficultiesAndTowerSizes` completes the maximum first gust for Gentle/Normal/Wild, both directions, and three/five creatures. `CorrectCounterTiltOutperformsNeutralAndWrongTiltOnTheSameGust` verifies lower drift and non-shorter survival. |
| AC2 | PASS | EditMode verifies both screen halves and the center dead zone. Runtime source-side control, first-run instruction, and left/right direction prompts use one contract. The gameplay capture shows `WIND FROM RIGHT · HOLD RIGHT SIDE`. |
| AC3 | PASS | EditMode verifies the preview begins before physical force. PlayMode verifies cool-blue streak color and both travel directions. The gameplay capture shows the cyan preview against the warm background. |
| AC4 | PASS | PlayMode verifies the horizontal capsule covers at least 94% of the rendered beam width and checks compact adjacent collider contacts. Start/gameplay captures show the compact stack and rounded beam ends. |
| AC5 | PASS | PlayMode verifies three different sprite states: calm, wind, and impact. Start, gameplay, collapse, and results captures were inspected at `1179 × 2556`. |
| AC6 | PASS | iOS export uses `BuildOptions.None`; Xcode reports `BUILD SUCCEEDED`; `codesign --verify --deep --strict` passes; CoreDevice confirms current app install and a device launch receipt. |
| AC7 | REJECTED BY OWNER | Final suites passed `10/10` EditMode and `9/9` PlayMode, and delivery succeeded. The physical iPhone test then rejected control clarity and fallability, so this build did not pass the product gate. |

## Final receipts

- EditMode: `10` passed, `0` failed.
- PlayMode: `9` passed, `0` failed.
- Mac smoke: built and rendered with Metal.
- Captures: `docs/ios/screenshots/start.jpg`, `gameplay.jpg`,
  `collapse.jpg`, and `results.jpg`.
- iOS: non-Development export, valid signed arm64 app, installed on the paired
  iPhone, launch confirmed.

## Owner verdict

The device gate failed. Binary source-side control and hidden support
constraints were removed in the replacement task; this historical receipt must
not be used as current feel evidence.

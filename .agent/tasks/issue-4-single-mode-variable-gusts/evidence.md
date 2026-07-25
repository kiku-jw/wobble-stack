# Evidence

| Criterion | Result | Evidence |
| --- | --- | --- |
| AC1 | PASS | Native and web difficulty controls/state were deleted. The inspected native start capture contains Play, Friends, and Motion only. Source search finds no difficulty runtime symbols. |
| AC2 | PASS | One `0.000055–0.000135` range uses a squared continuous random sample. Domain/web tests prove exact bounds, Normal-biased midpoint, deterministic schedules, and bounded timing. |
| AC3 | PASS | Web meter markup/CSS/runtime code is deleted. Native preview and active streak intensity, audio, and faces normalize from sampled force; PlayMode proves stronger intensity produces more, more opaque streaks. |
| AC4 | PASS | Unity strongest-gust delayed and early-hold matrices pass for both directions and 3/5 bodies. Neutral and wrong input still collapse. Jointless/free-rotation assertions remain green. |
| AC5 | PASS | Both runtimes keep the former Normal best-score slots. Web tests/build, Unity tests, Mac capture, iOS export, signed arm64 build, device install, and device launch pass. Physical feel remains with the owner. |

## Verification summary

- Web: `10/10` tests and Vite production build pass.
- Unity EditMode: `12/12` pass.
- Unity PlayMode: `13/13` pass.
- Mac smoke: build and inspected portrait start capture pass.
- iOS: non-Development export, Xcode build, strict code-sign check, arm64
  executable, paired-device install, and launch pass.

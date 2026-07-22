# Wobble Stack Unity client

- Unity version: `6000.3.19f1`.
- Product code lives under `Assets/WobbleStack`; keep generated Unity state out of Git.
- Keep deterministic rules in `Domain` and scene/runtime behavior in `Runtime`.
- Editor automation and build configuration live in `Editor`; tests live in `Tests`.
- Do not add SDKs, services, accounts, ads, analytics, multiplayer, or generalized frameworks without an explicit product decision.
- Do not claim iOS export, signing, TestFlight, or device QA until iOS Build Support and full Xcode are installed and verified.
- Validate meaningful changes with batch compile, EditMode tests, and an iPhone-aspect capture.

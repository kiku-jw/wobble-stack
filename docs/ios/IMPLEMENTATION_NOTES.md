# iPhone implementation notes

## 2026-07-22 — production lane

- Keep the web prototype unchanged except for shared product documentation and links.
- Build the production client as layered Unity 2D. Realtime 3D would make lighting attractive but multiply rigging, modeling, performance, and asset-pipeline cost before device validation.
- Reuse the proven gameplay semantics, not raw Matter.js constants. Unity tuning is centralized in deterministic domain profiles and a small set of runtime physics constants; no speculative tuning framework was added.
- First visual slice uses generated user-owned sprites derived from the concept-frame direction plus procedural wind and impact FX.
- No new SDKs or runtime services in M0/M1.
- Generated regular/impact atlases let every character change expression on contact while the crown becomes an independent physics gag.
- A tiny local audio synthesizer supplies wind, start, save, click, and impact feedback without third-party packs.
- Portrait QA uses a deterministic `Camera.Render` + `RenderTexture` capture path, so screenshots do not depend on a visible macOS player window.
- The 1024 px generated app icon is assigned to current Standalone and iPhone icon slots by the project bootstrap.

## Tooling constraint

- Active developer directory is `/Applications/Xcode.app/Contents/Developer`; Xcode `26.6` is licensed and its first-launch setup is complete.
- Unity `6000.3.19f1` has `PlaybackEngines/iOSSupport`; `BuildIosDevelopment` exports the Xcode project.
- The iOS `26.5` platform runtime is installed and available. Xcode compiles the exported Debug configuration for generic `iphoneos` with code signing disabled.
- The verified output is an unsigned arm64 app bundle with bundle identifier `dev.kikuai.wobblestack` and minimum iOS version `15.0`.
- Do not claim a signed IPA until Apple development credentials and signing complete.
- Do not claim a TestFlight build until Apple Developer Program and App Store Connect access complete that distribution gate.
- Do not claim a physical-device pass until the Development build runs on a connected iPhone.

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

## 2026-07-23 — first device-test recovery

- Replaced precision drag with a binary source-side hold: when the prompt says
  wind comes from the left, hold the left half of the screen. The beam recenters
  on release and cannot move before the gust becomes active.
- Added a 1.3-second cyan preview and an explicit direction prompt before
  physical force begins. Streak speed, density, opacity, face tension, and audio
  build into the gust.
- Removed the duplicate synthetic counter-force. A bounded feedback controller
  now tilts the physical beam using tower rotation, velocity, displacement, and
  gust feed-forward.
- Replaced the undersized box beam with a world-aligned horizontal capsule.
  Creature support now uses measured colliders, a flat-bottom base silhouette,
  a bounded beam grip, and contact-point hinges. Constraints release on failure.
- Added a calm-face atlas; the existing regular atlas is now the wind/panic
  state and the impact atlas remains the dazed state.
- The first gust has a longer warning and calibrated force ranges. The maximum
  first gust is covered by supported-tower tests for every difficulty,
  direction, and three/five-creature setup.
- The iOS menu export is non-Development, removing Unity's in-game development
  console and watermark from device playtests.

## Tooling constraint

- Active developer directory is `/Applications/Xcode.app/Contents/Developer`; Xcode `26.6` is licensed and its first-launch setup is complete.
- Unity `6000.3.19f1` has `PlaybackEngines/iOSSupport`;
  `BuildIosDevelopment` remains the batch-compatible method name while the
  editor menu reads `Build iOS Device`.
- The iOS `26.5` platform runtime is installed and available. Xcode compiles
  the exported project as a signed arm64 app with bundle identifier
  `dev.kikuai.wobblestack` and minimum iOS version `15.0`.
- CoreDevice has confirmed install and launch on the paired development iPhone.
- Do not claim an archive or distributable IPA until that separate build is
  produced and verified.
- Do not claim a TestFlight build until Apple Developer Program and App Store Connect access complete that distribution gate.
- Do not claim the physical-feel gate until the owner completes the second
  playtest.

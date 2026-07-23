# iPhone architecture

## Decision

Use Unity `6000.3.19f1` as a nested production client at `ios/WobbleStack`. The existing Vite/Matter.js build remains an independent public prototype.

## Why Unity

- Unity 6 is already installed and is the shortest path to 2D physics, particles, animation, audio, haptics bridges, and iOS export.
- The game needs art iteration and physical feel more than platform-native navigation.
- One engine scene can later support capture and limited non-iOS QA without changing the iPhone-first product boundary.

## Runtime boundaries

- `Domain`: deterministic gust schedule, envelope, counter-force math, score keys, and game-state transitions. No Unity scene dependencies where practical.
- `Gameplay`: beam control, Rigidbody2D stack, wind force, collapse, impact receipts, and reset.
- `Presentation`: regular/impact character sprites, crown, wind streaks, dust, camera shake, synthesized audio, UI, and accessibility motion scale.
- `Platform`: local persistence, safe area, haptics, audio interruption, and iOS build settings.

## Scene contract

- Reference canvas: `1179 × 2556`, portrait.
- World camera: orthographic, one fixed gameplay composition.
- Physics timestep: `1/60` with interpolation on visible bodies.
- Beam: kinematic Rigidbody2D following a bounded target angle.
- Creatures: dynamic Rigidbody2D bodies with unique silhouette colliders and restrained neighbor springs.
- Wind: deterministic sampled gust plus a smooth attack/hold/release envelope.
- Counter-tilt: beam angle contributes a mass-proportional horizontal acceleration to the whole stack during a gust. It remains partial, so overcorrection can reverse drift.

## Art pipeline

- Layered 2D sprites, not realtime 3D, for the first candidate.
- One soft layered background, beam/fulcrum, regular and impact character atlases, crown, particles, and toy UI for the first slice.
- Generated assets inherit from user-owned concept frames and are stored with a generation manifest.
- Runtime uses one shared soft-light material only if the default sprite material cannot preserve the target look.

## Build boundary

Unity iOS Build Support is installed for `6000.3.19f1`, Xcode `26.6` is configured, and the exported project compiles as an unsigned Debug arm64 `iphoneos` app. Physical-device proof still requires a connected iPhone and a signing team; TestFlight requires Apple Developer Program and App Store Connect access.

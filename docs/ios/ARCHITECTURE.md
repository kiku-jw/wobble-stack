# iPhone architecture

## Decision

Use Unity `6000.3.19f1` as a nested production client at `ios/WobbleStack`. The existing Vite/Matter.js build remains an independent public prototype.

## Why Unity

- Unity 6 is already installed and is the shortest path to 2D physics, particles, animation, audio, haptics bridges, and iOS export.
- The game needs art iteration and physical feel more than platform-native navigation.
- One engine scene can later support capture and limited non-iOS QA without changing the iPhone-first product boundary.

## Runtime boundaries

- `Domain`: deterministic gust schedule, preview/envelope math, control bounds,
  score keys, and game-state transitions. No Unity scene dependencies where
  practical.
- `Gameplay`: signed touch control with force-calibrated beam authority,
  unconstrained Rigidbody2D stack, wind force, collapse, impact receipts, and
  reset.
- `Presentation`: calm/wind/impact character sprites, crown, cyan wind streaks,
  dust, camera shake, synthesized audio, UI, and accessibility motion scale.
- `Platform`: local persistence, safe area, haptics, audio interruption, and iOS build settings.

## Scene contract

- Reference canvas: `1179 × 2556`, portrait.
- World camera: orthographic, one fixed gameplay composition.
- Physics timestep: `1/60` with interpolation on visible bodies.
- Beam: kinematic Rigidbody2D with a rounded horizontal capsule matching the
  visible beam and following a bounded target angle.
- Creatures: dynamic Rigidbody2D bodies with measured flat-contact silhouette
  colliders, free rotation, and no joint, tether, beam grip, or hidden
  stabilization. Small initial separation lets the solver settle without
  explosive overlap.
- Wind: deterministic sampled gust plus a smooth attack/hold/release envelope
  and a 1.3-second visual preview before force.
- Counter-tilt: normalized touch position maps continuously to signed control
  amount, including before the gust. Touching toward an end raises that same
  end. The sampled gust strength calibrates the useful angle range, so a
  moderate same-side hold can survive without pixel-perfect thumb placement.
  Beam angle directly changes effective gust acceleration for every creature.
  A bounded correct-tilt damper opposes only current downwind velocity; it is
  zero for calm air, neutral/wrong tilt, and a body already recovering upwind.
  The player still owns side and amount, and the runtime never chooses a side
  or position-corrects the tower.
- Fallability: contact friction is low enough for strong gusts to matter, while
  higher creatures receive more wind exposure and a small off-center torque.
  Neutral and wrong input receive no damping and still collapse under maximum
  Wild gusts.

## Art pipeline

- Layered 2D sprites, not realtime 3D, for the first candidate.
- One soft layered background, beam/fulcrum, regular and impact character atlases, crown, particles, and toy UI for the first slice.
- Generated assets inherit from user-owned concept frames and are stored with a generation manifest.
- Runtime uses one shared soft-light material only if the default sprite material cannot preserve the target look.

## Build boundary

Unity iOS Build Support is installed for `6000.3.19f1`, Xcode `26.6` is
configured, and the non-Development export compiles as a signed arm64
`iphoneos` app. Install and launch are verified on the paired development
iPhone. Subjective physical feel remains an owner test; TestFlight requires
Apple Developer Program and App Store Connect access.

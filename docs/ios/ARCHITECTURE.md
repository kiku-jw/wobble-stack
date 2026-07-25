# iPhone architecture

## Decision

Use Unity `6000.3.19f1` as the production client at `ios/WobbleStack`. The
existing Vite/Matter.js build remains an independent public prototype; its
stationary beam is no longer authoritative for native gameplay.

## Runtime boundaries

- `Domain`: deterministic gust schedule, preview/envelope math, input bounds,
  route/badge rules, and game-state transitions. No Unity scene dependencies
  where practical.
- `Gameplay`: relative touch drive, dynamic wheel/plank vehicle, free creature
  bodies, wind force, route triggers, breakable visible grips, collapse, impact
  receipts, and reset.
- `Presentation`: articulated creature rigs, personality reactions, parallax
  layers, clouds, windmill, cyan wind, terrain, dust, camera impulse,
  synthesized audio, minimal UI, and accessibility motion scale.
- `Platform`: local persistence, safe area, haptics, audio interruption, and
  iOS build settings.

## Rolling vehicle contract

- Reference canvas: `1179 × 2556`, portrait.
- Physics timestep: `1/60` with interpolation on visible bodies.
- Road: one continuous visible clay surface backed by a flat `BoxCollider2D`.
  The generated terrain sprite tiles independently of the collision shape.
- Wheel: a dynamic circular `Rigidbody2D` with measured visual/collider
  alignment, high road friction, and enough physical mass to stay grounded
  without a positional constraint.
- Plank: a dynamic `Rigidbody2D` with a rounded capsule collider. A single
  native `WheelJoint2D` supplies the legitimate one-wheel connection and
  suspension; code never assigns a target plank angle.
- Drive: thumb displacement relative to touch-down maps to signed wheel motor
  speed. Mid-range input has a broad catch band; the outer range adds a short
  rescue-speed boost. Returning near the origin while touching applies bounded
  motor braking; releasing the finger disables the motor and lets the vehicle
  coast physically.
- Camera: horizontal smoothing, dead-zone-like lag, and velocity look-ahead
  follow the wheel. Camera impulse is layered on top and disabled by Reduced
  Motion.
- Wind: one deterministic continuous force range, biased toward ordinary
  gusts, with smooth attack/hold/release and a `1.3 s` visual preview. Wind
  applies to creatures, not through a hidden counter-force or plank-angle
  damper.
- Fallability: higher creatures receive more exposure and a small off-center
  torque. Neutral and wrong input collapse under maximum wind. Failure is based
  on physical loss of height/order or road impact, not crossing an invisible
  horizontal line.

## Character rig target

- Each character keeps one dynamic torso collider for robust stack physics.
- Visible feet, arms, ears, leaves, wings, crest, jelly skirt, and crown are
  separate sprite parts driven by damped secondary motion.
- Hands use two-bone visual reach toward nearby authored grip anchors. A
  contacted grip may create one weak breakable joint whose visible hands show
  the connection; no permanent tether, rotation lock, or positional
  auto-balance is allowed.
- Personality state combines calm, anticipation, effort, panic, relief,
  impact, and dazed behavior with irregular blink and gaze timing.

## World and progression target

- Camera-relative background, slower distant layers, world-space midground,
  physical road, and faster foreground form the parallax stack.
- Clouds translate on independent deterministic loops. Windmill blades rotate
  as separate sprites; no video runtime is needed.
- First route uses three bodies, then authored join stops rebuild the safe
  stationary stack with Rabbit and Jelly. Five is the final active roster.
- Badge triggers are world-space collectibles and record the best count per
  route. No server or economy is introduced.

## Art pipeline

- Layered 2D sprites, not realtime 3D.
- User-owned concept frames define identity, material, lighting, and
  composition.
- Generated source assets are committed with a receipt in
  `Assets/WobbleStack/Art/Generated/ASSET_MANIFEST.md`.
- Opaque cutouts may use the existing chroma material. Alpha terrain and later
  separated environment layers use normal sprite rendering.

## Build boundary

Unity iOS Build Support is installed for `6000.3.19f1`, Xcode `26.6` is
configured, and a non-Development export can compile as a signed arm64
`iphoneos` app. Install and launch have been proven on the paired development
iPhone. Every materially changed control loop requires another device feel
gate; TestFlight remains a separate Apple Developer Program/App Store Connect
boundary.

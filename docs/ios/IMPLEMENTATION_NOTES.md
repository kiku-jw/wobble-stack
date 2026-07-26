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

## 2026-07-23 — second device-test recovery

- The first recovery was rejected on-device: binary source-side input obscured
  the beam/thumb relationship, and the grip plus contact hinges made collapse
  effectively impossible.
- Removed the tower-feedback controller, base grip, neighbor hinges, and every
  rotation lock. Dynamic creatures are now connected only by collisions.
- Restored continuous direct manipulation with a larger visible range: touch
  position maps to beam angle before, during, and after a gust; the touched end
  rises.
- Replaced rounded-bottom creature capsules with measured flat-contact
  polygons. A tiny initial separation keeps the free tower stable through the
  teaching window without adding hidden constraints.
- Reduced contact friction and applied progressively stronger, slightly
  off-center wind forces higher in the tower. Neutral and wrong input now
  collapse under the maximum Wild gust.
- Updated the first-run hint to explain the physical effect, then name the end
  to raise during the cyan wind preview.

## 2026-07-23 — third device-test recovery

- The direct-angle build was still rejected on-device: the automated proof fed
  an exact changing angle that a thumb could not reproduce, so the first gust
  still blew the tower away during real play.
- Touch now owns a signed control amount. Known gust strength and phase
  calibrate only the available beam authority; there is no tower-state
  feedback and no automatic choice of side.
- The response saturates inside the outer quarter of the screen and keeps a
  small visible teaching angle, so the player does not need to pin a finger to
  the bezel or guess whether the beam moved.
- The physics matrix now holds one constant human-like screen position for the
  full gust. That input completes every maximum first-gust case, while neutral
  and wrong input still collapse under maximum Wild wind.

## 2026-07-23 — fourth device-test recovery

- The constant pre-held proof was rejected on-device: it did not cover a normal
  delayed reaction or imprecise thumb placement.
- Beam angle now directly changes effective gust acceleration for every
  creature instead of relying on the bottom contact to transmit all authority.
- Scalar counter-force was unstable across real input strength: too little
  still lost the Wild/five case downwind, while too much could reverse a strong
  hold. The final helper only damps velocity that is currently moving with the
  wind, only while the player has the correct tilt.
- The helper becomes zero for calm air, neutral/wrong tilt, or a creature
  already moving back upwind. It cannot pull the tower through recovery, choose
  the correct side, or make neutral play immortal.
- The new human proof starts neutral, lets wind act for `0.35 s`, then holds one
  unchanged touch at 32%/68% width. It completes the maximum first-gust matrix
  alongside the earlier strong 22%/78% hold proof.

## 2026-07-25 — single-mode wind

- The owner accepted the delayed-correction build: the first gust is now
  survivable through normal beam input.
- Deleted player-selectable difficulty and retained one continuous
  `0.000055–0.000120` gust range. A squared uniform sample keeps most gusts near
  the accepted Normal feel while allowing occasional soft and strong outliers.
- Preview and active cyan streaks scale with sampled force; the game adds no
  intensity meter, tier name, number, or badge.
- Creature count and reduced motion remain setup options. Existing Normal score
  slots remain the persistence key so local records are not lost.

## 2026-07-25 — rolling-world core replacement

- The stationary direct-angle mechanic was superseded by the owner-approved
  travelling design. Native gameplay now drives a physical star wheel on a
  continuous road; the plank remains dynamic and connects through one native
  `WheelJoint2D`.
- Touch uses displacement from its own touch-down origin. A square-root
  mid-range gives a forgiving steady catch band, while the outer portion adds
  bounded rescue speed for delayed reactions. No tower state selects direction
  or position.
- Removed the old plank-angle gust cancellation and downwind velocity damper.
  Wind now acts directly on the bodies, while wheel travel catches their
  displacement through contact physics.
- A heavier wheel and road friction keep the physical collider grounded
  without a tether or position snap. The generated wheel sprite's measured
  opaque fill is separated from the physics root so the visible circle also
  reaches the road.
- Failure no longer triggers at an invisible absolute horizontal boundary.
  Height/order loss and road impact own failure, which also preserves a longer
  visible fall.
- Camera x follows the wheel with smoothing and velocity look-ahead; the baked
  background and wind field follow until full parallax layers replace them.
- Built-in image generation produced the alpha terracotta road tile. Explicit
  `#00FF00` removal replaced rejected border auto-detection, which had sampled
  the orange terrain.
- The first proof uses existing rigid torso sprites deliberately. Articulated
  character rigs, route content, minimal UI, and final fall presentation remain
  the next production gates.

## 2026-07-25 — articulated five-friend cast

- Replaced baked full-character sprites with five generated clay part sheets
  plus a shared face vocabulary. Native child transforms, not an Animator,
  skeletal package, or runtime service, drive arms, feet, wings, ears, leaves,
  crown, pupils, brows, blinks, and mouths.
- Acceleration, angular velocity, wind, fall speed, and impact feed damped
  secondary motion. Each character has different emotion thresholds, gaze and
  blink timing, appendage response, and relief behavior.
- Torso-sized colliders keep the painted silhouettes in dense visual contact.
  Upper friends may make one short, weak, visible, breakable hand grip during
  measured wind danger; the grip releases, cools down, and never becomes a
  permanent tether.
- Collapse now releases grips and layers tumbling, appendage flail, dazed
  faces, crown flight, dust, colored toy chips, camera impulse, haptics, sound,
  and impact-only slow motion.

## 2026-07-25 — travelling routes and final native loop

- Authored three finite roads: Orchard Road, Cloud Bridge, and Windmill Hill.
  The first begins with Pear, Cube, and Bird, then adds Rabbit and Jelly at two
  safe stops. Five remains the complete version-1 roster.
- Added world-space badge triggers, per-route best counts, route unlocks, a
  festival finish, relief reactions, and a brief finish celebration. The UI
  now keeps only badge count and Pause during play; Reduced Motion lives inside
  Pause and difficulty/count setup was removed.
- Generated one empty route sky and one isolated prop sheet. Native parallax
  layers move mesas and trees, four clouds drift on separate loops, and the
  windmill rotor turns independently. The generated source sheets remain
  unchanged and are cropped/keyed at runtime.
- Calm travel uses a gentle physical wheel motor. Touch displacement replaces
  that cruise speed while the finger is down, so a counter-slide can roll the
  wheel under the tower without fighting an added forward input.
- Route distance is an odometer derived from the wheel's absolute angular
  surface speed. It scrolls authored route layers independently from the
  wheel's local balance corrections, so a necessary leftward catch does not
  erase journey progress and no vehicle body receives a scripted translation.
- Blue preview and active gusts pause route distance. The same physical wheel
  motor remains the only player balance control.
- A deterministic human-like pacing proof alternates a forward roll with short
  counter-slides and reaches the first friend stop inside thirty seconds.
  Strongest-gust proofs still require correct travel and still reject neutral
  and wrong input.
- Native Unity primitives won the dependency ladder throughout:
  `Rigidbody2D`, `WheelJoint2D`, sprite transforms, triggers, local
  `PlayerPrefs`, and the existing render-capture path. No runtime package,
  backend, analytics SDK, video layer, or content framework was added.

## 2026-07-26 — owner feel correction

- The first attempt to move the live `WheelJoint2D.anchor` failed the accepted
  gust matrix because changing a suspension constraint injected solver energy.
  A physical slider-carriage experiment also changed the proven vehicle
  response. Both were rejected before release.
- The accepted model leaves the one proven wheel joint and flat-road collider
  untouched. Broad input moves the independent rendered wheel horizontally and
  applies only a short torque while that visible support is moving. Small
  corrections remain precise, release recenters the support, and no body is
  assigned a position or angle.
- The road renderer now shares the gameplay layer's route offset, eliminating
  the old contradiction where road texture implied leftward travel while trees
  implied rightward travel.
- Far mesas were lowered behind the road edge and windmill bases moved below
  the ground line. Portrait travel and finish captures confirm planted bases.
- The old five character-specific impact poses now flash cleanly for `0.72 s`.
  The articulated renderers are temporarily hidden to avoid doubled bodies,
  then restored; large shared `DazedMouth` and `GritMouth` reactions are no
  longer used for Pear/Rabbit impact and effort.
- Cloud Bridge badges weave sideways and Windmill Hill badges trace a larger
  figure-eight. Reused clay UI art supplies `1/2/3` visible road bumps; each
  adds a bounded calm-travel plank jolt, dust, sound, and camera response.
- No new package, generated asset, runtime service, HUD element, currency, or
  generalized route framework was introduced.

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
- The owner accepted delayed-correction survivability. Do not claim the new
  single-mode intensity mix feels right until the owner tests this build.

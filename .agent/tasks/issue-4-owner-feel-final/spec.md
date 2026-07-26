# Issue 4 — owner feel final

Status: Frozen for implementation
Frozen: 2026-07-26
Canonical issue: `kiku-jw/wobble-stack#4`

## Owner playtest

The signed iPhone candidate proved that the rolling control and wind are now
understandable. The remaining release blockers are physical readability,
presentation, and replay texture:

- the wheel rolls across the road but appears pinned to the plank center;
- the road texture scrolls opposite the trees;
- far mesas read as floating islands;
- the shared brown-lipped impact face replaced the fun character-specific
  reactions from the earlier art;
- two completed routes expose too little variation.

Wind force and creature contact physics are accepted and remain unchanged
unless the new moving fulcrum requires a bounded retune.

## Acceptance criteria

### AC1 — Moving physical fulcrum

- Sustained signed thumb input moves the wheel support point at least `0.45`
  world units left or right beneath the plank.
- The support travel is bounded, smooth, visible, and returns toward center
  after release.
- Moving the support point changes the plank's physical weight distribution;
  the plank is never positioned or rotated directly and no invisible
  auto-balance is introduced.
- The wheel remains grounded and correct catches still survive the strongest
  accepted gust while neutral and wrong input remain fallible.

### AC2 — Coherent rightward journey

- As route progress increases, the visible road surface and gameplay landmarks
  both move left relative to the camera.
- The physical wheel still moves right for rightward thumb input.
- Camera follow, route odometer, Retry reset, and finish placement remain
  coherent.

### AC3 — Grounded scenery

- Every generated far mesa has its visual base at or below the authored far
  ground line.
- Windmill towers remain visibly planted instead of reading as airborne
  islands.
- A portrait journey capture has no floating standalone mesa or windmill.

### AC4 — Funny character-specific impacts

- Impact no longer gives all five friends the same `DazedMouth`.
- The existing five character-specific impact poses briefly flash on hard
  contact while articulated secondary parts continue to move.
- Pear and Rabbit no longer use the large teeth/lip `GritMouth` during ordinary
  effort or panic.
- At least four distinct impact sprites are observable across the roster in an
  automated probe and an inspected collapse capture.

### AC5 — Route texture without more UI

- Cloud Bridge badges weave sideways and Windmill Hill badges move in a larger
  figure-eight pattern; Orchard Road remains the readable introduction.
- One gentle visible road bump appears on Orchard Road, two on Cloud Bridge,
  and three on Windmill Hill. Each produces a short bounded physical jolt
  during calm travel and cannot cause an unavoidable fall by itself.
- The three routes retain distinct wind direction patterns, friend flow, badge
  layouts, and destinations.
- No difficulty selector, event meter, tutorial text, currency, shop, backend,
  or additional gameplay HUD is added.

### AC6 — Release proof

- EditMode and PlayMode suites pass with new probes for fulcrum travel, world
  scroll direction, grounded scenery, impact variety, and route-specific badge
  motion.
- Portrait captures cover normal journey and collapse impact.
- A fresh non-Development arm64 build signs, installs, launches, and remains
  running on the paired iPhone.
- Final owner acceptance still requires a physical-device feel verdict.

## Constraints

- Reuse the existing `WheelJoint2D`, route layers, impact art, and badge
  component; add no package, Animator graph, prefab migration, or runtime
  service.
- Preserve the accepted wind model and one-thumb relative input.
- Keep changes inside the existing runtime and PlayMode test surfaces.
- Five friends and three finite routes remain the version-one scope.

# Implementation notes

- This is a replacement of the core interaction, not a tuning pass over direct
  beam control.
- First lower rung: one flat physical road, one dynamic wheel, one dynamic
  plank connected with `WheelJoint2D`, three existing torso sprites, relative
  thumb motor input, follow camera, and deterministic fallability probes.
- Articulation, grips, authored terrain, route progression, and new generated
  art stay behind the rolling-loop gate.
- Five characters are the complete version-1 roster. Additional characters,
  currencies, shops, and procedural routes are deliberately excluded.
- Higgsfield is not required at runtime. Environment movement should be built
  from separated sprite layers and deterministic Unity animation; generation
  tools may supply source layers only.
- Existing web behavior remains published as the historical prototype. Native
  iPhone design is now authoritative.

## Rolling-core receipt

- Kept the vehicle inside `WobbleStackGame`; no controller framework, package,
  prefab migration, or input-system dependency was added.
- Native primitives won the lazy-senior ladder: `Rigidbody2D`,
  `WheelJoint2D`, `CircleCollider2D`, a flat road collider, and the existing
  camera.
- The first large patch was rejected atomically on an outdated context and then
  reapplied in bounded pieces; no partial broken scene was retained.
- The initial road chroma pass was rejected because border sampling selected
  orange. Explicit `#00FF00` produced the validated alpha asset.
- The first actual-physics capture exposed visual wheel/collider padding. The
  measured `268/310` opaque height now sizes a child visual independently from
  the unscaled circle collider.
- Rolling-core result: `13/13` EditMode, `15/15` PlayMode, Mac smoke build, and
  inspected start/playing Metal captures.

## Articulated-character receipt

- Generated five blank-body rig sheets plus one shared face vocabulary instead
  of adding skeletal animation, Spine, or another runtime package. Explicit
  sprite crops and pivots keep the source sheets immutable.
- Native child transforms plus `SmoothDampAngle` won the lazy-senior ladder:
  acceleration, angular velocity, wind, contact, falling speed, and impact
  drive appendage lag. The result needs no Animator graph or imported rigging
  framework.
- Collider dimensions now follow the torso rather than the complete painted
  silhouette. Feet and accents overlap neighboring bodies while physical
  torsos stay in dense contact.
- Every upper creature receives one disabled `DistanceJoint2D` hand grip. A
  grip can only activate during wind plus measured separation danger, is
  max-distance-only, short-lived, low-force, staggered, and breakable. The
  strongest catch matrix still rejects neutral and wrong play.
- Personality is encoded in distinct thresholds, mouth/brow choices,
  irregular blink and gaze schedules, and secondary-motion response: Pear
  braces, Cube worries and reaches, Bird overreacts, Rabbit clings with delayed
  ears, and Jelly remains cheerful or regal longest.
- Failure releases grips, adds a small physically bounded separation impulse,
  drives falling stretch and appendage flail, then switches to dazed impact
  faces. Dust puffs, colored toy chips, crown, gravity arcs, impact-only slow
  motion, haptic/audio hooks, and squash make the collapse readable.
- Articulation result: `13/13` EditMode, `19/19` PlayMode, Mac smoke build, and
  inspected calm-stack and collapse Metal captures.

## Travelling-route receipt

- Added three authored finite routes, `7/8/9` world-space badges, two
  first-route friend joins, per-route best counts, route unlocks, finish
  celebration, and one five-character roster. No shop, currency, backend,
  online mode, procedural route system, or difficulty selector was added.
- Generated one empty route sky and one isolated clay environment sheet.
  Explicit crop rectangles and pivots feed four drifting clouds, two parallax
  scenery depths, foreground props, safe stops, festival arch, and an
  independently rotating windmill rotor.
- Kept route movement and tower balance legible by separating their jobs:
  calm travel uses a gentle physical wheel motor, while finger displacement
  replaces that cruise and remains the exclusive catch control. A route
  odometer integrates absolute wheel angular surface speed independently from
  local balance corrections. Preview, active wind, safe stops, and finish
  pause the odometer. No vehicle body is translated or assigned an angle.
- The first-run instruction now says exactly what the gesture does and pairs it
  with a brief horizontal thumb track. Gameplay HUD contains only badge count
  and Pause; Reduced Motion moved into Pause.
- A human-like automated drive alternates forward roll with short
  counter-slides, survives, and reaches the first friend stop inside thirty
  seconds. Strongest-gust, wrong-input, wheel-grounding, pickup-trigger, joins,
  finish, parallax, and UI proofs pass in the same suite.
- Travelling-route result: `13/13` EditMode, `25/25` PlayMode, Mac smoke build,
  and inspected start, journey, impact, and festival-finish Metal captures.

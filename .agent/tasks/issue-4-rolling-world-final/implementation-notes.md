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

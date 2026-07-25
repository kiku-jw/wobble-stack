# Rolling-world final — evidence

Result: phase pass, continue

## Verified in this phase

- AC1 grounded rolling vehicle: pass for the flat-road vertical slice.
- AC2 travel camera and readable balance: pass for horizontal follow and the
  strongest deterministic catch matrix; authored route space remains pending.
- AC3 dense articulated stack: pass. Five distinct rigs, torso-following
  colliders, secondary motion, visual hand reach, and four breakable weak grips
  are active without rotation locks or permanent tethers.
- AC4 character-specific life/emotion: pass for calm, alert, effort, panic,
  relief, falling, impact, irregular blink, and independent gaze behavior.
- AC5 living world: generated repeating clay road pass only; clouds, windmill,
  and full parallax remain pending.
- AC7 fall presentation: phase pass for grip release, free tumbling, falling
  stretch/flail, dazed faces, crown arc, layered dust, colored toy chips,
  impact-only slow motion, haptic/audio hooks, and camera impulse. Finish
  celebration remains pending.
- AC8 release proof: local compile/test/Metal capture pass; fresh iPhone export
  remains pending until the next cohesive device build.

## Receipts

- Unity `6000.3.19f1` EditMode: `13/13`.
- Unity `6000.3.19f1` PlayMode: `19/19`.
- Mac smoke build: pass.
- Actual-physics portrait captures:
  `raw/rolling-core-start.png` and `raw/rolling-core-playing.png`.
- Articulation portrait captures:
  `raw/articulated-start.jpg` and `raw/articulated-impact.jpg`.
- Current articulation reports:
  `raw/articulated-editmode.xml` and `raw/articulated-playmode.xml`.

## Physics claims proved

- One `WheelJoint2D` connects the dynamic plank to the dynamic star wheel.
- Creature bodies remain freely rotating. Only four disabled-at-rest,
  max-distance, weak hand grips supplement the one vehicle joint.
- Signed wheel input moves in the finger direction before wind.
- Touch-down origin drives the wheel from anywhere outside UI; release
  disables the motor instead of applying hidden stabilization.
- The camera follows real wheel travel and resets to the route origin on Retry.
- The visible wheel/circle collider remains grounded through a strong
  five-friend catch.
- Correct steady travel completes maximum gusts in both directions with three
  and five bodies.
- A delayed broad rescue gesture completes the same matrix.
- Neutral and wrong travel still collapse.

## Pending acceptance

- Remaining AC5 parallax/cloud/windmill work.
- AC6 journey, badges, joins, persistence, and minimal UI.
- Remaining AC7 near-save and finish presentation.
- Full AC8 iPhone install, performance, owner feel, and release verdict.

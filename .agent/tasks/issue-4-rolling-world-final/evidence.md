# Rolling-world final — evidence

Result: native feature-complete candidate; physical iPhone feel gate pending

## Verified in this phase

- AC1 grounded rolling vehicle: pass for the flat-road vertical slice.
- AC2 travel camera and readable balance: pass for horizontal follow,
  human-like first-stop travel, and the strongest deterministic catch matrix.
- AC3 dense articulated stack: pass. Five distinct rigs, torso-following
  colliders, secondary motion, visual hand reach, and four breakable weak grips
  are active without rotation locks or permanent tethers.
- AC4 character-specific life/emotion: pass for calm, alert, effort, panic,
  relief, falling, impact, irregular blink, and independent gaze behavior.
- AC5 living world: pass. Empty route sky, four drifting clouds, far/mid/front
  parallax, route landmarks, and independently rotating windmill are active.
- AC6 journey and progression: pass. Three routes, `7/8/9` badges, two
  first-route joins, best counts, unlocks, finish flow, and minimal gameplay UI
  are implemented.
- AC7 fall presentation: pass for grip release, free tumbling, falling
  stretch/flail, dazed faces, crown arc, layered dust, colored toy chips,
  impact-only slow motion, haptic/audio hooks, camera impulse, near-save relief,
  and finish celebration.
- AC8 release proof: local compile/test/Metal capture pass; fresh iPhone export,
  install, launch, performance, and owner feel remain pending.

## Receipts

- Unity `6000.3.19f1` EditMode: `13/13`.
- Unity `6000.3.19f1` PlayMode: `25/25`.
- Mac smoke build: pass.
- Actual-physics portrait captures:
  `raw/rolling-core-start.png` and `raw/rolling-core-playing.png`.
- Articulation portrait captures:
  `raw/articulated-start.jpg` and `raw/articulated-impact.jpg`.
- Current articulation reports:
  `raw/articulated-editmode.xml` and `raw/articulated-playmode.xml`.
- Travelling-route captures:
  `raw/route-start.jpg`, `raw/route-playing.jpg`, `raw/route-impact.jpg`, and
  `raw/route-finish.jpg`.
- Travelling-route reports:
  `raw/route-editmode.xml` and `raw/route-playmode.xml`.

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
- A physical-wheel odometer advances the authored route without translating
  the vehicle or letting a leftward balance correction erase journey progress.
- A human-like full-route probe reaches both friend stops and the festival
  finish while preserving the physical-wheel, join, and badge flow.
- Creature contact collects each badge once; two authored stops add Rabbit and
  Jelly; finish transitions through relief and celebration to route results.
- Four clouds move independently, the camera drives parallax, and the windmill
  rotor changes angle.

## Pending acceptance

- Fresh non-Development iOS export, signed arm64 build, install, and launch.
- Physical-device performance, readable control, first-route pacing, Retry,
  and owner feel verdict.

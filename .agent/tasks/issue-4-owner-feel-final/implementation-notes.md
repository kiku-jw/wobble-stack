# Issue 4 — owner feel final implementation notes

## Delivered changes

- The star-wheel support now travels visibly left and right beneath the plank,
  follows the existing relative swipe control, and smoothly recenters.
- The accepted grounded `WheelJoint2D` vehicle remains the physical source of
  road contact. Support travel adds only a bounded transient torque while it
  moves, so it does not replace the already-proven gust balance model.
- Direct live joint-anchor mutation and a separate slider carriage were tested
  and rejected because both injected solver energy and regressed the strongest
  gust matrix.
- Road texture and route landmarks now share the same rightward-journey
  convention.
- Far mesas and both windmill tower layers were lowered onto the road plane.
- Route badges now have route-specific motion. Orchard uses a small vertical
  bob, Cloud Bridge moves horizontally and vertically, and Windmill Hill uses
  the widest figure-eight motion.
- The three routes contain one, two, and three visible road bumps. A bump waits
  for a calm window, triggers only when it reaches the visible wheel, and adds
  a small survivable jolt, dust beat, sound, and camera response.
- Ground impact temporarily swaps each articulated rig for its own existing
  comic impact pose, then restores the live ragdoll. The old unattractive
  generic lip states are no longer used for Pear and Rabbit impact reactions.
- Final portrait captures replace the README gameplay, collapse, and finish
  screenshots.

## Lazy-senior receipt

- Lower rung selected: extend the existing `WheelJoint2D`, road visual,
  parallax layers, impact sprites, and `RoutePickup`; no new dependency or
  framework is justified.
- GitHub prior-art search is skipped because these are repository-local
  physics and presentation regressions with direct Unity primitive solutions.
- New reusable code is limited to bounded axle travel, route-specific badge
  choreography, and authored road-bump beats. The other fixes are
  existing-asset reuse, coordinate correction, and parameter changes.

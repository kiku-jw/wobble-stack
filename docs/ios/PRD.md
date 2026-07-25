# Wobble Stack for iPhone

## Product goal

Ship a portrait, one-thumb iPhone journey whose core pleasure is rolling a
one-wheel plank underneath a living stack of expressive toy friends. The
production target is the grounded physical comedy and emotional clarity of the
concept frames, not a dressed-up version of the stationary web prototype.

## Player promise

In seconds, the player understands: put a thumb down anywhere, slide left or
right to roll the star wheel, catch the leaning tower, collect a risky golden
badge, watch five personalities react, and immediately want one cleaner trip.

## Story and release shape

Five friends are late for the sunset celebration at the windmill. Their cart
has broken down, so they travel on its remaining plank and star wheel while
recovering the golden festival badges scattered along the road.

- Offline, single-player, portrait-only.
- Three short handcrafted routes: Orchard Road, Cloud Bridge, and Windmill
  Hill.
- The first route begins with Pear, Cube, and Bird. Rabbit and Jelly King join
  at authored safe stops, building the full concept tower through play.
- Exactly five version-1 characters. Personality depth takes priority over a
  catalogue, currency, or shop.
- One continuously varied gust model with no difficulty or intensity HUD.
- Three badge pickups per route at different heights, collected by creature
  contact and persisted as route mastery.
- Start, travel, pause/settings, collapse, Retry, route finish, and route
  selection flows.
- Sound, haptics, reduced motion, safe areas, interruption-safe pause, and local
  progress only.

## Core loop

1. Touch anywhere outside UI and slide relative to the touch-down point.
2. Roll the wheel under the direction in which the stack is leaning; return
   toward the touch-down point to coast or brake.
3. Read gust buildup from blue wind, appendages, faces, and sound rather than
   text or a meter.
4. Travel toward the windmill, collect optional badges with the creatures, and
   add the two missing friends at first-route safe stops.
5. Recover from near-falls or lose a friend to the ground in a comic physical
   collapse.
6. Retry immediately; completing the route records badges and opens the next
   route.

## Explicit non-goals

- Accounts, backend, cloud saves, analytics SDKs, ads, IAP, shop, battle pass,
  multiplayer, or live operations.
- Endless character collection, procedural route generation, a general content
  framework, or configurable physics architecture.
- Directly rotating or positioning the plank as player control.
- Recreating the concept images as flat backgrounds with gameplay painted over
  them.

## Quality gates

- Correct wheel travel materially outperforms neutral and wrong travel on the
  same gust; doing nothing is never the best hidden strategy.
- The visible wheel remains in contact with the visible road through ordinary
  and strong catches.
- The plank is always dynamic and reacts to wheel acceleration, terrain,
  bodies, and wind rather than a scripted target angle.
- Five silhouettes remain distinguishable without faces; at gameplay scale
  each character has readable calm, effort, panic, relief, and impact behavior.
- The tower is the first visual read, grounded vehicle second, world movement
  third, and UI last.
- Every primary state can produce a credible store screenshot.
- A physical iPhone run is mandatory before calling the game release-ready.

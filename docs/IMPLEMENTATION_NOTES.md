# Wobble Stack implementation notes

## Reference lock

Primary reference: `docs/concepts/last-second-save.png`.

Preserve:

- 9:16 single-stage composition;
- warm coral sky, distant floating islands, and a tactile central tower;
- five distinct colors and silhouettes;
- minimal score/pause HUD;
- the emotional peak is a visible last-second save.

Secondary reference: `docs/concepts/comedic-collapse.png`.

Preserve the collapse as the punchline and make Retry the largest result action.

The team-selection frame is explicitly deferred. It is evidence for possible future character treatment, not prototype scope.

## Decision ledger

- Canvas is code-native game media; generated art is a target, not a baked background.
- Matter.js owns collision and gravity. The prototype does not invent a physics engine.
- A single direct target-angle input is used. Alternate kernels and configurable control architecture are deferred.
- Interface accent is coral. Purple appears only as one creature color from the reference, not as a generic UI theme.
- No sound assets, analytics, haptics framework, save schema, routing, or service worker.
- Local storage holds only the best time and gracefully degrades when unavailable.

## lazy-senior receipt

- Lower rung: browser Canvas + one small established physics dependency.
- GitHub prior art: `liabru/matter-js` is MIT-licensed, active, and purpose-built; adoption = add dependency.
- New code is limited to the game loop, input, drawing, and visible state transitions.

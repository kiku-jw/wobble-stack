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
- Local storage holds per-setup best times and the last setup, and gracefully degrades when unavailable.

## M4 reference lock

- Primary direction: preserve the existing immersive game canvas, warm coral sky, minimal HUD, and one large coral Play/Retry action.
- Borrow only: native labeled form controls, 44px minimum hit targets, and visible `:focus-visible` treatment from the craft references.
- Role rules: cream is the setup surface, coral is the primary action/selected state, and character colors never become generic interface accents.
- Motion: setup feedback stays within 120–200ms; the wind animation exists only to explain force direction and timing.
- Reject: a separate settings screen, modal cards, sliders with hidden numeric meaning, new dependencies, and a general level system.

## M4 calibration decisions

- `Gentle`, `Normal`, and `Wild` are three fixed profiles, not user-editable physics parameters.
- Gravity stays `0.00105`. Early Normal wind starts at `0.000065`, requiring about 3.5 degrees of theoretical counter-tilt.
- The platform range increases to about 26 degrees and follows a bounded target angle, so the stack cannot torque the control past the player's request.
- Gentle remains counterable at maximum wind; Normal approaches the control limit late; Wild may exceed it late by design.
- Gust force uses an attack/hold/release envelope. No frame receives full force at gust start.
- Stack position is derived from collider heights for the selected 1–5 creatures; no separate scene or prefab variants.
- Paired low-stiffness contact links keep neighbors recognizable as a tower, while air damping removes the old post-gust launch.
- Keyboard counter-tilt uses the current announced gust force; pointer input stays fully analog.
- Best scores use the key `<difficulty>:<count>`. The old single best migrates to `normal:5` when present.

## lazy-senior receipt

- Lower rung: browser Canvas + one small established physics dependency.
- GitHub prior art: `liabru/matter-js` is MIT-licensed, active, and purpose-built; adoption = add dependency.
- New code is limited to the game loop, input, drawing, and visible state transitions.

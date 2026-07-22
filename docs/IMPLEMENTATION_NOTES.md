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
- Gravity stays `0.00105`. Normal samples `0.000065–0.000105`; the keyboard counter target includes a calibrated stack-leverage allowance while pointer input remains analog.
- The platform range increases to about 26 degrees and follows a bounded target angle, so the stack cannot torque the control past the player's request.
- Gentle, Normal, and Wild use non-overlapping random force ranges rather than an elapsed-time ramp.
- Gust force uses an attack/hold/release envelope. No frame receives full force at gust start.
- Stack position is derived from collider heights for the selected 3–5 creatures; no separate scene or prefab variants.
- Paired low-stiffness contact links keep neighbors recognizable as a tower, while air damping removes the old post-gust launch.
- Keyboard counter-tilt uses the sampled gust force plus a calibrated leverage allowance; pointer input stays fully analog.
- Best scores use the key `<difficulty>:<count>`. The old single best migrates to `normal:5` when present.

## M5 collapse reference lock

- Primary reference: `docs/concepts/comedic-collapse.png`; preserve separated bodies, exaggerated panic, visible motion, and collapse as the punchline.
- Motion role: impact slowdown exists to make ground contact and the face change readable, not as a reusable timeline system.
- State sequence: calm/effort → panic while airborne → dazed only after catch-floor contact → results.
- Impact feedback: one small dust burst and brief shake per creature; no screen flash, camera zoom, new overlay, sound dependency, or particle framework.
- Reduced motion: keep semantic face feedback, weaken shake, use near-normal physics speed, and shorten the result delay.
- Result timing: fall at normal speed, slow briefly on first registered impact, return to normal speed, then wait for the face reaction; use a hard timeout for off-screen or missed-floor failures.

## M6 reference lock

- Primary direction: preserve the immersive coral stage and use the existing code-native wind streaks as the only directional cue.
- Motion role: a gust starts faint and slow, then the same envelope increases both physical force and the streak count, speed, length, and opacity.
- Reject: persistent WIND/LEAN instructions, arrow pills, sequential power creep, a tutorial pause, or new animation dependencies.
- Difficulty: randomness lives inside fixed non-overlapping ranges, so a hard mode is immediately stronger rather than becoming stronger only after waiting.
- Creature count: three is the smallest readable tower and the lower product boundary; one- and two-body score data is left untouched but no longer selectable.

## M6 lazy-senior receipt

- Lower rung: reuse the gust envelope, Canvas renderer, Matter collision event, and existing settings storage.
- GitHub prior art: skipped because this is local physics calibration and visual state, not a reusable package or protocol.
- New code is limited to profile sampling, impact-only time scale state, Canvas intensity mapping, and debug-only QA receipts.

## lazy-senior receipt

- Lower rung: browser Canvas + one small established physics dependency.
- GitHub prior art: `liabru/matter-js` is MIT-licensed, active, and purpose-built; adoption = add dependency.
- New code is limited to the game loop, input, drawing, and visible state transitions.

## M5 lazy-senior receipt

- Lower rung: existing `Matter.Events`, Canvas face primitives, and the current particle loop.
- GitHub prior art: skipped because this is a repo-local visual state and no reusable widget or protocol is being introduced.
- New code is limited to one collision receipt, one dazed-face branch, result timing, and debug-only QA access.

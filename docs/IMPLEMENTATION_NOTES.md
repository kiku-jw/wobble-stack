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
- Browser music uses one native `Audio` element; there is no Web Audio graph,
  audio framework, analytics, haptics framework, routing, or service worker.
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

## M6 wind-speed regression fix

- Root cause: `time * changingSpeed` used absolute animation time, so the derivative included the speed change itself and produced an exaggerated burst during attack.
- Fix: horizontal travel is integrated once per frame as `distance += speed(intensity) × deltaTime`; drawing reads the accumulated distance.
- The visual speed mapping is monotonic and bounded. Gust force, envelope timing, difficulty ranges, and player control are unchanged.

## M7 counter-tilt calibration

- Root cause: wind applied a mass-proportional force to every creature, while beam angle acted mainly through the bottom contact. Tilting therefore added stack shear without giving the upper bodies a comparable counter-force.
- Fix: during an active gust, the beam angle contributes a mass-proportional horizontal acceleration to every creature. Correct tilt cancels 72% of the sampled wind acceleration at the calculated counter-angle; wrong tilt adds the same amount.
- The remaining 28% keeps countering analog and imperfect. Excess tilt can reverse the drift, so the control is not a binary wind-cancel button.
- Keyboard counter-angle no longer doubles sampled force. Pointer input remains fully analog and uses the same platform angle and force path.
- Wind profiles, gust timing and envelope, gravity, collision bodies, constraints, and failure sequencing are unchanged.

## M7 lazy-senior receipt

- Lower rung: reuse Matter.js body forces, the existing gust envelope, and the already bounded platform angle.
- GitHub prior art: skipped because this is repo-local feel calibration, not a reusable physics package or protocol.
- New code is one pure acceleration helper, one authority constant, its call site, and deterministic tests; no dependency or new state machine was added.

## M8 single-mode wind

- Deleted the difficulty selector, three profiles, selected-difficulty state,
  and web intensity meter.
- One continuous `0.000055–0.000135` force range keeps the accepted Normal
  center. Squaring the uniform force sample favors ordinary gusts while
  preserving occasional exact soft/strong bounds and deterministic seeds.
- Wind streaks and physics remain the intensity language. Best-score keys keep
  using the former Normal slots to preserve local records.

### M8 lazy-senior receipt

- Lower rung: delete product state and reuse the current scheduler/envelope.
- GitHub prior art: skipped because this is repo-local simplification, not a
  reusable widget or scheduler.
- New code is limited to bounded force sampling and intensity normalization;
  the net production diff removes substantially more code than it adds.

## M9 browser music

- Four owner-supplied parade tracks form one shuffled bag. Every track plays
  once before reshuffling, and the first track of a new bag cannot immediately
  repeat the previous one.
- A single native `Audio` element owns playback. Only the current track receives
  a source URL, with metadata-only preload before playback.
- The first `play()` call stays inside the Play gesture for mobile autoplay
  compatibility. Pause, Resume, page hiding, Retry, and route selection reuse
  that same player and position.
- Music defaults to 50%. Synchronized native range controls live in the start
  and pause menus; the preference is stored separately from journey progress
  and falls back safely when storage is unavailable.
- Public MP3 exports are metadata-free, 128 kbps, and normalized around
  `-16 LUFS` with a ceiling no higher than `-1.5 dBTP`.

### M9 lazy-senior receipt

- Lower rung: one platform `Audio` element, two native range inputs, and the
  existing local-storage boundary.
- GitHub prior art: skipped because the feature is repo-local media playback,
  not a reusable protocol or package.
- No Web Audio graph, mixer abstraction, module, dependency, worker, analyser,
  or playlist state machine was added.

## M10 direct mobile control and jump obstacles

- Pointer steering no longer reads the current gust before deciding how far the
  wheel may move. A full drag directly requests `84%` of the existing
  44-logical-pixel support range during and between gusts.
- The current support and platform easing remain the only horizontal response
  layer. Wrong-way input still applies the wrong platform angle and receives no
  hidden correction.
- One short stationary pointer release starts a fixed `0.72 s`, 54-pixel sine
  jump. Peak pointer travel classifies the gesture, so dragging away and back
  cannot become a jump. Keyboard auto-repeat is ignored so a held jump key
  cannot queue another jump after cooldown.
- The static Matter.js platform moves through the arc; its existing base
  constraint carries the living stack. The wheel drawing follows the same
  height while the road and obstacle stay grounded.
- Existing route `bumpDistances` remain the only obstacle positions. Sufficient
  jump height clears once. A grounded hit pauses progress for `0.52 s`, lifts
  the platform through a bounded 16-pixel arc, applies a strong alternating
  tilt, and gives each creature a deterministic velocity/angular-velocity
  impulse. The result remains recoverable rather than scripting a loss.
- Known Telegram contexts and the explicit `from=telegram` share query receive
  an honest start-menu notice. Clipboard uses the native browser API, supported
  Telegram Mini App `openLink` is used only when already present, and ordinary
  pages never promise a forced external-browser launch.
- Pointer capture is best-effort with window-level release handling. Only
  browsers without Pointer Events receive the non-passive touch fallback.

### M10 lazy-senior receipt

- Lower rung: tiny local code in existing input, route-crossing, static-platform,
  start-overlay, and debug seams.
- GitHub prior art: skipped because this is a bounded repo-local control and
  physics change, not a reusable gesture or obstacle system.
- Rejected: a new dependency, gesture framework, physical obstacle bodies,
  Telegram SDK/bot, backend, second jump button, and route-schema expansion.

## M11 privacy-first public playtest

- Telemetry is active only for allow-listed `playtest` and `source` query
  values. The ordinary game remains unchanged and sends nothing.
- Consent is explicit. Private play stores only that choice; joining creates a
  random installation UUID and a bounded 50-event local outbox.
- Events are semantic round summaries: start, finish, Retry, duration,
  progress, badges, jumps, obstacle hits/clears, route, source, and local day.
  Names, Telegram identifiers, messages, user agent, IP, and precise controls
  are not stored in the application database.
- A first-party Cloudflare Worker validates exact shapes and origin, limits
  bodies and request rate, uses prepared D1 statements, and exposes no report
  route. Raw events expire after 30 days.
- The first 20 distinct TG1 starters occupy fixed cohort slots. Withdrawal
  deletes their events and anonymizes the slot without admitting participant
  21 into the primary cohort.
- The private SQL report measures five-round repetition, another-local-day
  return, Retry, jumps, obstacle hits/clears, duration, and progress.

### M11 lazy-senior receipt

- Lower rung: native browser storage/fetch plus Cloudflare Worker, D1, SQL, and
  rate-limit bindings.
- GitHub prior art: larger analytics and Worker stacks were broader than this
  three-route collector; adoption = ignore.
- Added only the official Wrangler deployment dependency. Rejected analytics
  SDKs, frameworks, ORMs, queues, dashboards, fingerprinting, and control-level
  telemetry.

## lazy-senior receipt

- Lower rung: browser Canvas + one small established physics dependency.
- GitHub prior art: `liabru/matter-js` is MIT-licensed, active, and purpose-built; adoption = add dependency.
- New code is limited to the game loop, input, drawing, and visible state transitions.

## M5 lazy-senior receipt

- Lower rung: existing `Matter.Events`, Canvas face primitives, and the current particle loop.
- GitHub prior art: skipped because this is a repo-local visual state and no reusable widget or protocol is being introduced.
- New code is limited to one collision receipt, one dazed-face branch, result timing, and debug-only QA access.

# Issue 4 — web mobile control, Telegram handoff, and jump obstacles

Status: Frozen
Frozen: 2026-07-30
Canonical issue: `kiku-jw/wobble-stack#4`

## Owner request

Make the public phone-first browser game more interesting and more controllable:
the wheel should respond strongly to a thumb, Telegram users should not be
trapped in an unusable embedded browser, and the authored road bumps should
become obstacles the player can jump.

## Acceptance criteria

### AC1 — Direct, responsive one-thumb steering

- Relative horizontal drag controls the wheel during and between gusts.
- A full drag reaches at least 34 of the existing 44 logical pixels of support
  travel; the mapping is independent of gust direction.
- Small drags respond without a large dead region and release recenters
  smoothly.
- Wrong-way steering remains physically dangerous; the game does not choose the
  corrective side for the player.
- Keyboard control, pause, reduced motion, collapse, and Retry remain intact.

### AC2 — Unambiguous tap-to-jump gesture

- A short, stationary canvas tap starts one bounded jump.
- Horizontal drag does not trigger a jump on release.
- Jump input has a short cooldown and cannot be stacked or held for extra
  height.
- The wheel, plank, and creature stack visibly leave the road and land; jumping
  does not bypass wind balance.
- The control hint and accessible status explain both steering and jumping.

### AC3 — Authored obstacles become player decisions

- Existing route `bumpDistances` remain the single authored obstacle source:
  Orchard, Cloud, and Windmill retain `1/2/3` obstacles.
- Each obstacle is visible before contact.
- Crossing while sufficiently airborne records a clear and restrained success.
- Crossing without enough jump height applies the existing bump-like physical
  kick and feedback rather than an instant scripted failure.
- Each obstacle resolves exactly once per run and resets on Retry.

### AC4 — Telegram handoff and touch resilience

- Known Telegram contexts and an explicit `from=telegram` query show concise
  external-browser instructions before Play.
- The notice offers a copy-link action and a non-blocking “play here anyway”
  route; it does not falsely claim that an ordinary page can force Safari or
  Chrome.
- A Telegram Mini App host may use its supported external-link API only after a
  user click; ordinary pages remain dependency-free.
- Pointer capture failure cannot strand input, and browsers without Pointer
  Events receive a non-passive touch fallback.
- No account, backend, telemetry, external script, or new runtime dependency is
  added.

### AC5 — Mobile and release proof

- Pure control, gesture, obstacle, Telegram-detection, and jump-curve helpers
  have deterministic unit coverage.
- `pnpm test`, `pnpm build`, and `git diff --check` pass.
- A fresh production preview passes at `320 × 700`, `390 × 844`, and desktop,
  including real drag, tap jump, obstacle clear/miss, Telegram notice, pause,
  Retry, and zero console errors.
- The verified commit is pushed safely to `origin/main`, the existing GitHub
  Pages workflow succeeds, and a fresh live smoke confirms the public revision.

## Constraints and non-goals

- Reuse Vite, Canvas, Matter.js, the current route catalog, debug hook, art, and
  GitHub Pages deployment.
- Keep one-thumb portrait play; do not add a second on-screen button or vertical
  swipe gesture.
- Do not add levels, currencies, analytics, a bot, a Telegram Mini App, a
  backend, or a new deployment mechanism.
- Do not tune the Unity client in this delivery.
- Physical-phone feel and Telegram-client behavior remain a human acceptance
  gate after automated browser proof.

## Verification plan

1. Unit-test pure steering, tap classification, jump height, obstacle outcome,
   and Telegram-context detection.
2. Run deterministic debug-hook checks for full control, jump arc, obstacle
   clear, and obstacle miss.
3. Exercise real mouse/pointer gestures in a local production preview at mobile
   viewports and inspect screenshots plus console output.
4. Run the full repository check, whitespace check, independent read-only
   verification, safe Git publication, Pages workflow verification, and live
   smoke.

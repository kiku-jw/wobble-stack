# Issue 4 — web mobile control and jump evidence

Date: 2026-07-30
Target: local production preview from the acceptance-scoped working tree

## Automated checks

- `pnpm test`: PASS, 22 tests, 0 failures.
- `pnpm build`: PASS, Vite production build.
- `git diff --check`: PASS.

## Browser behavior

- 390 × 844 real pointer drag:
  - held support offset: `36.4155`
  - held target: `36.96`
  - released support offset after 300 ms: `0.3970`
  - released target: `0`
  - game state: `playing`
- 390 × 844 real short tap:
  - sampled jump height: `47.9745`
  - jump active: `true`
- Dragging 80 px away and back before release:
  - sampled jump height: `0`
  - jump active: `false`
- Held-key repeat:
  - Space, Arrow Up, and W dispatch on the initial key event
  - repeated keydown events are rejected before jump dispatch
  - production preview remained at jump height `0`, inactive, and cooldown `0`
    after a repeated W keydown sent beyond the first jump's cooldown
- Orchard first obstacle:
  - no jump: `[[0, "hit"]]`
  - current-frame jump height `39.1540`: `[[0, "cleared"]]`
  - each run recorded exactly one outcome
- Telegram share route at 320 × 700:
  - notice visible before dismissal
  - copy and continue actions: `44 px` high
  - document width: `320 px`; no horizontal or vertical overflow
  - copy result: `Link copied`
  - live status named Safari or Chrome
  - “Play here anyway” dismissed the notice
- Normal route:
  - Telegram notice absent
  - Escape pause and “Keep rolling” resume passed
  - deliberate collapse reached results
  - Retry returned to `playing` with jump height `0` and no obstacle outcomes
- Layout:
  - 390 × 844 document matched viewport
  - 1280 × 900 document matched viewport; game frame was `415.875 × 899.984`
- Console: 0 errors, 0 warnings.

## Visual inspection

The production preview screenshot at peak jump showed the wheel, plank, and
three-creature stack visibly airborne while the road stayed grounded. The
existing portrait composition and controls remained readable.

## Remaining human gate

Automated browser evidence cannot prove physical iPhone feel or the exact
behavior of every Telegram client version. Those remain explicit post-release
tester gates in Issue #4.

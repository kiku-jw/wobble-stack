# Issue 4 — web mobile control and jump implementation notes

Date: 2026-07-30

## Frozen decisions

- Steering is direct input. Wind state may affect the outcome, but it must not
  silently reduce or choose the player's wheel travel.
- A short stationary tap is the jump gesture; drag remains steering. This keeps
  the game one-thumb and avoids a second control surface.
- Existing `bumpDistances` own obstacle placement. The delivery changes their
  interaction, not the route schema.
- An ordinary web page cannot reliably force Telegram to leave its embedded
  browser. The product response is honest guidance, copy-link support, a
  Mini-App-only supported external-open path when present, and resilient input.
- No dependency or backend is justified for this slice.

## Build notes

- Direct pointer travel uses `84%` of the existing 44-logical-pixel support
  range and the existing support easing; no parallel movement model was added.
- Tap recognition records peak two-dimensional travel so a drag away and back
  cannot become a jump on release.
- Keyboard auto-repeat is rejected before jump dispatch, so holding Space,
  Arrow Up, or W cannot trigger another jump after cooldown.
- The existing static Matter.js platform moves through a fixed sine arc. Jump
  state advances before journey crossings so obstacle resolution reads the
  current frame's height.
- Each authored bump resolves once into `cleared` or `hit`; a miss reuses the
  existing physical bump response.
- Telegram detection is deliberately narrow: explicit share query, Telegram
  user agent, or an already-present Mini App bridge. Generic mobile webview
  guessing was rejected.
- Pointer capture is best-effort, with window-level completion and a small
  non-Pointer-Events touch fallback.

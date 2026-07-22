# Wobble Stack web prototype test plan

## Automated checks

From `prototypes/wobble-stack`:

```sh
pnpm install --frozen-lockfile
pnpm test
pnpm build
```

## Browser smoke

1. Open the development build at 390 × 844.
2. Press Play and confirm the five bodies settle on the beam.
3. Drag left and right; confirm the beam follows without page scrolling.
4. Wait for `GUST!`; confirm the arrow appears before the shove.
5. Deliberately drop a body; confirm the result and Retry appear within one second.
6. Press Retry; confirm score, bodies, beam, and hazard timing reset without navigation.
7. Pause and resume; confirm time and physics stop while paused.
8. Repeat input using Left/Right or A/D and Enter/Space.
9. Check browser console for uncaught errors.

## Visual checks

- Mobile: 390 × 844.
- Small mobile: 320 × 700.
- Desktop: 1280 × 900.
- Important HUD and controls stay inside safe visible bounds.
- Overlay copy never hides Retry.
- Character silhouettes and gust direction remain legible without relying on text alone.

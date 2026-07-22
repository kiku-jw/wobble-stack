# Wobble Stack web prototype test plan

## Automated checks

From the repository root:

```sh
pnpm install --frozen-lockfile
pnpm test
pnpm build
```

Assertions cover:

- all difficulty profiles ramp from lower to higher force;
- Gentle < Normal < Wild at the same elapsed time;
- early Normal counter-angle is modest and late Normal stays inside the control limit;
- the gust envelope ramps in and out instead of applying peak force immediately;
- stack layouts for 1–5 creatures stay in contact with the beam and each other.
- failure results wait for either the configured impact hold or the hard timeout.

## Browser smoke

1. Open the development build at 390 × 844.
2. Press Play and confirm the five bodies settle on the beam.
3. Drag left and right; confirm the beam follows without page scrolling.
4. Wait for the wind cue; confirm `WIND` and the opposite `LEAN` arrow appear before the shove.
5. Deliberately collapse the stack; confirm physics slows, bodies separate, and a face changes only after ground impact.
6. Confirm dust appears at impact and the dazed face remains visible before the result overlay.
7. Confirm results appear within the hard cinematic timeout even if no creature reaches the floor.
8. Press Retry; confirm score, bodies, beam, face state, and hazard timing reset without navigation.
9. Pause and resume; confirm time and physics stop while paused.
10. Repeat input using Left/Right or A/D and Enter/Space.
11. Check browser console for uncaught errors.

## Calibration matrix

1. Gentle, 5 creatures: the untouched stack survives the first weak gust long enough to teach the response.
2. Normal, 5 creatures: follow the `LEAN` cue and confirm counter-tilt visibly resists the first gust.
3. Wild, 5 creatures: force ramps faster but still shows a warning before the shove.
4. Change the count to 1, 3, and 5; confirm the preview and physical stack rebuild immediately.
5. Record a best score, change one setup dimension, and confirm the displayed best changes with it.
6. Lose a run and press Retry; confirm the selected setup is preserved.
7. Lose a run and press Change Setup; confirm the settings return without reloading.

## Collapse matrix

1. Normal motion: the failure time scale is visibly slower than play and results wait for an impact reaction or the hard timeout.
2. One creature: the single body can receive the dazed face and reach results.
3. Five creatures: soft links release and ground reactions are tracked independently.
4. Reduced motion: the sequence is shorter and closer to normal speed, but the impact face still appears.
5. Retry: every creature starts the next run without a stale impact reaction.

## Visual checks

- Mobile: 390 × 844.
- Small mobile: 320 × 700.
- Desktop: 1280 × 900.
- Important HUD and controls stay inside safe visible bounds.
- Overlay copy never hides Retry.
- Character silhouettes and gust direction remain legible without relying on text alone.

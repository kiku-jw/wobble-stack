# Wobble Stack web prototype test plan

## Automated checks

From the repository root:

```sh
pnpm install --frozen-lockfile
pnpm test
pnpm build
```

Assertions cover:

- every gust samples force, rest, and duration inside its profile range;
- the complete Gentle force range is below Normal, and Normal is below Wild;
- Normal force is meaningful but stays inside the control limit;
- the gust envelope ramps in and out instead of applying peak force immediately;
- wind streak travel speed increases monotonically with visual intensity;
- stack layouts for 3–5 creatures stay in contact with the beam and each other;
- failure results wait for either the configured impact hold or the hard timeout.

## Browser smoke

1. Open the development build at 390 × 844.
2. Press Play and confirm the five bodies settle on the beam.
3. Drag left and right; confirm the beam follows without page scrolling.
4. Wait for the first wind streaks; confirm they move with the push and increase in number, speed, opacity, and force without an arrow pill.
   Compare equal time windows during attack: horizontal distance must be smallest near onset, larger while building, and largest at peak.
5. Deliberately collapse the stack; confirm bodies fall at normal speed, slow only on first ground impact, and a face changes on impact.
6. Confirm dust appears at impact and the dazed face remains visible before the result overlay.
7. Confirm results appear within the hard cinematic timeout even if no creature reaches the floor.
8. Press Retry; confirm score, bodies, beam, face state, and hazard timing reset without navigation.
9. Pause and resume; confirm time and physics stop while paused.
10. Repeat input using Left/Right or A/D and Enter/Space.
11. Check browser console for uncaught errors.

## Calibration matrix

1. Gentle, 5 creatures: the first sampled force stays below every possible Normal gust.
2. Normal, 5 creatures: first force arrives in 2.2–3.8 seconds; holding the counter direction survives the full gust while doing nothing loses soon after it.
3. Wild, 5 creatures: the first gust arrives sooner, lasts longer, and samples above every possible Normal gust.
4. Change the count to 3, 4, and 5; confirm the preview and physical stack rebuild immediately, the minus button disables at three, and stored lower values clamp to three.
5. Record a best score, change one setup dimension, and confirm the displayed best changes with it.
6. Lose a run and press Retry; confirm the selected setup is preserved.
7. Lose a run and press Change Setup; confirm the settings return without reloading.

## Collapse matrix

1. Normal motion: failure begins at time scale 1, first impact starts a short slow-motion beat, and physics returns to 1 before results.
2. Three creatures: all bodies can receive independent dazed faces and reach results.
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

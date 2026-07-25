# Wobble Stack web prototype test plan

## Automated checks

From the repository root:

```sh
pnpm install --frozen-lockfile
pnpm test
pnpm build
```

Assertions cover:

- every gust samples force, rest, and duration inside one bounded profile;
- the squared continuous force sample favors ordinary wind while still reaching
  exact soft and strong bounds;
- the complete force range stays inside the control limit;
- correct platform angle reduces effective gust acceleration for the whole stack, while wrong angle increases it;
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

1. Confirm setup contains creature count only; there is no difficulty control or
   wind meter.
2. Sample several seeded gusts and confirm force varies independently inside
   the single range rather than increasing with elapsed time.
3. Compare weakest and strongest gusts: streak density, speed, length, opacity,
   and physical response must be clearly different without a HUD label.
4. For the strongest gust, record neutral, correct, and wrong input from the
   same setup. Correct must survive longest; neutral and wrong must still fail.
5. Change the count to 3, 4, and 5; confirm the preview and physical stack rebuild immediately, the minus button disables at three, and stored lower values clamp to three.
6. Record a best score, change creature count, and confirm the displayed best changes with it while an existing Normal score remains available.
7. Lose a run and press Retry; confirm the selected count is preserved.
8. Lose a run and press Change Setup; confirm the settings return without reloading.

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

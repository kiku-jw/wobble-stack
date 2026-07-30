# Wobble Stack browser edition test plan

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
- the music shuffle includes all four tracks exactly once per cycle and avoids
  an immediate repeat at the cycle boundary.
- direct pointer support reaches at least 34 of 44 logical pixels without
  depending on gust state;
- peak-travel gesture classification separates short taps from drags and holds;
- the jump arc returns from zero through one bounded peak to zero;
- obstacle clearance and Telegram-context detection stay deterministic.

## Browser smoke

1. Open the development build at 390 × 844.
2. Press Play and confirm the route's starting bodies settle on the beam.
3. Drag left and right before the first gust; confirm the wheel uses most of its
   visible range, the beam follows without page scrolling, and release
   recenters.
4. Short-tap the canvas; confirm wheel, beam, and stack jump. Drag away and back
   before releasing; confirm that gesture does not jump.
5. Clear one authored obstacle and deliberately hit another. Confirm each
   resolves once. A clear must keep progress moving with no impact lift. A hit
   must stall at the obstacle, lift and tilt the platform, physically displace
   the stack, and remain recoverable rather than causing an instant scripted
   failure.
6. Wait for the first wind streaks; confirm they move with the push and increase in number, speed, opacity, and force without an arrow pill.
   Compare equal time windows during attack: horizontal distance must be smallest near onset, larger while building, and largest at peak.
7. Deliberately collapse the stack; confirm bodies fall at normal speed, slow only on first ground impact, and a face changes on impact.
8. Confirm dust appears at impact and the dazed face remains visible before the result overlay.
9. Confirm results appear within the hard cinematic timeout even if no creature reaches the floor.
10. Press Retry; confirm score, bodies, beam, face, jump cooldown, and obstacle
    outcomes reset without navigation.
11. Pause and resume; confirm time and physics stop while paused.
12. Repeat steering using Left/Right or A/D and jumping using Space, Arrow Up,
    or W.
13. Open `?from=telegram` at 320 × 700. Confirm the notice, two 44-pixel
    actions, copy failure fallback, “Play here anyway”, and no overflow.
14. From a clean browser profile, confirm Music reads 50% and no MP3 is
    requested before Play.
15. Press Play and confirm exactly one shuffled track starts. Pause must pause
    it; Resume must continue it.
16. Change Music in either menu, reload, and confirm both controls restore the
    same saved value.
17. Check browser console for uncaught errors.

## Calibration matrix

1. Confirm the route selector contains no difficulty control or wind meter.
2. Sample several seeded gusts and confirm force varies independently inside
   the single range rather than increasing with elapsed time.
3. Compare weakest and strongest gusts: streak density, speed, length, opacity,
   and physical response must be clearly different without a HUD label.
4. For the strongest gust, record neutral, correct, and wrong input from the
   same setup. Correct must survive longest; neutral and wrong must still fail.
5. Confirm Orchard begins with three friends and its two safe stops add Rabbit
   and Jelly; Cloud and Windmill begin with five.
6. Compare small, half, and full drags between gusts. Travel must be direct,
   monotonic, and bounded; full drag must not exceed 44 logical pixels.
7. Confirm route obstacle counts remain `1/2/3` and that Retry clears all prior
   clear/hit outcomes.

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

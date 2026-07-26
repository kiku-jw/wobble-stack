# Wobble Stack browser edition status

Updated: 2026-07-26

## Current state

- Phase: public browser release candidate
- Canonical task: [GitHub Issue #4](https://github.com/kiku-jw/wobble-stack/issues/4)
- Distribution: [GitHub Pages](https://kiku-jw.github.io/wobble-stack/)
- Runtime: Vite, Canvas, Matter.js, no backend
- Blockers: production deployment and live smoke remain

## What changed

- Replaced the setup-driven greybox with the current finite-journey game loop.
- Reused the owned iPhone clay art as lightweight transparent browser sprites.
- Added calm, panic, and distinct impact poses for all five friends.
- Added a grounded rolling star wheel and relative pointer control. The wheel
  visibly travels 44 logical pixels beneath the plank and recenters on release.
- Added Orchard Road, Cloud Bridge, and Windmill Hill with authored lengths,
  7/8/9 badges, 1/2/3 bumps, and route-specific scenery.
- Orchard Road begins with Pear, Cube, and Bird; Rabbit and Jelly join at the
  two authored safe stops.
- Added local route unlocks and best badge counts, route selection, reset,
  next-route, Retry, pause, and refresh-safe state.
- Kept one variable wind profile with blue world-space telegraph streaks and no
  difficulty or strength HUD.
- Kept Matter.js bodies and weak friendship links during play. Collapse releases
  the links, falls freely, shows comic impact art, emits dust/stars, and slows
  only at first ground contact.
- Removed prototype language, creature-count setup, survival-time scoring, and
  non-final presentation from the public page.

## Local verification

- `pnpm test`: 14/14 passing.
- `pnpm build`: passing production build.
- Production preview console: 0 errors, 0 warnings.
- Real pointer drag moved support to `43.80`, changed plank angle to `-0.196`,
  then released to support `0.03` and angle `-0.009`.
- Correct deterministic gust control remained playing through 20 seconds,
  reached 81% of Orchard Road, and collected six badges; neutral play had
  already failed near the first third of the route.
- Pause/resume, Retry, first-impact reactions, route finish/unlock, reduced
  motion, 320 × 700, 390 × 844, and 1280 × 900 passed browser checks.

## Next

- Push the verified revision to `main`.
- Wait for the Pages workflow and repeat a clean live smoke on the exact commit.
- Share the Pages URL with testers and use their feedback to tune feel, not to
  add meta systems before the core loop earns Retry.

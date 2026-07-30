# Wobble Stack browser edition status

Updated: 2026-07-30

## Current state

- Phase: published phone-first browser game
- Canonical task: [GitHub Issue #4](https://github.com/kiku-jw/wobble-stack/issues/4)
- Distribution: [GitHub Pages](https://kiku-jw.github.io/wobble-stack/)
- Runtime: Vite, Canvas, Matter.js, no backend
- Release evidence: recorded in the canonical Issue
- Human acceptance gate: physical iPhone feel and Telegram-client behavior

## Current game

- Orchard Road, Cloud Bridge, and Windmill Hill have authored lengths, badges,
  safe stops, scenery, and `1/2/3` visible road obstacles.
- Orchard begins with Pear, Cube, and Bird; Rabbit and Jelly join at the two
  safe stops.
- Relative one-thumb drag directly moves the wheel through `84%` of its
  44-logical-pixel support range during and between gusts, then recenters.
- A short stationary tap jumps the wheel, plank, and living stack. A drag never
  becomes a jump on release.
- Clearing an authored bump gives restrained success feedback. Hitting it
  briefly stalls forward travel, lifts and tilts the platform, and physically
  disturbs the stack instead of allowing an ordinary drive-through or forcing
  an automatic loss.
- Known Telegram contexts and the `from=telegram` share link receive honest
  instructions, copy-link support, and a non-blocking route to keep playing.
- Pointer capture is best-effort, release is handled at window level, and
  browsers without Pointer Events receive a touch fallback.
- Route unlocks, best badge counts, Retry, pause, keyboard controls, reduced
  motion, and local music preferences remain intact.
- Four owner-supplied parade tracks play through a non-repeating shuffled bag
  after the player starts the game.

## Local verification

- `pnpm test`: 23/23 passing, including held-key repeat rejection and the
  deterministic obstacle-impact response.
- `pnpm build`: passing production build.
- `git diff --check`: passing.
- A real full drag at 390 × 844 held support at `36.42/36.96`; release returned
  it to `0.40` while the run remained active.
- A real short tap reached `47.97` pixels during the jump. A drag away and back
  produced no jump.
- Deterministic first-obstacle checks recorded exactly one `hit` without a jump
  and exactly one `cleared` outcome at `39.15` pixels of jump height.
- Follow-up impact proof measured a `0.52 s` progress pause, `15.98 px` maximum
  platform lift, `0.0839 rad` maximum tilt, and `27.60 px` maximum creature
  displacement while the run remained playable. A jump clear measured zero
  impact lift and zero progress pause.
- At 320 × 700 the Telegram notice had no overflow, both actions were 44 pixels
  high, copy-link succeeded, and “Play here anyway” dismissed the notice.
- Normal launch did not show the Telegram notice. Pause/resume and Retry passed;
  Retry reset jump height and obstacle outcomes.
- 320 × 700, 390 × 844, and 1280 × 900 had no document overflow. Browser console:
  0 errors, 0 warnings.

## Next

- Test the feel on a physical iPhone and inside Telegram, then tune from observed
  play rather than adding a larger progression system.

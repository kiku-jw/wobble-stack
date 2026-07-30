# Wobble Stack browser edition status

Updated: 2026-07-30

## Current state

- Phase: privacy-first public-link playtest
- Canonical task: [GitHub Issue #4](https://github.com/kiku-jw/wobble-stack/issues/4)
- Distribution: [GitHub Pages](https://kiku-jw.github.io/wobble-stack/)
- Runtime: Vite, Canvas, Matter.js, first-party Cloudflare Worker + EU D1
- Release evidence: recorded in the canonical Issue
- Human acceptance gate: physical iPhone feel and Telegram-client behavior
- Primary share link:
  `https://kiku-jw.github.io/wobble-stack/?from=telegram&playtest=TG1&source=telegram`
- Primary cohort: the first 20 anonymous browser installations to start a
  round. The earlier three-friend test is qualitative context, not part of the
  measured cohort.

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
- The public-link mode asks before sending anonymous round summaries. Private
  play stores no participant UUID and makes no collector request.
- Joined play measures rounds, finish/loss, Retry, duration, progress, badges,
  jumps, obstacle hits/clears, source, and local day. It does not store names,
  Telegram IDs, messages, user agent, IP, or precise controls.
- Failed delivery stays in a bounded local outbox. Players can delete their
  telemetry; the cohort slot remains empty so later players cannot replace
  someone who withdrew.

## Local verification

- `pnpm test`: 58/58 passing, including collector validation, fixed first-20
  cohort slots, offline delivery, deletion, and Telegram handoff.
- `pnpm build`: passing production build with the live collector endpoint.
- `pnpm worker:check`: passing Worker bundle and binding validation.
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
- The local end-to-end collector accepted start, finish, Retry, and jump
  summaries; offline play stayed active with one pending event and delivered it
  after reconnect. Deletion removed events and anonymized the occupied slot.
- The live Worker health check, exact-origin CORS, foreign-origin rejection,
  EU D1 migration, and 30-day retention trigger are active.

## Next

- Share the primary link, wait for 20 starters, then evaluate whether at least
  12 reach five rounds and at least 6 return on another local day before adding
  larger progression.

# Issue 4 — public web release

Status: Frozen for implementation
Frozen: 2026-07-26
Canonical issue: `kiku-jw/wobble-stack#4`

## Owner request

Replace the public greybox browser prototype with a real web edition of the
current Wobble Stack game so the owner can send one URL to more testers.

The web edition is a distribution surface for the accepted game direction,
not a second product. It should communicate the same story, visual identity,
one-thumb wheel control, finite journeys, wind balance, character comedy, and
fast Retry as the iPhone candidate.

## Acceptance criteria

### AC1 — Production presentation

- The live page no longer calls itself a prototype, greybox, toy, or
  non-final build.
- The stage uses the current clay character, route, road, plank, wheel, and
  world art already owned by the iPhone project.
- The tower, grounded wheel/plank, moving world, and minimal HUD retain the
  same visual priority as the iPhone candidate.
- Loading has an explicit state and does not expose magenta chroma-key sheets.

### AC2 — Wheel journey control

- Pointer control is relative to touch-down; keyboard uses Left/Right or A/D.
- Input visibly moves the star wheel at least 28 CSS pixels left or right
  beneath the plank and changes the plank response.
- Releasing input smoothly recenters the support and resumes forward travel.
- Road, badges, bumps, foreground, and landmarks move left for a journey that
  reads as rightward.
- The wheel remains visually on the road throughout ordinary play.

### AC3 — Playable wind balance

- One continuously varied gust profile remains, with no difficulty selector
  or wind-strength HUD.
- Blue streaks build before and with physical force.
- On the same deterministic first gust, correct wheel travel survives longer
  than neutral or wrong travel; wrong or absent play remains fallible.
- No direct hidden auto-save or invulnerable tower is added.

### AC4 — Three finite routes

- Orchard Road, Cloud Bridge, and Windmill Hill use their authored lengths,
  `7/8/9` badges, `1/2/3` road bumps, and distinct badge movement.
- Orchard Road begins with Pear, Cube, and Bird; Rabbit and Jelly join at the
  two authored safe stops.
- Completing a route stores its best badge count and unlocks the next route.
- Route selection, completion, next-route, Retry, and reset behavior work
  without accounts or a backend.

### AC5 — Character and collapse comedy

- Five clay characters have distinct silhouettes and calm, panic, and comic
  impact art.
- Bodies remain Matter.js physics bodies with dense stack contact and weak
  friendship constraints during play.
- A collapse releases the constraints, falls freely, changes each impacted
  creature to its own impact pose, emits dust/stars, and slows only around the
  first ground impact.
- Results wait long enough for at least one impact reaction to be readable,
  and Retry fully resets reactions.

### AC6 — Public tester ergonomics

- The game is usable at 320 × 700, 390 × 844, and a desktop viewport.
- Primary controls have at least 44 CSS-pixel targets, visible keyboard focus,
  and meaningful accessible names/status.
- Pause/resume, page-hidden pause, reduced motion, local storage failure, and
  browser refresh are safe.
- No analytics, account, ads, service worker, install prompt, new runtime
  dependency, or tester form is added.

### AC7 — Release proof

- `pnpm test` and `pnpm build` pass with deterministic route/control helpers.
- A fresh local production preview passes Playwright start, control,
  pause/resume, collapse/impact, Retry, route-finish, responsive-layout, and
  zero-console-error checks.
- GitHub Pages deploys the exact verified `main` revision.
- The production URL serves the new art and no longer contains the old
  prototype copy.

## Constraints

- Reuse the existing Vite, Canvas, Matter.js, storage, input, wind, failure,
  and debug surfaces.
- Reuse and derive public web sprites only from repository-owned iPhone art.
- Prefer committed lightweight web-ready crops over shipping full chroma-key
  source sheets or introducing a runtime image-processing dependency.
- Keep GitHub Pages as the existing deployment mechanism.
- The web edition may approximate native joint physics, haptics, and sound,
  but must preserve the visible control/outcome contract for tester feedback.

## Verification plan

1. Unit-test pure route catalog, badge motion, support travel, and progress
   rules.
2. Run the production build and inspect output asset paths/sizes.
3. Exercise deterministic debug hooks in a local production preview through
   Playwright at the three target viewports.
4. Capture ready, journey, impact, and finish screenshots.
5. Deploy through the existing Pages workflow and repeat the live smoke.

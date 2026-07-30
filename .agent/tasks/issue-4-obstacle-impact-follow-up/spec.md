# Issue 4 — consequential web obstacle impacts

Status: Frozen
Frozen: 2026-07-30
Canonical issue: `kiku-jw/wobble-stack#4`

## Owner acceptance failure

Driving into a visible obstacle currently feels like ordinary road travel.
Because a grounded hit is not meaningfully different from a jump clear, the
jump decision is optional.

## Acceptance criteria

### AC1 — A grounded hit is unmistakable

- Crossing an authored bump below clearance records exactly one `hit`.
- Forward progress pauses at the obstacle for at least `0.35 s`.
- The wheel and platform visibly lift at least `12 px`.
- The platform reaches at least `0.06 rad` of impact tilt and the living stack
  receives lateral and rotational motion.
- The impact remains recoverable; it does not force an immediate scripted loss.

### AC2 — A valid jump avoids the penalty

- Crossing the same obstacle above clearance records exactly one `cleared`.
- A clear adds no obstacle-impact lift and no obstacle progress pause.
- Forward progress continues through the crossing.

### AC3 — Existing lifecycle remains correct

- Authored `1/2/3` bump positions remain the only obstacle source.
- Each obstacle resolves once per run.
- Retry resets obstacle outcomes, impact motion, and pause state.
- Pause, collapse, route completion, steering, and jump cooldown remain intact.

### AC4 — Verification and release

- Deterministic tests cover the obstacle hit response profile.
- `pnpm test`, `pnpm build`, and `git diff --check` pass.
- A production preview at 390 × 844 proves the measured hit/clear difference,
  remains playable, and reports zero console errors.
- A fresh read-only verifier returns PASS.
- The verified revision reaches `origin/main`, the existing Pages workflow
  succeeds, and a fresh live smoke proves the public revision.

## Constraints and non-goals

- Keep the current Vite, Canvas, Matter.js, route catalog, bump art, and debug
  hook.
- Do not add collision bodies, a new route schema, a dependency, a second jump
  control, damage points, or an automatic loss.
- Do not modify the Unity client.

## Verification plan

1. Unit-test the deterministic hit response direction and strength.
2. Use the debug hook to cross Orchard's first bump without and with a jump,
   sampling progress, impact lift, tilt, outcomes, and game state.
3. Exercise Retry and inspect the production-preview console.
4. Run a fresh read-only verifier, publish safely, monitor Pages, and repeat the
   hit/clear smoke against the public build.

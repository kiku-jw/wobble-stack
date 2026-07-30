# Issue 4 — obstacle impact follow-up evidence

Date: 2026-07-30
Target: local production preview at 390 × 844

## Automated checks

- `pnpm test`: PASS, 23 tests, 0 failures.
- `pnpm build`: PASS.
- `git diff --check`: PASS.
- Production preview console: 0 errors, 0 warnings.

## Grounded hit

Orchard's first authored bump was crossed at ground height:

- outcome: exactly `[[0, "hit"]]`
- hit progress: `19.00337`
- maximum progress pause: `0.52 s`
- maximum impact lift: `15.9799 px`
- maximum absolute platform angle: `0.08387 rad`
- maximum creature displacement: `27.5983 px`
- progress after 850 ms: `19.48999`
- final state: `playing`

The wheel paused over the bump, the platform and wheel lifted visibly, and the
stack moved laterally while remaining recoverable.

## Jump clear

The same bump was crossed after the jump exceeded clearance:

- outcome: exactly `[[0, "cleared"]]`
- clear progress: `19.00249`
- maximum impact lift: `0`
- maximum impact pause: `0`
- progress after 500 ms: `19.75731`
- final state: `playing`

## Lifecycle

After a deliberate collapse, Retry returned to `playing` with:

- pause: `0`
- impact height: `0`
- impact active: `false`
- obstacle outcomes: `[]`

## Remaining release steps

Fresh read-only verification, Git publication, Pages workflow, and public smoke
remain pending.

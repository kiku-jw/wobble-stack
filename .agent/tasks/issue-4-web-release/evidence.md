# Issue 4 — public web release evidence

Date: 2026-07-26

## Automated checks

- `pnpm test`: 14/14 passing.
- `pnpm build`: Vite production build passing.
- Output: HTML `6.09 kB`, CSS `9.32 kB`, JavaScript `112.85 kB`; committed
  browser art is approximately `428 kB`.
- `git diff --check`: clean.
- Production preview console: 0 errors, 0 warnings after the startup guard fix.

## Browser behavior

Playwright production-preview session: `web-port-local`.

- Ready/start, Retry, route selection, Pause/Resume, route finish, next route,
  refresh, and reset controls were exercised.
- A real pointer drag from canvas center toward the right moved the support to
  `43.80` and the plank to `-0.196`. Release returned the support to `0.03` and
  the plank to `-0.009`.
- A real `ArrowRight` input during a deterministic rightward `0.0001` gust
  moved support to `10.10` and the plank to `-0.051`; release recentered both
  while the run remained `playing`.
- Dynamic correct counter-control remained `playing` through 20 seconds,
  reached Orchard progress `30.74 / 38` (`81%`), added both friends, and
  collected six badges. Neutral play had already reached results near progress
  `12.5 / 38`.
- Forced collapse released the stack, registered distinct impact reactions,
  showed impact art, dust and stars, and then presented Retry.
- Forced Orchard finish stored the badge best and unlocked Cloud Bridge. The
  next-route control started Cloud Bridge with five friends and `0 / 8`.
- Windmill Hill rendered nine-badge route state and a grounded festival
  arch/windmill at completion.

## Layout and accessibility

- `320 × 700`: document `320 × 700`, no overflow. Visible interactive targets
  measured `61`, `61`, `61`, `47`, and `44` CSS pixels high.
- `390 × 844`: full gameplay, impact, and finish captures inspected.
- `1280 × 900`: centered `415.88 × 899.98` game frame, no document overflow.
- Route choices remain semantic buttons with selected/locked states.
- Canvas and buttons have visible keyboard focus and meaningful accessible
  labels/status.
- Reduced-motion collapse reached results safely with all five impact reactions.

## Visual artifacts

- `artifacts/menu-390x844.png`
- `artifacts/journey-390x844.png`
- `artifacts/live-journey-390x844.png`
- `artifacts/impact-390x844.png`
- `artifacts/finish-390x844.png`
- `artifacts/menu-320x700.png`
- `artifacts/windmill-grounded.png`

## Independent review

Strong-tier read-only adversarial review: no findings and no release blocker.
Residual gaps were limited to build/live proof unavailable inside its
read-only, network-disabled environment and the deliberate absence of a
checked-in Playwright dependency.

## Deployment

- Implementation commit: `a4f0091` (`feat: ship the full browser journey`).
- GitHub Pages workflow
  [`30199823677`](https://github.com/kiku-jw/wobble-stack/actions/runs/30199823677)
  completed successfully for the implementation commit.
- Fresh live session opened
  `https://kiku-jw.github.io/wobble-stack/?debug&release=a4f0091`.
- New-user state exposed only Orchard Road; Cloud Bridge and Windmill Hill were
  locked.
- Live real drag moved support to `43.80` and plank to `-0.196`; release
  returned support to `0.08` while state remained `playing`.
- Live `route-sky.webp`, `wheel.webp`, and `pear-calm.webp` loaded from the
  Pages asset path; direct sky request returned HTTP `200`.
- Live document contained none of `prototype`, `greybox`, `physics toy`, or
  `nothing here is final`.
- Live console: 0 errors, 0 warnings.

# Wobble Stack web prototype plan

Date: 2026-07-22

## Goal

Prove whether the one-thumb loop feels readable and retryable in a browser before committing to a native production build.

## Scope

- Standalone portrait prototype in this repository.
- Five physical bodies on one controllable beam.
- Pointer, touch, and keyboard input.
- Telegraph-first wind gusts, survival time, best score, collapse, pause, and Retry.
- No route, navigation item, analytics, account, backend, shop, progression, or final art.

## Milestones

### M1 — shell and reference lock

Acceptance:

- Prototype runs independently from any site or service.
- Reference decisions are recorded in `IMPLEMENTATION_NOTES.md`.

Verify:

```sh
pnpm install --frozen-lockfile
pnpm build
```

### M2 — playable loop

Acceptance:

- Play starts without a page load.
- Drag or arrow keys tilt the beam.
- Gusts show a warning before force is applied.
- A fallen creature ends the run and Retry resets it without navigation.

Verify:

```sh
pnpm test
pnpm dev
```

### M3 — browser proof

Acceptance:

- The full loop works at 390 × 844 and desktop sizes.
- No uncaught console errors.
- Controls remain keyboard accessible and reduced-motion is respected.
- Retry becomes actionable within one second of failure.

Verify with the repository Playwright CLI workflow and a mobile screenshot.

## Kill gate

This prototype earns a native greybox only if several fresh players voluntarily press Retry and can explain why they lost. Visual polish alone is not a pass.

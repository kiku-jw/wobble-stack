# Wobble Stack web prototype plan

Date: 2026-07-22

## Goal

Prove whether the one-thumb loop feels readable and retryable in a browser before committing to a native production build.

## Scope

- Standalone portrait prototype in this repository.
- Three to five physical bodies on one controllable beam.
- Pointer, touch, and keyboard input.
- Self-telegraphing wind gusts, survival time, best score, collapse, pause, and Retry.
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
- Gusts visibly build before reaching peak force.
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

### M4 — readable wind and configurable runs

Acceptance:

- Gentle, Normal, and Wild use explicit force curves rather than one hidden multiplier.
- Early Normal wind needs roughly 3.5 degrees of counter-tilt; late Normal remains within the 26-degree control range.
- Every gust eases in before reaching peak force.
- The stage gives a readable direction cue before peak force.
- Setup supports 3–5 creatures and rebuilds a contact-safe stack before play.
- Best time is isolated by difficulty and creature count.
- Retry keeps the current setup; Change Setup returns to the start controls.

Verify:

```sh
pnpm test
pnpm build
```

Then run the browser matrix in `TEST_PLAN.md` and verify the deployed GitHub Pages build.

### M5 — cinematic collapse

Acceptance:

- Failure releases the soft stack links and continues physics at a visibly slower rate.
- Each creature keeps its panic face while falling and switches to a distinct dazed face only after touching the catch floor.
- Ground contact emits a restrained dust burst and keeps the reaction visible before results appear.
- A hard timeout prevents the cinematic from blocking Retry when no body reaches the floor.
- Reduced-motion shortens the sequence and uses near-normal physics speed while preserving the face reaction.

Verify:

```sh
pnpm test
pnpm build
```

Then use the debug collapse trigger at 390 × 844 to capture the falling and impacted states, repeat with reduced motion, and smoke the deployed Pages build.

### M6 — honest wind and impact timing

Acceptance:

- Every gust samples a random force inside a non-overlapping Gentle, Normal, or Wild range; elapsed run time does not increase force.
- Normal begins within 2.2–3.8 seconds and requires visible input during the first gust.
- Longer gusts progressively build both Canvas wind intensity and physical force without a separate direction pill.
- Holding the correct keyboard direction keeps five creatures on the beam through a calibrated Normal gust.
- Setup exposes only 3–5 creatures and clamps old stored values below three.
- Failure falls at normal speed, slows briefly on first ground impact, and returns to normal speed before results.

Verify:

```sh
pnpm test
pnpm build
```

Then run the difficulty, impact, reduced-motion, and 320 × 700 checks in `TEST_PLAN.md` before verifying the deployed Pages build.

## Kill gate

This prototype earns a native greybox only if several fresh players voluntarily press Retry and can explain why they lost. Visual polish alone is not a pass.

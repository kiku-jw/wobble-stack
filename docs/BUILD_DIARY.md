# Build diary

## Status

- artifact type: playable demo repository
- current phase: M6 published and live
- public-safe gate: passed
- demo: https://kiku-jw.github.io/wobble-stack/
- review status: live Pages build verified; human playtest pending

## Checklist

- [x] Questions: prove whether the one-thumb balance verb is understandable.
- [x] Evidence: tests, production build, and mobile browser loop passed.
- [x] Related assets: real gameplay capture and two labeled concept frames.
- [x] Brief: small public repository with a direct playable link.
- [x] Draft: README grounded in the running prototype.
- [x] Critique: no usage, fun, or retention claims were invented.
- [x] Packaging: MIT license, repository metadata, and automated Pages deploy.
- [x] Publish proof: GitHub Pages deployment and public browser smoke passed.

## Evidence

- Configurable three-to-five-body Matter.js stack with touch, mouse, and keyboard input.
- Three explicit random wind profiles, long attack/hold/release gusts, code-native direction cue, collapse, pause, scoped local best score, and reset without navigation.
- Seven deterministic logic tests.
- Browser checks at 320 x 700, 390 x 844, and 1280 x 900.
- Retry became actionable within one second during the prototype QA run.
- An untouched Normal stack survived its first calibrated gust through 11.5 seconds.
- Holding the instructed keyboard direction kept all five figures upright and recovered the tower by 11.9 seconds.
- The public page, JavaScript bundle, and CSS returned HTTP 200 from GitHub Pages.
- The live 390 x 844 game started successfully with zero console errors or warnings.
- Pages run `29948112671` deployed commit `b4c0ea5`; the live build accepted Gentle with three creatures, entered play, and paused at 0.4 seconds without console errors.
- M5 browser proof: normal collapse stayed in slow-motion failure at 0.8 seconds, registered four independent impact reactions, and exposed Retry at about 1.0 second.
- Reduced-motion preserved crossed-eye impact faces and exposed Retry in about 0.3 seconds.
- No-impact failure stayed cinematic at 2.3 seconds and reached the 2.6-second hard timeout without hanging.
- M6 browser calibration sampled first gusts at 4.52 seconds / `0.0000418` (Gentle), 3.37 seconds / `0.0000919` (Normal), and 2.25 seconds / `0.0001268` (Wild).
- An untouched Normal run collapsed at 9.0 seconds; holding the correct direction kept five figures upright through the full first gust at 7.75 seconds.
- A natural untouched Normal collapse entered failure at time scale `1`, fell for about 1017 ms before ground contact, then slowed to `0.18` for 360 ms and returned to `1`; the deterministic debug scatter hit the floor in 83 ms and exposed results after the 900 ms reaction hold.
- Reduced-motion on 320 × 700 used `0.86` for 100 ms at impact, returned to `1`, kept three independent reactions, and produced no horizontal overflow.
- A stored or debug count below three clamped to three and disabled the minus control; the old one- and two-body best-score keys remain untouched.
- The arrow pill no longer exists in the DOM; the wind envelope drove measured visual intensity from `0.013` near onset to `0.808` at Normal peak.
- Nine deterministic tests, production build, and browser console checks pass with zero errors.
- Pages run `29953856192` deployed commit `c4ffa84`; live debug smoke confirmed the 3-creature floor, random Normal timing/force, missing arrow pill, `1 → 0.18 → 1` failure time scale, and zero console errors.
- Wind-speed regression QA measured equal 250 ms windows at about 17 px near onset, 53 px while building, and 70 px at peak (`69 → 213 → 279 px/s`). The fix changes only Canvas travel integration; gust physics and timing are untouched.
- Pause held accumulated wind travel at exactly zero delta across 500 ms; 10 deterministic tests, production build, and browser console checks passed.
- Pages run `29954743370` deployed commit `52930eb`; live 390 × 844 sampling measured `18.6 → 65.7 → 77.4 px` across equal 250 ms windows (`91 → 283 → 310 px/s`) while the game remained playable and the console stayed clean.
- Eight deterministic tests, production build, and browser console checks passed.
- Pages run `29949106957` deployed commit `5404501`; the live 390 × 844 collapse registered multiple dazed faces at 0.8 seconds and exposed Retry with zero console errors.

## Exclusions

- No account, backend, network API, analytics, ads, progression, or shop.
- Concept art is labeled as direction rather than current gameplay.
- No claim that the mechanic is fun until fresh-player evidence exists.

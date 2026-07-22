# Wobble Stack web prototype status

Updated: 2026-07-22

## Current state

- Phase: calibrated public build
- Current milestone: M5 — cinematic collapse live
- Blockers: none

## Completed

- Standalone repository boundary chosen; no external site or product registry is required.
- Concept references and product constraints converted into a narrow browser target.
- Shell, responsive UI, pure timing helpers, and test scaffold created.
- Five-body Matter.js stack, target-angle control, wind telegraph, pause, score, collapse, and Retry implemented.
- Pointer and keyboard controls verified in Chromium at 390 × 844.
- Retry became visible in 796 ms; pause held the run clock at a zero delta.
- Layout inspected at 320 × 700, 390 × 844, and 1280 × 900.
- Reduced-motion and touch-action behavior verified; browser console remained clean.
- Unit tests and production build passed.
- Public repository metadata, MIT license, topics, preview assets, and README published.
- GitHub Pages build and deploy jobs passed; the live game loaded its HTML, JavaScript, and CSS with HTTP 200 and no console errors.
- Gentle, Normal, and Wild now have separate calm periods, force ramps, cadence, and gust durations.
- Gust force uses a slow attack, short hold, and release; the cue states both wind and lean direction.
- The start screen rebuilds a 1–5 creature preview immediately and saves the last setup.
- Best scores are isolated by difficulty and creature count; the old score migrates to Normal with five creatures.
- The beam is now a bounded player-controlled surface, so the stack cannot torque it past the requested angle.
- Soft contact links and higher air damping keep the figures acting like one readable tower while preserving collapse.
- In browser calibration, an untouched Normal stack survived the first gust through 11.5 s.
- Holding the instructed arrow through that gust kept all five figures upright and recovered to near-center by 11.9 s.
- Settings, scoped best scores, Retry persistence, Change Setup, pause, reduced motion, and 320 × 700 layout passed browser checks with no console errors.
- Seven deterministic logic tests and the Vite production build pass.
- GitHub Pages run `29948112671` built and deployed commit `b4c0ea5`; the live setup, three-creature Gentle run, pause state, and bundled assets were re-verified with no browser console errors.
- Failure now releases the soft stack links and continues Matter.js physics at `0.26` time scale; reduced-motion uses `0.82`.
- Catch-floor collisions switch each creature independently from panic to a dazed crossed-eye face and emit a small dust burst.
- Results wait 900 ms after the first normal-motion impact, 180 ms with reduced motion, or use a 2600/1100 ms hard timeout when no impact arrives.
- Browser QA captured distinct falling and impacted frames at 390 × 844 and a legible five-creature impact frame at 320 × 700.
- One- and five-creature collapse, Retry reaction reset, reduced motion, no-impact timeout, and zero-console-error checks passed.
- Eight deterministic logic tests and the Vite production build pass.
- GitHub Pages run `29949106957` deployed commit `5404501`; the live five-creature collapse stayed in slow motion at 0.8 s, showed independent impact reactions, reached Retry, and logged no console errors.

## Next

- Put the updated build in front of fresh players.

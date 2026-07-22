# Wobble Stack web prototype status

Updated: 2026-07-22

## Current state

- Phase: calibrated public build
- Current milestone: M6 — honest wind and impact timing live
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
- Gentle, Normal, and Wild now sample random force from non-overlapping ranges and use distinct rest and duration ranges.
- Gust force uses a long attack, hold, and release; the same envelope drives increasingly dense, fast, opaque wind streaks.
- The direction pill and opposing arrows have been removed; motion in the stage is the wind cue.
- The first Normal gust starts within 2.2–3.8 seconds, and an untouched calibrated run collapsed at 9.0 seconds instead of allowing twenty idle seconds.
- Holding the correct keyboard direction kept all five figures upright through the first Normal gust at 7.75 seconds.
- The start screen rebuilds a 3–5 creature preview immediately, clamps old lower values to three, and saves the last setup.
- Best scores are isolated by difficulty and creature count; the old score migrates to Normal with five creatures.
- The beam is now a bounded player-controlled surface, so the stack cannot torque it past the requested angle.
- Soft contact links and higher air damping keep the figures acting like one readable tower while preserving collapse.
- In browser calibration, an untouched Normal stack survived the first gust through 11.5 s.
- Holding the instructed arrow through that gust kept all five figures upright and recovered to near-center by 11.9 s.
- Settings, scoped best scores, Retry persistence, Change Setup, pause, reduced motion, and 320 × 700 layout passed browser checks with no console errors.
- Seven deterministic logic tests and the Vite production build pass.
- GitHub Pages run `29948112671` built and deployed commit `b4c0ea5`; the live setup, three-creature Gentle run, pause state, and bundled assets were re-verified with no browser console errors.
- Failure now releases the soft stack links and falls at normal time scale until ground contact.
- Catch-floor collisions switch each creature independently from panic to a dazed crossed-eye face and emit a small dust burst.
- First impact applies a 360 ms beat at `0.18` time scale; reduced motion uses 100 ms at `0.86`, then physics returns to normal before results.
- Browser QA captured distinct falling and impacted frames at 390 × 844 and a legible five-creature impact frame at 320 × 700.
- Three- and five-creature collapse, Retry reaction reset, reduced motion, no-impact timeout, and zero-console-error checks passed.
- Eight deterministic logic tests and the Vite production build pass.
- GitHub Pages run `29949106957` deployed commit `5404501`; the live five-creature collapse stayed in slow motion at 0.8 s, showed independent impact reactions, reached Retry, and logged no console errors.
- Nine deterministic logic tests and the Vite production build pass for M6.
- GitHub Pages run `29953856192` built and deployed commit `c4ffa84`; the live 390 × 844 build clamped two creatures to three, scheduled Normal at 3.37 seconds with random force `0.0000919`, removed the arrow pill, slowed only on impact, returned to normal physics before results, and logged no console errors.
- Wind-speed regression fix: Canvas streak travel now integrates frame-by-frame distance instead of multiplying a changing speed by absolute time. Local browser samples increased from about 69 px/s near onset to 213 px/s while building and 279 px/s at peak; Pause held travel at a zero delta for 500 ms, 10 logic tests and the build pass, publication pending.

## Next

- Put the updated build in front of fresh players.

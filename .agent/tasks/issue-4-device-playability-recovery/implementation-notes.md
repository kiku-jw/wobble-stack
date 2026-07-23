# Implementation notes

## Lazy-senior receipt

- lower rung: reuse the existing beam angle, gust calculation, and force loop
- GitHub prior art: skipped because this is a measured repo-local regression
- new code justified: Unity lost the web prototype's direct relationship
  between platform angle and effective gust acceleration

## Decision

- Beam angle now directly reduces or amplifies effective horizontal gust
  acceleration for every creature.
- Scalar counter-authority alone had no robust coefficient: `0.20` still lost
  the delayed Wild/five case downwind, while `0.40` could throw a strong early
  hold back against the wind.
- The final bounded helper therefore damps only current downwind velocity while
  the beam is tilted correctly and the gust is active. It becomes zero for
  neutral/wrong tilt, calm air, and any body already moving back upwind.
- It reads no tower position or rotation, never chooses a side, and cannot pull
  a body through recovery. Neutral and wrong input remain fully fallible.
- Existing jointless bodies, direct touch-to-beam mapping, gust profiles, and
  force loop remain in place.

## Spec deviations

- AC3 originally prohibited all tower-state feedback. Red/green calibration
  showed that a scalar-only force multiplier was unstable across ordinary
  thumb timings and magnitudes, so the spec now permits the narrower
  correct-input-only downwind damper described above.

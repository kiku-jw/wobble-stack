# Implementation notes

## Lazy-senior receipt

- lower rung: existing Unity 2D primitives, current atlas pipeline, and local
  tuning
- GitHub prior art: skipped because this is a repo-local physical-feel,
  sign-mapping, and asset-state regression
- new code justified: the physical iPhone playtest falsified the current
  control and verification contract

## Decisions

- Corrected the physical sign contract and reduced it to a binary, readable
  action: hold the side the wind comes from.
- Reduced the maximum beam angle and counter authority together so the full
  touch range corresponds to the actual difficulty bands instead of making the
  useful input a narrow strip around screen center.
- Kept one existing wind/panic atlas and one impact atlas, adding only a calm
  atlas rather than introducing skeletal or layered facial animation.
- Matched simple capsule/box colliders to measured non-chroma sprite bounds;
  no polygon-collider generation system was added.
- Kept the legacy `BuildIosDevelopment` batch method name for current scripts,
  but changed its iOS player output to non-Development so device tests do not
  expose Unity's console or watermark.

## Spec deviations

- The frozen AC1/AC2 wording was amended before final verification. First-gust
  ordering is measured by lower drift plus non-shorter survival, and the input
  model is named by wind source rather than destination. The implementation and
  first-run hint use the same source-side contract.

## Owner verdict

- The second physical playtest rejected the binary control and constrained
  fallability; automation had proved only bounded mechanics and presentation
  invariants.

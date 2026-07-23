# Implementation notes

## Lazy-senior receipt

- lower rung: direct scalar touch mapping, built-in Rigidbody2D/colliders, and
  existing gust/capture/device pipelines
- external prior art: skipped because the physical iPhone rejection identified
  a repo-local control and constraint regression
- new code justified: tests now cover the two missing product properties,
  direct ownership and fallability

## Decisions

- Replaced binary input and the tower-feedback controller with continuous
  normalized signed control.
- Gust strength calibrates available beam authority without reading tower
  position, rotation, or velocity. Side and amount remain player-owned.
- The useful response saturates inside the outer quarter, and a minimum
  teaching angle makes the beam response visible on the lightest wind.
- Increased maximum beam travel to make the thumb/beam relationship visible;
  the beam responds during rest rather than waiting for active wind.
- Removed the base friction joint, every neighbor hinge, and every rotation
  lock. The dynamic tower contains no `Joint2D`.
- Replaced rounded-bottom capsules with small flat-contact polygons and a tiny
  initial collider gap. This keeps the free tower stable through the teaching
  window without a hidden constraint.
- Reduced contact friction and scaled wind exposure by stack height. Wind is
  applied slightly above each center of mass so upper bodies visibly wobble and
  wrong/no input can collapse.
- Kept the first recovery's rounded beam, calm/wind/impact faces, cyan preview,
  and non-Development device build.

## Spec deviations

- None.

## Residual owner gate

- Automation holds one constant outer-side screen position and proves
  first-gust survival plus bounded failure. The physical iPhone test must still
  judge whether the control feels natural and the collapse feels fun rather
  than merely possible.

# iPhone status

Updated: 2026-07-25

## Current

- Phase: M3 rolling-world production, core vehicle gate verified locally.
- Canonical task: `kiku-jw/wobble-stack#4`.
- Next actor: Agent, implementing articulated characters and the first
  travelling route.
- Blocker: none for local production. Subjective control remains a physical
  iPhone gate; App Store distribution remains a separate Apple Developer
  Program boundary.

## Verified rolling-core outcome

- The direct-angle kinematic beam is gone from native gameplay.
- A dynamic star wheel rolls on a continuous physical road and drives a dynamic
  plank through one native `WheelJoint2D`.
- Touch displacement is relative to the initial contact point, works away from
  the bezel, and controls wheel motion rather than plank angle.
- A smoothed camera follows horizontal vehicle travel with velocity look-ahead;
  wind rendering moves with the camera.
- Correct wheel travel completes the strongest deterministic gust in both
  directions with three and five creatures. Neutral and wrong travel still
  collapse.
- A delayed broad rescue gesture also completes the strongest matrix without
  tower-state feedback or per-frame scripted correction.
- The wheel stays grounded through a strong five-friend catch, and measured
  sprite fill now matches its circular collider at the road contact.
- A generated alpha clay terrain tile replaces the temporary flat-color road.
- Current actual-physics start/gameplay captures are stored under the canonical
  proof bundle and reflected in the README screenshots.
- Unity batch compile, `13/13` EditMode, `15/15` PlayMode, and Mac Metal capture
  pass.

## Still in production

- Articulated limbs/ears/leaves/wings, weak visible grips, blink/gaze, and
  character-specific emotional behavior.
- First travelling route, parallax layers, moving clouds, windmill, badges,
  join stops, finish, and local route progression.
- Ground-impact deformation/debris and a longer readable collapse.
- Removal of obsolete creature-count/setup UI and final minimal start/results
  presentation.
- Fresh iOS export, signed install/launch, and owner feel approval after the
  next cohesive device build.

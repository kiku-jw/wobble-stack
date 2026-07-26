# iPhone test plan

## Automated foundation

- Batch project import and C# compile with Unity `6000.3.19f1`.
- EditMode covers gust sampling, preview/envelope timing, direction, input
  bounds, relative touch drive, setup bounds retained for persistence
  compatibility, and state transitions.
- PlayMode covers scene bootstrap, dense contacts, expression progression,
  blue wind direction, the single legitimate wheel joint, free creature
  rotation, calm warmup, signed wheel travel, visual/collider ground contact,
  readable plank angle, camera-follow inputs, strongest-gust catch matrices,
  neutral/wrong fallability, weak grip release, parallax, pickups, friend joins,
  complete first-route travel, finish, and interruption-safe failure.
- Current native-candidate receipt: `13/13` EditMode and `25/25` PlayMode pass.

## Deterministic rolling matrix

For the strongest force in the single wind profile:

1. Neutral establishes the failure baseline.
2. Correct steady wheel travel completes the gust in both directions with
   three and five creatures.
3. Wrong travel and neutral both collapse sooner on the same seed.
4. A broad constant mid-range input works for three; the taller five-friend
   tower requires a stronger but still constant input.
5. A second human-repeatable scenario starts neutral, allows `0.35 s` of
   physical wind, performs one full rescue swipe, then holds a fixed follow
   amount. It completes both directions and tower sizes without tower-state
   feedback.
6. The wheel's collider remains within `0.18` world units of the road throughout
   a strong five-friend catch.

The runtime never feeds a scripted target angle, body position, current tower
lean, or perfect per-frame correction into these proofs.

## Interaction matrix

- Touch-down establishes a local origin anywhere outside UI.
- Horizontal displacement maps continuously to wheel travel; the bezel is not
  a target.
- Holding displacement maintains travel. Returning near the origin brakes.
  Release resumes the gentle physical cruise and never snaps or rotates the
  plank.
- The wheel spins consistently with translation while the plank remains
  dynamic and readable.
- Camera follows local vehicle travel and preserves a useful look-ahead.
- A physical-wheel odometer advances route space while calm; balance
  corrections cannot reverse route progress and no vehicle body receives a
  scripted travel offset.
- Pause freezes time, physics, particles, and score.
- App focus loss pauses safely.
- Retry rebuilds vehicle, bodies, faces, crown, particles, wind seed, and route
  state without a scene load.

## Route and presentation automation

- Articulated appendage lag, blink/gaze variability, personality thresholds,
  visible weak grip creation, grip break, and no-immortality matrix.
- Badge trigger contact, first-route Rabbit/Jelly join stops, route finish, and
  local progression persistence.
- Cloud looping, windmill rotation, parallax ratios, camera bounds, and terrain
  seam visibility.
- Impact squash, ground dust, crown/debris arcs, result hold, finish
  celebration, and Reduced Motion semantics.

The current suite proves the runtime presence and state changes above. Fine
visual quality, seam visibility, gesture clarity, and motion comfort remain
human inspection gates rather than pixel assertions.

## Presentation matrix

- Capture title, calm travel, wheel catch, gust, badge pickup, five-friend
  stack, collapse flight, first ground impact, finish, and Retry at native
  portrait aspect.
- Compare each capture against `ART_DIRECTION.md` and the two concept frames.
- Verify wheel and road visually touch, the plank visibly rests above the
  wheel, and world layers do not obscure controls or bodies.
- Current actual-physics Metal captures are stored in
  `.agent/tasks/issue-4-rolling-world-final/raw/`.

## Device gate

The next physical-iPhone gate must verify:

- A fresh player understands touch, slide, catch, and return-to-origin without
  an explanation.
- Rolling the wheel under the lean visibly changes the outcome.
- The wheel never appears to float, skate, or penetrate on the flat first
  route.
- Soft and strong gusts read differently without a meter or direction text.
- Neutral/wrong play still creates a believable fall.
- Camera follow, touch latency, haptics, safe areas, interruption behavior,
  frame pacing, thermal behavior, and voluntary Retry feel acceptable.

Articulation, route content, minimal UI, and final presentation have automated
and inspected-capture receipts. The fresh signed device run and owner feel
verdict remain mandatory before release.

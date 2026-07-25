# Issue 4 — single mode with variable gusts

Status: Finalized
Frozen: 2026-07-25
Canonical issue: `kiku-jw/wobble-stack#4`

## Owner decision

The physical iPhone build at `5cf4416` is the first build where a normal beam
correction can survive a gust. Remove player-selectable difficulty. Keep one
Normal-calibrated game mode, but let individual gusts have meaningfully
different intensity. Do not add an intensity HUD.

## Acceptance criteria

### AC1 — One game mode

- The Unity start screen and web setup contain no Gentle/Normal/Wild control.
- Runtime scheduling does not accept or persist a selected difficulty.
- Creature count and reduced-motion setup remain available.

### AC2 — Gusts vary without hidden mode progression

- Every gust independently samples force from one bounded continuous range.
- The range includes soft, ordinary, and occasional strong gusts while staying
  centered near the accepted Normal feel.
- Seeded scheduling remains deterministic.
- Elapsed time and score do not increase gust strength.

### AC3 — Intensity is shown by the world, not a HUD

- The web wind meter and its label/bars are removed.
- Wind streak density/speed/opacity, wind audio, faces, and physical response
  continue to scale with the sampled force and attack/hold/release envelope.
- No Soft/Medium/Hard name, number, bar, or badge is introduced.

### AC4 — Accepted control and fallability survive the simplification

- A neutral start followed by one delayed, imprecise correct hold completes
  the strongest possible gust in both directions with three and five creatures.
- Neutral and wrong-direction input still collapse under the strongest gust.
- The weakest gust is visually distinct from the strongest gust.
- No joint, grip, tether, rotation lock, positional auto-balance, or automatic
  side choice is introduced.

### AC5 — Scores and delivery remain coherent

- Existing Normal best scores remain readable after removing difficulty.
- Web tests/build and Unity EditMode/PlayMode pass.
- A fresh iPhone build is exported, signed, installed, and launched.
- Physical feel remains an owner gate.

## Constraints

- Keep the accepted `5cf4416` beam authority and downwind-only damper.
- Add no dependency, analytics, progression, unlocks, or new settings system.
- Keep the existing art, colliders, expressions, impact beat, and overall HUD.
- Change both the native iPhone game and public web prototype so product setup
  does not diverge.

## Verification plan

1. Replace difficulty profiles with one continuous force range in both runtimes.
2. Bias the continuous sample toward ordinary Normal-like wind without named
   intensity tiers.
3. Remove difficulty and wind-meter UI, then assert their absence.
4. Update deterministic, force-range, delayed-correction, and fallability tests.
5. Run web tests/build and Unity EditMode/PlayMode.
6. Capture the current portrait UI, perform an adversarial review, then export,
   sign, install, and launch the iPhone build.

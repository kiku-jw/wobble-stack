# Issue 4 — iPhone feel recovery

Status: Frozen
Frozen: 2026-07-23
Canonical issue: `kiku-jw/wobble-stack#4`

Amended: 2026-07-23 after the first implementation probe. AC1 compares drift
and non-shorter survival because the intentionally forgiving first gust can
complete under multiple inputs. AC2 now names the source-side hold model shown
by the runtime prompt; the earlier push-side wording was ambiguous.

## Original task

Recover the first physical-iPhone build after the owner found that the stack,
wind, controls, reactions, and collision silhouettes do not yet produce a
playable or understandable first gust.

## Acceptance criteria

### AC1 — The first gust is winnable

- For Gentle, Normal, and Wild, a scripted correct counter-input survives the
  complete first gust with both three and five creatures.
- Correct input materially reduces maximum drift and survives at least as long
  as neutral and wrong-way input on the same force and direction.
- The automated probe uses a supported, gravity-enabled tower and a full gust;
  it may not replace the tower with gravity-free floating bodies or sample only
  one physics frame.

### AC2 — Control direction and useful range are understandable

- Holding the side the wind comes from selects the correct counter response.
- Full horizontal control travel maps to a useful counter range for the three
  difficulty bands instead of requiring a narrow precision zone near screen
  center.
- The first-run hint describes the actual input model without contradicting the
  visual beam motion.

### AC3 — Wind arrives visibly before force

- Direction is visible for at least one second before physical force begins.
- Wind streaks are clearly cool blue/cyan against the warm background.
- Visual speed and density build with force rather than beginning at maximum.

### AC4 — Visible silhouettes match collision support

- The beam uses a rounded physical silhouette whose top and ends match the
  visible beam closely enough that creatures do not appear to pass through it.
- At rest, adjacent creature visuals read as one compact stack without obvious
  air gaps or unstable initial overlap.
- Runtime tests measure renderer/collider alignment and resting visual gaps.

### AC5 — Faces tell three distinct beats

- Ready begins with calm faces.
- Wind changes the face before strong displacement.
- Ground impact uses a clearly distinct dazed reaction.
- The three states reuse the existing sprite-atlas pipeline; no general
  animation framework is introduced.

### AC6 — The device build is presentation-clean

- The iOS export is not a Unity Development player, so the Development Console
  and Development Build watermark are absent.
- The signed app installs and launches on the connected iPhone.

### AC7 — Verification remains honest

- Batch compile, EditMode tests, PlayMode tests, portrait captures, iOS export,
  signed Xcode build, install, and launch pass.
- Physical feel remains an owner gate: automation cannot claim that controls
  feel good or that Retry is desirable.

## Constraints

- Unity version remains `6000.3.19f1`.
- Keep the web prototype unchanged.
- No new runtime dependencies, SDKs, services, accounts, or generalized
  configuration systems.
- Preserve the current portrait composition and user-owned art direction.
- Generated assets must remain covered by the local generation manifest.

## Non-goals

- App Store/TestFlight distribution.
- New levels, progression, economy, analytics, or meta systems.
- Replacing the layered 2D art pipeline with realtime 3D.

## Assumptions

- The connected iPhone and Personal Team signing remain available.
- A generated calm-face atlas can preserve the existing five-character order
  and chroma-key layout closely enough for the current crop contract.

## Verification plan

1. Add domain tests for corrected force sign, control range, and preview timing.
2. Replace the one-frame/floating physics proof with full-gust supported-tower
   measurements across difficulties, directions, and three/five creatures.
3. Add runtime checks for beam collider shape, sprite/collider alignment,
   compact rest spacing, and face-state transitions.
4. Run Unity batch compile, EditMode, and PlayMode suites.
5. Produce start, active-gust, collapse, and results portrait captures and
   inspect them.
6. Export a non-Development iOS Xcode project, sign, install, and launch it.
7. Hand the owner a bounded second playtest focused on first-gust survival,
   direction comprehension, collider trust, and facial readability.

# Issue 4 — obstacle impact follow-up implementation notes

Date: 2026-07-30

## Decisions

- Preserve the existing crossing-based obstacle model. Physical collision
  bodies would add tunneling, balancing, and art-alignment risk without being
  necessary to make the decision meaningful.
- A miss combines three existing seams: a bounded platform lift, the existing
  `journeyPause`, and deterministic creature velocity/angular velocity.
- A clear continues to use jump height only and receives none of the miss
  penalties.
- The impact is deliberately recoverable and does not script a collapse.

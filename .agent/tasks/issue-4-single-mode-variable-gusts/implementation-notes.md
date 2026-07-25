# Implementation notes

- Lower rung: delete difficulty state and controls; reuse the existing seeded
  scheduler, gust envelope, wind rendering, audio, and physics.
- GitHub prior art skipped because this is a repo-local product simplification,
  not a new widget, scheduler, or reusable subsystem.
- A continuous squared random sample is allowed to bias the single force range
  toward Normal-like gusts while keeping occasional strong gusts. It must not
  create named or player-visible tiers.
- Preserve the existing Normal score storage slots so removing difficulty does
  not erase accepted local records.
- Final production diff deletes the difficulty types/state, native/web controls,
  and web meter. No dependency, new state machine, named intensity tier, or
  progression rule was added.

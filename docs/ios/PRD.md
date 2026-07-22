# Wobble Stack for iPhone

## Product goal

Ship a portrait, one-thumb iPhone game whose core pleasure is rescuing a stack of expressive toy creatures from readable gusts. The production target is the clay-toy quality and emotional clarity of the concept frames, not a dressed-up web port.

## Player promise

In seconds, the player understands: drag the beam against the wind, watch five personalities panic, recover at the last moment, and immediately want one cleaner run.

## First App Store candidate

- Offline, single-player, portrait-only.
- One stage theme with a layered sunset environment.
- Three wind profiles and three-to-five-creature runs.
- Five visually and physically distinct creatures.
- Start, play, pause, collapse, results, Retry, and setup flows.
- Local best scores by difficulty and creature count.
- Sound and haptic hooks, reduced motion, safe-area support, and interruption-safe pause.
- A short play-first onboarding and bounded local unlock path only after physical iPhone validation.

## Explicit non-goals

- Accounts, backend, cloud saves, analytics SDKs, ads, IAP, shop, battle pass, multiplayer, or live operations.
- Procedural level generation, a general content framework, or configurable physics architecture.
- Recreating the concept images as flat backgrounds with gameplay painted over them.

## Core loop

1. Choose wind and stack size.
2. Touch and drag horizontally to set the beam angle.
3. Read the gust as it builds and counter-tilt without overcorrecting.
4. Survive, recover, and accumulate score.
5. Collapse with a short comic impact beat.
6. Retry immediately or change setup.

## Quality gates

- Correct counter-tilt materially outperforms a neutral beam on the same seed.
- Wrong tilt fails sooner; doing nothing is never the best hidden strategy.
- Five silhouettes remain distinguishable without faces.
- The tower is the first visual read, the beam second, UI third.
- Every primary state can produce a credible store screenshot.
- A physical iPhone run is mandatory before calling the game release-ready.

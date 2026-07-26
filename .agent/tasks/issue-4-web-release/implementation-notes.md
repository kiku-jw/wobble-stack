# Issue 4 — public web release implementation notes

## Lazy-senior receipt

- Lower rung: evolve the existing Vite + Canvas + Matter.js game and reuse the
  iPhone art; Unity WebGL and a second framework are unnecessary.
- GitHub prior art: skipped because the repository already has the accepted
  browser physics dependency and the task is a product-specific port.
- New code is justified only for the finite route loop, relative wheel
  control, sprite presentation, and deterministic release probes.

## Initial decision

The old web prototype will be replaced in place. Existing wind, collision,
failure timing, storage safety, keyboard/pointer input, and GitHub Pages
deployment remain the base. Web-ready transparent crops are derived from the
owned iPhone chroma sheets to avoid a runtime chroma-key processor and the
weight of Unity WebGL.

## Implemented rung

- Kept Vite, Canvas, Matter.js, the single seeded gust profile, local storage,
  impact-only slow motion, pause, and GitHub Pages.
- Replaced survival-time setup with the three authored iPhone routes and local
  badge/unlock progress.
- Changed direct pointer angle selection into relative drag that moves a
  44-pixel star-wheel support beneath the plank.
- Added pure route/support helpers in `src/game-content.js` and asset loading in
  `src/game-art.js`; no runtime dependency was added.
- Derived 15 character pose sprites plus world, road, beam, wheel, and effect
  sprites from repository-owned iPhone art. The public build never loads the
  magenta chroma sheets.
- Preserved weak Matter.js friendship constraints in play and releases them for
  free collapse, independent impact poses, dust/stars, and first-contact slow
  motion.

## Deliberately not built

- Unity WebGL or a second application framework.
- Accounts, backend, analytics, ads, service worker, install prompt, or tester
  form.
- A difficulty selector, wind-strength meter, creature-count chooser, or
  survival-time mode.
- Browser emulation of native haptics, sound, or the full Unity articulated
  joint rig.

## Adversarial review

The read-only strong-tier reviewer returned `No findings` and no release
blocker. Its build and live-deployment checks were unavailable in the
read-only/network-disabled worker environment; the coordinator owns those
proofs. The missing checked-in Playwright suite is accepted as a trade-off:
release QA uses the existing external Playwright wrapper and does not add a new
runtime or test dependency to this small static game.

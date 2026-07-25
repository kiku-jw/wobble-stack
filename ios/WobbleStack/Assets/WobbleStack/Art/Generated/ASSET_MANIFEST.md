# Generated asset manifest

Generated: 2026-07-22; extended 2026-07-25

Source direction:

- `docs/concepts/last-second-save.png`
- `docs/concepts/comedic-collapse.png`
- the team-selection frame from the same user-owned concept pass

Assets:

- `sunset-stage.png`: empty portrait clay landscape for the gameplay background.
- `characters-calm-chroma.png`: matching calm ready-state expressions for all
  five characters over `#FF00FF` chroma.
- `characters-chroma.png`: five isolated character sprites in pear, cube, bird, rabbit, jelly order over `#FF00FF` chroma.
- `characters-impact-chroma.png`: matching post-impact expressions for all five characters over `#FF00FF` chroma.
- `props-ui-chroma.png`: beam, fulcrum, crown, toy plates, dust, and impact stars over `#FF00FF` chroma.
- `AppIcon.png`: square three-character App Store icon with the same clay material and sunset palette.
- `road-tile.png`: edge-to-edge side-view terracotta clay terrain with a
  softly rounded sunlit rim, generated on `#00FF00` chroma and converted to
  alpha for continuous horizontal tiling beneath the physical wheel.
- `rig-pear.png`, `rig-cube.png`, `rig-bird.png`, `rig-rabbit.png`, and
  `rig-jelly.png`: five original 1536 × 1024 articulated clay character kits.
  Each sheet separates the blank torso, arms or wings, feet, and the
  character-specific leaves, crest, ears, or crown over `#FF00FF` chroma.
- `rig-face-parts.png`: one original 1536 × 1024 shared expression kit with
  separate eyes, pupils, brows, blinks, blushes, and calm, uncertain, effort,
  panic, grit, joy, and dazed mouths over `#FF00FF` chroma.

Generation receipt for `road-tile.png`:

- Built-in OpenAI image generation, `stylized-concept` use case.
- Prompt target: a seamless orthographic side-scrolling clay road cross-section
  spanning the full width, warm coral soil, quiet tactile imperfections, no
  props, text, characters, wheel, plank, sky, or UI.
- Chroma removal used the explicit `#00FF00` key after border auto-detection was
  rejected for sampling the orange terrain.

Generation receipt for the articulated rig sheets:

- Built-in OpenAI image generation, one generation per character kit plus one
  shared face-parts generation.
- Character prompts used the existing user-owned five-friend concept as the
  identity and clay-material reference, then requested a front-facing blank
  torso and fully isolated appendages without text, shadows, scenery, UI, or
  baked facial features.
- The face prompt requested a coherent oversized stop-motion eye, pupil, brow,
  blink, blush, and mouth vocabulary able to express calm, anticipation,
  effort, panic, relief, impact, and daze at portrait gameplay scale.
- Runtime crops use explicit component rectangles and pivots; the original
  chroma sheets are preserved unchanged and keyed by the existing material.

SHA-256:

- `rig-pear.png`: `1bbd8ef87c2893ddb94c352e3190ed6bd9231d48bba4d6963f11f0b373b37528`
- `rig-cube.png`: `54d15534ea02b2ccdb7727387b2399562a0bf65711406c3e387f95809c8148f9`
- `rig-bird.png`: `38a5ec24dc533121723ce436ba71edd1f3e4128f839d44fadb7d57a18bd25655`
- `rig-rabbit.png`: `8eea1976838b22780f652938a2f5669d1a65bbe0596510adb3dcf28dbe534a02`
- `rig-jelly.png`: `837fbdc42f974b11b29b4aa242b1c15d4ed6a53a76db69847625caabb90e5514`
- `rig-face-parts.png`: `8ad3063c71b4ee89365df69da8250e2ac62f6e23840167afc6e1d37a75cd2ad0`

Rights and use:

- Generated specifically for this user-owned game from user-owned generated concept frames.
- No third-party logos, text, stock photography, or identifiable people.
- Chroma removal is performed at runtime by the project sprite material so the original generation receipts remain unchanged.

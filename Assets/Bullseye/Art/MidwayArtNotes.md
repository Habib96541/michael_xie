# Midway art and typography

The November 4 visual update uses a vintage carnival at dusk, with crimson canvas, cream lettering, brass borders, warm bulbs, painted timber, and a teal evening sky.

## Fonts

Rye Regular is used for the title, countdown, and timer. Lato Bold is used for controls, instructions, HUD copy, target values, and reward feedback. Both font files and their supplied OFL license texts are stored in Assets/Bullseye/Art/Fonts.

Font sources are the Google Fonts repository.

- https://github.com/google/fonts/tree/main/ofl/rye
- https://github.com/google/fonts/tree/main/ofl/lato

## Artwork

MidwayTwilight.png was generated using the built-in image generation tool. MidwayShelf.png, MidwayRecess.png, and MidwayAwning.png are original procedural game assets. The layered booth uses separate SpriteRenderers, and the shelf fronts retain their gameplay blockers.

The background is fitted to the camera at runtime. Menus use a narrower ticket frame on wide screens and expand on portrait screens. Title and body fonts are assigned through BlitzConfig.asset.

## Generation prompt

Create a polished 2D game background illustration for a vintage American carnival midway at blue hour. Landscape 3:2. Rich hand-painted storybook arcade illustration with muted midnight teal sky, warm amber festoon bulbs, cream-and-crimson striped circus tents at outer edges, distant ferris wheel on right, little soft crowd silhouettes only far at the sides, subtle grain and atmospheric depth. Center half is open unobstructed muted navy mist and empty midway space, intended for a separate foreground shooting booth. Decorations frame the outside, quiet contrast in center. No lettering, no words, no UI, no weapon, no targets, no central booth. Finished commercial game art, softly rounded forms, warm light against teal shadows.

## Editing

Apply midway art is an explicit Editor command. It dresses the existing game scene and wires the font assets. Normal Web builds use the saved scene and configuration.

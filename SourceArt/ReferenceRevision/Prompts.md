# Original generated art — built-in image generation

These assets were generated for this project with the built-in image-generation
tool. No reference-game art or branding was extracted into the player.

## Vehicles (final selected source: Vehicles-Source.png)

Reference image role: the project's Idle_00.png, style reference only.

Prompt: Finished transparent game vehicle atlas for a premium 2.5D cartoon road
crossing game. Match the chicken's soft rounded 3D cartoon forms, controlled
highlights and upper-left light. Ten isolated sprites, two rows/five columns:
coral compact hatchback, yellow taxi, teal panel van, lavender cargo truck,
orange/cream delivery van. First row faces down/front; second row faces up/rear.
Restrained overhead 3/4 perspective at roughly65 degrees, nearly symmetrical,
vertical axes, no isometric yaw. Consistent material, navy windows, clean wheels,
no faces/logos/text. Generous transparent gutters,1536x1024.

Cleanup prompt: Background removal only. Preserve all ten vehicles, details,
proportions and arrangement. Remove all gradients, haze, glows and ground shadows
outside the actual silhouettes; transparent gaps, no invented models or objects.

The generator retained low-alpha edge noise. Inspected extraction removes alpha
below24/255, crops native bounds and adds8px transparent padding. No resampling
of the delivered sprites. See Extraction.json for exact rectangles and trims.
A discarded unrelated generation was not admitted to the project.

## Props (Props-Source.png)

Prompt: Premium rounded2.5D cartoon environment atlas, four isolated original
sprites in2x2: top-left oval steel-blue raised road grate with a dark slotted
center; top-right portable white/coral striped traffic safety barrier with navy
feet; bottom-left lush softly shaded mint shrub in a slate planter; bottom-right
beveled blue-grey tiled sidewalk segment. Camera about65 degrees overhead,
consistent upper-left light, soft highlights and shadows, no labels/logos,
transparent background and generous gutters. Finished game art, not placeholders.

## Asphalt (Asphalt-Final.png)

Initial texture was rejected during in-game visual review because its coarse
grain resembled realistic gravel and competed with the characters.

Final edit prompt: Smooth stylized cartoon road material. Remove realistic
pebble grain. Lower contrast at least90 percent. Uniform dark navy #1b3042 with
gentle painted surface variation and barely visible fine soft noise. Visually
quiet, polished, seamless in every direction, overhead even lighting. No gravel,
individual stones, photographic texture, cracks, markings, props, text, vignette,
highlights, borders or gradients. Opaque full canvas. It fills the scene behind
colorful cartoon characters and must be extremely subdued.

Final selected assets are copied to Assets/ChickenRush/Art/Arcade/. Original
generation sources, rejected coarse material and extraction metadata stay here
outside the player import tree.

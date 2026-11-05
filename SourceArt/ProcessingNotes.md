# Sprite processing provenance

Input artwork was supplied by the user. Originals are preserved here. Image
edits used the built-in image generation tool, not a paid-provider CLI.

## Sheet cleanup prompt

Use case: background-extraction. Edit target is the attached sprite sheet. This
is technical cleanup for Unity animation, not a redesign. Preserve the exact
original chicken artwork, poses, colors, shading, accessories, frame order,
number of chickens and proportions. Remove ALL purple title/label panels, ALL
letters/numbers/lines, ALL red/yellow/magenta/blue stray background speckles and
glows, ALL dust/confetti/stars and decorative effects behind the character.
Output only the isolated chickens on genuinely transparent alpha, crisp clean
antialiased silhouettes. Original rows: row1 idle eleven chickens, row2 walk
eleven chickens, row3 hop ten chickens, row4 hit/falling ten chickens, row5
celebrate eleven chickens. Keep five horizontal rows in that same order. Keep
each row of chickens separated by ample transparent gutter and every individual
chicken separated from neighbors. Do not crop any part of any chicken. No text,
labels, ground, shadows, backdrop, extra frames, new characters, borders or
checkered pattern. Retain each original pose, especially row4 fall trajectory
and final lying-down poses. Keep full source image quality. Transparent
background required.

Output: `CleanedSheet.png`. Residual low-alpha color noise was removed during
content-aware extraction. The final sprites retain native resolution; no frame
is enlarged. See the extraction script and QA contact sheet.

## Blink edit prompt

Precise sprite edit, identity preserve. Input is a 192x192 transparent game
sprite. Change ONLY the chicken's open black eye into a gently CLOSED eyelid,
a short curved dark line to depict a single natural blink. Keep the exact same
pixel placement, resolution, transparent canvas, body silhouette, feet, comb,
goggles, backpack, scarf, feathers, colors, camera, proportions and lighting.
No other alteration whatsoever. Do not upscale, recenter, zoom or repaint the
body. Output the same 192x192 game sprite with genuinely transparent background
and one closed eye. No text, no shadow, no floor.

Output: `BlinkEdit.png`. The generator enlarged/repainted more than requested,
so the full output was rejected as a replacement. Only its small closed-eye
patch was registered to the original idle drawing; the body and silhouette
remain the original pixels. See `Tools/prepare_blink.py` and `QA/BlinkDetail.png`.

These cleaned low-resolution drawings are usable animation assets, with the
small original drawing inconsistencies visible in the contact sheet. They do
not replace a hand-drawn production animation master.

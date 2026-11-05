"""Prepare only the replacement action skins; scene/import metadata stays untouched."""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/LittleDepths/Art/RemasteredSheets'
OUT = ROOT / 'Assets/LittleDepths/Art/Provided'
# Existing canvases remain exactly the same size, retaining every scene socket.
ASSETS = (
    ('ref_cast_meter', 'matched_cast_hd', (2162, 721)),
    ('ref_dive_panel', 'matched_dive_hd', (2232, 744)),
    ('ref_depth_badge', 'matched_depth_hd', (1092, 1365)),
    ('ref_reel_action', 'matched_reel_hd', (1511, 719)),
)


def import_matching_actions(manifest):
    for name, key, size in ASSETS:
        source = SOURCE / (key + '.png')
        if not source.exists():
            continue
        image = Image.open(source).convert('RGBA')
        # Remove nearly invisible generated exterior residue while retaining edge
        # antialiasing and making the painted panel interiors fully opaque.
        image.putalpha(image.getchannel('A').point(
            lambda a: 0 if a < 32 else min(255, round((a - 32) * 255 / 96))))
        box = image.getbbox()
        if box is None:
            raise ValueError('Empty replacement: ' + key)
        image = image.crop(box)
        scale = min((size[0] - 20) / image.width, (size[1] - 20) / image.height)
        image = image.resize((round(image.width * scale), round(image.height * scale)), Image.Resampling.LANCZOS)
        result = Image.new('RGBA', size)
        result.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
        result.paste((0, 0, 0, 0), (0, 0, *size), result.getchannel('A').point(lambda a: 255 if a == 0 else 0))
        result.save(OUT / (name + '.png'))
        manifest['sprites'] = [entry for entry in manifest['sprites'] if entry['name'] != name]
        manifest['sprites'].append(dict(name=name, source=key + '.png', size=list(size),
            complete_outline=True, matches_supplied_equipment=True, preserves_existing_canvas=True))


if __name__ == '__main__':
    path = OUT / 'RemasterManifest.json'
    manifest = json.loads(path.read_text(encoding='utf-8-sig'))
    import_matching_actions(manifest)
    path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print('Prepared four matching action skins; scene, transforms, GUIDs and import settings were not edited.')

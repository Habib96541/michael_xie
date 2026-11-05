"""Content-aware extraction of the supplied, irregularly spaced chicken poses.

The imagegen cleanup is retained as input. Bounds are measured from alpha, never
assumed from the misleading printed frame counts. No sprite is rescaled.
"""
from pathlib import Path
import json
import shutil
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'SourceArt'
OUT = ROOT / 'Assets/ChickenRush/Art/Chicken'
QA = ROOT / 'QA'
SOURCE.mkdir(exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)
QA.mkdir(exist_ok=True)
original = Path(r'C:\Users\tahmi\Downloads\Cartoon Chicken Animation Sprite Sheet.png')
cleaned = Path(r'C:\Users\tahmi\.codex\generated_images\01a190df-ff1d-7f32-b772-ab0e2fa0842d\exec-a6b1781f-a30a-4a9c-ba10-50fa42bb4e9b.png')
for src, name in [(original, 'OriginalSheet.png'), (cleaned, 'CleanedSheet.png'),
                  (Path(r'C:\Users\tahmi\Downloads\Chicken Rush Character Design Sheet.png'), 'DesignReference.png')]:
    if src.exists():
        shutil.copy2(src, SOURCE / name)
sheet = np.array(Image.open(SOURCE / 'CleanedSheet.png').convert('RGBA'))
# Low-alpha chroma noise in this source is not part of the painted silhouette.
# Retain the antialias band and normalize its near-opaque (253/255) interiors.
alpha = sheet[:, :, 3].astype(np.float32)
sheet[:, :, 3] = np.clip((alpha - 48) * 255 / (253 - 48), 0, 255).astype(np.uint8)
sheet[sheet[:, :, 3] == 0, :3] = 0
rows = [('Idle', 0, 145, 11), ('Walk', 150, 289, 11),
        ('Hop', 295, 435, 10), ('Hit', 445, 578, 10), ('Celebrate', 587, 724, 11)]
records = []
contact = Image.new('RGB', (11 * 192, 5 * 212), '#242036')
draw = ImageDraw.Draw(contact)
for ri, (name, y0, y1, expected) in enumerate(rows):
    occupied = (sheet[y0:y1, :, 3] > 128).sum(axis=0) > 3
    edges = np.diff(np.r_[False, occupied, False].astype(int))
    ranges = [(int(a), int(b)) for a, b in zip(np.where(edges == 1)[0], np.where(edges == -1)[0]) if b-a > 25]
    assert len(ranges) == expected, (name, ranges)
    for i, (x0, x1) in enumerate(ranges):
        # Individual content bounds plus a small antialias allowance; no grid slicing.
        x0, x1 = max(0, x0-3), min(sheet.shape[1], x1+3)
        crop = sheet[y0:y1, x0:x1].copy()
        ys, xs = np.where(crop[:, :, 3] > 128)
        tight = crop[max(0, ys.min()-2):min(len(crop), ys.max()+3), :, :]
        # A common canvas and foot pivot. Painted flight height is removed because
        # the actual jump arc is owned by the presentation transform in Unity.
        sprite = Image.fromarray(tight)
        canvas = Image.new('RGBA', (192, 192))
        px = (192 - sprite.width) // 2
        py = 174 - sprite.height
        canvas.paste(sprite, (px, py))
        path = OUT / f'{name}_{i:02}.png'
        canvas.save(path)
        bg = Image.new('RGBA', (192, 192), '#f3e7d3' if i % 2 else '#242036')
        bg.alpha_composite(canvas)
        contact.paste(bg.convert('RGB'), (i*192, ri*212))
        draw.text((i*192+7, ri*212+194), f'{name} {i:02}', fill='white')
        records.append(dict(clip=name, frame=i, sourceBounds=[x0, y0+int(ys.min()), x1, y0+int(ys.max())+1],
                            canvas=[192,192], footPixel=[96,174], rescaled=False))
contact.save(QA / 'SpriteContactSheet.png')
(QA / 'SpriteExtraction.json').write_text(json.dumps(dict(sourceSize=[2172,724], count=len(records),
    alphaCleanup='Remove alpha <=48 chroma contamination; normalize 253 to255, preserve edge ramp.',
    frames=records), indent=2))
print(f'Extracted {len(records)} frames at native resolution; contact sheet: {QA / "SpriteContactSheet.png"}')

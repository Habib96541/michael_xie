"""Slice the reference-guided image_gen remasters into Unity's stable sprite paths.

Original supplied JPGs and untouched generated PNGs stay in their own folders.
This step only extracts assets, removes atlas mattes, pads canvases and prepares
blank reusable faces from the generated artwork. It does not draw replacement art.
"""
from collections import deque
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/LittleDepths/Art/RemasteredSheets'
OUT = ROOT / 'Assets/LittleDepths/Art/Provided'
images = {p.stem: Image.open(p).convert('RGBA') for p in SOURCE.glob('*.png')}
manifest = {'generator': 'built-in image_gen', 'source': 'Reference-guided remasters; untouched inputs in RemasteredSheets, original supplied JPGs in ProvidedSheets', 'sprites': []}


def crop(key, box, dimensions=(1024, 572)):
    im = images[key]
    return im.crop(tuple(round(v * im.size[i % 2] / dimensions[i % 2]) for i, v in enumerate(box)))


def exterior(im, tolerance=42):
    """Flood only the connected atlas matte, retaining the sprite's dark outlines."""
    im = im.copy()
    pixels = im.load()
    w, h = im.size
    bg = pixels[0, 0][:3]
    seen = bytearray(w*h)
    queue = deque([(x, 0) for x in range(w)] + [(x, h-1) for x in range(w)] + [(0, y) for y in range(h)] + [(w-1, y) for y in range(h)])
    while queue:
        x, y = queue.popleft()
        n = y*w+x
        if seen[n]:
            continue
        seen[n] = 1
        p = pixels[x, y]
        if sum((p[c]-bg[c])**2 for c in range(3)) >= tolerance*tolerance:
            continue
        pixels[x, y] = (0, 0, 0, 0)
        if x: queue.append((x-1, y))
        if x+1 < w: queue.append((x+1, y))
        if y: queue.append((x, y-1))
        if y+1 < h: queue.append((x, y+1))
    return im


def largest(im):
    """Remove disconnected sheet labels/adjacent objects outside this cutout."""
    im = im.copy()
    w, h = im.size
    p = im.load()
    seen = bytearray(w*h)
    biggest = []
    for start in range(w*h):
        if seen[start] or p[start % w, start // w][3] < 32:
            continue
        queue = deque([start]); seen[start] = 1; region = []
        while queue:
            n = queue.popleft(); region.append(n); x = n % w; y = n // w
            for dx, dy in ((-1,0),(1,0),(0,-1),(0,1),(-1,-1),(1,1),(-1,1),(1,-1)):
                xx, yy = x+dx, y+dy
                if 0 <= xx < w and 0 <= yy < h:
                    nn = yy*w+xx
                    if not seen[nn] and p[xx, yy][3] >= 32:
                        seen[nn] = 1; queue.append(nn)
        if len(region) > len(biggest): biggest = region
    mask = bytearray(w*h)
    for n in biggest: mask[n] = 1
    keep = Image.frombytes('L', im.size, bytes(255 if x else 0 for x in mask))
    im.putalpha(keep)
    return im


def clean(im):
    # Generated transparent PNGs can contain very faint disconnected residue.
    # Keep their antialiasing and RGB; discard only nearly invisible exterior alpha.
    im = im.copy()
    im.putalpha(im.getchannel('A').point(lambda a: 0 if a < 32 else min(255,round((a-32)*255/96))))
    return im


def antialias_cutout(im):
    # Atlas RGB was painted over a matte. Feather the extracted boundary at native
    # resolution and extend its own edge colors into that subpixel coverage.
    alpha=im.getchannel('A')
    rgb=im.convert('RGB')
    expanded=rgb.filter(ImageFilter.MaxFilter(3))
    rgb.paste(expanded,(0,0),alpha.point(lambda a:255 if a==0 else 0))
    rgb.putalpha(alpha.filter(ImageFilter.GaussianBlur(.45)))
    return rgb


def padded(im, aspect=None, padding=8, trim=True):
    im = clean(im)
    if trim:
        box = im.getbbox()
        if not box: raise ValueError('Empty generated sprite')
        im = im.crop(box)
    w, h = im.width+padding*2, im.height+padding*2
    if aspect:
        if w/h < aspect: w = math.ceil(h*aspect)
        else: h = math.ceil(w/aspect)
    result = Image.new('RGBA', (w, h))
    result.paste(im, ((w-im.width)//2, (h-im.height)//2))
    # Zero hidden RGB as well, so previews and filtering never expose a matte.
    result.paste((0,0,0,0),(0,0,w,h),result.getchannel('A').point(lambda a:255 if a==0 else 0))
    return result


def save(name, im, source, **details):
    im.save(OUT/(name+'.png'))
    manifest['sprites']=[entry for entry in manifest['sprites'] if entry['name']!=name]
    manifest['sprites'].append({'name': name, 'source': source, 'size': im.size, **details})
    return im


panels = {}
for name, key, aspect in [
    ('ref_hud','hud',525/86), ('ref_dock','dock',525/206),
    ('ref_character','character',507/308), ('ref_upgrades','upgrades',459/308),
    ('ref_prizes','prizes',463/308), ('ref_results','results',497/201),
    ('ref_pause','pause',136/199), ('ref_quests','quests',241/199),
]:
    source_key='quests_blank' if key=='quests' and 'quests_blank' in images else key
    if key=='prizes' and 'prizes_blank' in images:source_key='prizes_blank'
    panels[key] = save(name, padded(images[source_key], aspect), source_key+'.png', complete_outline=True)
save('ref_mountains', padded(images['mountains'], padding=3), 'mountains.png', complete_outline=True)

# The new icon atlas has one complete icon per equal cell, with transparent gutters.
for i, name in enumerate(['outfit','hat','boat','hook','shop','medal','trophy','home','settings']):
    im = images['icons']; x = i % 3; y = i // 3
    cell = im.crop((x*im.width//3,y*im.height//3,(x+1)*im.width//3,(y+1)*im.height//3))
    save('ref_icon_'+name, padded(cell, 1, padding=18), 'icons.png', cell=i, complete_outline=True)
# Retain the old path for compatibility; it now contains an actual fishing hook.
save('ref_icon_accessory', Image.open(OUT/'ref_icon_hook.png'), 'icons.png', alias_of='ref_icon_hook')
if 'close' in images:save('ref_icon_close',padded(images['close'],1,padding=18),'close.png',complete_outline=True)

# Other environment sprites retain their same atlas geometry and natural aspect.
for name, box in [
    ('ref_canyon',(670,37,1009,165)), ('ref_pines',(12,187,319,330)),
    ('ref_pine',(12,197,90,330)), ('ref_oak',(405,216,488,330)),
    ('ref_gold_tree',(551,207,650,330)), ('ref_willow',(749,204,849,330)),
    ('ref_bushes',(13,374,118,426)), ('ref_shore_left',(559,374,777,502)),
    ('ref_shore_right',(789,439,1009,502)), ('ref_cloud',(795,372,863,405)),
    ('ref_cloud_long',(912,372,1002,405)), ('ref_rocks',(13,496,126,561)),
]:
    im = exterior(crop('nature', box))
    if name != 'ref_pines': im = largest(im)
    save(name, padded(antialias_cutout(im), padding=4), 'nature.png', rect_1024=box)

# Standalone large foreground illustrations replace the small atlas crops.
for name,key in [('ref_pines','pines_hd'),('ref_oak','oak_hd'),('ref_gold_tree','fruit_tree_hd'),('ref_shore_left','shore_left_hd'),('ref_shore_right','shore_right_hd')]:
    if key in images:save(name,padded(images[key],padding=10),key+'.png',complete_outline=True,standalone_high_resolution=True)

for name,key,aspect in [('ref_cast_meter','cast_meter_hd',3),('ref_dive_panel','dive_panel_hd',3),('ref_depth_badge','depth_badge_hd',.8),('ref_reel_action','reel_button_hd',None)]:
    if key in images:save(name,padded(images[key],aspect,padding=10),key+'.png',complete_outline=True)
if 'timing_parts_hd' in images:
    parts=images['timing_parts_hd'];split=round(parts.width*.4)
    save('ref_cast_cursor',padded(parts.crop((0,0,split,parts.height)),padding=10),'timing_parts_hd.png',complete_outline=True)
    save('ref_cast_target',padded(parts.crop((split,0,parts.width,parts.height)),padding=10),'timing_parts_hd.png',complete_outline=True)


def panelcrop(key, box, dimensions):
    im = panels[key]
    return im.crop(tuple(round(v*im.size[i%2]/dimensions[i%2]) for i,v in enumerate(box)))


def rounded_cut(im, radius):
    mask = Image.new('L', im.size)
    ImageDraw.Draw(mask).rounded_rectangle((1,1,im.width-2,im.height-2),radius=radius,fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(.45))
    im.putalpha(mask)
    return padded(im, padding=6)


# Reuse the large, newly generated dial at full native resolution in Timing mode.
dial = panelcrop('dock',(43,19,215,191),(525,206))
mask = Image.new('L',dial.size)
ImageDraw.Draw(mask).ellipse((2,2,dial.width-3,dial.height-3),fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(1))
dial.putalpha(mask)
save('ref_play',padded(dial,1,padding=12),'dock.png',complete_outline=True)

# Blank skins retain the generated rounded outlines and vertical face shading.
green = panelcrop('dock',(244,141,316,172),(525,206))
save('ref_button_green',rounded_cut(green,green.height*.45),'dock.png',complete_outline=True)
for name,key,box,dims,textbox,strip in [
    ('ref_button_blue','results',(259,140,370,182),(497,201),(20,8,95,34),(97,8,98,34)),
    ('ref_button_red','pause',(16,163,120,189),(136,199),(30,5,76,22),(80,5,81,22)),
]:
    im=panelcrop(key,box,dims);sx=im.width/(box[2]-box[0]);sy=im.height/(box[3]-box[1])
    r=tuple(round(v*(sx if i%2==0 else sy)) for i,v in enumerate(textbox))
    s=tuple(round(v*(sx if i%2==0 else sy)) for i,v in enumerate(strip))
    im.paste(im.crop(s).resize((r[2]-r[0],r[3]-r[1]),Image.Resampling.BICUBIC),r[:2])
    save(name,rounded_cut(im,im.height*.45),key+'.png',complete_outline=True)

# Full resolution arrows and prize card faces are also derived from the remasters.
for name,box in [('ref_character_back',(30,134,57,168)),('ref_character_next',(265,134,292,168))]:
    save(name,padded(exterior(panelcrop('character',box,(507,308)))), 'character.png')
original_prizes=padded(images['prizes'],463/308)
back=original_prizes.crop(tuple(round(v*original_prizes.size[i%2]/(463,308)[i%2]) for i,v in enumerate((55,77,123,145))))
save('ref_prize_back',rounded_cut(back,back.width*.22),'prizes.png')
if 'prize_front_opaque' in images:
    front=antialias_cutout(largest(exterior(images['prize_front_opaque'],60)))
    save('ref_prize_front',padded(front,1,padding=12),'prize_front_opaque.png',complete_outline=True)
elif 'prize_front_blank' in images:
    front=images['prize_front_blank'].copy()
    # The generated white card needs an opaque rounded silhouette; background
    # extraction otherwise mistakes part of its white lower rim for sky.
    mask=Image.new('L',front.size)
    ImageDraw.Draw(mask).rounded_rectangle((100,61,1169,1155),radius=180,fill=255)
    front.putalpha(mask.filter(ImageFilter.GaussianBlur(.6)))
    save('ref_prize_front',padded(front,1,padding=12),'prize_front_blank.png',complete_outline=True)

# Clear dynamic level/description fields using letter-free samples from the new
# panel itself; feather their edges instead of pasting old low resolution patches.
def field(name, key, strip, size, dims):
    im=panelcrop(key,strip,dims)
    scale=panels[key].width/dims[0]
    im=im.resize((round(size[0]*scale),round(size[1]*scale)),Image.Resampling.BICUBIC)
    mask=Image.new('L',im.size)
    ImageDraw.Draw(mask).rectangle((3,3,im.width-4,im.height-4),fill=255)
    mask=mask.filter(ImageFilter.GaussianBlur(2))
    im.putalpha(mask)
    save(name,im,key+'.png',blank_face_from_same_panel=True)

field('ref_level_fill','upgrades',(42,169,43,190),(110,21),(459,308))
field('ref_description_fill','upgrades',(46,196,47,225),(113,29),(459,308))
save('ref_preview_fill',panelcrop('character',(74,72,247,225),(507,308)),'character.png')

# Live quest checkmarks and progress use the same generated artwork as the panel.
quest=images['quests']
for name,box in [('ref_quest_check',(115,275,250,395)),('ref_quest_progress',(253,558,913,628))]:
    save(name,padded(largest(exterior(quest.crop(box),60)),padding=4),'quests.png',source_pixels=box)

from import_matching_action_art import import_matching_actions
import_matching_actions(manifest)
(OUT/'RemasterManifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Imported',len(manifest['sprites']),'remastered sprites without upscaling; native largest panel',max(s['size'][0] for s in manifest['sprites']))

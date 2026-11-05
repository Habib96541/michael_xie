"""Slice the user's supplied artwork. No drawing or generated replacement art.

RGB pixels come from the supplied sheets. Background removal changes alpha only;
reusable blank faces repeat clean, letter-free pixels from the same original part.
"""
from pathlib import Path
from collections import deque
from PIL import Image
import json
import shutil
import math
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/LittleDepths/Art/Provided'
if (OUT/'RemasterManifest.json').exists() and '--restore-original' not in sys.argv:
    raise SystemExit('The active artwork is remastered. Run python Tools/import_remastered_art.py; add --restore-original here only to deliberately restore the old JPG crops.')
SOURCES = ROOT / 'Assets/LittleDepths/Art/ProvidedSheets'
OUT.mkdir(parents=True, exist_ok=True)
SOURCES.mkdir(parents=True, exist_ok=True)
UI_EDGES_ONLY = '--ui-edges-only' in sys.argv
TEMP = Path('C:/Users/UEF33A~1.RAW/AppData/Local/Temp')
inputs = {key: SOURCES/(key+'.jpg') for key in ['game','menus','nature']}
images = {}
for key, path in inputs.items():
    images[key] = Image.open(path).convert('RGBA')
manifest = {'source': 'Exact crops of user supplied sheets; no image generation or redrawing', 'sprites': []}
if UI_EDGES_ONLY and (OUT/'ExtractionManifest.json').exists():
    manifest=json.loads((OUT/'ExtractionManifest.json').read_text(encoding='utf-8'))

PANEL_SHAPES={
    'ref_hud': (23,10,541,91,21),
    'ref_dock': (23,125,541,323,20),
}
DISC_SHAPES={'ref_play':(64,139,241,317)}
BUTTON_SHAPES={
    'ref_button_green':(160,506,277,551,18),
    'ref_button_blue':(284,506,402,551,18),
    'ref_button_red':(293,523,403,552,13),
}

ICON_SHAPES = {
    'ref_icon_play': (550,372,614,435,'ellipse'),
    'ref_icon_pause': (621,372,683,435,'ellipse'),
    'ref_icon_shop': (688,372,753,434,'ellipse'),
    'ref_icon_home': (758,373,823,435,'ellipse'),
    'ref_icon_medal': (620,440,682,501,'rounded'),
    'ref_icon_trophy': (691,440,753,501,'rounded'),
    'ref_icon_map': (621,504,682,565,'rounded'),
    'ref_icon_gift': (682,503,741,561,'rounded'),
    'ref_icon_settings': (616,503,676,560,'rounded'),
    'ref_icon_outfit': (345,73,413,140,'ellipse'),
    'ref_icon_hat': (424,72,493,140,'ellipse'),
    'ref_icon_accessory': (345,161,414,225,'ellipse'),
}

def complete_icon(im,key,box,shape):
    # Mask around the complete original frame, never inside its outline. Positions
    # belong to the actual source sheet, rather than the center of a rough crop.
    im=main_region(alpha_edges(im))
    full=images[key];sx=full.width/1024;sy=full.height/(579 if key=='game' else 572)
    left,top,right,bottom,kind=shape
    l=(left-box[0])*sx;t=(top-box[1])*sy;r=(right-box[0])*sx;b=(bottom-box[1])*sy
    cx=(l+r)/2;cy=(t+b)/2;rx=(r-l)/2;ry=(b-t)/2;corner=min(rx,ry)*.38
    pix=im.load();w,h=im.size;feather=max(sx,sy)*.9
    for y in range(h):
        for x in range(w):
            if kind=='ellipse':
                distance=(math.hypot((x+.5-cx)/rx,(y+.5-cy)/ry)-1)*min(rx,ry)
            else:
                qx=max(l+corner,min(x+.5,r-corner));qy=max(t+corner,min(y+.5,b-corner))
                distance=math.hypot(x+.5-qx,y+.5-qy)-corner
            alpha=round(255*max(0,min(1,.5-distance/feather)))
            pix[x,y]=pix[x,y][:3]+(min(pix[x,y][3],alpha),)
    bounds=im.getbbox()
    if not bounds:raise ValueError('Empty icon')
    # Equal canvases retain natural proportions in square navigation hit targets.
    art=im.crop(bounds);side=max(art.size);padding=max(4,math.ceil(side*.065))
    result=Image.new('RGBA',(side+2*padding,side+2*padding))
    result.paste(art,((result.width-art.width)//2,(result.height-art.height)//2))
    pixels=result.load()
    for y in range(result.height):
        for x in range(result.width):
            if pixels[x,y][3]==0:pixels[x,y]=(0,0,0,0)
    return result

def complete_panel(im,key,box,shape):
    full=images[key];sx=full.width/1024;sy=full.height/(579 if key=='game' else 572)
    left,top,right,bottom,corner=shape
    l=(left-box[0])*sx;t=(top-box[1])*sy;r=(right-box[0])*sx;b=(bottom-box[1])*sy
    radius=corner*min(sx,sy);feather=min(sx,sy)*.9;p=im.load()
    for y in range(im.height):
        for x in range(im.width):
            cx=max(l+radius,min(x+.5,r-radius));cy=max(t+radius,min(y+.5,b-radius))
            distance=math.hypot(x+.5-cx,y+.5-cy)-radius
            alpha=round(255*max(0,min(1,.5-distance/feather)))
            p[x,y]=p[x,y][:3]+(alpha,) if alpha else (0,0,0,0)
    return im

def complete_disc(im,key,box,shape):
    full=images[key];sx=full.width/1024;sy=full.height/(579 if key=='game' else 572)
    l,t,r,b=shape;cx=((l+r)/2-box[0])*sx;cy=((t+b)/2-box[1])*sy
    rx=(r-l)*sx/2;ry=(b-t)*sy/2;p=im.load()
    for y in range(im.height):
        for x in range(im.width):
            distance=(math.hypot((x+.5-cx)/rx,(y+.5-cy)/ry)-1)*min(rx,ry)
            alpha=round(255*max(0,min(1,-distance/(4*min(sx,sy)))))
            p[x,y]=p[x,y][:3]+(alpha,) if alpha else (0,0,0,0)
    return im

def cut(key, box):
    im = images[key]
    bh = 579 if key == 'game' else 572
    return im.crop(tuple(round(v * (im.width/1024 if i%2 == 0 else im.height/bh)) for i,v in enumerate(box)))

def alpha_edges(im, tolerance=26):
    # Remove only the connected exterior JPEG matte; preserve all artwork RGB.
    im = im.copy(); pix=im.load(); w,h=im.size
    bg=pix[0,0][:3]
    def matte(x,y):
        p=pix[x,y]
        return sum((p[c]-bg[c])**2 for c in range(3)) < tolerance*tolerance
    seen=bytearray(w*h); queue=deque()
    for x in range(w): queue.extend(((x,0),(x,h-1)))
    for y in range(h): queue.extend(((0,y),(w-1,y)))
    while queue:
        x,y=queue.popleft(); n=y*w+x
        if seen[n]: continue
        seen[n]=1
        if not matte(x,y): continue
        p=pix[x,y]; pix[x,y]=p[:3]+(0,)
        if x: queue.append((x-1,y))
        if x+1<w: queue.append((x+1,y))
        if y: queue.append((x,y-1))
        if y+1<h: queue.append((x,y+1))
    return im

def main_region(im):
    im=im.copy();w,h=im.size;p=im.load();seen=bytearray(w*h);biggest=[]
    for start in range(w*h):
        if seen[start] or p[start%w,start//w][3]<32:continue
        queue=deque([start]);seen[start]=1;region=[]
        while queue:
            n=queue.popleft();region.append(n);x=n%w;y=n//w
            for dx,dy in [(-1,0),(1,0),(0,-1),(0,1),(-1,-1),(1,-1),(-1,1),(1,1)]:
                xx=x+dx;yy=y+dy
                if 0<=xx<w and 0<=yy<h:
                    nn=yy*w+xx
                    if not seen[nn] and p[xx,yy][3]>=32:seen[nn]=1;queue.append(nn)
        if len(region)>len(biggest):biggest=region
    keep=bytearray(w*h)
    for n in biggest:keep[n]=1
    for n in range(w*h):
        if not keep[n]:
            x=n%w;y=n//w;p[x,y]=p[x,y][:3]+(0,)
    return im

def save(name,key,box,alpha=False):
    if UI_EDGES_ONLY and name not in ICON_SHAPES and name not in PANEL_SHAPES and name not in DISC_SHAPES:return
    im=cut(key,box)
    if name in ICON_SHAPES:
        im=complete_icon(im,key,box,ICON_SHAPES[name])
    elif name in PANEL_SHAPES:
        im=complete_panel(im,key,box,PANEL_SHAPES[name])
    elif name in DISC_SHAPES:
        im=complete_disc(im,key,box,DISC_SHAPES[name])
    elif alpha:
        im=alpha_edges(im)
        if name!='ref_pines':im=main_region(im)
    if name=='ref_mountains':
        p=im.load()
        for y in range(min(5,im.height)):
            for x in range(im.width):
                v=p[x,y]
                if max(v[:3])<90:p[x,y]=v[:3]+(0,)
    im.save(OUT/(name+'.png'))
    manifest['sprites']=[s for s in manifest['sprites'] if s['name']!=name]
    manifest['sprites'].append({'name':name,'source':key+'.jpg','rect_1024':box,'exterior_alpha_removed':alpha,'complete_outline':name in ICON_SHAPES or name in PANEL_SHAPES})
    return im

def blank(name,key,box,inner,strip):
    if UI_EDGES_ONLY and name not in BUTTON_SHAPES:return
    im=cut(key,box); patch=cut(key,strip); full=images[key]
    sx=full.width/1024; sy=full.height/(579 if key=='game' else 572)
    r=tuple(round(v*(sx if i%2==0 else sy)) for i,v in enumerate(inner))
    im.paste(patch.resize((r[2]-r[0],r[3]-r[1]),Image.Resampling.NEAREST),(r[0],r[1]))
    im=complete_panel(im,key,box,BUTTON_SHAPES[name]) if name in BUTTON_SHAPES else main_region(alpha_edges(im))
    im.save(OUT/(name+'.png'))
    manifest['sprites']=[s for s in manifest['sprites'] if s['name']!=name]
    manifest['sprites'].append({'name':name,'source':key+'.jpg','rect_1024':box,'blank_face_from_same_sheet':strip})

# Full, precomposed UI panels. Their framing, decoration, titles and icons stay intact.
for name,key,box in [
    ('ref_hud','game',(20,8,545,94)),
    ('ref_dock','game',(20,122,545,328)),
    ('ref_prizes','game',(555,14,1018,322)),
    ('ref_results','game',(24,364,521,565)),
    ('ref_character','menus',(26,11,533,319)),
    ('ref_upgrades','menus',(548,11,1007,319)),
    ('ref_pause','menus',(278,363,414,562)),
    ('ref_quests','menus',(25,363,266,562)),
    ('ref_stage','menus',(419,363,531,562)),
    ('ref_play','game',(60,135,246,321)),
    ('ref_prize_back','game',(614,89,684,161)),
    ('ref_prize_front','game',(614,161,684,232)),
    ('ref_retry','game',(157,505,274,544)),
    ('ref_home_text','game',(282,505,401,544)),
    ('ref_select','menus',(145,260,267,299)),
    ('ref_back','menus',(282,260,403,299)),
    ('ref_wood_patch','game',(903,418,992,433)),
    ('ref_green_patch','game',(324,270,326,284)),
    ('ref_white_patch','game',(175,438,180,449)),
    ('ref_wood_counter','game',(882,407,1013,447)),
    ('ref_coin','game',(47,33,87,75)),
    ('ref_plus','game',(199,31,228,74)),
    ('ref_capacity_fish','game',(259,33,301,73)),
    ('ref_preview_fill','menus',(105,86,119,123)),
    ('ref_color_cover','menus',(500,157,512,249)),
    ('ref_character_back','menus',(60,146,80,177)),
    ('ref_character_next','menus',(293,145,315,178)),
    ('ref_panel_patch','game',(599,110,600,111)),
]: save(name,key,box,name in {'ref_hud','ref_dock','ref_prizes','ref_results','ref_character','ref_upgrades','ref_pause','ref_quests','ref_stage','ref_retry','ref_home_text','ref_select','ref_back','ref_prize_back','ref_prize_front','ref_coin','ref_plus','ref_capacity_fish'})

for name,key,box in [
    ('ref_icon_play','game',(547,369,618,439)),('ref_icon_pause','game',(617,369,688,439)),
    ('ref_icon_shop','game',(684,368,758,439)),('ref_icon_home','game',(754,369,828,440)),
    ('ref_icon_medal','game',(616,436,687,506)),('ref_icon_trophy','game',(687,436,758,506)),
    ('ref_icon_map','game',(617,500,687,570)),('ref_icon_gift','menus',(678,499,746,566)),
    ('ref_icon_settings','menus',(612,499,681,565)),('ref_icon_outfit','menus',(341,69,418,144)),
    ('ref_icon_hat','menus',(420,68,498,144)),('ref_icon_accessory','menus',(341,156,419,230)),
]: save(name,key,box,True)

# Blank scalable controls are extracted from existing borders and letter-free faces.
blank('ref_button_green','game',(156,502,282,555),(19,12,109,43),(172,514,174,545))
blank('ref_button_blue','game',(280,502,407,555),(18,12,109,43),(295,514,297,545))
blank('ref_button_red','menus',(289,519,408,557),(20,8,98,29),(302,528,305,548))
# Empty original card/frame, filled with pixels from the original clean card face.
blank('ref_card_white','game',(254,157,337,297),(8,8,75,129),(260,180,264,210))
blank('ref_panel','game',(555,14,1018,322),(10,9,453,298),(599,110,600,111))
for name,box in [('gold',(574,76,703,301)),('silver',(713,76,842,301)),('epic',(850,76,980,301))]:
    blank('ref_frame_'+name,'menus',box,(7,7,122,217),(594,184,595,185))

for name,box in [
    ('ref_mountains',(15,30,677,165)),('ref_canyon',(674,37,1008,165)),
    ('ref_pines',(13,190,320,329)),('ref_pine',(13,197,91,329)),
    ('ref_oak',(406,216,487,329)),('ref_gold_tree',(551,207,650,329)),
    ('ref_willow',(749,204,848,330)),('ref_bushes',(14,374,118,426)),
    ('ref_shore_left',(560,375,777,502)),('ref_shore_right',(790,439,1008,502)),
    ('ref_cloud',(795,372,863,405)),('ref_cloud_long',(912,372,1002,405)),
    ('ref_rocks',(13,496,126,559)),
]: save(name,'nature',box,True)

(OUT/'ExtractionManifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

# Clear only the printed sample values with letter-free pixels from the same
# counter/panel. A feathered alpha edge joins the untouched original background.
def field(name,strip,width,height,feather=4):
    if UI_EDGES_ONLY:return
    scale=images['game'].width/1024 if name.startswith('ref_wallet') or name.startswith('ref_capacity') else 1
    key='game' if scale!=1 else 'menus'
    im=cut(key,strip).resize((round(width*scale),round(height*scale)),Image.Resampling.BICUBIC)
    mask=Image.new('L',im.size);p=mask.load();w,h=im.size;f=feather*scale
    for y in range(h):
        for x in range(w):p[x,y]=round(255*min(1,min(x,y,w-1-x,h-1-y)/f))
    im.putalpha(mask);im.save(OUT/(name+'.png'))
field('ref_wallet_fill',(88,32,91,74),107,42)
field('ref_capacity_fill',(361,32,363,74),70,42)
field('ref_color_cover',(500,157,512,249),91,99)
field('ref_preview_fill',(105,86,119,123),181,166)
field('ref_level_fill',(594,184,595,185),110,21,1.5)
field('ref_description_fill',(594,184,595,185),109,28,2)
field('ref_price_hook',(685,272,687,286),50,25,2)
field('ref_price_depth',(824,272,826,286),50,25,2)
field('ref_price_speed',(963,272,965,286),50,25,2)
print(f'Extracted {len(manifest["sprites"])} source-art sprites into {OUT}')

"""Nine-sliced Unity UI artwork matching the user's blue/glossy references.
Only draws new UI primitives; does not modify the user's reference images.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import math

OUT=Path(__file__).resolve().parents[1]/'Assets/LittleDepths/Art/UIV2'
OUT.mkdir(parents=True,exist_ok=True)
S=3

def rgb(h): return tuple(bytes.fromhex(h))
def plate(name,top,bottom,border,inner='ffffff',wood=False):
    w=h=192; im=Image.new('RGBA',(w*S,h*S));d=ImageDraw.Draw(im)
    d.rounded_rectangle((3*S,8*S,189*S,191*S),26*S,fill='#071b46aa')
    d.rounded_rectangle((3*S,2*S,189*S,184*S),25*S,fill='#'+border)
    mask=Image.new('L',im.size);ImageDraw.Draw(mask).rounded_rectangle((10*S,9*S,182*S,174*S),19*S,fill=255)
    grad=Image.new('RGBA',im.size);gd=ImageDraw.Draw(grad)
    c1,c2=rgb(top),rgb(bottom)
    for y in range(h*S):
        t=max(0,min(1,(y/S-9)/165));c=tuple(round(a*(1-t)+b*t) for a,b in zip(c1,c2))
        gd.line((0,y,w*S,y),fill=c)
    im.paste(grad,(0,0),mask);d=ImageDraw.Draw(im)
    d.rounded_rectangle((10*S,9*S,182*S,174*S),19*S,outline='#'+inner+'b0',width=2*S)
    if wood:
        grain=Image.new('RGBA',im.size);d=ImageDraw.Draw(grain)
        for y in (44,81,118,151):
            d.line([(12*S,y*S),(90*S,(y+6)*S),(180*S,(y-3)*S)],fill='#6a341f50',width=3*S)
            d.line([(13*S,(y+3)*S),(175*S,(y+1)*S)],fill='#ffe4a124',width=S)
        im=Image.alpha_composite(im,grain)
    im.resize((w,h),Image.Resampling.LANCZOS).save(OUT/(name+'.png'))

plate('ui_panel','176bb6','313090','4bafe5','7dd8f8')
plate('ui_inset','153b79','11275b','10295e','285e9f')
plate('ui_wood','c4874e','874b2b','442b22','f0ba78',True)
plate('ui_white','fffefa','e2efff','788db1','ffffff')
plate('ui_green','9fe527','39a50e','184e24','d8ff78')
plate('ui_red','ff7066','d52d32','682139','ffb4a4')
plate('ui_blue','5edbff','138cd3','114e87','adf5ff')
plate('ui_purple','da8bff','8e42cf','412b73','f1c8ff')
plate('ui_gold','fff076','ffc326','965a16','fff9b2')
plate('ui_bronze_frame','3b658f','20366d','b97d53','e6b385')
plate('ui_silver_frame','3b6899','283a78','90b9df','d7ecff')
plate('ui_gold_frame','345c93','32377c','f6c943','fff5ae')
plate('ui_epic_frame','635290','3e3179','ba68e4','f0b9ff')

im=Image.new('RGBA',(384*S,384*S));d=ImageDraw.Draw(im)
d.ellipse((10*S,16*S,374*S,382*S),fill='#07335980')
d.ellipse((7*S,3*S,377*S,373*S),fill='#895021')
d.ellipse((13*S,9*S,371*S,367*S),fill='#ffeaa2')
mask=Image.new('L',im.size);ImageDraw.Draw(mask).ellipse((22*S,18*S,362*S,358*S),fill=255)
grad=Image.new('RGBA',im.size);gd=ImageDraw.Draw(grad)
for y in range(384*S):
    t=max(0,min(1,y/(384*S)));c=tuple(round(a*(1-t)+b*t) for a,b in zip((255,226,84),(255,92,42)))
    gd.line((0,y,384*S,y),fill=c)
im.paste(grad,(0,0),mask)
gloss=Image.new('RGBA',im.size);d=ImageDraw.Draw(gloss)
for y in range(179,350,25):
    pts=[(x*S,(y+5*math.sin(x*.055))*S) for x in range(29,355)]
    d.line(pts,fill='#ffef8b50',width=6*S)
d.ellipse((52*S,39*S,251*S,161*S),fill='#ffffff30')
gloss.putalpha(ImageChops.multiply(gloss.getchannel('A'),mask));im=Image.alpha_composite(im,gloss);d=ImageDraw.Draw(im)
d.arc((40*S,36*S,344*S,340*S),-92,-7,fill='#b34e25',width=13*S)
d.arc((40*S,36*S,344*S,340*S),-90,-9,fill='#f99e27',width=8*S)
im.resize((384,384),Image.Resampling.LANCZOS).save(OUT/'ui_play_disc.png')

# UI-specific decorative seaweed, silhouette, and progress track.
im=Image.new('RGBA',(160*S,300*S));d=ImageDraw.Draw(im)
for b in range(4):
    ys=range(35+b*14,301)
    edge1=[((16+b*34+12*math.sin(y*.035+b))*S,y*S) for y in ys]
    edge2=[((30+b*34+12*math.sin(y*.035+b))*S,y*S) for y in reversed(ys)]
    d.polygon(edge1+edge2,fill=['#4eae2b','#70c744','#2b983f','#8cd949'][b])
im.resize((160,300),Image.Resampling.LANCZOS).save(OUT/'ui_seaweed.png')
im=Image.new('RGBA',(200*S,120*S));d=ImageDraw.Draw(im)
d.ellipse((53*S,23*S,185*S,99*S),fill='#14294f');d.polygon([(70*S,60*S),(16*S,19*S),(16*S,101*S)],fill='#14294f')
im.resize((200,120),Image.Resampling.LANCZOS).save(OUT/'ui_fish_shadow.png')
im=Image.new('RGBA',(32,32),'white');im.save(OUT/'ui_pixel.png')
print('Created glossy blue panels, rarity frames, wooden counters, and buttons.')

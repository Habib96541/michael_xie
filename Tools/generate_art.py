"""Original Hi/Lo card set and UI art. Pillow, no external art dependencies."""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from pathlib import Path
import math

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/HiLo/Resources/Art'
OUT.mkdir(parents=True, exist_ok=True)
FONT = str(ROOT / 'Assets/HiLo/Resources/Fonts/Outfit.ttf')
NAVY = '#14252e'
TEAL = '#40dfbc'
RED = '#dc665e'
GOLD = '#e9c479'

def suit(d, s, cx, cy, size, fill):
    def poly(points): d.polygon([(cx+x*size,cy+y*size) for x,y in points], fill=fill)
    if s == 3: poly([(0,-.55),(.4,0),(0,.55),(-.4,0)])
    elif s == 1:
        pts=[]
        for i in range(100):
            t=i/100*math.tau
            x=16*math.sin(t)**3/34
            y=-(13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t))/34
            pts.append((x,y))
        poly(pts)
    elif s == 0:
        pts=[]
        for i in range(100):
            t=i/100*math.tau
            pts.append((16*math.sin(t)**3/34, (13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t))/34-.08))
        poly(pts); poly([(-.16,.45),(.16,.45),(0,.1)])
    else:
        for x,y in [(0,-.22),(-.22,.05),(.22,.05)]:
            d.ellipse((cx+(x-.24)*size,cy+(y-.24)*size,cx+(x+.24)*size,cy+(y+.24)*size),fill=fill)
        poly([(-.16,.48),(.16,.48),(0,.1)])

W,H=420,588
for s in range(4):
    color = RED if s in (1,3) else NAVY
    for rank in range(2,15):
        im=Image.new('RGBA',(W,H)); d=ImageDraw.Draw(im)
        d.rounded_rectangle((1,1,W-2,H-2),radius=22,fill='#f7f4e9',outline='#ffffff',width=3)
        label={11:'J',12:'Q',13:'K',14:'A'}.get(rank,str(rank))
        f=ImageFont.truetype(FONT,56)
        d.text((43,22),label,font=f,fill=color,anchor='mt')
        suit(d,s,43,105,31,color)
        corner=im.crop((0,0,85,135)).rotate(180)
        im.alpha_composite(corner,(W-85,H-135))
        d=ImageDraw.Draw(im)
        if rank>=11 and rank<=13:
            d.rounded_rectangle((96,133,324,455),radius=80,outline=color,width=2)
            d.line([(116,294),(210,174),(304,294),(210,414),(116,294)],fill=color,width=2)
            suit(d,s,210,293,110,color)
            d.text((210,148),label,font=ImageFont.truetype(FONT,30),fill=color,anchor='mt')
            d.text((210,407),label,font=ImageFont.truetype(FONT,30),fill=color,anchor='mt')
        elif rank==14: suit(d,s,210,294,164,color)
        else:
            positions={2:[(.5,.26),(.5,.74)],3:[(.5,.26),(.5,.5),(.5,.74)],4:[(.32,.26),(.68,.26),(.32,.74),(.68,.74)],5:[(.32,.26),(.68,.26),(.5,.5),(.32,.74),(.68,.74)],6:[(x,y) for y in (.25,.5,.75) for x in (.32,.68)],7:[(x,y) for y in (.25,.5,.75) for x in (.32,.68)]+[(.5,.375)],8:[(x,y) for y in (.25,.5,.75) for x in (.32,.68)]+[(.5,.375),(.5,.625)],9:[(x,y) for y in (.23,.41,.59,.77) for x in (.32,.68)]+[(.5,.5)],10:[(x,y) for y in (.23,.41,.59,.77) for x in (.32,.68)]+[(.5,.32),(.5,.68)]}[rank]
            for x,y in positions: suit(d,s,W*x,H*y,55,color)
        im.save(OUT/f'card_{s}_{rank}.png',optimize=True)

im=Image.new('RGBA',(W,H));d=ImageDraw.Draw(im)
d.rounded_rectangle((1,1,W-2,H-2),22,fill='#193e46',outline='#75a395',width=3)
d.rounded_rectangle((16,16,W-17,H-17),14,outline='#60897d',width=2)
for y in range(-W,H+W,32): d.line([(24,y),(W-24,y+W-48)],fill='#25515a',width=2)
for y in range(-W,H+W,32): d.line([(W-24,y),(24,y+W-48)],fill='#25515a',width=2)
d.ellipse((118,202,302,386), fill='#193e46',outline='#d8bf83',width=2)
d.polygon([(210,229),(249,282),(224,282),(224,344),(196,344),(196,282),(171,282)], fill='#dcc489')
im.save(OUT/'card_back.png',optimize=True)

im=Image.new('RGBA',(64,64));ImageDraw.Draw(im).rounded_rectangle((0,0,63,63),12,fill='white');im.save(OUT/'rounded.png')
im=Image.new('RGBA',(64,64));ImageDraw.Draw(im).ellipse((0,0,63,63),fill='white');im.save(OUT/'circle.png')
im=Image.new('RGBA',(256,256))
for y in range(256):
    for x in range(256):
        a=max(0,1-math.sqrt(((x-128)/128)**2+((y-128)/128)**2))**2
        im.putpixel((x,y),(65,207,170,int(a*90)))
im.save(OUT/'glow.png')

def icon(name,draw):
    im=Image.new('RGBA',(128,128));draw(ImageDraw.Draw(im));im.resize((96,96),Image.Resampling.LANCZOS).save(OUT/f'icon_{name}.png')
icon('up',lambda d:d.line([(27,69),(64,32),(101,69),(64,32),(64,102)],fill='white',width=9,joint='curve'))
icon('down',lambda d:d.line([(27,59),(64,96),(101,59),(64,96),(64,26)],fill='white',width=9,joint='curve'))
def coin(d):
    d.ellipse((24,24,104,104),outline='white',width=7);d.ellipse((36,36,92,92),outline='white',width=2);d.line((64,44,64,84),fill='white',width=6)
icon('coin',coin)
def trophy(d):
    d.line([(37,26),(91,26),(87,65),(77,79),(51,79),(41,65),(37,26)],fill='white',width=7,joint='curve')
    d.line([(37,36),(21,36),(24,60),(43,64)],fill='white',width=6);d.line([(91,36),(107,36),(104,60),(85,64)],fill='white',width=6)
    d.line((64,80,64,103),fill='white',width=6);d.line((43,104,85,104),fill='white',width=7)
icon('trophy',trophy)
def flame(d):
    d.polygon([(66,14),(82,47),(94,35),(103,70),(96,97),(76,111),(47,110),(27,91),(24,70),(39,42),(40,69),(55,54)],fill='white')
icon('flame',flame)
def gear(d):
    for i in range(8):
        a=i*math.tau/8
        d.line([(64+30*math.cos(a),64+30*math.sin(a)),(64+46*math.cos(a),64+46*math.sin(a))],fill='white',width=14)
    d.ellipse((29,29,99,99),outline='white',width=12);d.ellipse((53,53,75,75),outline='white',width=5)
icon('settings',gear)
icon('collect',lambda d:(d.line([(64,23),(64,76),(43,55),(64,76),(85,55)],fill='white',width=7,joint='curve'),d.line([(27,78),(27,103),(101,103),(101,78)],fill='white',width=7,joint='curve')))
icon('close',lambda d:(d.line((34,34,94,94),fill='white',width=8),d.line((94,34,34,94),fill='white',width=8)))
icon('help',lambda d:(d.ellipse((21,21,107,107),outline='white',width=6),d.text((64,34),'?',font=ImageFont.truetype(FONT,60),fill='white',anchor='mt')))
icon('fullscreen',lambda d:(d.line([(47,25),(25,25),(25,47)],fill='white',width=7),d.line([(81,25),(103,25),(103,47)],fill='white',width=7),d.line([(25,81),(25,103),(47,103)],fill='white',width=7),d.line([(81,103),(103,103),(103,81)],fill='white',width=7)))
print('Generated 52 faces, card back, 10 icons, UI sprites.')

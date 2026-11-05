"""Original vector-style artwork and synthesized audio. No third-party art/SFX."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import math, random, wave, struct

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/CoinFlip/Resources/Art'
AUDIO = ROOT / 'Assets/CoinFlip/Resources/Audio'
ART.mkdir(parents=True, exist_ok=True)
AUDIO.mkdir(parents=True, exist_ok=True)
S = 3

def save(im, name):
    im.resize((im.width//S, im.height//S), Image.Resampling.LANCZOS).save(ART / (name+'.png'))

def star(d, x,y,r,fill,points=4):
    p=[]
    for i in range(points*2):
        a=-math.pi/2+i*math.pi/points; rr=r if i%2==0 else r*.27
        p.append(((x+math.cos(a)*rr)*S,(y+math.sin(a)*rr)*S))
    d.polygon(p,fill=fill)

def face(name, tails=False):
    """Draw the coin's two sides as restrained, immediately recognizable icons."""
    im=Image.new('RGBA',(512*S,512*S)); d=ImageDraw.Draw(im)
    def ellipse(box,fill,outline=None,width=1):
        d.ellipse(tuple(round(v*S) for v in box),fill=fill,outline=outline,width=width*S)
    def line(points,fill,width=1):
        d.line([(round(x*S),round(y*S)) for x,y in points],fill=fill,width=width*S,joint='curve')
    edge,metal,field,ink,highlight = (
        ('#153c4b','#287c8c','#78cfce','#e0fbef','#2a7680') if tails else
        ('#684514','#b27a25','#f1c25c','#fff2cf','#9c6822')
    )
    ellipse((11,11,501,501),edge)
    ellipse((20,20,492,492),metal)
    ellipse((31,31,481,481),field)
    ellipse((48,48,464,464),None,highlight,3)
    ellipse((70,70,442,442),None,ink,2)
    if tails:
        # A small three-feather emblem for the reverse side.
        d.polygon([(256*S,356*S),(196*S,260*S),(198*S,205*S),(222*S,159*S),
                   (256*S,193*S),(290*S,159*S),(314*S,205*S),(316*S,260*S)],fill=ink)
        d.polygon([(256*S,341*S),(210*S,250*S),(213*S,211*S),(232*S,181*S),
                   (256*S,218*S),(280*S,181*S),(299*S,211*S),(302*S,250*S)],fill=highlight)
        line([(256,335),(256,197)],ink,5)
        for y,half in [(231,24),(253,31),(276,34),(300,25)]:
            line([(256,y),(256-half,y-24)],ink,4)
            line([(256,y),(256+half,y-24)],ink,4)
    else:
        # A simple bust portrait with an uncluttered face and shoulders.
        ellipse((194,116,318,248),ink)
        ellipse((202,123,310,239),highlight)
        d.rounded_rectangle((228*S,219*S,284*S,296*S),radius=17*S,fill=highlight)
        d.rounded_rectangle((154*S,277*S,358*S,382*S),radius=82*S,fill=ink)
        d.polygon([(216*S,279*S),(256*S,316*S),(296*S,279*S),(320*S,366*S),(192*S,366*S)],fill=highlight)
        line([(233,282),(256,307),(279,282)],ink,4)
        ellipse((222,174,232,184),ink)
        ellipse((276,174,286,184),ink)
        d.arc((230*S,182*S,279*S,219*S),10,100,fill=ink,width=4*S)
    save(im,name)

face('Heads'); face('Tails',True)

# Reusable 9-sliced rounded surfaces and crisp icons.
im=Image.new('RGBA',(96*S,96*S)); d=ImageDraw.Draw(im)
d.rounded_rectangle((0,0,96*S-1,96*S-1),radius=24*S,fill='white'); save(im,'Rounded')
im=Image.new('RGBA',(128*S,128*S)); d=ImageDraw.Draw(im); d.ellipse((1,1,128*S-2,128*S-2),fill='white');save(im,'Circle')
im=Image.new('RGBA',(128*S,128*S));d=ImageDraw.Draw(im);d.ellipse((3*S,3*S,125*S,125*S),outline='white',width=2*S);save(im,'Ring')
im=Image.new('RGBA',(64*S,64*S));star(ImageDraw.Draw(im),32,32,29,'white');save(im,'Sparkle')
im=Image.new('RGBA',(256*S,64*S));d=ImageDraw.Draw(im);d.ellipse((20*S,15*S,236*S,49*S),fill=(0,0,0,180)); im=im.filter(ImageFilter.GaussianBlur(9*S));save(im,'Shadow')
im=Image.new('RGBA',(512*S,512*S)); d=ImageDraw.Draw(im)
for r in range(255,0,-1):
    alpha=round(20*(1-r/256)**1.6);d.ellipse(((256-r)*S,(256-r)*S,(256+r)*S,(256+r)*S),fill=(87,168,180,alpha))
save(im,'Aura')
im=Image.new('RGBA',(64*S,64*S));d=ImageDraw.Draw(im)
d.polygon([(13*S,26*S),(23*S,26*S),(35*S,16*S),(35*S,48*S),(23*S,38*S),(13*S,38*S)],fill='white')
d.arc((21*S,15*S,52*S,49*S),-60,60,fill='white',width=3*S);d.arc((27*S,22*S,45*S,42*S),-60,60,fill='white',width=3*S);save(im,'Sound')
im=Image.new('RGBA',(64*S,64*S));d=ImageDraw.Draw(im)
d.arc((12*S,12*S,52*S,52*S),-65,225,fill='white',width=4*S);d.polygon([(9*S,31*S),(10*S,13*S),(26*S,21*S)],fill='white');save(im,'Reset')
im=Image.new('RGBA',(64*S,64*S));d=ImageDraw.Draw(im)
d.line([(17*S,33*S),(27*S,43*S),(48*S,20*S)],fill='white',width=5*S);save(im,'Check')
im=Image.new('RGBA',(64*S,64*S));d=ImageDraw.Draw(im)
d.line((19*S,19*S,45*S,45*S),fill='white',width=4*S);d.line((45*S,19*S,19*S,45*S),fill='white',width=4*S);save(im,'Close')

# Original, deterministic PCM synthesis. Small clips are compressed by the importer.
SR=22050
def synth(name,duration,notes,vol=.35,noise=0):
    rng=random.Random(name); n=int(SR*duration); samples=[]
    for i in range(n):
        t=i/SR; value=0
        for freq,start,length,gain in notes:
            q=t-start
            if 0<=q<length:
                envelope=min(1,q/.006)*math.exp(-q*6/length)*(1-q/length)
                value+=gain*envelope*(math.sin(math.tau*freq*q)+.18*math.sin(math.tau*freq*2.01*q))
        value+=noise*rng.uniform(-1,1)*math.exp(-t*35)
        samples.append(struct.pack('<h',int(max(-1,min(1,value*vol))*32767)))
    with wave.open(str(AUDIO/(name+'.wav')),'wb') as f:
        f.setparams((1,2,SR,n,'NONE','not compressed'));f.writeframes(b''.join(samples))
synth('Hover',.08,[(1100,0,.07,.5)],.12)
synth('Press',.12,[(480,0,.1,.8),(720,.025,.08,.3)],.25)
synth('Select',.19,[(660,0,.1,.6),(990,.045,.14,.5)],.28)
synth('Launch',.22,[(290+i*90,i*.018,.09,.3) for i in range(8)],.22,.1)
synth('Tick',.055,[(1800,0,.045,.3),(2800,0,.02,.25)],.22,.25)
synth('Slow',.13,[(920,0,.12,.4),(1350,.015,.07,.3)],.22,.15)
synth('Land',.24,[(180,0,.18,.65),(730,0,.22,.35),(1450,.015,.18,.25)],.4,.2)
synth('Win',.65,[(659,0,.3,.5),(831,.09,.3,.5),(988,.18,.4,.5),(1318,.28,.35,.4)],.35)
synth('Lose',.42,[(440,0,.2,.45),(330,.1,.25,.4),(294,.18,.22,.3)],.25)
synth('Balance',.12,[(1450,0,.11,.35)],.18)
synth('Transition',.16,[(520,0,.15,.4),(780,.04,.1,.2)],.22)
# Eight-second seamless ambient pad with a slow attack/release at loop boundaries.
duration=8;n=SR*duration;frames=[]
for i in range(n):
    t=i/SR; env=min(1,t/.8,(duration-t)/.8)
    v=sum(math.sin(math.tau*f*t)*.035 for f in [130.8125,164.8125,196,261.625])
    frames.append(struct.pack('<h',int(v*env*32767)))
with wave.open(str(AUDIO/'Music.wav'),'wb') as f:
    f.setparams((1,2,SR,n,'NONE','not compressed'));f.writeframes(b''.join(frames))

try:
    from fontTools.ttLib import TTFont
    from fontTools.varLib.instancer import instantiateVariableFont
    path=ROOT/'Assets/CoinFlip/Resources/Fonts/Outfit.ttf'
    font=TTFont(path)
    if 'fvar' in font:
        instantiateVariableFont(font,{'wght':500},inplace=True).save(path)
except ImportError:
    pass
print('Generated original artwork, 12 audio clips, and font.')

"""Reproducible original sprites and synthesized music/SFX. No asset-store content."""
from pathlib import Path
import math, wave, urllib.request
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT/'Assets/BrickRush/Resources/Art'
AUDIO = ROOT/'Assets/BrickRush/Resources/Audio'
FONTS = ROOT/'Assets/BrickRush/Fonts'
for p in (ART, AUDIO, FONTS): p.mkdir(parents=True, exist_ok=True)

def rounded(name, size, radius, mode='brick'):
    s=4; w,h=size; im=Image.new('RGBA',(w*s,h*s)); d=ImageDraw.Draw(im)
    d.rounded_rectangle((s,s,w*s-s,h*s-s),radius=radius*s,fill=(255,255,255,255))
    if mode=='brick':
        d.rounded_rectangle((3*s,3*s,(w-3)*s,(h-3)*s),radius=max(1,(radius-2)*s),fill=(175,185,210,255))
        d.rounded_rectangle((4*s,4*s,(w-4)*s,(h*.50)*s),radius=radius*s/2,fill=(235,245,255,255))
        d.line((9*s,5*s,(w-9)*s,5*s),fill=(255,255,255,255),width=s)
    im.resize(size,Image.Resampling.LANCZOS).save(ART/(name+'.png'))

rounded('brick',(192,72),10)
rounded('panel',(96,96),16,'flat')
rounded('paddle',(256,48),22)
for name,kind in [('armored','armored'),('explosive','explosive'),('bonus','bonus'),('obstacle','obstacle')]:
    im=Image.new('RGBA',(128,64)); d=ImageDraw.Draw(im)
    if kind=='armored':
        for x in (51,73): d.line((x,17,x,47),fill=(255,255,255,230),width=4)
    elif kind=='explosive':
        d.polygon([(64,12),(69,26),(83,31),(69,36),(64,51),(59,36),(45,31),(59,26)],fill='white')
    elif kind=='bonus':
        d.polygon([(64,14),(82,32),(64,50),(46,32)],outline='white',width=3)
        d.ellipse((61,29,67,35),fill='white')
    else:
        for x in range(20,110,17): d.line((x,18,x-15,46),fill=(255,255,255,170),width=3)
    im.save(ART/(name+'.png'))

for name in ('ball','glow','ring','spark'):
    n=128; y,x=np.mgrid[:n,:n]; r=np.sqrt((x-(n-1)/2)**2+(y-(n-1)/2)**2)/(n/2)
    if name=='glow': a=np.clip(np.exp(-r*r*5.5)*np.clip(1-r,0,1)**.6,0,1)
    elif name=='ring': a=np.clip(1-np.abs(r-.80)/.055,0,1)
    elif name=='spark': a=np.clip(1-r,0,1)**2
    else: a=np.clip((.87-r)*30,0,1)
    rgba=np.ones((n,n,4),dtype=np.uint8)*255; rgba[:,:,3]=(a*255).astype(np.uint8)
    Image.fromarray(rgba).save(ART/(name+'.png'))

SR=22050
def save(name,data):
    data=np.tanh(data)*.8
    with wave.open(str(AUDIO/(name+'.wav')),'w') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(SR); f.writeframes((data*32767).astype('<i2').tobytes())
def tone(freq,duration,kind='sine'):
    t=np.arange(int(SR*duration))/SR; env=(1-np.exp(-t*120))*np.exp(-t*6/duration)
    v=np.sin(2*np.pi*freq*t)
    if kind=='bell': v+=.35*np.sin(2*np.pi*freq*2.01*t)+.15*np.sin(2*np.pi*freq*3*t)
    return v*env
for name,f,dur in [('wall',360,.065),('paddle',190,.12),('hit',570,.075),('break',800,.14),('countdown',480,.16),('launch',960,.2),('collect',700,.3),('combo',1100,.35),('highscore',880,.7),('gameover',180,.8),('lost',130,.45),('laser',1400,.1),('click',650,.08)]:
    data=tone(f,dur,'bell')
    if name in ('collect','combo','highscore'):
        for j,ratio in enumerate([1.25,1.5,2]):
            off=int(SR*.045*(j+1)); data[off:]+=tone(f*ratio,dur,'bell')[:len(data)-off]*.55
    save(name,data*.35)
rng=np.random.default_rng(83); t=np.arange(int(SR*.5))/SR
save('explosion',rng.uniform(-1,1,len(t))*np.exp(-t*12)*.45+np.sin(2*np.pi*(90*t-50*t*t))*np.exp(-t*10)*.6)
for name,tempo,amp in [('menu',100,.08),('music',120,.11)]:
    beat=60/tempo; length=beat*32; data=np.zeros(int(SR*length))
    chords=[[45,52,57,60],[41,48,53,57],[48,55,60,64],[43,50,55,59]]
    def mix(sample,start,gain=1):
        off=int(start*SR); count=min(len(sample),len(data)-off)
        if count>0:data[off:off+count]+=sample[:count]*gain
    for step in range(64):
        chord=chords[(step//16)%4]; midi=chord[step%4]+12
        freq=440*2**((midi-69)/12)
        mix(tone(freq,beat*.9,'bell'),step*beat/2,amp)
    for b in range(32):
        midi=chords[b//8][0]; mix(tone(440*2**((midi-69)/12),beat*1.4),b*beat,amp*1.2)
        if name=='music':
            t=np.arange(int(SR*.19))/SR
            kick=np.sin(2*np.pi*(60*t+8*(1-np.exp(-t*30))))*np.exp(-t*24)
            mix(kick,b*beat,.22)
            noise=rng.uniform(-1,1,int(SR*.055))*np.exp(-np.arange(int(SR*.055))/SR*90)
            mix(noise,b*beat+beat/2,.035)
            if b%2:mix(rng.uniform(-1,1,int(SR*.12))*np.exp(-np.arange(int(SR*.12))/SR*38),b*beat,.065)
    save(name,data)

for family,file in [('rajdhani','Rajdhani-Bold.ttf'),('barlow','Barlow-Medium.ttf')]:
    target=FONTS/file
    if not target.exists():urllib.request.urlretrieve('https://raw.githubusercontent.com/google/fonts/main/ofl/'+family+'/'+file,target)
    urllib.request.urlretrieve('https://raw.githubusercontent.com/google/fonts/main/ofl/'+family+'/OFL.txt',FONTS/(family+'-OFL.txt'))
print('Created original sprites, 16 audio clips, and OFL-licensed fonts.')

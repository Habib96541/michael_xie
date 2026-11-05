"""Reproducible original UI artwork and synthesized audio. No external assets."""
from pathlib import Path
import math, random, struct, wave
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1] / 'Assets/HydraulicPress/Art'
(root / 'Icons').mkdir(parents=True, exist_ok=True)
(root / 'Audio').mkdir(parents=True, exist_ok=True)
S = 4
for name in ['press', 'wallet', 'settings', 'replay', 'broken', 'sound', 'check', 'coin']:
    im = Image.new('RGBA', (128*S, 128*S))
    d = ImageDraw.Draw(im)
    def line(points, width=7): d.line([(int(x*S), int(y*S)) for x,y in points], fill='white', width=width*S, joint='curve')
    def box(rect, radius=5, fill=None, width=7):
        d.rounded_rectangle(tuple(int(x*S) for x in rect), radius=radius*S, fill=fill, outline='white', width=width*S)
    if name in ('press', 'broken'):
        line([(19,105),(19,22),(109,22),(109,105)])
        line([(12,106),(116,106)])
        line([(64,24),(64,57)])
        box((38,58,90,72),3, 'white')
        if name == 'press': box((46,85,82,97),5,'white')
        else: line([(56,78),(66,87),(56,94),(71,102)],5)
    elif name == 'wallet':
        box((18,37,111,103),10)
        line([(22,40),(22,29),(91,19),(98,37)])
        box((80,59,113,84),5,'white'); d.ellipse((91*S,68*S,96*S,73*S),fill=(0,0,0,0))
    elif name == 'settings':
        for y,x in [(30,45),(64,84),(98,49)]:
            line([(20,y),(108,y)],6); box((x-9,y-10,x+9,y+10),4,'white')
    elif name == 'replay':
        d.arc((25*S,25*S,104*S,104*S),-60,245,fill='white',width=8*S)
        line([(25,28),(25,55),(51,55)],8)
    elif name == 'sound':
        line([(20,52),(39,52),(62,31),(62,97),(39,77),(20,77),(20,52)])
        d.arc((49*S,37*S,94*S,91*S),-65,65,fill='white',width=6*S)
        d.arc((45*S,20*S,118*S,108*S),-60,60,fill='white',width=6*S)
    elif name == 'check': line([(27,66),(53,91),(103,37)],10)
    elif name == 'coin':
        d.ellipse((20*S,20*S,108*S,108*S),outline='white',width=7*S)
        line([(64,40),(64,87)],7); line([(49,46),(64,37),(78,46)],6)
    im.resize((128,128),Image.Resampling.LANCZOS).save(root/'Icons'/f'{name}.png')

rate=22050
durations={'motor':1.2,'impact':.4,'metal':.65,'glass':.5,'plastic':.28,'soft':.32,'liquid':.5,'elastic':.4,'success':.35,'collect':.85,'failure':1.25,'button':.09,'multiplier':.25,'fragment':.45,'music':8}
for name,duration in durations.items():
    rng=random.Random(name); samples=[]; smooth=0
    for i in range(int(rate*duration)):
        t=i/rate; p=t/duration; noise=rng.uniform(-1,1); smooth=smooth*.93+noise*.07
        env=min(1,t*180)*math.exp(-p*5)
        if name=='motor':
            v=(math.sin(2*math.pi*68*t)*.18+math.sin(2*math.pi*136*t)*.07+smooth*.3)*( .85+.15*math.sin(t*2*math.pi*5))
        elif name=='impact': v=(math.sin(2*math.pi*(100*t-70*t*t))*.55+smooth*.7)*env
        elif name=='metal': v=(math.sin(t*1540)*.22+math.sin(t*2330)*.16+smooth*.4)*env
        elif name=='glass': v=(noise*.2+math.sin(t*12500)*.13+math.sin(t*17070)*.15)*env*(.6+.4*math.sin(t*91)**2)
        elif name=='plastic': v=(noise*.3+smooth*.6)*env*(.2+.8*max(0,math.sin(t*90)))
        elif name=='soft': v=(smooth*2+math.sin(2*math.pi*(170*t-160*t*t))*.35)*env
        elif name=='liquid': v=(smooth*1.7+math.sin(t*(2100-1400*p))*.15)*env
        elif name=='elastic': v=math.sin(2*math.pi*(300*t-220*t*t+math.sin(t*45)*.4))*.4*env
        elif name in ('success','multiplier','collect'):
            notes=[523.25,659.25,783.99,1046.5] if name=='collect' else [659.25,987.77]
            step=duration/len(notes); k=min(len(notes)-1,int(t/step)); phase=t-k*step
            v=(math.sin(2*math.pi*notes[k]*phase)*.28+math.sin(4*math.pi*notes[k]*phase)*.06)*math.exp(-phase*10)*min(1,phase*200)
        elif name=='failure': v=(smooth*1.6+math.sin(2*math.pi*(74*t-14*t*t))*.3+noise*.10)*env
        elif name=='button': v=math.sin(2*math.pi*(600*t-1000*t*t))*.25*env
        elif name=='fragment': v=noise*.25*env*max(0,math.sin(t*61))**6
        else:
            fade=math.sin(math.pi*p)**2
            v=(math.sin(2*math.pi*130.8128*t)*.06+math.sin(2*math.pi*196.2192*t)*.035+math.sin(2*math.pi*261.6256*t)*.025)*fade
        samples.append(int(max(-.95,min(.95,v))*32767))
    with wave.open(str(root/'Audio'/f'{name}.wav'),'wb') as out:
        out.setnchannels(1); out.setsampwidth(2); out.setframerate(rate); out.writeframes(struct.pack('<'+'h'*len(samples),*samples))
print('Created 8 original icons and 15 original audio clips.')

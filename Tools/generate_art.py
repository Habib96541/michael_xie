"""Original, editable vector-style art and audio for Little Depths.
Run from the project root with Python, Pillow, aggdraw and fonttools.
No reference-game artwork is copied into this project.
"""
from pathlib import Path
import math, random, wave, struct, urllib.request
from PIL import Image, ImageDraw
import aggdraw

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/LittleDepths/Art'
ART.mkdir(parents=True, exist_ok=True)
S = 3

class Art:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new('RGBA', (w*S, h*S))
    def ellipse(self, box, color):
        ImageDraw.Draw(self.im).ellipse(tuple(int(v*S) for v in box), fill=color)
    def poly(self, points, color):
        ImageDraw.Draw(self.im).polygon([(int(x*S), int(y*S)) for x,y in points], fill=color)
    def line(self, points, color, width):
        ImageDraw.Draw(self.im).line([(int(x*S), int(y*S)) for x,y in points], fill=color, width=int(width*S), joint='curve')
    def path(self, commands, color, stroke=None, width=1):
        p = aggdraw.Path()
        for c in commands:
            kind, *a = c
            getattr(p, {'M':'moveto','L':'lineto','C':'curveto','Q':'curveto','Z':'close'}[kind])(*[v*S for v in a])
        d = aggdraw.Draw(self.im)
        d.path(p, aggdraw.Brush(color) if color else None, aggdraw.Pen(stroke, width*S) if stroke else None)
        d.flush()
    def save(self, name):
        self.im.resize((self.w,self.h), Image.Resampling.LANCZOS).save(ART / (name+'.png'))

def fish(name, body, fin, style='normal'):
    a=Art(240,150)
    a.path([('M',171,72),('C',193,47,207,40,225,44),('L',215,75),('L',226,108),('C',204,112,187,98,171,83),('Z',)],fin)
    a.path([('M',73,49),('C',87,15,119,13,139,42),('L',118,62),('Z',)],fin)
    a.path([('M',89,101),('C',100,137,132,140,146,108),('Z',)],fin)
    a.path([('M',25,76),('C',59,13,145,14,187,74),('C',147,137,61,137,25,76),('Z',)],body)
    if style=='stripe':
        for x in (86,120,151):
            a.path([('M',x,35),('C',x-18,65,x+21,85,x-3,119),('L',x+13,114),('C',x+37,79,x-3,62,x+15,40),('Z',)],fin)
    elif style=='clown':
        for x in (72,133):
            a.path([('M',x,35),('C',x-9,66,x-8,91,x+4,119),('L',x+25,116),('C',x+9,85,x+13,61,x+20,36),('Z',)],'#fff4df')
    elif style=='spot':
        for x,y,r in [(90,58,7),(127,89,9),(157,67,6),(84,97,5)]: a.ellipse((x-r,y-r,x+r,y+r),fin)
    elif style=='moon':
        a.ellipse((80,33,160,118),'#d4c9ff')
        a.ellipse((96,31,169,111),body)
    a.path([('M',98,80),('C',118,66,139,68,144,88),('C',132,102,111,100,98,80),('Z',)],fin)
    a.ellipse((42,51,73,83),'#fff9ea')
    a.ellipse((48,59,63,74),'#143c4b')
    a.ellipse((49,59,54,64),'#ffffff')
    a.path([('M',31,83),('C',37,88,43,89,48,84)],None,'#194858',2.5)
    a.ellipse((52,85,66,94),'#f2b5a1')
    a.save('fish_'+name)

fish('minnow','#6cddd8','#249fb4')
fish('perch','#c9e971','#68ad73','stripe')
fish('clown','#ff9a59','#e76550','clown')
fish('bluefin','#84bbf1','#4f7ec6','spot')
fish('sunfish','#ffc65a','#ef8c36','stripe')
fish('moonfish','#b3a3f1','#7d68c4','moon')
fish('ruby','#f28a9c','#b84c81','spot')
fish('golden','#ffe25c','#ffa62f','stripe')
fish('abyss','#66e4c8','#277f9f','moon')

# Jellyfish, another collectible species.
a=Art(190,210)
for x,h in [(50,174),(79,193),(112,182),(140,169)]:
    a.path([('M',x,103),('C',x-18,137,x+24,h,x+5,h+5)],None,'#9c83e8',9)
a.path([('M',26,105),('C',20,8,166,8,164,105),('C',143,129,49,127,26,105),('Z',)],'#c4b2fa')
a.path([('M',40,72),('C',52,33,109,25,133,48)],None,'#e3d8ff',8)
for x in (65,124): a.ellipse((x,79,x+10,92),'#294564')
a.path([('M',88,96),('C',94,102,101,102,107,96)],None,'#584988',3)
a.save('fish_jelly')

# Fisherman and boat. Side-on flat illustration with warm, tactile shapes.
a=Art(720,470)
a.ellipse((111,400,676,436),'#266e8030')
a.path([('M',107,329),('C',248,356,486,369,680,289),('C',652,391,562,422,389,417),('L',183,399),('Z',)],'#a6583e')
a.path([('M',117,329),('C',305,354,499,358,680,289),('L',657,328),('C',509,398,304,388,150,363),('Z',)],'#e0a661')
a.path([('M',173,365),('C',335,391,518,384,625,348)],None,'#f0c57b',6)
a.line([(264,369),(279,401)],'#804c3e',5)
a.line([(462,377),(450,412)],'#804c3e',5)
# dark boots and trousers
a.path([('M',401,267),('L',491,269),('L',505,348),('L',462,355),('L',434,307),('L',405,343),('L',355,339),('Z',)],'#263c48')
a.path([('M',345,331),('L',408,331),('L',414,350),('L',340,350),('Z',)],'#172f3d')
# coat and arms
a.path([('M',390,161),('C',465,123,500,181,501,281),('C',472,302,404,299,377,273),('L',367,205),('Z',)],'#ed7957')
a.path([('M',401,175),('C',397,205,370,238,334,242),('L',286,219),('L',296,197),('L',339,209),('L',370,173),('Z',)],'#f58c62')
a.line([(385,187),(365,228),(338,232)],'#cc594b',6)
a.poly([(421,160),(444,164),(451,285),(429,287)],'#d65a4c')
# face, beard and wool cap
a.path([('M',366,93),('C',407,74,448,99,440,149),('L',416,187),('L',380,179),('L',368,143),('C',343,143,347,125,362,123),('Z',)],'#f1bc91')
a.path([('M',366,137),('C',385,155,415,148,435,133),('C',451,182,417,208,391,193),('C',371,177,371,157,366,137),('Z',)],'#88503c')
a.ellipse((379,123,385,131),'#193947')
a.path([('M',358,112),('C',361,43,430,34,441,108),('Z',)],'#294b60')
a.path([('M',354,102),('C',380,94,416,96,445,103),('L',443,119),('L',355,118),('Z',)],'#426a7c')
a.ellipse((398,39,421,62),'#365d70')
for x in range(369,439,11): a.line([(x,80),(x-2,99)],'#527488',2)
a.ellipse((281,196,309,225),'#f4c69e')
# rod
a.path([('M',302,211),('C',255,164,200,98,133,75)],None,'#244c56',7)
a.path([('M',302,211),('L',274,184)],None,'#b99864',12)
a.ellipse((286,202,309,227),'#e2bd74')
a.ellipse((292,208,304,220),'#46666d')
a.path([('M',135,76),('C',159,90,177,117,185,152)],None,'#fff3cd',2)
# net
a.path([('M',519,331),('C',505,302,535,259,564,273),('L',612,318),('L',571,356),('Z',)],'#365766',None)
for x in range(516,600,15): a.line([(x,294),(x+43,333)],'#88aaa5',2)
for x in range(526,612,15): a.line([(x,341),(x+20,294)],'#88aaa5',2)
a.path([('M',519,331),('C',505,302,535,259,564,273),('L',612,318),('L',571,356),('Z',)],None,'#dfb278',5)
a.save('boat')

# Distant tree line. Transparent, repeats naturally.
random.seed(7)
a=Art(1600,260)
for i in range(42):
    x=i*43-30; h=random.randint(75,220); base=260
    c=random.choice(['#5b9e99','#79b4a6','#96c7af','#458d8c'])
    if i%3:
        a.poly([(x,base-h),(x-39,base-27),(x-16,base-35),(x-50,base),(x+54,base),(x+19,base-36),(x+38,base-25)],c)
    else:
        a.line([(x,base),(x,base-h//2)],'#5b9180',7)
        a.ellipse((x-35,base-h,x+35,base-22),c)
a.save('forest')

# Organic river banks and decorative coral.
for side in ('left','right'):
    a=Art(500,1600)
    edge=[]
    for y in range(-20,1640,8):
        edge.append((437+22*math.sin(y*.011)+12*math.cos(y*.025),y))
    a.poly([(0,0),(460,0)]+edge+[(0,1600)],'#766580')
    a.line(edge,'#b88496',10)
    a.path([('M',340,0),('C',260,280,425,380,308,650),('C',236,862,420,1010,303,1240),('L',330,1600),('L',0,1600),('L',0,0),('Z',)],'#695d78')
    if side=='right': a.im=a.im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    a.save('bank_'+side)

for name,color in [('coral','#4585a5'),('kelp','#52bfb5'),('deep_coral','#5b519a')]:
    a=Art(200,260)
    for branch in range(6):
        x=32+branch*27; top=35+(branch%3)*38
        a.path([('M',102,253),('C',102,185,x,162,x,top)],None,color,9)
        for y in range(top+36,195,32):
            a.path([('M',x,y),('C',x-20,y-4,x-28,y-15,x-27,y-28)],None,color,7)
            a.path([('M',x,y+8),('C',x+17,y+2,x+24,y-13,x+23,y-26)],None,color,7)
    a.save(name)

a=Art(180,180)
a.path([('M',105,30),('L',105,114),('C',105,167,37,166,38,114),('L',38,95)],None,'#ffffff',12)
a.poly([(25,107),(39,79),(50,111)],'#ffffff')
a.ellipse((92,7,119,34),'#ffffff')
a.ellipse((100,14,111,26),(0,0,0,0))
a.save('hook')

# UI sprites: nine-sliced cream cards, bubbles, stars, and small pictograms.
a=Art(96,96)
ImageDraw.Draw(a.im).rounded_rectangle((0,0,96*S-1,96*S-1),radius=23*S,fill='#ffffff')
a.save('rounded')
a=Art(96,96); a.ellipse((1,1,95,95),'#ffffff'); a.save('circle')
a=Art(90,90)
a.path([('M',45,0),('C',49,31,59,39,90,45),('C',59,49,49,59,45,90),('C',39,59,31,49,0,45),('C',31,39,39,31,45,0),('Z',)],'#ffffff')
a.save('sparkle')
a=Art(80,80); a.ellipse((5,5,75,75),'#d2fbff70'); a.ellipse((15,12,35,28),'#ffffffb0'); a.save('bubble')

def gradient(name, colors, height):
    im=Image.new('RGBA',(64,height)); pixels=im.load()
    for y in range(height):
        t=y/(height-1)*(len(colors)-1); k=min(int(t),len(colors)-2); f=t-k
        c=tuple(round(colors[k][i]*(1-f)+colors[k+1][i]*f) for i in range(3))+(255,)
        for x in range(64): pixels[x,y]=c
    im.save(ART/(name+'.png'))
gradient('sky',[(252,239,211),(222,239,210),(167,220,207)],512)
gradient('water',[(65,172,200),(48,126,185),(49,84,162),(67,52,139),(39,32,90)],2048)
a=Art(1600,100)
wavepoints=[(x,25+8*math.sin(x*.013)+3*math.sin(x*.031)) for x in range(0,1601,5)]
a.poly(wavepoints+[(1600,100),(0,100)],'#50b8c9');a.line(wavepoints,'#a3dfd1',3);a.save('surface')
a=Art(500,1400)
a.poly([(110,0),(185,0),(480,1400),(0,1400)],'#ffffff0a');a.save('sunray')
for name in ('coin','depth','capacity','income','journal','gift','settings','trophy','sound','trash'):
    a=Art(120,120)
    if name=='coin':
        a.ellipse((10,10,110,110),'#f8bd51'); a.ellipse((22,22,98,98),'#ffe49a'); a.ellipse((31,31,89,89),'#f6c564')
        a.line([(60,37),(60,82)],'#c18538',8)
    elif name=='depth':
        a.line([(62,12),(62,98)],'#508e9e',8); a.poly([(39,78),(62,106),(85,78)],'#508e9e')
        for y,w in [(27,30),(46,20),(65,30)]: a.line([(12,y),(12+w,y)],'#89bec1',6)
    elif name=='capacity':
        a.ellipse((8,31,92,91),'#63bfc1'); a.poly([(80,60),(114,35),(114,87)],'#3c98a9'); a.ellipse((24,47,35,59),'#214b60')
        a.line([(82,15),(82,43)],'#ef9168',7); a.line([(68,29),(96,29)],'#ef9168',7)
    elif name=='income':
        a.poly([(17,32),(104,32),(104,94),(17,94)],'#8fc7a0'); a.poly([(25,22),(96,22),(96,84),(25,84)],'#599d82')
        a.ellipse((43,36,80,71),'#c4e4ac'); a.line([(61,41),(61,66)],'#4f8d74',5)
    elif name=='journal':
        a.path([('M',17,23),('C',38,19,49,24,60,31),('C',77,22,90,21,104,24),('L',104,98),('C',88,92,73,95,60,102),('C',42,92,29,94,17,98),('Z',)],'#60999b')
        a.line([(60,31),(60,94)],'#d7e9ce',4)
        for y in (43,57,71): a.line([(29,y),(48,y+2)],'#d7e9ce',4); a.line([(73,y),(92,y-2)],'#d7e9ce',4)
    elif name=='gift':
        a.poly([(19,49),(101,49),(101,104),(19,104)],'#ef9d72'); a.poly([(14,38),(106,38),(106,58),(14,58)],'#f3bd80'); a.poly([(52,39),(68,39),(68,104),(52,104)],'#dc6660')
        a.path([('M',60,40),('C',-9,26,37,-8,60,40),('C',78,-8,129,27,60,40)],None,'#dc6660',8)
    elif name=='settings':
        for r in range(8):
            t=r*math.pi/4; a.line([(60+math.cos(t)*29,60+math.sin(t)*29),(60+math.cos(t)*43,60+math.sin(t)*43)],'#507c85',15)
        a.ellipse((26,26,94,94),'#507c85'); a.ellipse((46,46,74,74),'#f8f1df')
    elif name=='trophy':
        a.path([('M',34,19),('L',86,19),('L',82,62),('C',76,88,44,88,38,62),('Z',)],'#f7c55f')
        a.path([('M',36,31),('C',1,18,10,71,42,66),('M',84,31),('C',119,18,110,71,78,66)],None,'#dfa447',8)
        a.line([(60,78),(60,98)],'#dfa447',8); a.line([(40,101),(80,101)],'#dfa447',9)
    elif name=='sound':
        a.poly([(17,48),(37,48),(61,26),(61,95),(37,74),(17,74)],'#507c85')
        a.path([('M',76,38),('C',98,51,98,71,76,84)],None,'#507c85',7)
    else:
        a.path([('M',35,19),('L',65,19),('L',65,63),('L',98,76),('C',115,103,75,109,54,95),('L',32,81),('Z',)],'#7d8e91')
        a.line([(38,34),(64,34)],'#b4b7a2',6)
    a.save('icon_'+name)

# Short synthesized UI sounds, deliberately subtle.
audio=ROOT/'Assets/LittleDepths/Audio'; audio.mkdir(parents=True, exist_ok=True)
random.seed(20)
for name,freq,duration in [('catch',740,.19),('click',440,.085),('sell',523,.46),('legendary',880,.7),('splash',150,.34)]:
    rate=22050; samples=[]
    for i in range(int(rate*duration)):
        t=i/rate; env=math.sin(math.pi*t/duration)**1.5
        f=freq*(1+int(t/.12)*.25) if name in ('sell','legendary') else freq
        v=(math.sin(2*math.pi*f*t)*.65+math.sin(2*math.pi*f*2*t)*.15)*env*.23
        if name=='splash': v=(random.random()*2-1)*env*.12+math.sin(2*math.pi*freq*(1-t)*t)*env*.06
        samples.append(struct.pack('<h',int(v*32767)))
    with wave.open(str(audio/(name+'.wav')),'w') as out:
        out.setnchannels(1); out.setsampwidth(2); out.setframerate(rate); out.writeframes(b''.join(samples))

fonts=ROOT/'Assets/LittleDepths/Fonts'; fonts.mkdir(parents=True, exist_ok=True)
if not (fonts/'Nunito-SemiBold.ttf').exists():
    from fontTools.ttLib import TTFont
    from fontTools.varLib.instancer import instantiateVariableFont
    url='https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/'
    data=urllib.request.urlopen(url+'Nunito%5Bwght%5D.ttf').read()
    import io
    for weight,name in [(650,'Nunito-SemiBold'),(900,'Nunito-Black')]:
        font=instantiateVariableFont(TTFont(io.BytesIO(data)), {'wght':weight}, inplace=False)
        font.save(fonts/(name+'.ttf'))
    (fonts/'OFL.txt').write_bytes(urllib.request.urlopen(url+'OFL.txt').read())
print('Generated original art, UI sprites, audio, and OFL-licensed Nunito fonts.')

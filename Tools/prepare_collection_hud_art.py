"""Import complete generated skins and repair the code-authored repeating banks."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, math, wave, struct

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'Assets/LittleDepths/Art/RemasteredSheets'
OUT = ROOT / 'Assets/LittleDepths/Art/Provided'

def clean(image):
    image = image.convert('RGBA')
    image.putalpha(image.getchannel('A').point(lambda a: 0 if a < 32 else min(255, round((a-32)*255/96))))
    return image

def skin(source, name, size, box=None):
    image = Image.open(SRC/source)
    if box: image = image.crop(box)
    image = clean(image)
    image = image.crop(image.getbbox())
    image.thumbnail((size[0]-24, size[1]-24), Image.Resampling.LANCZOS)
    result = Image.new('RGBA', size)
    result.paste(image, ((size[0]-image.width)//2, (size[1]-image.height)//2))
    result.paste((0,0,0,0),(0,0,*size),result.getchannel('A').point(lambda a:255 if a==0 else 0))
    result.save(OUT/(name+'.png'))

skin('collection_panel_hd.png', 'ref_collection_panel', (1536, 860))
skin('collection_card_hd.png', 'ref_collection_card', (880, 1190))
skin('side_dive_hd.png', 'ref_dive_side', (960, 1480))
skin('pause_live_audio_hd.png', 'ref_pause', Image.open(OUT/'ref_pause.png').size)
for name,box in [('sound_on',(180,30,720,510)),('sound_off',(820,30,1370,510)),('music_on',(180,515,720,1010)),('music_off',(820,515,1370,1010))]:
    skin('audio_buttons_hd.png','ref_'+name,(512,512),box)

# These are code-authored scenery sprites. The endpoints and tangent of every
# layer repeat, including the inner rock shade; both banks share the same seam.
art = ROOT/'Assets/LittleDepths/Art'
for side in ['left','right']:
    path = art/('bank_'+side+'.png')
    w,h=Image.open(path).size
    scale=w/500
    image=Image.new('RGBA',(w,h))
    draw=ImageDraw.Draw(image)
    edge=[]; inner=[]
    for y in range(h+1):
        t=y/h*math.tau
        edge.append((round((437+22*math.sin(3*t)+12*math.cos(5*t))*scale), y))
        inner.append((round((330+35*math.sin(2*t+.5)+20*math.cos(3*t))*scale), y))
    draw.polygon([(0,0)]+edge+[(0,h)],fill='#766580')
    draw.line(edge,fill='#b88496',width=round(10*scale))
    draw.polygon([(0,0)]+inner+[(0,h)],fill='#695d78')
    # Pixel-periodic edge samples avoid even a one-pixel horizontal seam.
    image.paste(image.crop((0,0,w,1)),(0,h-1))
    if side=='right':image=image.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    image.save(path)

path=art/'sunray.png';w,h=Image.open(path).size
image=Image.new('RGBA',(w,h));draw=ImageDraw.Draw(image)
for y in range(h):
    t=y/(h-1);a=round(10*(1-t)**2)
    left=(110*(1-t))*w/500;right=(185*(1-t)+480*t)*w/500
    draw.line([(left,y),(right,y)],fill=(255,255,255,a),width=1)
image.save(path)

# A quiet ambient loop gives the separate music icon a real audio channel.
# Effects keep their existing source and clips. Unity imports this as Vorbis.
audio=ROOT/'Assets/LittleDepths/Resources';audio.mkdir(exist_ok=True)
rate=22050;duration=32;notes=[(130.8128,164.8138,195.9977),(110,130.8128,164.8138),(87.3071,110,130.8128),(97.9989,123.4708,146.8324)]
with wave.open(str(audio/'LittleDepthsAmbience.wav'),'wb') as file:
    file.setnchannels(1);file.setsampwidth(2);file.setframerate(rate)
    for i in range(rate*duration):
        t=i/rate;section=int(t/8)%4;local=t%8
        envelope=math.sin(math.pi*local/8)**2
        value=sum(math.sin(math.tau*f*t)*.014 for f in notes[section])*envelope
        file.writeframesraw(struct.pack('<h',round(value*32767)))
print('Prepared 7 complete UI skins, periodic bank tiles, fading rays and a quiet independent music loop.')

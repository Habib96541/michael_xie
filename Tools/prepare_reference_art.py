"""Extract inspected, irregular sprites without stretching; retain native cutouts."""
from pathlib import Path
import json
import shutil
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[1]
GEN=Path(r'C:/Users/tahmi/.codex/generated_images/01a190df-ff1d-7f32-b772-ab0e2fa0842d')
SRC=ROOT/'SourceArt/ReferenceRevision'
OUT=ROOT/'Assets/ChickenRush/Art/Arcade'
SRC.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
VEHICLE='exec-dbb6ad6c-9d13-4321-a4a8-006a704774c9.png'
PROPS='exec-ec636718-2e01-4697-98e4-ee6566ce1f85.png'
ASPHALT='exec-f357d7a8-1af6-429f-ab21-4fe3eab994a2.png'
for name,label in [(VEHICLE,'Vehicles-Source.png'),(PROPS,'Props-Source.png'),(ASPHALT,'Asphalt-Source.png')]:shutil.copy2(GEN/name,SRC/label)
records=[]
def extract(im,box,name):
    a=np.array(im.crop(box).convert('RGBA'))
    # Generator supplies alpha; discard only the documented low-alpha fringe.
    a[a[:,:,3]<24]=0
    a[:,:,3]=np.minimum(255,a[:,:,3].astype(np.uint16)*255//254).astype(np.uint8)
    frame=Image.fromarray(a);bounds=frame.getbbox()
    if not bounds:raise ValueError(name)
    frame=frame.crop(bounds)
    canvas=Image.new('RGBA',(frame.width+16,frame.height+16));canvas.paste(frame,(8,8))
    canvas.save(OUT/(name+'.png'))
    records.append(dict(name=name,sourceBox=box,trim=bounds,size=canvas.size,alphaZeroBorder=True))
    return canvas

vehicles=Image.open(SRC/'Vehicles-Source.png')
# These inspected separators follow actual gutters, not blind grid slicing.
xs=[0,320,620,925,1230,1536]
for row,direction in enumerate(['Down','Up']):
    for col,kind in enumerate(['Compact','Taxi','Van','Truck','Delivery']):
        extract(vehicles,(xs[col],0 if row==0 else 530,xs[col+1],530 if row==0 else 1024),kind+direction)
props=Image.open(SRC/'Props-Source.png')
for name,box in [('LandingPad',(0,0,690,565)),('Barrier',(695,0,1280,560)),('Shrub',(0,575,680,1240)),('Sidewalk',(700,565,1280,1235))]:extract(props,box,name)
shutil.copy2(SRC/('Asphalt-Final.png' if (SRC/'Asphalt-Final.png').exists() else 'Asphalt-Source.png'),OUT/'RoadSurface.png')
(SRC/'Extraction.json').write_text(json.dumps(records,indent=2))
(SRC/'Provenance.md').write_text('Original AI artwork generated with the built-in image-generation tool for this revision. Vehicles match the supplied chicken style. Prompts are recorded in the implementation report. Separate front/rear views; no reference-game artwork shipped. Native cuts only, 24/255 low-alpha noise cutoff, 8px transparent padding, no sprite resampling.\n')
thumbs=Image.new('RGB',(1400,780),'#17283b');draw=ImageDraw.Draw(thumbs)
for i,p in enumerate(sorted(OUT.glob('*.png'))):
    im=Image.open(p).convert('RGBA');im.thumbnail((220,220),Image.Resampling.LANCZOS)
    x=(i%6)*233+(233-im.width)//2;y=(i//6)*260
    thumbs.paste(im,(x,y),im);draw.text(((i%6)*233+10,y+235),p.stem,fill='white')
thumbs.save(SRC/'ContactSheet.png')
print(json.dumps(records,indent=2))

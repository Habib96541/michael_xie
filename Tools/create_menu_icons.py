from pathlib import Path
from PIL import Image, ImageDraw
folder=Path(__file__).resolve().parents[1]/'Assets/HydraulicPress/Art/Icons'
for name in ('pause','play','home'):
    im=Image.new('RGBA',(512,512)); d=ImageDraw.Draw(im)
    if name=='pause':
        for x in (140,292):d.rounded_rectangle((x,96,x+80,416),radius=16,fill='white')
    elif name=='play': d.polygon([(160,90),(422,256),(160,422)],fill='white')
    else:
        d.line([(68,238),(256,76),(444,238)],fill='white',width=28,joint='curve')
        d.line([(120,210),(120,426),(392,426),(392,210)],fill='white',width=28,joint='curve')
        d.line([(220,426),(220,296),(292,296),(292,426)],fill='white',width=24)
    im.resize((128,128),Image.Resampling.LANCZOS).save(folder/(name+'.png'))
print('Created pause, play and home icons.')

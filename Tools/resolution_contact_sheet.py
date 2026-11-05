from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

root=Path(__file__).resolve().parents[1]/'QA/Overhaul'
canvas=Image.new('RGB',(1900,1140),'#132234')
draw=ImageDraw.Draw(canvas)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
specs=[('1920x1080',16,16,608,342),('1366x768',646,16,608,342),('1280x720',1276,16,608,342),
       ('390x844',16,415,292,632),('360x800',326,415,284,632),('844x390',632,415,608,281),('768x1024',1262,415,474,632)]
for name,x,y,w,h in specs:
    image=Image.open(root/f'{name}-Gameplay.png').convert('RGB');image.thumbnail((w,h),Image.Resampling.LANCZOS)
    canvas.paste(image,(x,y+29));draw.text((x,y),name,fill='white',font=font)
danger=root/'Desktop-HighDanger.png'
if danger.exists():
    image=Image.open(danger).convert('RGB');image.thumbnail((608,342),Image.Resampling.LANCZOS);canvas.paste(image,(632,773));draw.text((632,744),'Lanes 11+ / traffic escalation',fill='white',font=font)
canvas.save(root/'Resolution-Matrix.png')
print(root/'Resolution-Matrix.png')

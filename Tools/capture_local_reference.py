from pathlib import Path
import subprocess
import imageio_ffmpeg
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[1]
out=root/'QA/ReferenceRevision/Reference'
out.mkdir(parents=True,exist_ok=True)
video=Path(r'C:/Users/tahmi/Downloads/Classic Crossy Roads.mp4')
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
result=subprocess.run([ffmpeg,'-hide_banner','-i',str(video)],capture_output=True,text=True)
(out/'LocalVideoMetadata.txt').write_text(result.stderr)
print(result.stderr[:2500])
sheet=Image.new('RGB',(1500,900),'#10202f');draw=ImageDraw.Draw(sheet)
for i,seconds in enumerate([2,5,8,10,14,17]):
    path=out/f'Local-{seconds:03}.png'
    result=subprocess.run([ffmpeg,'-y','-ss',str(seconds),'-i',str(video),'-frames:v','1',str(path)],capture_output=True)
    if path.exists():
        im=Image.open(path).convert('RGB');im.thumbnail((490,410))
        x=(i%3)*500;y=(i//3)*450;sheet.paste(im,(x,y));draw.text((x+10,y+420),f'{video.name} at {seconds}s',fill='white')
sheet.save(out/'Local-ContactSheet.jpg')

"""Register the imagegen eyelid to the native sprite; preserve all other pixels."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import shutil

root=Path(__file__).resolve().parents[1]
donor=Path(r'C:\Users\tahmi\.codex\generated_images\01a190df-ff1d-7f32-b772-ab0e2fa0842d\exec-5b893b96-3ee6-47a7-9188-f0c56195c4b0.png')
if donor.exists(): shutil.copy2(donor,root/'SourceArt/BlinkEdit.png')
else: donor=root/'SourceArt/BlinkEdit.png'
base=Image.open(root/'Assets/ChickenRush/Art/Chicken/Idle_00.png').convert('RGBA')
# The generator enlarged the canvas despite its request; use only its closed eye.
# Register that small patch to the original eye, not the regenerated silhouette.
eye=Image.open(donor).crop((727,550,885,705)).resize((23,23),Image.Resampling.LANCZOS)
mask=Image.new('L',(23,23));ImageDraw.Draw(mask).ellipse((1,1,21,21),fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(1))
base.paste(eye,(106,76),mask)
base.save(root/'Assets/ChickenRush/Art/Chicken/IdleBlink.png')
base.resize((768,768)).save(root/'QA/BlinkDetail.png')
print('Blink uses an eye patch only; original body pixels and foot pivot preserved.')

from pathlib import Path
from PIL import Image
import numpy as np
import json

root=Path(__file__).resolve().parents[1]
rows=[]
for path in sorted((root/'Assets/ChickenRush/Art/Chicken').glob('*.png')):
    a=np.array(Image.open(path))
    y,x=np.where(a[:,:,3]>128)
    border=int(max(a[0,:,3].max(),a[-1,:,3].max(),a[:,0,3].max(),a[:,-1,3].max()))
    rows.append(dict(file=path.name,size=list(a.shape[:2]),
        opaqueBox=[int(x.min()),int(y.min()),int(x.max()),int(y.max())],borderAlphaMax=border,
        hiddenColoredPixels=int(np.count_nonzero(np.any(a[:,:,:3]!=0,axis=2)&(a[:,:,3]==0)))))
    assert list(a.shape)==[192,192,4],path
    assert border==0,path
    assert a[:,:,3].max()==255,path
(root/'QA/TransparencyAudit.json').write_text(json.dumps(rows,indent=2))
print(f'PASS: {len(rows)} RGBA frames; all 192x192, clear canvas borders, fully opaque interiors.')

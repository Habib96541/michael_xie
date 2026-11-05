"""Refuse to copy a scoped Unity scene upgrade if it changed unrelated objects."""
from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Assets/Scenes/LittleDepths.unity'
candidate=ROOT/'Logs/SpriteValidation/Assets/Scenes/LittleDepths.unity'
def parse(path):
    return {i:(k,b) for k,i,b in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',path.read_text(encoding='utf-8-sig'),re.S)}
a,b=parse(source),parse(candidate)
names={i:re.search(r'm_Name: (.*)',v).group(1) for i,(k,v) in a.items() if k=='1'}
owner=lambda v: re.search(r'm_GameObject: \{fileID: (-?\d+)\}',v).group(1) if re.search(r'm_GameObject: \{fileID: (-?\d+)\}',v) else None
transforms={i:v for i,(k,v) in a.items() if k in ('4','224')}
roots=[i for i,v in transforms.items() if names.get(owner(v)) in ('Dive HUD','Crafted cast timing meter')]
assert len(roots)==2,'Expected the existing two action HUD roots'
scope=set(roots)
while True:
    more={i for i,v in transforms.items() if re.search(r'm_Father: \{fileID: (-?\d+)\}',v).group(1) in scope}
    if more<=scope:break
    scope|=more
objects={owner(transforms[i]) for i in scope}
scope|=objects|{i for i,(k,v) in a.items() if owner(v) in objects}
parents={re.search(r'm_Father: \{fileID: (-?\d+)\}',transforms[i]).group(1) for i in roots}
deleted=set(a)-set(b);added=set(b)-set(a)
assert deleted<=scope,'Removed unrelated scene objects: '+str(deleted-scope)
changed={i for i in a.keys()&b.keys() if a[i]!=b[i]}
allowed=[]
for i in changed:
    k,v=a[i];name=names.get(owner(v),'')
    if i in parents:
        strip=lambda t:re.sub(r'  m_Children:\n(?:  - \{fileID: -?\d+\}\n)*','',t)
        assert strip(v)==strip(b[i][1]),'Changed a parent layout: '+name
    elif k=='4' and re.fullmatch(r'(Left|Right) bank [1-6]',name):
        strip=lambda t:re.sub(r'  m_LocalPosition:.*\n','',t)
        assert strip(v)==strip(b[i][1]),'Changed bank properties beyond tile alignment'
    elif k=='114' and ('  timingLane:' in v or '  gameCamera:' in v):
        pass
    else:raise AssertionError('Changed unrelated scene object '+i+' '+name)
    allowed.append(dict(id=i,name=name or ('FishingHUD' if '  timingLane:' in v else 'FishingGame')))
report=dict(passed=True,preservedOriginalObjects=len(a)-len(deleted)-len(changed),removedActionHudObjects=len(deleted),addedActionHudObjects=len(added),targetedChanges=allowed,sourceSceneSha256=__import__('hashlib').sha256(source.read_bytes()).hexdigest())
(ROOT/'Logs/CollectionScenePreservation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))

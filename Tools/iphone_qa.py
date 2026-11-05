from pathlib import Path
source=Path(__file__).with_name('reference_qa.py').read_text()
start=source.index('CASES=[');end=source.index('async def state',start)
source=source[:start]+"CASES=[(375,812,'Portrait',2,True),(393,852,'Portrait',2,True),(414,896,'Portrait',2,True),(430,932,'Portrait',2,True)]\n"+source[end:]
source=source.replace("ROOT/'QA/ReferenceRevision'","ROOT/'QA/iPhoneReview'").replace('case,i==0','case,False')
exec(compile(source,__file__,'exec'))

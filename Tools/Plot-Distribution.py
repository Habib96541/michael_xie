"""Plot actual Unity Physics2D samples; does not generate or steer game outcomes."""
from pathlib import Path
import csv,json,math,struct,re
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'SprintDoc/Polish/Distribution'
plt.rcParams.update({'font.size':9,'axes.spines.top':False,'axes.spines.right':False})
fig,axes=plt.subplots(3,3,figsize=(15,11),layout='constrained')
summary=[]
for rows,ax in zip(range(8,17),axes.flat):
 before=json.loads((DATA/f'before-rows{rows}.json').read_text(encoding='utf-8-sig'))
 after=json.loads((DATA/f'after-rows{rows}.json').read_text(encoding='utf-8-sig'))
 x=np.arange(rows+1)
 for d,offset,color,label in [(before,-.19,'#71859b','Before'),(after,.19,'#159b78','After')]:
  p=np.array(d['Frequencies']);n=d['Completed'];z=1.96
  center=(p+z*z/(2*n))/(1+z*z/n)
  half=z*np.sqrt(p*(1-p)/n+z*z/(4*n*n))/(1+z*z/n)
  ax.bar(x+offset,p*100,width=.36,color=color,label=label)
  ax.errorbar(x+offset,p*100,yerr=np.array([p-(center-half),(center+half)-p])*100,fmt='none',ecolor='#1d2d3d',elinewidth=.65,capsize=1.3)
  d['Wilson95Lower']=(center-half).tolist();d['Wilson95Upper']=(center+half).tolist()
  (DATA/f'{label.lower()}-rows{rows}.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
 ax.set_title(f'{rows} rows | central band {before["CentralShare"]:.1%} -> {after["CentralShare"]:.1%}')
 ax.set_xticks(x);ax.set_xlabel('Physical pocket index');ax.set_ylabel('Completed drops (%)');ax.grid(axis='y',alpha=.15)
 summary.append({'rows':rows,'completed_before':before['Completed'],'completed_after':after['Completed'],'accepted_after':after['Accepted'],'before_central_share':before['CentralShare'],'after_central_share':after['CentralShare'],'before_outer_four_share':before['OuterTwoShare'],'after_outer_four_share':after['OuterTwoShare'],'before_refunds':before['Refunds'],'after_refunds':after['Refunds'],'after_escaped':after['Escaped'],'after_stuck':after['Stuck'],'after_timeout':after['TimedOut'],'mean_seconds_before':before['MeanSeconds'],'mean_seconds_after':after['MeanSeconds'],'p95_seconds_after':after['P95Seconds'],'left_right_z_after':after['LeftRightZ']})
axes[0,0].legend(frameon=False)
fig.suptitle('PegRush / actual Physics2D pocket distribution\n2,000 completed drops per row and configuration; error bars: marginal 95% Wilson intervals',fontsize=16)
fig.savefig(DATA/'BeforeAfter.png',dpi=160);fig.savefig(DATA/'BeforeAfter.svg')
with (DATA/'Summary.csv').open('w',newline='',encoding='utf-8') as f:
 w=csv.DictWriter(f,fieldnames=summary[0]);w.writeheader();w.writerows(summary)
(DATA/'Summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
source=(ROOT/'Assets/PegRush/Generated/GameConfig.asset').read_text(encoding='utf-8-sig')
newtables={}
for row,profile,hexes in re.findall(r'Rows: (\d+)\s+Profile: (\d+)\s+Hundredths: ([0-9a-f]+)',source):
 newtables[(int(row),int(profile))]=list(struct.unpack('<'+'i'*(int(row)+1),bytes.fromhex(hexes)))
import tarfile
with tarfile.open(ROOT/'SprintDoc/Polish/BaselineSource.tar') as a:
 old=a.extractfile('Assets/PegRush/Generated/GameConfig.asset').read().decode('utf-8-sig')
oldtables={(int(r),int(p)):list(struct.unpack('<'+'i'*(int(r)+1),bytes.fromhex(h))) for r,p,h in re.findall(r'Rows: (\d+)\s+Profile: (\d+)\s+Hundredths: ([0-9a-f]+)',old)}
coin=[]
for rows in range(8,17):
 freq=json.loads((DATA/f'after-rows{rows}.json').read_text())['Frequencies']
 for profile in range(3):
  coin.append({'rows':rows,'profile':['Green','Gold','Red'][profile],'old_table_on_new_samples':sum(p*v/100 for p,v in zip(freq,oldtables[(rows,profile)])),'new_table_on_new_samples':sum(p*v/100 for p,v in zip(freq,newtables[(rows,profile)])),'multipliers': [v/100 for v in newtables[(rows,profile)]]})
(DATA/'ArcadeCoinPacing.json').write_text(json.dumps(coin,indent=2),encoding='utf-8')
print('Wrote distribution plots, Wilson intervals, row summaries and observed arcade coin pacing.')


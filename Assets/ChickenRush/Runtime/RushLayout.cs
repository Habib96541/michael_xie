using UnityEngine;

namespace ChickenRush
{
    public enum RushDisplay { Desktop, Tablet, Portrait, Landscape }
    public sealed class RushLayout
    {
        public RushDisplay mode;
        public float width,height,top,bottom;
        public Rect brand,subtitle,wallet,best,difficulty,settings,multiplier,potential,risk,hop,cash,status,viewport,controlPanel,next,roundLabel;
        public bool Compact => mode==RushDisplay.Landscape;
        public static RushLayout Calculate(float width,float height)
        {
            var l=new RushLayout{width=width,height=height};
            if(height<=500 && width>height)l.mode=RushDisplay.Landscape;
            else if(width>=1000)l.mode=RushDisplay.Desktop;
            else if(width>=600)l.mode=RushDisplay.Tablet;
            else l.mode=RushDisplay.Portrait;
            float margin=l.mode==RushDisplay.Desktop?24:16;
            if(l.mode==RushDisplay.Desktop)
            {
                l.top=88;l.bottom=24;
                l.brand=new Rect(margin,16,285,34);l.subtitle=new Rect(margin,54,285,18);
                l.wallet=new Rect(width-602,20,160,48);l.best=new Rect(width-426,20,140,48);
                l.difficulty=new Rect(width-270,20,174,48);l.settings=new Rect(width-76,20,48,48);
                float side=Mathf.Clamp(width*.22f,300,370),x=margin+24,w=side-48,y=l.top;
                l.controlPanel=new Rect(margin,y,side,height-y-24);
                l.roundLabel=new Rect(x,y+24,w,24);l.multiplier=new Rect(x,y+56,w,64);l.potential=new Rect(x,y+126,w,30);
                l.next=new Rect(x,y+180,w,54);l.risk=new Rect(x,y+266,w,66);
                l.hop=new Rect(x,y+370,w,64);l.cash=new Rect(x,y+450,w,64);
                l.status=new Rect(x,y+534,w,54);
                l.viewport=new Rect(side+margin*2,l.top,width-side-margin*3,height-l.top-l.bottom);
            }
            else if(l.mode==RushDisplay.Landscape)
            {
                l.top=58;l.bottom=0;
                l.brand=new Rect(16,11,Mathf.Clamp(width-620,80,190),34);l.subtitle=Rect.zero;
                l.wallet=new Rect(width-528,7,138,44);l.best=new Rect(width-378,7,112,44);
                l.difficulty=new Rect(width-254,7,182,44);l.settings=new Rect(width-60,7,44,44);
                l.controlPanel=new Rect(8,58,210,height-66);l.roundLabel=Rect.zero;
                l.multiplier=new Rect(22,69,182,38);l.potential=new Rect(22,109,182,23);
                l.next=new Rect(22,141,182,34);l.risk=new Rect(22,183,182,50);
                l.hop=new Rect(22,248,182,50);l.cash=new Rect(22,310,182,50);l.status=Rect.zero;
                l.viewport=new Rect(230,58,width-238,height-66);
            }
            else
            {
                l.top=112;l.bottom=228;
                l.brand=new Rect(margin,12,width-96,30);l.subtitle=Rect.zero;
                l.settings=new Rect(width-margin-44,8,44,44);
                float item=(width-2*margin-16)/3;
                l.wallet=new Rect(margin,60,item,44);l.best=new Rect(margin+item+8,60,item,44);
                l.difficulty=new Rect(margin+2*(item+8),60,item,44);
                float y=height-l.bottom;
                l.controlPanel=new Rect(8,y,width-16,l.bottom-8);l.roundLabel=Rect.zero;
                l.multiplier=new Rect(margin,y+13,154,38);
                l.risk=new Rect(width-174-margin,y+17,174,58);
                l.potential=new Rect(margin,y+51,Mathf.Max(100,l.risk.x-margin-12),23);
                float gap=12,available=width-2*margin-gap;
                l.next=new Rect(margin,y+88,width-2*margin,28);
                l.hop=new Rect(margin,y+126,available*.43f,60);
                l.cash=new Rect(l.hop.xMax+gap,y+126,available*.57f,60);
                l.status=new Rect(margin,y+194,width-2*margin,23);
                l.viewport=new Rect(0,l.top,width,Mathf.Max(80,height-l.top-l.bottom));
            }
            return l;
        }
    }
}

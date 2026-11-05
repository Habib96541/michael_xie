using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeviceScreen=UnityEngine.Device.Screen;

namespace BloomDoom
{
    /// <summary>Reflows the client HUD and board to use a landscape viewport.</summary>
    public sealed class GardenResponsiveLayout : MonoBehaviour
    {
        sealed class Original
        {
            public RectTransform Rect;public Vector2 Position,Size;public Vector3 Scale;public bool Active;
            public Original(RectTransform r){Rect=r;Position=r.anchoredPosition;Size=r.sizeDelta;Scale=r.localScale;Active=r.gameObject.activeSelf;}
            public void Restore(){Rect.anchoredPosition=Position;Rect.sizeDelta=Size;Rect.localScale=Scale;Rect.gameObject.SetActive(Active);}
        }
        readonly Dictionary<string,Original> originals=new Dictionary<string,Original>();
        readonly List<Original> allOriginals=new List<Original>();
        readonly Dictionary<string,Original> boardButtons=new Dictionary<string,Original>();
        RectTransform stage,field,actors,particles,modal;Image scenery;bool wasWide;
        Vector2 previousSize;float correction=1;
        public RectTransform Stage=>stage;
        public RectTransform Field=>field;
        public bool Wide {get;private set;}
        public static Rect SafeFrame(Vector2 canvas,Vector2 screen,Rect safe)
        {
            if(screen.x<=0||screen.y<=0||safe.width<=0||safe.height<=0)return new Rect(-canvas*.5f,canvas);
            return new Rect((safe.x/screen.x-.5f)*canvas.x,(safe.y/screen.y-.5f)*canvas.y,canvas.x*safe.width/screen.x,canvas.y*safe.height/screen.y);
        }

        public void Initialize(RectTransform stage,RectTransform field,RectTransform actors,RectTransform particles,RectTransform modal)
        {
            this.stage=stage;this.field=field;this.actors=actors;this.particles=particles;this.modal=modal;
            foreach(RectTransform rect in stage){var original=new Original(rect);originals[rect.name]=original;allOriginals.Add(original);}
            scenery=field.Find("Illustrated garden").GetComponent<Image>();
            foreach(string name in new[]{"1x","1x shadow","II","II shadow"})
                boardButtons.Add(name,new Original((RectTransform)field.Find(name)));
        }
        void Place(string name,float x,float y,float width=-1,float height=-1,float scale=1)
        {
            if(!originals.TryGetValue(name,out var original))return;
            var rect=original.Rect;rect.anchoredPosition=new Vector2(x,-y);rect.localScale=Vector3.one*scale;
            rect.sizeDelta=new Vector2(width<0?original.Size.x:width,height<0?original.Size.y:height);
        }
        void LateUpdate()
        {
            if(stage==null)return;
            var canvas=(RectTransform)stage.parent;
            Rect safe=SafeFrame(canvas.rect.size,new Vector2(DeviceScreen.width,DeviceScreen.height),DeviceScreen.safeArea);
            Vector2 size=safe.size;
            Wide=size.x/Mathf.Max(1,size.y)>=1.7f;
            if(Wide){
                float height=2160*size.y/size.x;
                stage.anchoredPosition=safe.center;
                stage.sizeDelta=new Vector2(2160,height);stage.localScale=Vector3.one*(size.x/2160);
                if(!wasWide||Vector2.Distance(size,previousSize)>.01f)Arrange(height);
                PreserveResidents();
                PreserveParticles();
                PlaceMenu(height);
                PlaceBoardText();
            }else{
                if(wasWide){
                    foreach(var original in allOriginals)original.Restore();
                    foreach(var original in boardButtons.Values)original.Restore();
                    stage.sizeDelta=new Vector2(1480,1040);
                    scenery.preserveAspect=true;
                    modal.SetParent(field,false);
                    modal.SetAsLastSibling();
                }
                foreach(RectTransform actor in actors)actor.localScale=Vector3.one;
                modal.localScale=Vector3.one;modal.anchoredPosition=Vector2.zero;
                stage.anchoredPosition=Vector2.zero;
                float scale=Mathf.Min(canvas.rect.width/1480,canvas.rect.height/1040)*.97f;
                stage.localScale=Vector3.one*scale;
                var veil=ActiveMenuPart("Menu veil");if(veil!=null){veil.anchoredPosition=Vector2.zero;veil.sizeDelta=new Vector2(1440,700);}
                field.Find("Location badge").localScale=Vector3.one;
                field.Find("Announcements").localScale=Vector3.one;
                var notice=field.Find("Announcements") as RectTransform;if(notice!=null)notice.anchoredPosition=new Vector2(120,-235);
            }
            wasWide=Wide;previousSize=size;
        }
        void Arrange(float height)
        {
            foreach(var original in allOriginals)original.Restore();
            Place("Brand flower",12,8,74,78);
            Place("Brand",96,6);
            Place("Brand tagline",100,54);
            Place("Chapter",730,13,600,30,1.3f);
            Place("Garden title",730,55,600,30,1.3f);
            foreach(string name in new[]{"Afternoon","Motto","Chapter strip","Footer"})
                originals[name].Rect.gameObject.SetActive(false);
            Place("SOUND",1565,5,height:62,scale:1.6f);
            Place("SOUND shadow",1565,10,height:62,scale:1.6f);
            Place("?",1750,5,height:62,scale:1.6f);
            Place("? shadow",1750,10,height:62,scale:1.6f);
            Place("SETTINGS",1860,5,width:170,height:62,scale:1.6f);
            Place("SETTINGS shadow",1860,10,width:170,height:62,scale:1.6f);
            Place("Frame shadow",8,113,2144,height-140);
            Place("Frame outline",8,109,2144,height-140);
            Place("Game frame",10,111,2140,height-144);
            foreach(var pair in originals){
                var value=pair.Value;string name=pair.Key;
                if(name.StartsWith("Sun ")||name=="Sunshine"||name=="Sunshine caption")Place(name,value.Position.x,-value.Position.y-24);
            }
            for(int i=0;i<8;i++){
                float x=154+i*160;
                Place("Seed "+GardenData.Seeds[i].Name,x,116,scale:1.4f);
            }
            // Seed shadows share a name, so place them in their hierarchy order.
            int seedShadow=0;
            foreach(RectTransform rect in stage){
                if(rect.name!="Seed shadow")continue;
                rect.anchoredPosition=new Vector2(154+seedShadow++*160,-120);
                rect.localScale=Vector3.one*1.4f;
            }
            Place("",1440,119,scale:1.48f);
            Place(" shadow",1440,124,scale:1.48f);
            Place("Wave heading",1575,135,550,28);
            Place("Wave track",1575,177,550,10);
            Place("Wave progress",1575,177);
            Place("Wave detail",1575,198,550,24);
            field.anchoredPosition=new Vector2(18,-268);
            float boardHeight=Mathf.Max(1,height-305);
            field.localScale=new Vector3(2124f/1440,boardHeight/700,1);
            correction=field.localScale.y/field.localScale.x;
            scenery.preserveAspect=false;
            Place("Garden tip",26,height-34,2100,27);
            PlaceBoardButton("1x",1832,boardHeight);
            PlaceBoardButton("II",1972,boardHeight);
        }
        void PlaceBoardButton(string name,float x,float boardHeight)
        {
            const float scale=2.25f;
            const float buttonHeight=42*scale;
            Vector3 fieldScale=field.localScale;
            var button=boardButtons[name].Rect;
            var shadow=boardButtons[name+" shadow"].Rect;
            button.localScale=shadow.localScale=new Vector3(scale/fieldScale.x,scale/fieldScale.y,1);
            button.anchoredPosition=new Vector2(x/fieldScale.x,-(boardHeight-buttonHeight-13)/fieldScale.y);
            shadow.anchoredPosition=button.anchoredPosition-new Vector2(0,5/fieldScale.y);
        }
        void PreserveResidents()
        {
            // Simulation coordinates remain on the original board. Only their
            // horizontal spacing expands; each illustration stays proportional.
            foreach(RectTransform actor in actors){
                actor.localScale=new Vector3(correction,1,1);
                actor.anchoredPosition+=new Vector2(actor.rect.width*(1-correction)*.5f,0);
            }
        }
        void PreserveParticles()
        {
            foreach(RectTransform particle in particles){
                var scale=particle.localScale;
                particle.anchoredPosition+=new Vector2(particle.rect.width*scale.x*(1-correction)*.5f,0);
                particle.localScale=new Vector3(scale.x*correction,scale.y,1);
            }
        }
        RectTransform ActiveMenuPart(string name)
        {
            foreach(RectTransform child in modal)
                if(child.gameObject.activeSelf && child.name==name)return child;
            return null;
        }
        void PlaceMenu(float height)
        {
            if(modal.parent!=stage){
                modal.SetParent(stage,false);
                modal.SetAsLastSibling();
            }
            var card=ActiveMenuPart("Menu card outline");
            var veil=ActiveMenuPart("Menu veil");
            if(card==null||veil==null)return;
            // The menu uses the full stage height even when a short browser
            // viewport compresses the lawn below the larger touch controls.
            float scale=Mathf.Min(1.7f,2112/card.rect.width,(height-38)/(card.rect.height+7));
            Vector2 origin=new Vector2((2160-1440*scale)*.5f,-(height-700*scale)*.5f);
            modal.localScale=Vector3.one*scale;
            modal.anchoredPosition=origin;
            veil.anchoredPosition=-origin/scale;
            veil.sizeDelta=new Vector2(2160,height)/scale;
        }
        void PlaceBoardText()
        {
            var textScale=new Vector3(correction,1,1);
            field.Find("Location badge").localScale=textScale;
            var notice=(RectTransform)field.Find("Announcements");
            notice.localScale=textScale;
            notice.anchoredPosition=new Vector2(720-600*correction,-235);
        }
    }
}

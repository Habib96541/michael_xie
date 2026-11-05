using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PocketBorough
{
    // Original, vertex-coloured models. Every recipe is baked into a single reusable mesh.
    public sealed class MeshWorkshop
    {
        readonly List<Vector3> vertices = new();
        readonly List<int> triangles = new();
        readonly List<Color> colors = new();
        public static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static readonly Color Cream=C("F6E9CD"), Ink=C("263F53"), Glass=C("80C6CD"), White=C("FFF9E9"), Rubber=C("26343F"), Gold=C("F9BD53"), Green=C("62AD83"), Coral=C("ED886F");
        void Face(Vector3 a, Vector3 b, Vector3 c, Color color)
        { if(Vector3.Cross(b-a,c-a).sqrMagnitude<.0000000001f)return;int n=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); colors.Add(color); colors.Add(color); colors.Add(color); triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2); }
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col) { Face(a,b,c,col); Face(a,c,d,col); }
        public void Box(Vector3 p, Vector3 s, Color c, float yaw=0)
        {
            var q=Quaternion.Euler(0,yaw,0); var h=s*.5f;
            Vector3 V(float x,float y,float z)=>p+q*new Vector3(x*h.x,y*h.y,z*h.z);
            Quad(V(-1,-1,-1),V(-1,1,-1),V(1,1,-1),V(1,-1,-1),c);
            Quad(V(1,-1,1),V(1,1,1),V(-1,1,1),V(-1,-1,1),c);
            Quad(V(-1,-1,1),V(-1,1,1),V(-1,1,-1),V(-1,-1,-1),c);
            Quad(V(1,-1,-1),V(1,1,-1),V(1,1,1),V(1,-1,1),c);
            Quad(V(-1,1,-1),V(-1,1,1),V(1,1,1),V(1,1,-1),c);
            Quad(V(-1,-1,1),V(-1,-1,-1),V(1,-1,-1),V(1,-1,1),c);
        }
        public void Cylinder(Vector3 p,float r,float h,Color c,int sides=10,float top=-1,Quaternion? rotation=null)
        {
            float rt=top<0?r:top; var q=rotation??Quaternion.identity;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 B(float ang,float rad,float y)=>p+q*new Vector3(Mathf.Cos(ang)*rad,y,Mathf.Sin(ang)*rad);
                var a0=B(a,r,0);var b0=B(b,r,0);var a1=B(a,rt,h);var b1=B(b,rt,h);
                Quad(a0,a1,b1,b0,c); Face(p+q*Vector3.up*h,b1,a1,c); Face(p,a0,b0,c);
            }
        }
        public void Roof(Vector3 p,float w,float d,float h,Color c)
        {
            var a=p+new Vector3(-w/2,0,-d/2);var b=p+new Vector3(w/2,0,-d/2);var c0=p+new Vector3(-w/2,0,d/2);var e=p+new Vector3(w/2,0,d/2);var f=p+new Vector3(0,h,-d/2);var g=p+new Vector3(0,h,d/2);
            Face(a,f,b,c);Face(c0,e,g,c);Quad(a,c0,g,f,c);Quad(b,f,g,e,c);
        }
        // Extruded car silhouette with bevelled bumpers and a sloping windscreen.
        public void Loft(float[] zs,float[] widths,float[] heights,float bottom,Color color)
        {
            for(int i=0;i<zs.Length-1;i++)
            {
                var a=new Vector3(-widths[i],bottom,zs[i]);var b=new Vector3(widths[i],bottom,zs[i]);var c=new Vector3(-widths[i],heights[i],zs[i]);var d=new Vector3(widths[i],heights[i],zs[i]);
                var e=new Vector3(-widths[i+1],bottom,zs[i+1]);var f=new Vector3(widths[i+1],bottom,zs[i+1]);var g=new Vector3(-widths[i+1],heights[i+1],zs[i+1]);var h=new Vector3(widths[i+1],heights[i+1],zs[i+1]);
                Quad(a,e,g,c,color);Quad(b,d,h,f,color);Quad(c,g,h,d,color);Quad(a,b,f,e,color);
                if(i==0)Quad(a,c,d,b,color); if(i==zs.Length-2)Quad(e,f,h,g,color);
            }
        }
        public Mesh Bake(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static readonly string[] BuildingNames={"Gable cottage","Garden duplex","Balcony apartments","Corner market","Noodle house","Terraced offices","Sawtooth warehouse","Foundry works","Borough arcade","Sunset hotel","Stepped studios","Skygarden residences","Crown skyscraper","Sail glass tower","Borough clock hall"};
        public static Mesh Building(int id)
        {
            var m=new MeshWorkshop(); Color[] palette={C("EAA081"),C("93B8B0"),C("E5B768"),C("79A4C0"),C("CF8D8B")}; Color wall=palette[id%5];
            float w=3.6f+(id%3)*.55f,d=3.5f+(id%2)*.65f,h=id<2?2.4f+id*1.6f:id<5?3.4f:id<10?5.4f:id<12?8:12;
            if(id==6||id==7){w=6;d=4.4f;h=3.4f;} if(id==8){w=6.5f;h=3;}
            if(id>=10){w=5.4f+(id-10)*.5f;d=5+(id-10)*.45f;}
            m.Box(new(0,.13f,0),new(w+.28f,.26f,d+.28f),Cream);
            m.Box(new(0,h/2+.2f,0),new(w,h,d),wall);
            m.Box(new(0,.46f,0),new(w+.08f,.24f,d+.08f),Cream);
            m.Box(new(0,h+.26f,0),new(w+.3f,.23f,d+.3f),Cream);
            if(id<2){m.Roof(new(0,h+.36f,0),w+.55f,d+.55f,1.3f,C("B55D59"));m.Box(new(w*.26f,h+1,0),new(.45f,1.4f,.6f),Cream);}
            else if(id==6){for(int i=0;i<3;i++)m.Roof(new(-2+i*2,h+.36f,0),2,4.6f,.8f,Ink);}
            else if(id==7){m.Cylinder(new(2,h,1),.4f,3.8f,Ink);m.Cylinder(new(-2,h,1),.5f,2,Coral);}
            else if(id==10||id==12){m.Box(new(.25f,h+.9f,0),new(w*.75f,1.5f,d*.75f),wall);m.Box(new(.25f,h+1.7f,0),new(w*.8f,.2f,d*.8f),Cream);if(id==12)m.Cylinder(new(.25f,h+1.8f,0),.15f,2.6f,Gold,8,0);}
            else if(id==13){m.Roof(new(0,h+.36f,0),w+.1f,d,2.8f,Glass);}
            else if(id==14){m.Box(new(0,h+1.5f,0),new(2.3f,3,2.3f),Coral);m.Roof(new(0,h+3,0),2.6f,2.6f,1.7f,Ink);m.Cylinder(new(0,h+1.5f,-1.17f),.72f,.06f,White,24,rotation:Quaternion.Euler(90,0,0));m.Box(new(0,h+1.65f,-1.23f),new(.08f,.5f,.04f),Ink);m.Box(new(.17f,h+1.5f,-1.23f),new(.4f,.07f,.04f),Ink);}
            else {m.Box(new(-w*.25f,h+.52f,0),new(1.2f,.4f,1.1f),Ink);m.Cylinder(new(w*.25f,h+.38f,d*.2f),.42f,.65f,Glass);}
            // Window reveals, sills, store glazing and architectural floor bands.
            int floors=Mathf.Max(1,Mathf.FloorToInt(h/1.45f));int cols=id==8?5:3;
            for(int floor=0;floor<floors;floor++)for(int col=0;col<cols;col++)
            {
                float x=-w*.5f+(col+.5f)*w/cols,y=1.1f+floor*1.45f;
                if(y>h-.2f)continue;
                Color glass=(col+floor)%4==0?Gold:Glass;float ww=w/cols*.61f;
                m.Box(new(x,y,-d/2-.035f),new(ww,.77f,.08f),Ink);m.Box(new(x,y+.03f,-d/2-.085f),new(ww-.14f,.61f,.025f),glass);
                m.Box(new(x,y-.43f,-d/2-.09f),new(ww+.12f,.1f,.2f),Cream);
                m.Box(new(x,y,d/2+.03f),new(ww,.77f,.08f),glass);
                for(int side=-1;side<=1;side+=2)m.Box(new(side*(w/2+.03f),y,x*.7f),new(.07f,.7f,.6f),glass);
                if((id==2||id==11)&&floor>0){m.Box(new(x,y-.55f,-d/2-.35f),new(ww+.3f,.14f,.8f),Cream);m.Box(new(x,y-.19f,-d/2-.7f),new(ww+.3f,.08f,.06f),Ink);for(int rail=0;rail<4;rail++)m.Box(new(x-ww/2+rail*ww/3,y-.37f,-d/2-.7f),new(.04f,.4f,.04f),Ink);}
            }
            m.Box(new(0,.83f,-d/2-.08f),new(.75f,1.4f,.14f),Ink);m.Box(new(0,.95f,-d/2-.16f),new(.54f,.93f,.03f),Glass);m.Box(new(.2f,.72f,-d/2-.2f),new(.06f,.06f,.04f),Gold);
            if(id==3||id==4||id==8||id==9)
            {
                m.Box(new(0,2.25f,-d/2-.18f),new(w-.4f,.6f,.2f),id==4?Coral:Ink);
                // Marquee stripes and hand-built pictographic signage.
                for(int i=0;i<9;i++)m.Box(new(-w/2+.25f+i*(w-.5f)/9,1.86f,-d/2-.46f),new((w-.5f)/9,.14f,.85f),i%2==0?White:Coral);
                for(int i=0;i<5;i++)m.Box(new(-.8f+i*.38f,2.25f,-d/2-.3f),new(.22f,.2f,.025f),Gold);
            }
            if(id==5||id==13)for(int i=0;i<5;i++)m.Box(new(-w/2+i*w/4,h/2, -d/2-.1f),new(.12f,h,.12f),Cream);
            if(id==8){for(int i=-1;i<=1;i+=2)m.Box(new(i*(w/2-.3f),1.4f,-d/2-.4f),new(.3f,2.6f,.3f),Cream);}
            return m.Bake(BuildingNames[id]);
        }
        public static readonly string[] VehicleNames={"Sedan","Hatchback","SUV","Pickup","Taxi","Police cruiser","Ambulance","Fire engine","City bus","Parcel van"};
        public static Mesh Vehicle(int id)
        {
            var m=new MeshWorkshop();Color[] body={C("E98270"),C("9CBB88"),C("77A6C4"),C("D59B72"),Gold,White,White,C("DA665E"),C("6CB9AF"),C("D6ADDC")};
            float len=id==8?4.9f:id>=6?3.9f:id==1?2.4f:id==2?3.2f:id==3?3.3f:2.8f,w=id>=6?1.45f:id==2?1.4f:1.25f;float half=len/2;
            m.Loft(new[]{-half,-half+.23f,half-.23f,half},new[]{w*.42f,w*.5f,w*.5f,w*.42f},new[]{.55f,.8f,.8f,.57f},.3f,body[id]);
            if(id<6)
            {
                float rear=id==1?-.98f:id==2?-.96f:id==3?-.1f:-.7f,front=id==3?1.2f:.87f,roof=id==2?1.6f:1.34f;
                m.Loft(new[]{rear,rear+.22f,front-.39f,front},new[]{w*.43f,w*.38f,w*.38f,w*.43f},new[]{.83f,roof,roof,.83f},.78f,Glass);
                m.Box(new(0,roof+.01f,(rear+.22f+front-.39f)/2),new(w*.78f,.12f,front-.39f-rear-.22f),id==5?Ink:body[id]);
                m.Box(new(0,1.08f,.12f),new(w*.89f,.05f,.06f),Ink);
                for(int side=-1;side<=1;side+=2)m.Box(new(side*w*.44f,1.04f,.04f),new(.055f,.55f,.08f),body[id]);
                if(id==3){m.Box(new(0,.91f,-.95f),new(w*.86f,.16f,.66f),Ink);}
            }
            else
            {
                m.Box(new(0,1.12f,-.22f),new(w*.91f,1.3f,len*.72f),body[id]);
                m.Box(new(0,1.22f,half-.4f),new(w*.83f,.66f,.08f),Glass);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<(id==8?5:2);i++)m.Box(new(side*w*.465f,1.35f,-half+.55f+i*.7f),new(.05f,.53f,.51f),Glass);
                if(id==6){for(int side=-1;side<=1;side+=2){m.Box(new(side*(w*.47f),1.2f,-.5f),new(.035f,.15f,.7f),Coral);m.Box(new(side*(w*.47f),1.2f,-.5f),new(.035f,.7f,.15f),Coral);}}
                if(id==7){m.Box(new(0,1.86f,-.25f),new(.8f,.12f,2.5f),Ink);for(int i=0;i<8;i++)m.Box(new(0,1.94f,-1.3f+i*.3f),new(1,.06f,.06f),Cream);}
            }
            for(int side=-1;side<=1;side+=2)for(int axle=-1;axle<=1;axle+=2)
            {var p=new Vector3(side*w*.5f,.35f,axle*len*.31f);m.Cylinder(p,.34f,.17f,Rubber,12,rotation:Quaternion.Euler(0,0,side*-90));m.Cylinder(p+Vector3.right*side*.18f,.17f,.025f,Cream,10,rotation:Quaternion.Euler(0,0,side*-90));}
            for(int side=-1;side<=1;side+=2){m.Box(new(side*w*.3f,.61f,half+.02f),new(.25f,.18f,.06f),White);m.Box(new(side*w*.3f,.6f,-half-.02f),new(.25f,.15f,.06f),Coral);}
            m.Box(new(0,.43f,half+.03f),new(w*.85f,.12f,.1f),Cream);m.Box(new(0,.47f,-half-.04f),new(.36f,.13f,.04f),White);
            if(id==4)m.Box(new(0,1.53f,0),new(.5f,.22f,.28f),Gold);
            if(id==5||id==6||id==7){float y=id<6?1.5f:1.86f;m.Box(new(-.24f,y,.45f),new(.35f,.16f,.25f),Coral);m.Box(new(.24f,y,.45f),new(.35f,.16f,.25f),C("759DEA"));}
            return m.Bake(VehicleNames[id]);
        }
        public static readonly string[] PropNames={"Street lantern","Traffic signal","Traffic cone","Litter bin","Fire hydrant","Park bench","Canopy tree","Round shrub","Flower planter","Picket fence","Postbox","Wayfinder","Bus shelter","Noodle cart","Picnic table","Borough sculpture","Billboard","Road barrier","Utility pole","Tiered fountain"};
        public static Mesh Prop(int id)
        {
            var m=new MeshWorkshop();void B(float x,float y,float z,float a,float b,float c,Color col)=>m.Box(new(x,y,z),new(a,b,c),col);
            void Pole(float x,float z,float h,float r=.07f)=>m.Cylinder(new(x,0,z),r,h,Ink);
            switch(id)
            {
                case 0: Pole(0,0,2.9f);B(.3f,2.9f,0,.7f,.1f,.1f,Ink);B(.55f,2.8f,0,.43f,.16f,.28f,Gold);m.Cylinder(Vector3.zero,.18f,.16f,Cream);break;
                case 1: Pole(0,0,2.1f);B(0,2.1f,0,.36f,.9f,.3f,Ink);for(int i=0;i<3;i++)m.Cylinder(new(0,2.37f-i*.26f,-.17f),.095f,.03f,i==0?Coral:i==1?Gold:Green,10,rotation:Quaternion.Euler(90,0,0));break;
                case 2: B(0,.05f,0,.54f,.1f,.54f,Rubber);m.Cylinder(new(0,.1f,0),.22f,.62f,Coral,12,0);m.Cylinder(new(0,.31f,0),.152f,.11f,White,12,.112f);break;
                case 3: m.Cylinder(Vector3.zero,.28f,.8f,Green,10);m.Cylinder(new(0,.81f,0),.31f,.09f,Ink,10);B(0,.67f,-.28f,.27f,.13f,.04f,Ink);break;
                case 4: m.Cylinder(Vector3.zero,.17f,.65f,Coral);m.Cylinder(new(0,.64f,0),.21f,.16f,Gold,10,.07f);B(0,.42f,0,.62f,.17f,.17f,Coral);break;
                case 5: for(int i=0;i<4;i++)B(0,.48f,-.27f+i*.18f,1.65f,.08f,.13f,C("CA9867"));for(int i=0;i<3;i++)B(0,.68f+i*.16f,.37f,1.65f,.11f,.08f,C("CA9867"));for(int s=-1;s<=1;s+=2){B(s*.6f,.25f,0,.09f,.5f,.65f,Ink);B(s*.6f,.7f,.4f,.09f,.8f,.09f,Ink);}break;
                case 6: m.Cylinder(Vector3.zero,.16f,1.65f,C("916B55"),7,.1f);m.Cylinder(new(0,1.05f,0),1,1.6f,Green,8,.55f);m.Cylinder(new(.12f,2.15f,0),.76f,.8f,C("85BE89"),7,0);break;
                case 7: m.Cylinder(Vector3.zero,.57f,.73f,Green,9,.32f);m.Cylinder(new(.28f,.1f,.13f),.34f,.65f,C("84BC87"),8,.1f);break;
                case 8: m.Cylinder(Vector3.zero,.45f,.48f,Coral,8,.55f);m.Cylinder(new(0,.48f,0),.48f,.15f,Green);for(int i=0;i<6;i++){float a=i*1.05f;m.Cylinder(new(Mathf.Cos(a)*.3f,.65f,Mathf.Sin(a)*.3f),.13f,.12f,i%2==0?Gold:White,6,0);}break;
                case 9: for(int i=0;i<6;i++){B(-.85f+i*.34f,.6f,0,.14f,1.1f,.12f,Cream);m.Roof(new(-.85f+i*.34f,1.15f,0),.14f,.12f,.16f,Cream);}B(0,.45f,0,2.1f,.1f,.1f,White);B(0,.87f,0,2.1f,.1f,.1f,White);break;
                case 10: Pole(0,0,.55f,.1f);B(0,.85f,0,.65f,.65f,.48f,C("6A9EB9"));B(0,1,-.25f,.4f,.08f,.03f,Ink);m.Roof(new(0,1.18f,0),.7f,.54f,.15f,Ink);break;
                case 11: Pole(0,0,1.8f);B(.1f,1.65f,0,1.1f,.3f,.1f,Green);B(-.1f,1.23f,0,1.1f,.27f,.1f,Coral);break;
                case 12: for(int i=-1;i<=1;i+=2)Pole(i*1.1f,0,2.2f);B(0,2.25f,0,2.6f,.18f,1.3f,Coral);B(0,1,.4f,2.3f,1.7f,.06f,Glass);B(0,.5f,.15f,1.9f,.12f,.5f,Cream);break;
                case 13: B(0,.65f,0,1.3f,.85f,.85f,Coral);B(0,1.1f,0,1.5f,.12f,1.05f,Cream);for(int s=-1;s<=1;s+=2){Pole(s*.55f,0,2);m.Cylinder(new(s*.55f,.25f,-.46f),.25f,.1f,Rubber,10,rotation:Quaternion.Euler(90,0,0));}m.Roof(new(0,2,0),1.9f,1.3f,.35f,Gold);break;
                case 14: B(0,.8f,0,1.8f,.13f,1.1f,Cream);B(0,.45f,0,.13f,.8f,1,Ink);B(0,.42f,-.8f,1.8f,.1f,.32f,Coral);B(0,.42f,.8f,1.8f,.1f,.32f,Coral);break;
                case 15: B(0,.18f,0,1.4f,.36f,1.4f,Cream);m.Cylinder(new(0,.36f,0),.5f,1.2f,Coral,5,.23f);m.Cylinder(new(0,1.56f,0),.52f,.6f,Gold,6,0);break;
                case 16: Pole(-1,0,2.4f);Pole(1,0,2.4f);B(0,2.3f,0,3,1.5f,.2f,Ink);B(0,2.3f,-.12f,2.7f,1.2f,.05f,Coral);for(int i=0;i<5;i++)B(-1+i*.5f,2.3f,-.16f,.3f,.5f,.03f,Gold);break;
                case 17: B(0,.65f,0,1.8f,.65f,.18f,Coral);for(int i=0;i<5;i++)m.Box(new(-.7f+i*.35f,.65f,-.1f),new(.15f,.58f,.03f),White,20);for(int i=-1;i<=1;i+=2)B(i*.65f,.18f,0,.13f,.35f,.7f,Ink);break;
                case 18: Pole(0,0,3.8f,.1f);B(0,3.2f,0,1.4f,.1f,.1f,C("A57F64"));for(int i=-1;i<=1;i++)m.Cylinder(new(i*.5f,3.25f,0),.12f,.24f,Cream,8);break;
                case 19: m.Cylinder(Vector3.zero,1.4f,.32f,Cream,20);m.Cylinder(new(0,.33f,0),1.2f,.08f,Glass,20);m.Cylinder(new(0,.4f,0),.2f,1.2f,Cream);m.Cylinder(new(0,1.1f,0),.75f,.15f,Cream,16);m.Cylinder(new(0,1.25f,0),.62f,.05f,Glass,16);m.Cylinder(new(0,1.3f,0),.1f,.65f,Glass,10,0);break;
            }
            return m.Bake(PropNames[id]);
        }
        public static Mesh HoleBowl()
        {
            var m=new MeshWorkshop();int n=80;
            for(int i=0;i<n;i++)
            {float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;Vector3 P(float t,float r,float y)=>new(Mathf.Cos(t)*r,y,Mathf.Sin(t)*r);
                m.Quad(P(a,1,.012f),P(a,.86f,-.24f),P(b,.86f,-.24f),P(b,1,.012f),C("263241"));
                m.Quad(P(a,.86f,-.24f),P(a,.3f,-1.8f),P(b,.3f,-1.8f),P(b,.86f,-.24f),C("111522"));
                m.Face(P(a,.3f,-1.8f),new(0,-1.85f,0),P(b,.3f,-1.8f),C("080D15"));}
            return m.Bake("Recessed eighty segment cavity");
        }
        public static Mesh Ring()
        {
            var m=new MeshWorkshop();int n=80;for(int i=0;i<n;i++){float a=i*2*Mathf.PI/n,b=(i+1)*2*Mathf.PI/n;Vector3 P(float t,float r,float y)=>new(Mathf.Cos(t)*r,y,Mathf.Sin(t)*r);m.Quad(P(a,1,.035f),P(b,1,.035f),P(b,1.075f,.065f),P(a,1.075f,.065f),White);m.Quad(P(a,1.075f,.065f),P(b,1.075f,.065f),P(b,1.1f,.01f),P(a,1.1f,.01f),White);}return m.Bake("Sculpted hole lip");
        }
    }
}

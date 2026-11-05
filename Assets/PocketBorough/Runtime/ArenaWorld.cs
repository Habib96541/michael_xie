using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PocketBorough
{
    public sealed class CityObject
    {
        public Transform transform;
        public ModelDefinition model;
        public Vector3 home,scale;
        public Quaternion rotation;
        public bool available=true, falling;
        public float fallTime;
        public Vector3 start;
        public HoleAgent eater;
    }
    public sealed class ArenaWorld
    {
        public readonly List<CityObject> objects=new(1300);
        public readonly List<CityObject> falling=new(160);
        const int GridCount=16;
        const float Cell=7;
        readonly List<CityObject>[] buckets=new List<CityObject>[GridCount*GridCount];
        readonly Transform root;
        readonly BoroughCatalog catalog;
        readonly System.Random random=new(1825);
        public int Remaining {get;private set;}
        public ArenaWorld(Transform parent,BoroughCatalog data)
        {
            catalog=data;root=new GameObject("City • pooled original models").transform;root.SetParent(parent);
            for(int i=0;i<buckets.Length;i++)buckets[i]=new List<CityObject>(16);
            Generate(); Remaining=objects.Count;
        }
        int Index(float x)=>Mathf.Clamp(Mathf.FloorToInt((x+56)/Cell),0,GridCount-1);
        float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
        void Place(int model,float x,float z,float yaw=0,float size=1)
        {
            var def=catalog.models[model];var go=Object.Instantiate(def.prefab,root);go.name=def.title;
            go.transform.SetPositionAndRotation(new(x,.025f,z),Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*size;
            var obj=new CityObject{transform=go.transform,model=def,home=go.transform.position,scale=go.transform.localScale,rotation=go.transform.rotation};objects.Add(obj);buckets[Index(x)+Index(z)*GridCount].Add(obj);
        }
        void Generate()
        {
            var ground=new MeshWorkshop();
            ground.Box(new(0,-.16f,0),new(112,.25f,112),MeshWorkshop.C("A9C5A2"));
            float[] roads={-36,-12,12,36};
            foreach(float r in roads)
            {
                ground.Box(new(r,-.013f,0),new(5.8f,.025f,110),MeshWorkshop.C("71858B"));
                ground.Box(new(0,-.011f,r),new(110,.025f,5.8f),MeshWorkshop.C("71858B"));
                for(int i=-51;i<53;i+=4)
                {if(!NearRoad(i,4)){ground.Box(new(r,.007f,i),new(.12f,.015f,1.6f),MeshWorkshop.Cream);ground.Box(new(i,.009f,r),new(1.6f,.015f,.12f),MeshWorkshop.Cream);}}
                foreach(float cross in roads)
                {for(int s=-1;s<=1;s+=2)for(int k=-2;k<=2;k++){ground.Box(new(r+k*.8f,.012f,cross+s*4),new(.48f,.02f,1.5f),MeshWorkshop.White);ground.Box(new(r+s*4,.014f,cross+k*.8f),new(1.5f,.02f,.48f),MeshWorkshop.White);}}
            }
            // Twenty-five authored blocks: downtown, residential, waterfront park and industrial quarter.
            float[] blocks={-48,-24,0,24,48};int buildingIndex=0;
            foreach(float x in blocks)foreach(float z in blocks)
            {
                float bw=Mathf.Abs(x)==48?14:17.5f,bd=Mathf.Abs(z)==48?14:17.5f;
                ground.Box(new(x,-.025f,z),new(bw,.035f,bd),MeshWorkshop.C("E8DCC5"));
                bool park=(x==0&&z==0)||(x==-24&&z==24);
                if(park)
                {
                    ground.Box(new(x,-.002f,z),new(14,.02f,14),MeshWorkshop.C("90B887"));
                    ground.Box(new(x,.013f,z),new(2,.02f,14),MeshWorkshop.C("ECD8AC"));ground.Box(new(x,.014f,z),new(14,.02f,2),MeshWorkshop.C("ECD8AC"));
                    Place(44,x,z);for(int a=0;a<8;a++){float t=a*Mathf.PI/4;Place(31,x+Mathf.Cos(t)*5.7f,z+Mathf.Sin(t)*5.7f, a*45);}
                    for(int s=-1;s<=1;s+=2){Place(30,x+s*3,z+3,90);Place(39,x+s*4,z-3);}
                }
                else
                {
                    for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)
                    {
                        int id;
                        if(x>=24&&z>=0)id=10+(buildingIndex%5);
                        else if(z<=-24&&x>0)id=6+(buildingIndex%2);
                        else if(x<=-24&&z<24)id=buildingIndex%3;
                        else id=3+buildingIndex%8;
                        Place(id,x+a*(bw*.24f),z+b*(bd*.24f),b<0?0:180);buildingIndex++;
                    }
                }
                // Hand-spaced pavement furniture and clusters of early-game targets.
                for(int k=0;k<8;k++)
                {float px=x-bw*.42f+k*bw*.12f;Place(27,px,z-bd*.43f);if(k%2==0)Place(28,px,z+bd*.43f);}
                for(int side=-1;side<=1;side+=2)
                {
                    Place(25,x+side*bw*.45f,z+bd*.36f);Place(29,x+side*bw*.45f,z-bd*.36f);
                    Place(35,x+side*bw*.45f,z);Place(32,x+side*bw*.44f,z+2.5f);
                    if(!park)Place(31,x+side*bw*.43f,z-bd*.2f);
                }
                Place(30,x-bw*.25f,z-bd*.43f);Place(33,x+bw*.24f,z-bd*.43f);
                Place(34,x+bw*.46f,z-3);Place(36,x-bw*.45f,z-2);
            }
            Place(40,-24,29);
            int vehicle=0;
            foreach(float road in roads)for(int k=-49;k<=50;k+=7)
            {
                if(NearRoad(k,4))continue;
                Place(15+vehicle++%10,road-1.55f,k,0);Place(15+vehicle++%10,k,road+1.55f,90);
                Place(26,road+3.5f,k+1);Place(27,road+3.4f,k-1);Place(28,k-1,road-3.5f);
            }
            // Distinct civic props and construction detail on the outskirts.
            for(int i=0;i<8;i++){Place(37,-43+i*5,-32,180);Place(38,-43+i*5,32);Place(41,-49+i*14,53,180);Place(42,43,-45+i*2,90);Place(43,50,-40+i*12);}
            var g=new GameObject("Combined roads • sidewalks • crosswalks • parks",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root);g.GetComponent<MeshFilter>().sharedMesh=ground.Bake("Borough ground");g.GetComponent<MeshRenderer>().sharedMaterial=catalog.groundMaterial;g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            // Bevelled board edge stays outside the play area; a calm blue surround frames the city.
            var edge=new MeshWorkshop();for(int s=-1;s<=1;s+=2){edge.Box(new(s*56.2f,-1.15f,0),new(.4f,2.3f,112.8f),MeshWorkshop.C("668B83"));edge.Box(new(0,-1.15f,s*56.2f),new(112.8f,2.3f,.4f),MeshWorkshop.C("668B83"));}
            var board=new GameObject("City island plinth",typeof(MeshFilter),typeof(MeshRenderer));board.transform.SetParent(root);board.GetComponent<MeshFilter>().sharedMesh=edge.Bake("Island edge");board.GetComponent<MeshRenderer>().sharedMaterial=catalog.bowlMaterial;
        }
        static bool NearRoad(float p,float range)=>Mathf.Abs(p+36)<range||Mathf.Abs(p+12)<range||Mathf.Abs(p-12)<range||Mathf.Abs(p-36)<range;
        public CityObject Target(Vector3 position,float radius)
        {
            CityObject best=null;float utility=float.MinValue;int cx=Index(position.x),cz=Index(position.z);
            for(int z=Mathf.Max(0,cz-4);z<=Mathf.Min(GridCount-1,cz+4);z++)for(int x=Mathf.Max(0,cx-4);x<=Mathf.Min(GridCount-1,cx+4);x++)
            foreach(var obj in buckets[x+z*GridCount])
            {
                if(!obj.available||!BoroughRules.Fits(radius,obj.model.footprint))continue;
                float distance=(obj.home-position).sqrMagnitude;
                float value=(obj.model.mass+3)/(.8f+Mathf.Sqrt(distance));
                if(value>utility){utility=value;best=obj;}
            }
            if(best==null)foreach(var obj in objects)
            {
                if(!obj.available||!BoroughRules.Fits(radius,obj.model.footprint))continue;
                float value=(obj.model.mass+3)/(.8f+Vector3.Distance(obj.home,position));if(value>utility){utility=value;best=obj;}
            }
            return best;
        }
        public void TryConsume(HoleAgent hole)
        {
            int minX=Index(hole.Position.x-hole.Radius),maxX=Index(hole.Position.x+hole.Radius),minZ=Index(hole.Position.z-hole.Radius),maxZ=Index(hole.Position.z+hole.Radius);
            for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)foreach(var obj in buckets[x+z*GridCount])
            {
                if(!obj.available||!BoroughRules.Fits(hole.Radius,obj.model.footprint))continue;
                float capture=Mathf.Max(.25f,hole.Radius-obj.model.footprint*.35f);
                if((obj.home-hole.Position).sqrMagnitude>capture*capture)continue;
                obj.available=false;obj.falling=true;obj.fallTime=0;obj.start=obj.transform.position;obj.eater=hole;falling.Add(obj);Remaining--;
            }
        }
        public void Animate(float dt,System.Action<HoleAgent,CityObject> reward)
        {
            for(int i=falling.Count-1;i>=0;i--)
            {
                var obj=falling[i];obj.fallTime+=dt;float t=Mathf.Clamp01(obj.fallTime/.78f);float ease=t*t*(3-2*t);
                var destination=obj.eater.Position;destination.y=-Mathf.Max(2,obj.model.footprint*2.2f);
                obj.transform.position=Vector3.Lerp(obj.start,destination,ease);
                obj.transform.rotation=obj.rotation*Quaternion.Euler(t*t*55,Mathf.Sin(t*4)*24,t*t*27);
                obj.transform.localScale=obj.scale*Mathf.Lerp(1,.13f,t*t);
                if(t>=1){obj.falling=false;obj.transform.gameObject.SetActive(false);falling.RemoveAt(i);reward(obj.eater,obj);}
            }
        }
        public void Reset()
        {
            falling.Clear();Remaining=objects.Count;
            foreach(var obj in objects){obj.available=true;obj.falling=false;obj.fallTime=0;obj.eater=null;obj.transform.SetPositionAndRotation(obj.home,obj.rotation);obj.transform.localScale=obj.scale;obj.transform.gameObject.SetActive(true);}
        }
        public int Restock(List<HoleAgent> holes)
        {
            int count=0;
            foreach(var obj in objects)
            {
                if(obj.model.category==0||obj.available||obj.falling)continue;
                bool safe=true;foreach(var hole in holes)if(hole.Alive&&(hole.Position-obj.home).sqrMagnitude<Mathf.Pow(hole.Radius+obj.model.footprint+2,2)){safe=false;break;}
                if(!safe)continue;
                obj.available=true;obj.eater=null;obj.fallTime=0;obj.transform.SetPositionAndRotation(obj.home,obj.rotation);obj.transform.localScale=obj.scale;obj.transform.gameObject.SetActive(true);count++;
            }
            Remaining+=count;return count;
        }
    }
}

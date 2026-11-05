using UnityEngine;
namespace PocketBorough
{
    public sealed class BoroughVfx
    {
        readonly Transform[] particles=new Transform[48];readonly Vector3[] velocity=new Vector3[48];readonly float[] life=new float[48];int cursor;
        public BoroughVfx(Transform parent,BoroughCatalog catalog)
        {
            var m=new MeshWorkshop();m.Cylinder(Vector3.zero,.1f,.24f,MeshWorkshop.White,5,0);var mesh=m.Bake("Growth spark");
            for(int i=0;i<particles.Length;i++){var go=new GameObject("Pooled growth spark",typeof(MeshFilter),typeof(MeshRenderer));particles[i]=go.transform;go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=catalog.rimMaterial;go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;go.SetActive(false);}
        }
        public void Burst(Vector3 position,Color color,int count=5)
        {
            for(int j=0;j<count;j++){int i=cursor++%particles.Length;float a=cursor*2.4f;particles[i].position=position+new Vector3(Mathf.Cos(a),.08f,Mathf.Sin(a));particles[i].localScale=Vector3.one;particles[i].gameObject.SetActive(true);velocity[i]=new Vector3(Mathf.Cos(a)*2,2+j*.12f,Mathf.Sin(a)*2);life[i]=.55f;var block=new MaterialPropertyBlock();block.SetColor("_Tint",color);particles[i].GetComponent<MeshRenderer>().SetPropertyBlock(block);}
        }
        public void Tick(float dt){for(int i=0;i<particles.Length;i++)if(life[i]>0){life[i]-=dt;velocity[i]+=Vector3.down*dt*5;particles[i].position+=velocity[i]*dt;particles[i].Rotate(120*dt,80*dt,0);particles[i].localScale=Vector3.one*Mathf.Clamp01(life[i]*3);if(life[i]<=0)particles[i].gameObject.SetActive(false);}}
        public void Clear(){for(int i=0;i<particles.Length;i++){life[i]=0;particles[i].gameObject.SetActive(false);}}
    }
}

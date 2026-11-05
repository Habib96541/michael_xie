using UnityEngine;
using TMPro;

namespace PocketBorough
{
    public sealed class HoleAgent
    {
        public string Name;
        public Color Color;
        public Vector3 Position,Velocity;
        public float Radius=BoroughRules.InitialRadius,Mass,DecisionAt,RespawnAt,VisualRadius=BoroughRules.InitialRadius;
        public int Score,Consumed,Level=1;
        public float MaxRadius=BoroughRules.InitialRadius;
        public bool IsPlayer,Alive=true;
        public CityObject Target;
        public Transform Root;
        readonly Transform rim;
        readonly TextMeshPro label;
        HoleAgent swallowedBy;float deathTime,deathRadius;
        public HoleAgent(string name,Color color,bool player,Transform parent,BoroughCatalog catalog)
        {
            Name=name;Color=color;IsPlayer=player;
            Root=new GameObject(name+" • hole").transform;Root.SetParent(parent);
            var bowl=new GameObject("Recessed cavity",typeof(MeshFilter),typeof(MeshRenderer));bowl.transform.SetParent(Root,false);bowl.GetComponent<MeshFilter>().sharedMesh=catalog.bowlMesh;bowl.GetComponent<MeshRenderer>().sharedMaterial=catalog.bowlMaterial;
            var lip=new GameObject("Coloured sculpted rim",typeof(MeshFilter),typeof(MeshRenderer));lip.transform.SetParent(Root,false);lip.GetComponent<MeshFilter>().sharedMesh=catalog.rimMesh;lip.GetComponent<MeshRenderer>().sharedMaterial=catalog.rimMaterial;rim=lip.transform;
            var block=new MaterialPropertyBlock();block.SetColor("_Tint",color);lip.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            var tag=new GameObject("Name",typeof(TextMeshPro));tag.transform.SetParent(parent,false);label=tag.GetComponent<TextMeshPro>();label.font=catalog.font;label.fontSize=2.5f;label.alignment=TextAlignmentOptions.Center;label.color=player?MeshWorkshop.White:color;label.text=player?"YOU":name.ToUpperInvariant();label.rectTransform.sizeDelta=new(12,2);label.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public void Reset(Vector3 position)
        {Position=position;Velocity=Vector3.zero;Mass=0;Radius=VisualRadius=BoroughRules.InitialRadius;MaxRadius=Radius;Score=Consumed=0;Level=1;Alive=true;Target=null;DecisionAt=0;swallowedBy=null;Root.gameObject.SetActive(true);label.gameObject.SetActive(true);}
        public void SinkInto(HoleAgent eater){Alive=false;swallowedBy=eater;deathTime=0;deathRadius=VisualRadius;}
        public void Reward(float mass,int points)
        {Mass+=mass*(IsPlayer?1:.72f);Score+=points;Consumed++;Radius=BoroughRules.Radius(Mass);MaxRadius=Mathf.Max(MaxRadius,Radius);Level=BoroughRules.Level(Radius);}
        public void Move(Vector2 input,float dt)
        {
            var desired=new Vector3(input.x,0,input.y)*(7.5f+Radius*.16f);
            Velocity=Vector3.Lerp(Velocity,desired,1-Mathf.Exp(-dt*9));Position+=Velocity*dt;
            Position.x=Mathf.Clamp(Position.x,-BoroughRules.ArenaLimit+Radius,BoroughRules.ArenaLimit-Radius);Position.z=Mathf.Clamp(Position.z,-BoroughRules.ArenaLimit+Radius,BoroughRules.ArenaLimit-Radius);
        }
        public void Draw(float dt,Camera camera,float time)
        {
            label.gameObject.SetActive(Alive);
            if(!Alive){deathTime+=dt;bool sinking=swallowedBy!=null&&deathTime<.6f;Root.gameObject.SetActive(sinking);if(sinking){float t=deathTime/.6f;Root.position=Vector3.Lerp(Position,swallowedBy.Position,t);Root.localScale=Vector3.one*deathRadius*(1-t*t);}return;}
            Root.gameObject.SetActive(true);
            VisualRadius=Mathf.Lerp(VisualRadius,Radius,1-Mathf.Exp(-dt*7));Root.position=Position;Root.localScale=Vector3.one*VisualRadius;
            rim.localScale=Vector3.one*(1+.008f*Mathf.Sin(time*5));
            label.transform.position=Position+new Vector3(0,.3f,VisualRadius+1);label.transform.rotation=camera.transform.rotation;label.fontSize=Mathf.Clamp(1.6f+VisualRadius*.15f,1.6f,3);
        }
    }
}

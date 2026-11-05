using UnityEngine;

namespace LittleDepths
{
    public class FishActor : MonoBehaviour
    {
        public FishSpecies species;
        public bool isTrash;
        public bool caught;
        public float direction = 1, speed = .5f;
        public SpriteRenderer visual;
        public Animator animator;
        float baseY, phase;
        Transform attachedHook;
        Vector3 hookPoint;
        float hangingAngle;
        public Vector3 MouthWorld => visual.transform.TransformPoint(MouthLocal());
        public Vector3 HookPointWorld => attachedHook?attachedHook.TransformPoint(hookPoint):MouthWorld;

        public void Initialize(FishSpecies type, bool trash, Sprite sprite, Vector2 position, float dir)
        {
            species = type; isTrash = trash; direction = dir;
            transform.position = position;
            baseY = position.y; phase = Random.value * 6.28f;
            speed = trash ? .12f : type.speed * Random.Range(.8f, 1.2f);
            var art=new GameObject("Fish visual • mouth pivot");art.transform.SetParent(transform,false);
            visual = art.AddComponent<SpriteRenderer>();
            visual.sprite = sprite; visual.sortingOrder = 12;
            if(!trash && type.swimController)
            {
                animator=art.AddComponent<Animator>();animator.runtimeAnimatorController=type.swimController;
                animator.speed=Random.Range(.85f,1.2f);animator.Play("Swim",0,Random.value);
            }
            float w = trash ? .43f : type.size;
            transform.localScale = Vector3.one * (w / AppearanceFit.Size(sprite).x);
            visual.flipX = direction > 0;
        }

        public void Swim(float dt, float time, float width)
        {
            if (caught)
            {
                return;
            }
            Vector3 p = transform.position;
            p.x += direction * speed * dt;
            if (p.x > width) { p.x = width; direction = -1; }
            if (p.x < -width) { p.x = -width; direction = 1; }
            p.y = baseY + Mathf.Sin(time * 1.4f + phase) * .055f;
            transform.position = p;
            visual.flipX = direction > 0;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 4 + phase) * 3);
        }

        public void Attach(Transform hook, int index,Vector2 biteUV)
        {
            caught = true;attachedHook=hook;
            hookPoint=AppearanceFit.Point(hook.GetComponent<SpriteRenderer>().sprite,biteUV);
            transform.SetParent(hook, true);
            transform.localPosition=hookPoint;visual.flipX=false;
            float fan=index==0?0:((index+1)/2)*(index%2==0?-1:1)*17;
            hangingAngle=(species.id=="jelly"||isTrash?0:-90)+Mathf.Clamp(fan,-55,55);
            AlignCaught();
            visual.sortingOrder = 32 + index;
        }
        Vector3 MouthLocal()
        {
            Vector2 uv=new Vector2(.5f,.88f);
            if(!isTrash)
            {
                int frame=System.Array.IndexOf(species.swimFrames,visual.sprite);frame=Mathf.Clamp(frame,0,species.mouthPoints.Length-1);uv=species.mouthPoints[frame];
            }
            return AppearanceFit.Point(visual.sprite,uv,visual.flipX);
        }
        void AlignCaught()
        {
            transform.localPosition=hookPoint;
            transform.localRotation=Quaternion.Euler(0,0,hangingAngle+Mathf.Sin(Time.time*5+phase)*7);
            visual.transform.localPosition=-MouthLocal();
        }
        void LateUpdate(){if(caught&&attachedHook)AlignCaught();}
    }
}

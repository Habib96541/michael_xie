using UnityEngine;
using UnityEngine.InputSystem;

namespace BrickRush
{
    public sealed class InputManager
    {
        public float Target {get;private set;}
        public bool Launch {get;private set;}
        public bool Pause {get;private set;}
        Vector2 previousMouse;bool first=true;
        public void Tick(Camera camera,float dt)
        {
            Launch=Pause=false;var keys=Keyboard.current;
            if(keys!=null){Pause=keys.escapeKey.wasPressedThisFrame||keys.pKey.wasPressedThisFrame;Launch=keys.spaceKey.wasPressedThisFrame;
                float axis=(keys.dKey.isPressed||keys.rightArrowKey.isPressed?1:0)-(keys.aKey.isPressed||keys.leftArrowKey.isPressed?1:0);if(axis!=0)Target+=axis*12*dt;}
            var touch=Touchscreen.current;
            if(touch!=null&&touch.primaryTouch.press.isPressed)
            {
                var pos=touch.primaryTouch.position.ReadValue();
                Rect area=camera.pixelRect;area.yMin-=area.width*.2f;
                if(area.Contains(pos)){Target=camera.ScreenToWorldPoint(pos).x;Launch|=touch.primaryTouch.press.wasPressedThisFrame;}
            }
            else if(Mouse.current!=null)
            {
                Vector2 pos=Mouse.current.position.ReadValue();
                if(camera.pixelRect.Contains(pos)){if(!first&&(pos-previousMouse).sqrMagnitude>.1f)Target=camera.ScreenToWorldPoint(pos).x;Launch|=Mouse.current.leftButton.wasPressedThisFrame;}
                previousMouse=pos;first=false;
            }
            Target=Mathf.Clamp(Target,-3.7f,3.7f);
        }
        public void Reset(){Target=0;first=true;}
        public void SetTarget(float target){Target=target;}
    }
    public sealed class PaddleController
    {
        public const float Y=-5.85f;
        public float X {get;private set;}
        public float Width {get;private set;}
        SpriteRenderer body,glow,ghost;float velocity,pulse;
        public PaddleController(Transform parent)
        {
            body=Art.Sprite("Paddle",Art.Paddle,new Vector2(0,Y),new Vector2(1.8f,.27f),Art.Cyan,12,parent);
            glow=Art.Sprite("Paddle glow",Art.Glow,new Vector2(0,Y),new Vector2(2.8f,.9f),new Color(.2f,1,1,.4f),10,parent);
            ghost=Art.Sprite("Paddle wake",Art.Paddle,new Vector2(0,Y),new Vector2(1.8f,.27f),new Color(.2f,1,1,.1f),9,parent);
        }
        public void Tick(float target,float width,float dt)
        {
            Width=Mathf.MoveTowards(Width,width,dt*5);float old=X;
            X=Mathf.SmoothDamp(X,Mathf.Clamp(target,-4.67f+Width/2,4.67f-Width/2),ref velocity,.025f,80,dt);
            pulse=Mathf.Max(0,pulse-dt);body.transform.position=new Vector2(X,Y);
            Art.Size(body,new Vector2(Width*(1+pulse*.4f),.27f*(1-pulse*1.2f)));
            glow.transform.position=body.transform.position;glow.color=new Color(.2f,1,1,.3f+pulse*2);
            ghost.transform.position=new Vector2(Mathf.Lerp(old,X,.2f),Y);Art.Size(ghost,new Vector2(Width+Mathf.Abs(velocity)*.01f,.22f));ghost.color=new Color(.2f,1,1,Mathf.Clamp01(Mathf.Abs(velocity)/30)*.22f);
        }
        public void Hit(){pulse=.18f;}
        public void Reset(){X=velocity=0;Width=1.8f;}
    }
}

using UnityEngine;

namespace LittleDepths
{
    [DefaultExecutionOrder(-40)]
    public class AnglerRig : MonoBehaviour
    {
        // The girl, worn headwear and occupied boat are painted into one sprite.
        public SpriteRenderer visual;
        public Animator animator;
        public Transform rodTip;
        public int CastAnimations { get; private set; }
        public int ManualReelAnimations { get; private set; }
        FishingConfig config;
        FishingGame game;
        FishingPhase previous;
        public int LookIndex { get; private set; }

        public void Initialize(FishingGame controller)
        {
            game=controller;Configure(game.config,game.Progress);previous=FishingPhase.Harbor;
            if(animator)animator.Play("Idle");
        }
        public void Configure(FishingConfig balance,GameProgress appearance){config=balance;Apply(appearance);}
        public void Apply(GameProgress p)
        {
            if(config==null)return;
            LookIndex=FishingConfig.CompleteIndex(p.outfit,p.hairstyle,p.hat,p.boatStyle);
            visual.sprite=config.completeAnglers[LookIndex];visual.color=Color.white;
            visual.transform.localScale=Vector3.one*AppearanceFit.CompleteScale(config,LookIndex);
            visual.transform.parent.localPosition=new Vector3(0,config.surfaceY-2.1f,0);
            visual.transform.localPosition=Vector3.zero;
            UpdateRodTip();
        }
        void Update()
        {
            if(!game||!animator)return;
            bool paused=game.hud&&game.hud.IsModalOpen;animator.speed=paused?0:1;
            if(paused)return;
            if(game.Phase!=previous)
            {
                previous=game.Phase;
                string state=previous==FishingPhase.Descending?"Cast":previous==FishingPhase.Ascending?"Reel":previous==FishingPhase.Results?"Celebrate":"Idle";
                if(state=="Cast")CastAnimations++;
                animator.CrossFadeInFixedTime(state,.13f);
            }
        }
        public void PlayManualReel()
        {
            ManualReelAnimations++;
            previous=FishingPhase.Ascending;
            if(animator){animator.speed=1;animator.CrossFadeInFixedTime("Reel",.13f);}
        }
        void LateUpdate()
        {
            if(config!=null&&visual.sprite)UpdateRodTip();
        }
        void UpdateRodTip()
        {
            rodTip.position=visual.transform.TransformPoint(AppearanceFit.Point(visual.sprite,config.completeRodTips[LookIndex]));
        }
    }
}

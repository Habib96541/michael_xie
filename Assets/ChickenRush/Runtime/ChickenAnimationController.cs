using UnityEngine;

namespace ChickenRush
{
    public sealed class ChickenAnimationController : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer sprite;
        bool terminal;
        static readonly int Moving = Animator.StringToHash("Moving");
        static readonly int Hop = Animator.StringToHash("Hop");
        static readonly int Hit = Animator.StringToHash("Hit");
        static readonly int Celebrate = Animator.StringToHash("Celebrate");
        static readonly int Reset = Animator.StringToHash("Reset");
        static readonly int HopSpeed = Animator.StringToHash("HopSpeed");
        static readonly int Danger = Animator.StringToHash("Danger");
        public Animator Animator => animator;
        public void Configure(Animator value, SpriteRenderer renderer) { animator = value; sprite = renderer; }
        public void ResetForRound()
        {
            terminal = false;
            animator.ResetTrigger(Hop);
            animator.ResetTrigger(Hit);
            animator.ResetTrigger(Celebrate);
            animator.SetBool(Moving, false);
            animator.SetBool(Danger, false);
            animator.SetTrigger(Reset);
            animator.Update(0);
            animator.ResetTrigger(Reset);
            sprite.color = Color.white;
        }
        public void Walk(bool moving) { if (!terminal) animator.SetBool(Moving, moving); }
        public void ReactToDanger(bool active){if(!terminal)animator.SetBool(Danger,active);}
        public void Jump(float seconds)
        {
            if (terminal) return;
            animator.SetBool(Moving, false);
            animator.SetFloat(HopSpeed, .6f / seconds);
            animator.SetTrigger(Hop);
        }
        public void Lose()
        {
            if (terminal) return;
            terminal = true;
            animator.ResetTrigger(Hop);
            animator.ResetTrigger(Celebrate);
            animator.SetBool(Moving, false);
            animator.SetTrigger(Hit);
        }
        public void Win()
        {
            if (terminal) return;
            terminal = true;
            animator.ResetTrigger(Hop);
            animator.ResetTrigger(Hit);
            animator.SetBool(Moving, false);
            animator.SetTrigger(Celebrate);
        }
    }
}

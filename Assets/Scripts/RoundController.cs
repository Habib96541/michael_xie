using UnityEngine;

namespace FlapDash
{
    public enum RoundState
    {
        Ready,
        Playing,
        Results
    }

    public sealed class RoundController : MonoBehaviour
    {
        [SerializeField] private FlapPlayer player;
        [SerializeField] private PipeCourse course;
        [SerializeField] private RoundFeedback feedback;
        [SerializeField, Min(1f)] private float roundDuration = 60f;

        public RoundState State { get; private set; } = RoundState.Ready;
        public bool IsPlaying => State == RoundState.Playing;
        public int Score { get; private set; }
        public float RemainingTime { get; private set; }
        public string EndReason { get; private set; } = string.Empty;

        private void Start()
        {
            ResetRound();
        }

        private void Update()
        {
            if (!IsPlaying)
                return;

            RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
            if (RemainingTime <= 0f)
                EndRound("Time is up.");
        }

        public void StartRound()
        {
            if (IsPlaying)
                return;

            ResetRound();
            State = RoundState.Playing;
            player.BeginRound();
            course.BeginCourse();
        }

        public void RestartRound()
        {
            if (State == RoundState.Results)
                StartRound();
        }

        public void AddPoint()
        {
            if (IsPlaying)
                Score++;
        }

        public void EndRound(string reason)
        {
            if (!IsPlaying)
                return;

            State = RoundState.Results;
            EndReason = reason;
            player.StopPlayer();
            course.StopCourse();
        }

        private void ResetRound()
        {
            State = RoundState.Ready;
            Score = 0;
            RemainingTime = roundDuration;
            EndReason = string.Empty;
            player.ResetPlayer();
            course.ResetCourse();
            feedback.ClearEffects();
        }
    }
}

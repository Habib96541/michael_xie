using UnityEngine;

namespace BrickRush
{
    [CreateAssetMenu(menuName = "Brick Rush/Configuration")]
    public sealed class GameConfig : ScriptableObject
    {
        public float duration = 90;
        public int lives = 3;
        public float initialSpeed = 8.8f;
        public float finalSpeed = 15.2f;
        public float paddleWidth = 1.8f;
        public float powerDuration = 10;
        public int maxBalls = 5;
        public int lifeBonus = 100;
    }
    public enum SessionState { Menu, Countdown, Playing, Paused, Results }
    public enum BrickKind { Standard, Armored, Explosive, Bonus, Obstacle }
    public enum PowerKind { Multiball, Wide, Fire, Laser, Slow, Double, Shield }

    public static class GameRules
    {
        public static int Multiplier(int hits) => hits >= 15 ? 5 : hits >= 10 ? 4 : hits >= 6 ? 3 : hits >= 3 ? 2 : 1;
        public static int BaseScore(BrickKind kind) => kind == BrickKind.Armored ? 25 : kind == BrickKind.Explosive ? 20 : kind == BrickKind.Bonus ? 50 : kind == BrickKind.Obstacle ? 0 : 10;
        public static Vector2 SafeDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < .001f) return new Vector2(.4f, .916515f);
            direction.Normalize();
            float y = Mathf.Clamp(Mathf.Abs(direction.y), .34f, .955f);
            return new Vector2(Mathf.Sqrt(1-y*y) * (direction.x < 0 ? -1 : 1), y * (direction.y < 0 ? -1 : 1));
        }
        public static Vector2 PaddleBounce(float offset) => SafeDirection(new Vector2(Mathf.Clamp(offset,-1,1)*.88f, Mathf.Sqrt(1-Mathf.Pow(Mathf.Clamp(offset,-1,1)*.88f,2))));
        public static int FinalScore(int points, int lives, int bonus) => points + Mathf.Max(0, lives)*bonus;
    }
    public sealed class ScoreManager
    {
        public int Points { get; private set; }
        public int Destroyed { get; private set; }
        public int Combo { get; private set; }
        public int HighestCombo { get; private set; } = 1;
        public int HighestStreak { get; private set; }
        public int Multiplier => GameRules.Multiplier(Combo);
        public void Reset() { Points=Destroyed=Combo=HighestStreak=0; HighestCombo=1; }
        public int Award(BrickKind kind, bool doubled)
        {
            Combo++; Destroyed++;
            HighestCombo=Mathf.Max(HighestCombo,Multiplier); HighestStreak=Mathf.Max(HighestStreak,Combo);
            int points=GameRules.BaseScore(kind)*Multiplier*(doubled?2:1); Points+=points; return points;
        }
        public void Return() => Combo=0;
    }
    public static class SaveManager
    {
        public static int Best => PlayerPrefs.GetInt("BrickRush.v1.best",0);
        public static bool SetBest(int score) { if(score<=Best)return false; PlayerPrefs.SetInt("BrickRush.v1.best",score); PlayerPrefs.Save(); return true; }
        public static float Volume(string key, float value) => PlayerPrefs.GetFloat("BrickRush.v1."+key,value);
        public static void Set(string key,float value) { PlayerPrefs.SetFloat("BrickRush.v1."+key,value); PlayerPrefs.Save(); }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace LittleDepths
{
    public enum FishingPhase { Harbor, Timing, Descending, Ascending, Results }

    public class FishingGame : MonoBehaviour
    {
        public const float PerfectCastThreshold=.92f;
        public FishingConfig config;
        public Camera gameCamera;
        public FishingHUD hud;
        public FishingWorld world;
        public Transform hook;
        public LineRenderer fishingLine;
        public AudioSource audioSource;
        public AudioSource musicSource;
        public AudioClip catchSound, clickSound, sellSound, legendarySound, splashSound;
        public GameProgress Progress { get; private set; }
        public FishingPhase Phase { get; private set; }
        public float Depth => Mathf.Max(0, (config.surfaceY - hook.position.y) / config.metersToWorld);
        public float TimingPower => .5f + .5f * Mathf.Sin(timingClock * 3.8f - Mathf.PI / 2);
        public float TargetDepth { get; private set; }
        public int HaulValue { get; private set; }
        public int HaulCount => caught.Count;
        public int LastCollectionBonus { get; private set; }
        public IReadOnlyList<FishActor> Caught => caught;
        public bool IsFishing => Phase == FishingPhase.Descending || Phase == FishingPhase.Ascending;
        public string SaveSlot = GameProgress.SaveKey;
        public event Action Landed;
        readonly List<FishActor> caught = new List<FishActor>();
        float timingClock, targetX, passiveClock, splashClock;
        int trashCount;
        public Vector3 HookEye => hook.TransformPoint(AppearanceFit.Point(config.hookSprite,new Vector2(105.5f/180f,160f/180f)));

        void Awake()
        {
#if UNITY_EDITOR
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-littleDepthsSmoke") >= 0 || UnityEditor.SessionState.GetBool("LittleDepths.Smoke.Active",false))
                SaveSlot = "LittleDepths.SmokeOnly";
#endif
            Progress = GameProgress.Load(SaveSlot);
            InitializeMusic();
            int offline = Progress.AccruePassive(config, GameProgress.Now);
            Save();
            Phase = FishingPhase.Harbor;
            hook.position = new Vector3(0, config.surfaceY - .55f, 0);
            world.Populate(config, Progress, false);
            ApplyHook();
            if(world.angler)world.angler.Initialize(this);
            hud.Bind(this);
            if (offline >= 1) hud.ShowWelcome(offline);
            else if (Progress.sessionCasts >= config.castsPerSession) hud.ShowSession();
        }

        void Update()
        {
            if (Progress == null) return;
            if(Progress.dailyQuests.EnsureDay(GameProgress.Now))Save();
            float dt = Mathf.Min(Time.deltaTime, .05f);
            passiveClock += Time.unscaledDeltaTime;
            if (passiveClock >= 10) { passiveClock = 0; Progress.AccruePassive(config, GameProgress.Now); Save(); }
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Screen.width <= Screen.height) return;
#endif
            HandleKeys();
            if (hud.IsModalOpen) { hud.Refresh(); return; }
            world.Animate(dt, Time.time);
            if (Phase == FishingPhase.Timing) timingClock += dt;
            if (IsFishing) TickFishing(dt);
            else if (Phase == FishingPhase.Harbor || Phase == FishingPhase.Timing)
            {
                hook.position = new Vector3(hook.position.x, config.surfaceY - .55f + Mathf.Sin(Time.time * 1.6f) * .025f, 0);
            }
            float cameraY = IsFishing ? Mathf.Min(0, hook.position.y + .65f) : 0;
            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, new Vector3(0, cameraY, -10), 1 - Mathf.Exp(-dt * 4.8f));
            hud.Refresh();
        }

        void LateUpdate()
        {
            if(!fishingLine||!hook||!world)return;
            if(world.angler&&world.angler.rodTip)
            {
                // Keep the real rod tip directly over the eye of the hook. Steering
                // moves the complete boat horizontally, so the line stays plumb.
                if(IsFishing)world.boat.position+=Vector3.right*(HookEye.x-world.angler.rodTip.position.x);
                else if(Phase==FishingPhase.Harbor || Phase==FishingPhase.Timing)
                {
                    var boat=world.boat.position;boat.x=1.6f;world.boat.position=boat;
                    hook.position+=Vector3.right*(world.angler.rodTip.position.x-HookEye.x);
                }
                else if(Phase==FishingPhase.Results)
                {
                    hook.rotation=Quaternion.identity;
                    hook.position+=world.angler.rodTip.position-HookEye;
                }
                fishingLine.SetPosition(0,world.angler.rodTip.position);
            }
            else fishingLine.SetPosition(0,HookEye+Vector3.up*2);
            fishingLine.SetPosition(1,HookEye);
        }

        void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (hud.IsModalOpen) hud.CloseModal();
                else if (Phase == FishingPhase.Timing) { Phase = FishingPhase.Harbor; hud.SetPhase(); }
                else hud.ShowSettings();
            }
            if (hud.IsModalOpen) return;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                if (Phase == FishingPhase.Harbor || Phase == FishingPhase.Timing) CastButton();
                else if (IsFishing) ReelIn();
            }
        }

        public void CastButton()
        {
            if (hud.IsModalOpen) return;
            if (Progress.sessionCasts >= config.castsPerSession) { hud.ShowSession(); return; }
            if (Phase == FishingPhase.Harbor)
            {
                Phase = FishingPhase.Timing; timingClock = 0;
                Sound(clickSound); hud.SetPhase();
            }
            else if (Phase == FishingPhase.Timing) Launch(TimingPower);
        }

        public void Launch(float power)
        {
            if (Phase != FishingPhase.Timing) return;
            ClearCaught(); HaulValue = 0; trashCount = 0; LastCollectionBonus = 0;
            float factor = Mathf.Lerp(.65f, 1, Mathf.Clamp01(power));
            TargetDepth = config.MaxDepth(Progress) * factor;
            Progress.sessionCasts++; Progress.totalCasts++;
            Progress.dailyQuests.RecordCast(GameProgress.Now);
            Save();
            world.Populate(config, Progress, true);
            hook.position = new Vector3(0, config.surfaceY - .55f, 0); targetX = 0;
            Phase = FishingPhase.Descending; splashClock = 0;
            Sound(splashSound); world.Burst(hook.position, new Color(.75f, 1, .94f), 16);
            hud.SetPhase();
            hud.Toast(power >= PerfectCastThreshold ? "PERFECT CAST!" : power >= .65f ? "NICE CAST" : "INTO THE BLUE", Color.white);
        }

        void TickFishing(float dt)
        {
            Vector3 previous = hook.position;
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                targetX = gameCamera.ScreenToWorldPoint(pointer.position.ReadValue()).x;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float axis = (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed ? 1 : 0);
                targetX += axis * dt * 5;
            }
            targetX = Mathf.Clamp(targetX, -config.riverHalfWidth, config.riverHalfWidth);
            float x = Mathf.MoveTowards(previous.x, targetX, dt * 7.5f);
            float speed = Phase == FishingPhase.Descending ? -config.descentMetersPerSecond : config.ascentMetersPerSecond;
            speed*=1+Progress.speedLevel*.15f;
            float returnY=config.surfaceY;
            if(world.angler&&world.angler.rodTip)
                returnY=world.angler.rodTip.position.y-(HookEye.y-hook.position.y);
            float y=previous.y+speed*config.metersToWorld*dt;
            if(Phase==FishingPhase.Ascending)y=Mathf.Min(y,returnY);
            hook.position = new Vector3(x,y,0);
            hook.rotation = Quaternion.Euler(0, 0, Mathf.Clamp((previous.x - x) * 160, -18, 18));
            Progress.deepest = Mathf.Max(Progress.deepest, Depth);
            Progress.dailyQuests.RecordDepth(Depth,GameProgress.Now);
            foreach (var fish in world.Fish)
            {
                if (fish.caught || HaulCount >= config.Capacity(Progress)) continue;
                Vector2 p = fish.transform.position;
                Vector2 a = previous, b = hook.position;
                Vector2 delta = b - a;
                float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, delta) / delta.sqrMagnitude) : 0;
                if (Vector2.Distance(p, a + delta * t) < .30f + (fish.isTrash ? 0 : fish.species.size * .22f)) Catch(fish);
            }
            if (Phase == FishingPhase.Descending && (Depth >= TargetDepth || HaulCount >= config.Capacity(Progress))) ReelEarly();
            splashClock += dt;
            if (splashClock > .18f) { splashClock = 0; world.Bubble(hook.position + Vector3.down * .15f); }
            if (Phase == FishingPhase.Ascending && hook.position.y >= returnY-.0001f)
            {
                hook.rotation=Quaternion.identity;
                if(world.angler&&world.angler.rodTip)hook.position+=world.angler.rodTip.position-HookEye;
                Land();
            }
        }

        public void Catch(FishActor fish)
        {
            if (fish.caught || !IsFishing || caught.Count >= config.Capacity(Progress)) return;
            bool fresh = !fish.isTrash && !Progress.discovered.Contains(fish.species.id)
                && !caught.Exists(f => !f.isTrash && f.species.id == fish.species.id);
            fish.Attach(hook,caught.Count,config.hookMouthPoint);caught.Add(fish);
            if (fish.isTrash) { trashCount++; hud.FloatText("LAKE CLEANED +1", hook.position, new Color(.7f,.9f,.85f)); }
            else
            {
                HaulValue += fish.species.value;
                bool legendary = fish.species.rarity == "Legendary";
                Sound(legendary ? legendarySound : catchSound);
                hud.FloatText((legendary ? "LEGENDARY!\n" : "") + "+$" + fish.species.value, hook.position, legendary ? new Color(1,.85f,.35f) : Color.white);
                if (fresh) hud.Toast("DISCOVERED  " + fish.species.displayName.ToUpperInvariant(), fish.species.color);
            }
            world.Burst(hook.position, fish.isTrash ? Color.white : fish.species.color, 7);
        }

        public void ReelEarly()
        {
            if (Phase != FishingPhase.Descending) return;
            Phase = FishingPhase.Ascending; hud.SetPhase();
        }

        // Manual and automatic returns share continuous movement at 1.4x descent.
        // The haul is paid only when the hook actually reaches the rod tip.
        public void ReelIn()
        {
            if (Phase!=FishingPhase.Descending || hud.IsModalOpen) return;
            if(world.angler)world.angler.PlayManualReel();
            ReelEarly();
        }

        void InitializeMusic()
        {
            if (!musicSource)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.clip = Resources.Load<AudioClip>("LittleDepthsAmbience");
                musicSource.volume = .3f;
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            ApplyMusicPreference();
        }

        public void ApplyMusicPreference()
        {
            if (!musicSource || !musicSource.clip) return;
            if (Progress.music && !musicSource.isPlaying) musicSource.Play();
            else if (!Progress.music) musicSource.Pause();
        }

        public void Land()
        {
            if (!IsFishing) return;
            Phase = FishingPhase.Results;
            foreach (var fish in caught)
                if (!fish.isTrash)
                {
                    if(!Progress.discovered.Contains(fish.species.id))Progress.discovered.Add(fish.species.id);
                    Progress.dailyQuests.RecordFish(fish.species.id,GameProgress.Now);
                }
            int fishCount = caught.Count - trashCount;
            Progress.totalFish += fishCount; Progress.sessionFish += fishCount;
            Progress.bestHaul = Mathf.Max(Progress.bestHaul, HaulValue);
            int milestones = Progress.discovered.Count / 5;
            LastCollectionBonus = Mathf.Max(0, milestones - Progress.collectionMilestones) * 150;
            Progress.collectionMilestones = milestones;
            Progress.coins += HaulValue + LastCollectionBonus;
            Progress.sessionCoins += HaulValue + LastCollectionBonus;
            Save(); Sound(sellSound);
            hud.SetPhase(); hud.ShowResults(); Landed?.Invoke();
        }

        public void ReturnToHarbor()
        {
            hud.CloseModal(true); ClearCaught();
            Phase = FishingPhase.Harbor; hook.rotation = Quaternion.identity;
            hook.position = new Vector3(0, config.surfaceY - .55f, 0);
            world.Populate(config, Progress, false); hud.SetPhase();
        }

        public void NextCast()
        {
            ReturnToHarbor();
            if (Progress.sessionCasts >= config.castsPerSession) hud.ShowSession();
            else CastButton();
        }

        public void NewSession()
        {
            Progress.sessionCasts = Progress.sessionCoins = Progress.sessionFish = 0;
            Save(); ReturnToHarbor();
        }

        public bool BuyUpgrade(int index)
        {
            if (Phase != FishingPhase.Harbor || index < 0 || index > 3) return false;
            if (config.UpgradeLevel(Progress, index) >= config.UpgradeLimit(index)) return false;
            int cost = config.UpgradeCost(Progress, index);
            if (Progress.coins < cost) { hud.Toast("A few more fish will do it!", Color.white); return false; }
            Progress.AccruePassive(config, GameProgress.Now);
            Progress.coins -= cost;
            if (index == 0) Progress.capacityLevel++;
            else if (index == 1) Progress.depthLevel++;
            else if (index == 3) Progress.speedLevel++;
            else Progress.incomeLevel++;
            Save(); Sound(sellSound); hud.Toast("UPGRADED!", new Color(.75f,.95f,.7f)); hud.Refresh(); return true;
        }

        public bool SelectHook(int index)
        {
            if (index < 0 || index >= config.hookColors.Length || !Progress.unlockedHooks.Contains(index)) return false;
            Progress.selectedHook = index; ApplyHook(); Save(); Sound(clickSound); return true;
        }
        void ApplyHook() => hook.GetComponent<SpriteRenderer>().color = config.hookColors[Mathf.Clamp(Progress.selectedHook, 0, config.hookColors.Length - 1)];
        public bool UnlockHook()
        {
            if (Progress.unlockedHooks.Count >= config.hookColors.Length || Progress.coins < 250) return false;
            Progress.coins -= 250;
            var locked = new List<int>();
            for (int i = 0; i < config.hookColors.Length; i++) if (!Progress.unlockedHooks.Contains(i)) locked.Add(i);
            int index = locked[UnityEngine.Random.Range(0, locked.Count)];
            Progress.unlockedHooks.Add(index); SelectHook(index); return true;
        }
        public int ClaimDailyQuest(int index)
        {
            int reward=Progress.dailyQuests.Claim(index,GameProgress.Now);
            if(reward==0)return 0;
            Progress.coins=(int)Math.Min((long)Progress.coins+reward,int.MaxValue/2);
            Save();Sound(sellSound);hud.Toast("DAILY QUEST +$"+reward,UiKit.Coral);hud.Refresh();return reward;
        }
        public int ClaimPrize(int cardIndex=0)
        {
            if (cardIndex<0||cardIndex>8||GameProgress.Now < Progress.prizeReadyAt) return 0;
            int reward = UnityEngine.Random.Range(2, 7) * 25;
            Progress.coins += reward; Progress.prizeReadyAt = GameProgress.Now + 4 * 3600;
            Progress.prizeLastCard=cardIndex;Progress.prizeLastReward=reward;
            Save(); Sound(legendarySound); return reward;
        }
        public void Sound(AudioClip clip) { if (Progress != null && Progress.sound && clip != null) audioSource.PlayOneShot(clip, .5f); }
        public void Save() { if (Progress != null) Progress.Save(SaveSlot); }
        void ClearCaught() { foreach (var f in caught) if (f) Destroy(f.gameObject); caught.Clear(); }
        void OnApplicationPause(bool paused) { if (paused && Progress != null) { Progress.AccruePassive(config, GameProgress.Now); Save(); } }
        void OnApplicationFocus(bool focused) { if (focused && Progress != null) { Progress.AccruePassive(config, GameProgress.Now); Save(); } }
        void OnApplicationQuit() { if (Progress != null) { Progress.AccruePassive(config, GameProgress.Now); Save(); } }
    }
}

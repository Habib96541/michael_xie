using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Round flow for Chest Shuffle 3D: place a bet, watch the three chests
/// shuffle, pick one, and collect stake x hidden multiplier. Also owns the
/// camera framing, chest motion, picking raycasts and saved bankroll.
/// </summary>
public sealed class ChestShuffleGame : MonoBehaviour
{
    public enum Phase { Idle, Shuffling, Picking, Reveal, Result }

    public const int StartingBalance = 1000;
    public const int TopUpAmount = 500;
    public static readonly int[] StakeOptions = { 10, 20, 50, 100, 250 };

    const string BalanceKey = "ChestShuffle3D.Balance";

    [Header("Timing")]
    [SerializeField] float leadInSeconds = 0.6f;
    [SerializeField] float swapSeconds = 0.42f;
    [SerializeField] float resultDelay = 2.4f;
    [SerializeField] float resultRollSeconds = 2.6f;

    static readonly Vector3[] SlotPositions =
    {
        new Vector3(-2.75f, 0.32f, 0f),
        new Vector3(0f, 0.32f, 0f),
        new Vector3(2.75f, 0.32f, 0f)
    };

    readonly System.Random rng = new System.Random();
    readonly int[] slotOfChest = new int[MultiplierTable.ChestCount];
    readonly List<Vector2Int> swaps = new List<Vector2Int>();
    readonly float[] multipliers = new float[MultiplierTable.ChestCount];

    Chest3D[] chests;
    ChestHud hud;
    ChestShuffleAudio audioFeedback;
    Camera cam;

    Phase phase = Phase.Idle;
    float phaseStart;
    float time;
    float revealStart;
    int balance;
    int bet = StakeOptions[0];
    int stake;
    int chosen = -1;
    int payout;
    int activeSwap = -1;
    int swapChestA = -1;
    int swapChestB = -1;
    bool othersOpened;
    bool payoutPaid;
    float shake;
    float punch;
    float cameraDistance;
    float cameraDistanceVelocity;
    bool cameraDistanceInitialized;
    int hoverChest = -1;
    string toast = string.Empty;
    float toastTime = -100f;

    public Phase CurrentPhase => phase;
    public int Balance => balance;
    public int Bet => bet;
    public int Stake => stake;
    public int Payout => payout;
    public int Net => payout - stake;
    public int ChosenChest => chosen;
    public int ChosenLane => chosen >= 0 ? slotOfChest[chosen] + 1 : 0;
    public int MinStake => StakeOptions[0];
    public Camera GameCamera => cam;
    public bool CanPick => phase == Phase.Picking;
    public bool CanStart => phase == Phase.Idle && balance >= bet;
    public bool NeedsTopUp => phase == Phase.Idle && balance < MinStake;
    public bool RoundActive => phase == Phase.Shuffling || phase == Phase.Picking || phase == Phase.Reveal;
    public bool IsJackpot => chosen >= 0 && multipliers[chosen] >= 10f;
    public string Toast => toast;
    public float ToastAge => time - toastTime;
    public float PhaseAge => time - phaseStart;
    public float ResultRollProgress => phase == Phase.Result ? Mathf.Clamp01(PhaseAge / Mathf.Max(0.1f, resultRollSeconds)) : 0f;
    public bool ResultReady => phase == Phase.Result && ResultRollProgress >= 1f;
    public int HoveredChest => hoverChest;

    void Awake()
    {
        cam = Camera.main;
        hud = GetComponent<ChestHud>();
        audioFeedback = GetComponent<ChestShuffleAudio>();
        if (audioFeedback == null) audioFeedback = gameObject.AddComponent<ChestShuffleAudio>();
        balance = Mathf.Max(0, PlayerPrefs.GetInt(BalanceKey, StartingBalance));
        BuildChests();
        for (int i = 0; i < slotOfChest.Length; i++) slotOfChest[i] = i;
        PickAffordableBet();
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.antiAliasing = 4;
        Input.simulateMouseWithTouches = true;
        phase = Phase.Idle;
        phaseStart = time;
        Say("Choose a stake, then press PLAY.");
    }

    void Update()
    {
        float dt = Time.deltaTime;
        time += dt;

        UpdateCamera(dt);
        UpdateHover();

        if (phase == Phase.Shuffling) UpdateShuffle();
        else if (phase == Phase.Reveal) UpdateReveal();
        else if (phase == Phase.Result) UpdateResult();
        else if (bet > balance) PickAffordableBet();

        for (int i = 0; i < chests.Length; i++) chests[i].Tick(dt, time);

        HandleKeys();
        if (ToastAge > 6f) toast = string.Empty;
        if (hud == null) hud = GetComponent<ChestHud>();
    }

    void BuildChests()
    {
        chests = new Chest3D[MultiplierTable.ChestCount];
        for (int i = 0; i < chests.Length; i++)
        {
            var go = new GameObject("Chest " + (i + 1));
            go.transform.SetParent(transform, false);
            go.transform.position = SlotPositions[i];
            var chest = go.AddComponent<Chest3D>();
            chest.Build(i + 1);
            chests[i] = chest;
            multipliers[i] = 1f;
        }
    }

    // ---------------------------------------------------------------- round flow

    public void SetBet(int amount)
    {
        if (phase != Phase.Idle) return;
        bool known = false;
        for (int i = 0; i < StakeOptions.Length; i++) if (StakeOptions[i] == amount) known = true;
        if (!known) return;
        if (amount > balance)
        {
            Say("Not enough coins for that stake.");
            return;
        }
        if (bet != amount && audioFeedback != null) audioFeedback.PlayButtonClick();
        bet = amount;
    }

    public void StartRound()
    {
        if (phase != Phase.Idle && phase != Phase.Result) return;
        if (balance < bet)
        {
            Say("Not enough coins for that stake.");
            return;
        }

        stake = bet;
        balance -= stake;
        SaveBalance();
        if (audioFeedback != null)
        {
            audioFeedback.PlayButtonClick();
            audioFeedback.PlayCoinSpend();
        }

        float[] rolled = MultiplierTable.RollDistinct(rng);
        for (int i = 0; i < multipliers.Length; i++) multipliers[i] = rolled[i];

        for (int i = 0; i < chests.Length; i++)
        {
            slotOfChest[i] = i;
            chests[i].ResetChest();
            chests[i].SetFocused(true);
            chests[i].transform.SetPositionAndRotation(SlotPositions[i], Quaternion.identity);
        }

        chosen = -1;
        payout = 0;
        payoutPaid = false;
        othersOpened = false;
        hoverChest = -1;
        BuildSwaps();
        activeSwap = -1;
        swapChestA = swapChestB = -1;
        phase = Phase.Shuffling;
        phaseStart = time;
        shake = 0f;
        punch = 0f;
        Say("Stake " + MultiplierTable.Coins(stake) + " locked in. Keep your eyes on the chests!");
    }

    public void Pick(int chest)
    {
        if (phase != Phase.Picking) return;
        if (chest < 0 || chest >= chests.Length) return;

        chosen = chest;
        phase = Phase.Reveal;
        phaseStart = time;
        revealStart = time;
        othersOpened = false;

        chests[chest].SetOpen(true);
        if (audioFeedback != null) audioFeedback.PlayLevelUp();
        for (int i = 0; i < chests.Length; i++)
        {
            chests[i].SetHover(false);
            chests[i].SetFocused(i == chest);
            chests[i].SetSpotlight(i == chest);
        }

        payout = MultiplierTable.Payout(stake, multipliers[chest]);

        punch = Mathf.Clamp(0.22f + multipliers[chest] * 0.06f, 0.22f, 0.95f);
        if (multipliers[chest] >= 3f) shake = 0.5f;
        Say("Lane " + ChosenLane + " selected. Revealing your prize!");
    }

    public void ReturnToBetting()
    {
        if (phase != Phase.Result || !ResultReady) return;
        if (audioFeedback != null) audioFeedback.PlayButtonClick();
        for (int i = 0; i < chests.Length; i++)
        {
            slotOfChest[i] = i;
            chests[i].ResetChest();
            chests[i].transform.SetPositionAndRotation(SlotPositions[i], Quaternion.identity);
        }
        chosen = -1;
        payout = 0;
        phase = Phase.Idle;
        phaseStart = time;
        PickAffordableBet();
        Say("Choose a stake, then press PLAY.");
    }

    public void PickBySlot(int slot)
    {
        if (phase != Phase.Picking) return;
        Pick(ChestAtSlot(slot));
    }

    public void TopUp()
    {
        if (audioFeedback != null) audioFeedback.PlayButtonClick();
        balance += TopUpAmount;
        SaveBalance();
        if (phase == Phase.Result) phase = Phase.Idle;
        PickAffordableBet();
        Say("Added " + MultiplierTable.Coins(TopUpAmount) + " coins. Keep playing!");
    }

    public void ResetBank()
    {
        if (RoundActive) return;
        if (audioFeedback != null) audioFeedback.PlayButtonClick();
        balance = StartingBalance;
        SaveBalance();
        if (phase == Phase.Result) phase = Phase.Idle;
        PickAffordableBet();
        Say("Bankroll reset to " + MultiplierTable.Coins(StartingBalance) + " coins.");
    }

    // ---------------------------------------------------------------- shuffle

    void BuildSwaps()
    {
        swaps.Clear();
        int count = 4 + rng.Next(3);
        for (int i = 0; i < count; i++)
        {
            Vector2Int pair;
            int guard = 0;
            do
            {
                int a = rng.Next(MultiplierTable.ChestCount);
                int b = rng.Next(MultiplierTable.ChestCount - 1);
                if (b >= a) b++;
                pair = new Vector2Int(Mathf.Min(a, b), Mathf.Max(a, b));
                guard++;
            }
            while (swaps.Count > 0 && swaps[swaps.Count - 1] == pair && guard < 32);
            swaps.Add(pair);
        }
    }

    void UpdateShuffle()
    {
        float elapsed = time - phaseStart - leadInSeconds;

        if (elapsed < 0f)
        {
            SnapToSlots();
            return;
        }

        float total = swaps.Count * swapSeconds;
        if (elapsed >= total)
        {
            if (activeSwap >= 0)
            {
                CommitSwap();
                if (audioFeedback != null) audioFeedback.PlayShuffleDrop();
            }
            activeSwap = -1;
            swapChestA = swapChestB = -1;
            SnapToSlots();
            phase = Phase.Picking;
            phaseStart = time;
            Say("Pick a chest \u2014 click it or press 1, 2 or 3.");
            return;
        }

        int index = Mathf.FloorToInt(elapsed / swapSeconds);
        if (index != activeSwap)
        {
            if (activeSwap >= 0)
            {
                CommitSwap();
                if (audioFeedback != null) audioFeedback.PlayShuffleDrop();
            }
            activeSwap = index;
            swapChestA = ChestAtSlot(swaps[index].x);
            swapChestB = ChestAtSlot(swaps[index].y);
            for (int i = 0; i < chests.Length; i++) chests[i].SetFocused(false);
        }

        float progress = Mathf.Clamp01(elapsed / swapSeconds - index);
        AnimateSwap(swaps[index].x, swaps[index].y, progress);
    }

    void AnimateSwap(int slotA, int slotB, float progress)
    {
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        float arc = Mathf.Sin(progress * Mathf.PI);

        for (int i = 0; i < chests.Length; i++)
        {
            Vector3 target = SlotPositions[Mathf.Clamp(slotOfChest[i], 0, 2)];
            Quaternion rotation = Quaternion.identity;

            if (i == swapChestA)
            {
                target = Vector3.Lerp(SlotPositions[slotA], SlotPositions[slotB], eased);
                target.y += arc * 0.95f;
                target.z -= arc * 0.95f;
                rotation = Quaternion.Euler(0f, 0f, (slotB > slotA ? -1f : 1f) * arc * 11f);
            }
            else if (i == swapChestB)
            {
                target = Vector3.Lerp(SlotPositions[slotB], SlotPositions[slotA], eased);
                target.y += arc * 0.95f;
                target.z += arc * 0.95f;
                rotation = Quaternion.Euler(0f, 0f, (slotA > slotB ? -1f : 1f) * arc * 11f);
            }

            chests[i].transform.SetPositionAndRotation(target, rotation);
        }
    }

    void CommitSwap()
    {
        if (swapChestA < 0 || swapChestB < 0) return;
        int a = slotOfChest[swapChestA];
        int b = slotOfChest[swapChestB];
        slotOfChest[swapChestA] = b;
        slotOfChest[swapChestB] = a;
    }

    void SnapToSlots()
    {
        for (int i = 0; i < chests.Length; i++)
            chests[i].transform.SetPositionAndRotation(SlotPositions[Mathf.Clamp(slotOfChest[i], 0, 2)], Quaternion.identity);
    }

    void UpdateReveal()
    {
        float age = time - revealStart;
        if (!othersOpened && age > 1.6f)
        {
            othersOpened = true;
            for (int i = 0; i < chests.Length; i++)
                if (i != chosen) chests[i].SetOpen(true);
        }

        if (age >= resultDelay)
        {
            phase = Phase.Result;
            phaseStart = time;
            if (audioFeedback != null) audioFeedback.PlaySlotReel();
        }
    }

    void UpdateResult()
    {
        if (payoutPaid || !ResultReady) return;
        balance += payout;
        SaveBalance();
        payoutPaid = true;
        if (audioFeedback != null)
        {
            audioFeedback.StopSlotReel();
            audioFeedback.PlayCashResult();
        }
        Say("Lane " + ChosenLane + " held " + MultiplierTable.Format(multipliers[chosen]) +
            " and paid " + MultiplierTable.Coins(payout) + " coins.");
    }

    // ---------------------------------------------------------------- input and camera

    void UpdateHover()
    {
        int hovered = -1;
        bool blocked = false;
        Vector2 pointer = Input.mousePosition;
        bool pointerDown = Input.GetMouseButtonDown(0);
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began) continue;
                pointer = touch.position;
                pointerDown = true;
                break;
            }
        }

        if (phase == Phase.Picking && cam != null)
        {
            blocked = hud != null && hud.PointerOverUi(pointer);
            if (!blocked)
            {
                Ray ray = cam.ScreenPointToRay(pointer);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 120f))
                {
                    var chest = hit.collider.GetComponentInParent<Chest3D>();
                    if (chest != null) hovered = Mathf.Clamp(chest.Number - 1, 0, chests.Length - 1);
                }
            }
        }

        if (hovered != hoverChest)
        {
            if (hoverChest >= 0 && hoverChest < chests.Length) chests[hoverChest].SetHover(false);
            hoverChest = hovered;
            if (hoverChest >= 0) chests[hoverChest].SetHover(true);
        }

        if (phase == Phase.Picking && hoverChest >= 0 && pointerDown)
            Pick(hoverChest);
    }

    void HandleKeys()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            if (phase == Phase.Result && ResultReady) ReturnToBetting();
            else if (CanStart) StartRound();
            else if (NeedsTopUp) TopUp();
        }

        if (phase != Phase.Picking) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) PickBySlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) PickBySlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) PickBySlot(2);
    }

    void UpdateCamera(float dt)
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        float aspect = Mathf.Max(0.35f, cam.aspect);
        float targetFov = aspect < 1.05f ? 52f : 38f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, dt * 4f);

        float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * aspect;
        bool showingPrizeChest = (phase == Phase.Reveal || phase == Phase.Result) && chosen >= 0;
        float targetDistance = Mathf.Max(5.05f / Mathf.Max(0.08f, tanH), 2.0f / Mathf.Max(0.08f, tanV))
            + (showingPrizeChest ? 5.5f : 1.0f) - (showingPrizeChest ? Mathf.Min(punch, 0.08f) : punch);

        Vector3 pivot = new Vector3(0f, 0.85f, 0f);
        Vector3 direction = new Vector3(0f, 0.46f, -1f).normalized;
        if (!cameraDistanceInitialized)
        {
            cameraDistance = targetDistance;
            cameraDistanceInitialized = true;
        }
        else
        {
            float smoothTime = showingPrizeChest ? 0.8f : 0.5f;
            cameraDistance = Mathf.SmoothDamp(cameraDistance, targetDistance, ref cameraDistanceVelocity,
                smoothTime, Mathf.Infinity, dt);
        }

        float mouseX = Screen.width > 0 ? Input.mousePosition.x / Screen.width - 0.5f : 0f;
        float yaw = Mathf.Clamp(mouseX, -0.5f, 0.5f) * 3f;
        Quaternion orbit = Quaternion.Euler(0f, yaw, 0f);
        Vector3 position = pivot + orbit * (direction * cameraDistance);

        shake = Mathf.Max(0f, shake - dt * 1.8f);
        punch = Mathf.MoveTowards(punch, 0f, dt * 1.6f);
        if (shake > 0f) position += Random.insideUnitSphere * (shake * 0.10f);

        cam.transform.position = position;
        Vector3 lookAt = pivot + new Vector3(0f, -0.42f, 0.45f);
        if (shake > 0f) lookAt += Random.insideUnitSphere * (shake * 0.03f);
        cam.transform.rotation = Quaternion.LookRotation(lookAt - position);
    }

    // ---------------------------------------------------------------- helpers

    public float MultiplierAt(int chest)
    {
        return chest >= 0 && chest < multipliers.Length ? multipliers[chest] : 1f;
    }

    public int SlotOf(int chest)
    {
        return chest >= 0 && chest < slotOfChest.Length ? slotOfChest[chest] : 0;
    }

    public int ChestAtSlot(int slot)
    {
        for (int i = 0; i < slotOfChest.Length; i++)
            if (slotOfChest[i] == slot) return i;
        return 0;
    }

    public bool IsRevealed(int chest)
    {
        return chest >= 0 && chest < chests.Length && chests[chest].Openness > 0.2f;
    }

    public Vector3 ChestAnchor(int chest)
    {
        if (chest < 0 || chest >= chests.Length) return Vector3.zero;
        return chests[chest].transform.position + Vector3.up * 2.1f;
    }

    public void Say(string message)
    {
        toast = message;
        toastTime = time;
    }

    void PickAffordableBet()
    {
        if (bet >= StakeOptions[0] && bet <= balance) return;

        int pick = StakeOptions[0];
        for (int i = 0; i < StakeOptions.Length; i++)
        {
            if (StakeOptions[i] <= balance) pick = StakeOptions[i];
            else break;
        }
        bet = pick;
    }

    void SaveBalance()
    {
        PlayerPrefs.SetInt(BalanceKey, balance);
        PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        SaveBalance();
    }
}

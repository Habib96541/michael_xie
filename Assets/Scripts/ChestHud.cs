using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Screen-space overlay for Chest Shuffle 3D: title, animated bankroll,
/// stake chips, play button, phase messaging, world-anchored chest badges and
/// multiplier reveals. Drawn with IMGUI so the project needs no UI packages
/// and no font assets beyond the bundled TTFs.
/// </summary>
public sealed class ChestHud : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.79f, 0.36f);
    static readonly Color GoldBright = new Color(1f, 0.91f, 0.60f);
    static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f);
    static readonly Color Dim = new Color(0.65f, 0.59f, 0.70f);
    static readonly Color Pink = new Color(1f, 0.44f, 0.53f);
    static readonly Color Teal = new Color(0.48f, 0.93f, 0.78f);
    static readonly Color Ink = new Color(0.10f, 0.065f, 0.02f);

    ChestShuffleGame game;
    Camera cam;
    BackgroundBlur backgroundBlur;

    Font displayFont;
    Font bodyFont;
    Font boldFont;
    Texture2D roundTex;
    Texture2D roundStrokeTex;
    GUIStyle roundFillStyle;
    GUIStyle roundStrokeStyle;
    Texture2D coinTex;
    Texture2D glowTex;
    Texture2D vignetteTex;
    Texture2D topFadeTex;
    Texture2D bottomFadeTex;
    GUIStyle clickStyle;
    readonly Dictionary<string, GUIStyle> textStyles = new Dictionary<string, GUIStyle>();
    readonly List<float> resultReelValues = new List<float>(MultiplierTable.Values.Length + 1);

    float scale = 1f;
    float viewW = 1440f;
    float viewH = 900f;
    float pad = 28f;
    bool portrait;

    Rect titleRect, subtitleRect, balanceRect, resetRect, dockRect, playRect, statusRect, toastRect;
    Rect resultButtonRect;
    readonly Rect[] chipRects = new Rect[5];

    float shownBalance;
    float balanceFrom;
    float balanceTarget;
    float balanceAnim = 1f;
    float balancePulse;
    string floatText = string.Empty;
    float floatTimer;
    int lastBalance;
    bool balanceReady;

    float flash;
    ChestShuffleGame.Phase observedPhase;
    bool phaseReady;

    void Update()
    {
        if (game == null) game = GetComponent<ChestShuffleGame>();
        if (game == null) return;
        if (cam == null) cam = game.GameCamera;
        if (cam != null && backgroundBlur == null)
        {
            backgroundBlur = cam.GetComponent<BackgroundBlur>();
            if (backgroundBlur == null) backgroundBlur = cam.gameObject.AddComponent<BackgroundBlur>();
        }
        if (backgroundBlur != null) backgroundBlur.Active = game.CurrentPhase == ChestShuffleGame.Phase.Result;

        if (!balanceReady)
        {
            shownBalance = balanceFrom = balanceTarget = lastBalance = game.Balance;
            balanceReady = true;
        }

        float dt = Time.unscaledDeltaTime;

        if (game.Balance != balanceTarget)
        {
            int delta = game.Balance - lastBalance;
            balanceFrom = shownBalance;
            balanceTarget = game.Balance;
            lastBalance = game.Balance;
            balanceAnim = 0f;
            balancePulse = 1f;
            if (delta != 0)
            {
                floatText = (delta > 0 ? "+" : "\u2212") + Mathf.Abs(delta).ToString("N0");
                floatTimer = 1.7f;
            }
        }

        if (balanceAnim < 1f)
        {
            balanceAnim = Mathf.Min(1f, balanceAnim + dt / 0.7f);
            float eased = 1f - Mathf.Pow(1f - balanceAnim, 3f);
            shownBalance = Mathf.Lerp(balanceFrom, balanceTarget, eased);
        }

        balancePulse = Mathf.Max(0f, balancePulse - dt * 2f);
        if (floatTimer > 0f) floatTimer = Mathf.Max(0f, floatTimer - dt);
        flash = Mathf.Max(0f, flash - dt * 1.7f);

        if (!phaseReady || observedPhase != game.CurrentPhase)
        {
            observedPhase = game.CurrentPhase;
            phaseReady = true;
            if (observedPhase == ChestShuffleGame.Phase.Result && game.IsJackpot) flash = 1f;
        }
    }

    void OnGUI()
    {
        if (game == null) game = GetComponent<ChestShuffleGame>();
        if (game == null) return;
        if (cam == null) cam = game.GameCamera;

        EnsureAssets();
        ComputeLayout();

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        GUI.color = Color.white;

        DrawAtmosphere();
        DrawWorldTags();
        DrawHeader();
        if (game.CurrentPhase != ChestShuffleGame.Phase.Result)
        {
            DrawStatus();
            DrawResultBanner();
            DrawDock();
            DrawToast();
        }
        DrawFlash();
        if (game.CurrentPhase == ChestShuffleGame.Phase.Result) DrawResultModal();

        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    public bool PointerOverUi()
    {
        if (scale <= 0f) return false;
        return PointerOverUi(Input.mousePosition);
    }

    public bool PointerOverUi(Vector2 screenPosition)
    {
        if (scale <= 0f) return false;
        Vector2 point = new Vector2(screenPosition.x / scale, (Screen.height - screenPosition.y) / scale);
        return dockRect.Contains(point) || resetRect.Contains(point) || playRect.Contains(point) || resultButtonRect.Contains(point);
    }

    // ---------------------------------------------------------------- layout

    void ComputeLayout()
    {
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        portrait = width < height * 0.95f;

        if (portrait)
        {
            scale = Mathf.Min(width / 460f, height / 900f);
            pad = 16f;
        }
        else
        {
            scale = Mathf.Max(0.78f, Mathf.Min(width / 1440f, height / 900f));
            pad = 28f;
        }
        scale = Mathf.Max(0.15f, scale);
        viewW = width / scale;
        viewH = height / scale;

        if (portrait)
        {
            titleRect = new Rect(pad, 16f, viewW - pad * 2f, 42f);
            subtitleRect = new Rect(pad, 56f, viewW - pad * 2f, 18f);
            balanceRect = new Rect(pad, 80f, viewW - pad * 2f - 82f, 58f);
            resetRect = new Rect(viewW - pad - 72f, 88f, 72f, 42f);
            statusRect = new Rect(pad, balanceRect.yMax + 14f, viewW - pad * 2f, 94f);

            float dockH = 252f;
            dockRect = new Rect(8f, viewH - dockH - 8f, viewW - 16f, dockH);
            float inner = 16f;
            float chipW = (dockRect.width - inner * 2f - 20f) / 3f;
            for (int i = 0; i < chipRects.Length; i++)
            {
                int row = i / 3;
                int column = i % 3;
                chipRects[i] = new Rect(dockRect.x + inner + column * (chipW + 10f), dockRect.y + 48f + row * 56f, chipW, 46f);
            }
            playRect = new Rect(dockRect.x + inner, dockRect.yMax - 72f, dockRect.width - inner * 2f, 56f);
            toastRect = new Rect(pad, dockRect.y - 52f, viewW - pad * 2f, 40f);
        }
        else
        {
            titleRect = new Rect(pad + 4f, 20f, 640f, 50f);
            subtitleRect = new Rect(titleRect.x, titleRect.yMax - 2f, 640f, 18f);
            balanceRect = new Rect(viewW - pad - 260f, 18f, 260f, 66f);
            resetRect = new Rect(balanceRect.x - 76f, 32f, 70f, 38f);
            statusRect = new Rect(viewW * 0.5f - 460f, 104f, 920f, 98f);

            float dockW = Mathf.Min(viewW - 90f, 1140f);
            float dockH = 164f;
            dockRect = new Rect((viewW - dockW) * 0.5f, viewH - dockH - 18f, dockW, dockH);
            float inner = 26f;
            playRect = new Rect(dockRect.xMax - inner - 260f, dockRect.y + 50f, 260f, 70f);

            float chipsX = dockRect.x + inner + 70f;
            float gap = 10f;
            float chipW = Mathf.Min(108f, (playRect.x - 150f - chipsX - gap * 4f) / 5f);
            chipW = Mathf.Max(60f, chipW);
            for (int i = 0; i < chipRects.Length; i++)
                chipRects[i] = new Rect(chipsX + i * (chipW + gap), dockRect.y + 74f, chipW, 46f);

            toastRect = new Rect(viewW * 0.5f - 320f, dockRect.y - 56f, 640f, 40f);
        }
    }

    // ---------------------------------------------------------------- sections

    void DrawAtmosphere()
    {
        GUI.color = Color.white;
        if (vignetteTex != null)
            GUI.DrawTexture(new Rect(0f, 0f, viewW, viewH), vignetteTex, ScaleMode.StretchToFill, true);
        if (topFadeTex != null)
            GUI.DrawTexture(new Rect(0f, 0f, viewW, portrait ? 160f : 190f), topFadeTex, ScaleMode.StretchToFill, true);
        if (bottomFadeTex != null)
            GUI.DrawTexture(new Rect(0f, viewH - 300f, viewW, 300f), bottomFadeTex, ScaleMode.StretchToFill, true);
    }

    void DrawHeader()
    {
        Text(titleRect, "CHEST SHUFFLE", displayFont, portrait ? 28 : 42, Gold, TextAnchor.MiddleLeft, true);
        Text(subtitleRect, "THREE CHESTS  \u00B7  ONE PICK  \u00B7  UP TO 10\u00D7", bodyFont, portrait ? 9 : 11, Dim, TextAnchor.MiddleLeft, false);

        bool pulse = balancePulse > 0.01f;
        Rect pill = balanceRect;
        if (pulse)
        {
            float grow = balancePulse * 0.02f;
            pill = new Rect(pill.x - pill.width * grow * 0.5f, pill.y - pill.height * grow * 0.5f,
                pill.width * (1f + grow), pill.height * (1f + grow));
        }

        Panel(pill, new Color(0.05f, 0.038f, 0.068f, 0.94f), new Color(1f, 0.80f, 0.40f, 0.32f), 18f);

        float coinSize = portrait ? 30f : 36f;
        Rect coin = new Rect(pill.x + 13f, pill.center.y - coinSize * 0.5f, coinSize, coinSize);
        DrawGlow(new Rect(coin.center.x - 30f, coin.center.y - 30f, 60f, 60f), new Color(1f, 0.76f, 0.30f, 0.14f + balancePulse * 0.18f));
        if (coinTex != null) GUI.DrawTexture(coin, coinTex, ScaleMode.ScaleToFit, true);

        float textX = coin.xMax + 10f;
        float textW = pill.xMax - textX - 12f;
        Text(new Rect(textX, pill.y + 8f, textW, 14f), "COINS", bodyFont, portrait ? 8 : 9, Dim, TextAnchor.MiddleLeft, false);
        Text(new Rect(textX, pill.y + (portrait ? 20f : 22f), textW, 34f),
            Mathf.RoundToInt(shownBalance).ToString("N0"), displayFont, portrait ? 20 : 25,
            balancePulse > 0.25f ? GoldBright : Cream, TextAnchor.MiddleLeft, true);

        if (floatTimer > 0f && !string.IsNullOrEmpty(floatText))
        {
            float progress = 1f - floatTimer / 1.7f;
            Color tint = floatText.StartsWith("+") ? Teal : Pink;
            tint.a = Mathf.Clamp01(floatTimer / 0.45f);
            Text(new Rect(pill.x, pill.yMax + 4f - progress * 16f, pill.width, 22f), floatText,
                boldFont, 14, tint, TextAnchor.MiddleRight, true);
        }

        Color resetBorder = resetRect.Contains(Event.current.mousePosition) ? Gold : new Color(1f, 0.8f, 0.4f, 0.18f);
        Panel(resetRect, new Color(0.05f, 0.038f, 0.068f, 0.86f), resetBorder, 12f);
        Text(resetRect, "RESET", bodyFont, 9, resetRect.Contains(Event.current.mousePosition) ? Gold : Dim, TextAnchor.MiddleCenter, false);
        if (game.CurrentPhase != ChestShuffleGame.Phase.Result && GUI.Button(resetRect, GUIContent.none, clickStyle)) game.ResetBank();
    }

    void DrawStatus()
    {
        // Reveal and result copy lives above the dock, clear of the multiplier plates.
        if (game.CurrentPhase == ChestShuffleGame.Phase.Reveal || game.CurrentPhase == ChestShuffleGame.Phase.Result) return;

        string headline;
        string subtitle;
        Color tone;
        int size = portrait ? 25 : 34;

        bool outOfCoins = game.NeedsTopUp;
        switch (game.CurrentPhase)
        {
            case ChestShuffleGame.Phase.Shuffling:
                headline = "SHUFFLING";
                subtitle = "Three chests, three hidden multipliers. Follow yours.";
                tone = Gold;
                break;
            case ChestShuffleGame.Phase.Picking:
                headline = "PICK A CHEST";
                subtitle = "Click any chest \u2014 or press 1, 2 or 3.";
                tone = Teal;
                break;
            case ChestShuffleGame.Phase.Reveal:
                headline = "OPENING LANE " + game.ChosenLane;
                subtitle = "Revealing the hidden multiplier\u2026";
                tone = Gold;
                break;
            default:
                if (outOfCoins)
                {
                    headline = "OUT OF COINS";
                    subtitle = "Press TOP UP to receive " + ChestShuffleGame.TopUpAmount + " coins.";
                    tone = Pink;
                }
                else
                {
                    headline = "PLACE YOUR BET";
                    subtitle = "Pick a stake, press PLAY, then choose one of three chests.";
                    tone = Gold;
                }
                break;
        }

        float headlineH = size * 1.7f;
        float headlineY = statusRect.y;
        if (game.CurrentPhase == ChestShuffleGame.Phase.Picking)
        {
            // Keep the phase title above the chest badges. Their numbered prompts
            // provide the picking instructions, so a second centered subtitle here
            // only collides with the middle badge on wide screens.
            headlineY = Mathf.Max(66f, statusRect.y - 34f);
            Text(new Rect(statusRect.x, headlineY, statusRect.width, headlineH), headline, displayFont, size, tone,
                TextAnchor.MiddleCenter, true, true);
            return;
        }

        Text(new Rect(statusRect.x, headlineY, statusRect.width, headlineH), headline, displayFont, size, tone,
            TextAnchor.MiddleCenter, true, true);
        Text(new Rect(statusRect.x + 16f, headlineY + headlineH - 2f, statusRect.width - 32f, 30f), subtitle,
            bodyFont, portrait ? 11 : 14, new Color(Cream.r, Cream.g, Cream.b, 0.82f), TextAnchor.MiddleCenter, true, true);
    }

    void DrawResultBanner()
    {
        bool reveal = game.CurrentPhase == ChestShuffleGame.Phase.Reveal;
        if (!reveal) return;

        string headline;
        string subtitle;
        Color tone;
        int size = portrait ? 22 : 28;

        if (reveal)
        {
            headline = "OPENING LANE " + game.ChosenLane;
            subtitle = "Revealing the hidden multiplier\u2026";
            tone = Gold;
        }
        else return;

        float width = Mathf.Min(portrait ? viewW - 40f : 880f, viewW - 40f);
        float headlineH = size * 1.7f;
        float height = headlineH + 30f;
        float blockY = toastRect.y - 10f - height;
        Rect block = new Rect(viewW * 0.5f - width * 0.5f, blockY, width, height);

        Panel(block, new Color(0.042f, 0.03f, 0.058f, 0.92f),
            new Color(tone.r, tone.g, tone.b, 0.4f), 16f);
        Text(new Rect(block.x + 12f, block.y + 2f, block.width - 24f, headlineH), headline, displayFont, size, tone,
            TextAnchor.MiddleCenter, true, true);
        Text(new Rect(block.x + 14f, block.y + headlineH - 2f, block.width - 28f, 22f), subtitle,
            bodyFont, portrait ? 10 : 12, new Color(Cream.r, Cream.g, Cream.b, 0.86f), TextAnchor.MiddleCenter, true, true);
    }

    void DrawResultModal()
    {
        float veilAlpha = 0.72f;
        GUI.color = new Color(0.025f, 0.018f, 0.04f, veilAlpha);
        GUI.DrawTexture(new Rect(0f, 0f, viewW, viewH), Texture2D.whiteTexture, ScaleMode.StretchToFill);
        GUI.color = Color.white;

        float panelWidth = Mathf.Min(portrait ? viewW - 28f : 760f, viewW - 28f);
        float panelHeight = portrait ? 640f : 560f;
        if (viewH < panelHeight + 24f) panelHeight = viewH - 24f;
        Rect panel = new Rect(viewW * 0.5f - panelWidth * 0.5f, viewH * 0.5f - panelHeight * 0.5f,
            panelWidth, panelHeight);
        Panel(panel, new Color(0.045f, 0.032f, 0.067f, 0.98f), new Color(1f, 0.82f, 0.48f, 0.72f), 24f);

        float inset = portrait ? 16f : 30f;
        Text(new Rect(panel.x + inset, panel.y + 20f, panel.width - inset * 2f, 22f),
            "ROUND COMPLETE", bodyFont, portrait ? 13 : 15, Dim, TextAnchor.MiddleCenter, false);
        Text(new Rect(panel.x + inset, panel.y + 42f, panel.width - inset * 2f, 50f),
            "YOUR PRIZE", displayFont, portrait ? 34 : 40, GoldBright, TextAnchor.MiddleCenter, true);

        float reelWidth = portrait ? 222f : 252f;
        float reelHeight = portrait ? 218f : 224f;
        Rect reel = new Rect(panel.center.x - reelWidth * 0.5f, panel.y + 96f, reelWidth, reelHeight);
        Panel(reel, new Color(0.10f, 0.065f, 0.02f, 1f), new Color(1f, 0.79f, 0.36f, 0.95f), 20f);
        Text(new Rect(reel.x + 8f, reel.y + 4f, reel.width - 16f, 24f), "MULTIPLIER", bodyFont, 11, GoldBright,
            TextAnchor.MiddleCenter, false);
        Rect track = new Rect(reel.x + 12f, reel.y + 32f, reel.width - 24f, reel.height - 44f);
        Panel(track, new Color(0.018f, 0.012f, 0.032f, 1f), new Color(1f, 0.83f, 0.48f, 0.28f), 12f);

        float progress = game.ResultRollProgress;
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        float targetMultiplier = game.MultiplierAt(game.ChosenChest);
        float[] values = MultiplierTable.Values;
        resultReelValues.Clear();
        int targetValueIndex = -1;
        for (int i = 0; i < values.Length; i++)
        {
            if (Mathf.Approximately(values[i], targetMultiplier))
            {
                targetValueIndex = resultReelValues.Count;
                resultReelValues.Add(values[i]);
                continue;
            }

            if (targetValueIndex < 0 && targetMultiplier < values[i])
            {
                targetValueIndex = resultReelValues.Count;
                resultReelValues.Add(targetMultiplier);
            }
            resultReelValues.Add(values[i]);
        }
        if (targetValueIndex < 0)
        {
            targetValueIndex = resultReelValues.Count;
            resultReelValues.Add(targetMultiplier);
        }

        const int rollSteps = 38;
        float reelPosition = eased * rollSteps;
        int step = Mathf.Clamp(Mathf.FloorToInt(reelPosition), 0, rollSteps);
        int valueIndex = (targetValueIndex - (rollSteps - step) % resultReelValues.Count + resultReelValues.Count) % resultReelValues.Count;
        float rowHeight = 48f;
        float rowSlide = progress >= 1f ? 0f : (reelPosition - step) * rowHeight;
        GUI.BeginGroup(track);
        float centerY = track.height * 0.5f;
        Rect winLine = new Rect(2f, centerY - rowHeight * 0.5f + 1f, track.width - 4f, rowHeight - 2f);
        DrawRounded(winLine, new Color(1f, 0.74f, 0.22f, 0.18f), 9f);
        DrawRounded(winLine, new Color(1f, 0.86f, 0.50f, 0.62f), 9f, true);

        for (int row = -2; row <= 2; row++)
        {
            int rowValueIndex = (valueIndex + row + resultReelValues.Count) % resultReelValues.Count;

            float y = centerY + row * rowHeight - rowSlide - rowHeight * 0.5f;
            float distanceFade = Mathf.Clamp01(1f - Mathf.Abs(row - rowSlide / rowHeight) * 0.36f);
            Color rowColor = row == 0 ? GoldBright : new Color(Cream.r, Cream.g, Cream.b, 0.78f * distanceFade);
            int rowFontSize = row == 0 ? (portrait ? 36 : 40) : 24;
            Text(new Rect(8f, y, track.width - 16f, rowHeight),
                MultiplierTable.Format(resultReelValues[rowValueIndex]),
                displayFont, rowFontSize, rowColor, TextAnchor.MiddleCenter, row == 0);
        }

        Rect topShade = new Rect(2f, 2f, track.width - 4f, 24f);
        Rect bottomShade = new Rect(2f, track.height - 26f, track.width - 4f, 24f);
        DrawRounded(topShade, new Color(0.012f, 0.008f, 0.022f, 0.62f), 8f);
        DrawRounded(bottomShade, new Color(0.012f, 0.008f, 0.022f, 0.62f), 8f);
        GUI.EndGroup();

        Color resultTone = MultiplierTable.ColorFor(game.MultiplierAt(game.ChosenChest));
        float resultAlpha = Mathf.Clamp01((progress - 0.78f) / 0.22f);
        resultTone.a = resultAlpha;
        float verdictY = reel.yMax + 4f;
        Text(new Rect(panel.x + inset, verdictY, panel.width - inset * 2f, 40f),
            MultiplierTable.Verdict(game.MultiplierAt(game.ChosenChest)), displayFont, portrait ? 25 : 29,
            resultTone, TextAnchor.MiddleCenter, true);

        Color detailColor = new Color(Cream.r, Cream.g, Cream.b, resultAlpha);
        float laneY = verdictY + 40f;
        Text(new Rect(panel.x + inset, laneY, panel.width - inset * 2f, 30f),
            "Lane " + game.ChosenLane + "  \u00B7  Stake " + MultiplierTable.Coins(game.Stake), bodyFont,
            portrait ? 15 : 17, detailColor, TextAnchor.MiddleCenter, true);

        float detailY = laneY + 40f;
        float cellW = (panel.width - inset * 2f) / 3f;
        ResultValueCell(new Rect(panel.x + inset, detailY, cellW, 48f), "PAYOUT", MultiplierTable.Coins(game.Payout), GoldBright, resultAlpha);
        ResultValueCell(new Rect(panel.x + inset + cellW, detailY, cellW, 48f), "NET", (game.Net >= 0 ? "+" : "\u2212") + MultiplierTable.Coins(Mathf.Abs(game.Net)), game.Net >= 0 ? Teal : Pink, resultAlpha);
        ResultValueCell(new Rect(panel.x + inset + cellW * 2f, detailY, cellW, 48f), "BALANCE", MultiplierTable.Coins(game.Balance), Cream, resultAlpha);

        resultButtonRect = new Rect(panel.x + inset, panel.yMax - 78f, panel.width - inset * 2f, 64f);
        if (game.ResultReady)
        {
            bool hover = resultButtonRect.Contains(Event.current.mousePosition);
            DrawRounded(resultButtonRect, hover ? new Color(1f, 0.88f, 0.55f) : Gold, 16f);
            DrawRounded(resultButtonRect, GoldBright, 16f, true);
            Text(resultButtonRect, "PLAY AGAIN", displayFont, portrait ? 22 : 25, Ink, TextAnchor.MiddleCenter, false);
            if (GUI.Button(resultButtonRect, GUIContent.none, clickStyle)) game.ReturnToBetting();
        }
        else
        {
            resultButtonRect = new Rect();
            Text(new Rect(panel.x + inset, panel.yMax - 74f, panel.width - inset * 2f, 44f),
                "COUNTING YOUR PRIZE\u2026", bodyFont, portrait ? 14 : 16, Dim, TextAnchor.MiddleCenter, false);
        }
    }

    void ResultValueCell(Rect rect, string label, string value, Color tone, float alpha)
    {
        tone.a = alpha;
        Color labelColor = new Color(Dim.r, Dim.g, Dim.b, alpha);
        Text(new Rect(rect.x, rect.y, rect.width, 18f), label, bodyFont, 11, labelColor, TextAnchor.MiddleCenter, false);
        Text(new Rect(rect.x, rect.y + 18f, rect.width, 30f), value, boldFont, portrait ? 18 : 20, tone, TextAnchor.MiddleCenter, true);
    }

    void DrawDock()
    {
        Panel(dockRect, new Color(0.042f, 0.03f, 0.058f, 0.95f), new Color(1f, 0.79f, 0.38f, 0.28f), 22f);

        if (portrait) DrawDockPortrait();
        else DrawDockLandscape();
    }

    void DrawDockLandscape()
    {
        Text(new Rect(dockRect.x + 26f, dockRect.y + 26f, 200f, 16f), "STAKE", bodyFont, 10, Dim, TextAnchor.MiddleLeft, false);
        Text(new Rect(dockRect.x + 26f, dockRect.y + 42f, 220f, 20f), "CHOOSE YOUR BET", bodyFont, 8,
            new Color(Dim.r, Dim.g, Dim.b, 0.65f), TextAnchor.MiddleLeft, false);

        DrawStakeChips();

        Rect info = new Rect(playRect.x - 150f, dockRect.y + 76f, 128f, 46f);
        Text(new Rect(info.x, info.y + 1f, info.width, 14f), "MAX WIN", bodyFont, 8, Dim, TextAnchor.MiddleRight, false);
        Text(new Rect(info.x, info.y + 16f, info.width, 24f), "10\u00D7 = " + MultiplierTable.Coins(game.Bet * 10),
            bodyFont, 12, Gold, TextAnchor.MiddleRight, true);

        DrawPlayButton();
    }

    void DrawDockPortrait()
    {
        Text(new Rect(dockRect.x + 16f, dockRect.y + 18f, 220f, 18f), "YOUR STAKE", bodyFont, 9, Dim, TextAnchor.MiddleLeft, false);
        Text(new Rect(dockRect.xMax - 200f, dockRect.y + 18f, 184f, 18f),
            "MAX 10\u00D7 = " + MultiplierTable.Coins(game.Bet * 10), bodyFont, 9, Gold, TextAnchor.MiddleRight, false);

        DrawStakeChips();
        DrawPlayButton();
    }

    void DrawStakeChips()
    {
        for (int i = 0; i < ChestShuffleGame.StakeOptions.Length && i < chipRects.Length; i++)
        {
            int value = ChestShuffleGame.StakeOptions[i];
            Rect rect = chipRects[i];
            bool activeRound = game.RoundActive;
            bool selected = (activeRound ? game.Stake : game.Bet) == value;
            bool affordable = selected && activeRound || game.Balance >= value;
            bool enabled = !activeRound && affordable;
            bool hover = enabled && rect.Contains(Event.current.mousePosition);

            Color fill = selected ? new Color(1f, 0.79f, 0.36f, 1f) : new Color(0.10f, 0.076f, 0.135f, 0.96f);
            Color border = selected ? GoldBright : hover ? Gold : new Color(1f, 1f, 1f, 0.10f);
            if (!enabled && !selected)
            {
                fill.a *= 0.55f;
                border.a *= 0.5f;
            }

            Panel(rect, fill, border, 12f);

            Color content = selected ? Ink : enabled ? Cream : new Color(Dim.r, Dim.g, Dim.b, 0.45f);
            float coinSize = 19f;
            Rect coin = new Rect(rect.x + 10f, rect.center.y - coinSize * 0.5f, coinSize, coinSize);
            if (coinTex != null) GUI.DrawTexture(coin, coinTex, ScaleMode.ScaleToFit, true, 0f, content, Vector4.zero, Vector4.zero);
            Text(new Rect(rect.x + 32f, rect.y, rect.width - 38f, rect.height), value.ToString("N0"),
                boldFont, portrait ? 14 : 16, content, TextAnchor.MiddleLeft, false);

            if (enabled && GUI.Button(rect, GUIContent.none, clickStyle)) game.SetBet(value);
        }
    }

    void DrawPlayButton()
    {
        bool topUp = game.NeedsTopUp;
        bool canPress = game.CanStart || topUp;

        string label;
        if (topUp) label = "TOP UP +" + ChestShuffleGame.TopUpAmount;
        else if (game.CurrentPhase == ChestShuffleGame.Phase.Result) label = "PLAY AGAIN";
        else if (game.CurrentPhase == ChestShuffleGame.Phase.Idle) label = "PLAY";
        else if (game.CurrentPhase == ChestShuffleGame.Phase.Shuffling) label = "SHUFFLING";
        else if (game.CurrentPhase == ChestShuffleGame.Phase.Picking) label = "PICK A CHEST";
        else label = "OPENING";

        bool hover = canPress && playRect.Contains(Event.current.mousePosition);
        Color fill = canPress
            ? (hover ? new Color(1f, 0.87f, 0.50f) : Gold)
            : new Color(0.16f, 0.13f, 0.19f, 1f);
        Color border = canPress ? GoldBright : new Color(1f, 1f, 1f, 0.07f);

        if (canPress)
            DrawGlow(new Rect(playRect.x - 20f, playRect.y - 14f, playRect.width + 40f, playRect.height + 34f),
                new Color(1f, 0.75f, 0.26f, hover ? 0.26f : 0.15f));

        DrawRounded(playRect, fill, 16f);
        DrawRounded(playRect, border, 16f, true);
        DrawRounded(new Rect(playRect.x + 7f, playRect.y + 4f, playRect.width - 14f, playRect.height * 0.4f),
            new Color(1f, 1f, 1f, canPress ? 0.20f : 0.04f), 11f);

        Text(playRect, label, displayFont, canPress ? (portrait ? 17 : 21) : 14,
            canPress ? Ink : Dim, TextAnchor.MiddleCenter, false);

        if (canPress)
        {
            Text(new Rect(playRect.x, playRect.yMax + 3f, playRect.width, 14f), "PRESS ENTER", bodyFont, 8,
                new Color(Dim.r, Dim.g, Dim.b, 0.8f), TextAnchor.MiddleCenter, false);
            if (GUI.Button(playRect, GUIContent.none, clickStyle))
            {
                if (topUp) game.TopUp();
                else game.StartRound();
            }
        }
    }

    void DrawWorldTags()
    {
        if (cam == null) return;

        for (int chest = 0; chest < MultiplierTable.ChestCount; chest++)
        {
            Vector3 screen = cam.WorldToScreenPoint(game.ChestAnchor(chest));
            if (screen.z <= 0f) continue;

            Vector2 point = new Vector2(screen.x / scale, (Screen.height - screen.y) / scale);
            if (point.x < -160f || point.x > viewW + 160f || point.y < -160f || point.y > viewH + 160f) continue;

            if (game.CanPick)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f + chest) * 0.03f;
                float size = (portrait ? 42f : 50f) * pulse;
                Rect badge = new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size);
                DrawGlow(new Rect(badge.center.x - size, badge.center.y - size, size * 2f, size * 2f),
                    new Color(1f, 0.76f, 0.30f, 0.24f));
                Panel(badge, new Color(0.055f, 0.042f, 0.08f, 0.95f), Gold, 24f);
                Text(badge, (game.SlotOf(chest) + 1).ToString(), displayFont, portrait ? 18 : 22, GoldBright,
                    TextAnchor.MiddleCenter, true);
                Text(new Rect(badge.x - 40f, badge.yMax + 2f, badge.width + 80f, 14f),
                    "CLICK OR PRESS " + (game.SlotOf(chest) + 1), bodyFont, 8, Dim, TextAnchor.MiddleCenter, false);
            }
            else if (game.IsRevealed(chest))
            {
                bool chosen = chest == game.ChosenChest;
                float multiplier = game.MultiplierAt(chest);
                Color accent = MultiplierTable.ColorFor(multiplier);
                float width = chosen ? (portrait ? 100f : 124f) : (portrait ? 84f : 100f);
                float height = chosen ? (portrait ? 64f : 76f) : (portrait ? 52f : 60f);
                Rect plate = new Rect(point.x - width * 0.5f, point.y - height, width, height);

                DrawGlow(new Rect(plate.center.x - width, plate.center.y - height, width * 2f, height * 2f),
                    new Color(accent.r, accent.g, accent.b, chosen ? 0.30f : 0.14f));
                Panel(plate, new Color(0.048f, 0.034f, 0.07f, 0.95f),
                    chosen ? accent : new Color(1f, 1f, 1f, 0.18f), 14f);
                Text(new Rect(plate.x, plate.y + 3f, plate.width, height * 0.62f),
                    MultiplierTable.Format(multiplier), displayFont,
                    chosen ? (portrait ? 22 : 30) : (portrait ? 17 : 22), accent, TextAnchor.MiddleCenter, true);
                Text(new Rect(plate.x, plate.yMax - 17f, plate.width, 13f),
                    chosen ? "YOUR PICK" : "CHEST " + (game.SlotOf(chest) + 1),
                    bodyFont, 8, chosen ? Cream : Dim, TextAnchor.MiddleCenter, false);
            }
        }
    }

    void DrawToast()
    {
        if (string.IsNullOrEmpty(game.Toast) || game.ToastAge > 5f) return;
        if (game.CurrentPhase == ChestShuffleGame.Phase.Reveal || game.CurrentPhase == ChestShuffleGame.Phase.Result) return;
        float alpha = Mathf.Clamp01(game.ToastAge / 0.18f) * Mathf.Clamp01((5f - game.ToastAge) / 1.2f);
        if (alpha <= 0f) return;

        Panel(toastRect, new Color(0.05f, 0.038f, 0.07f, 0.9f * alpha),
            new Color(1f, 0.8f, 0.4f, 0.28f * alpha), 12f);
        Text(toastRect, game.Toast, bodyFont, portrait ? 10 : 12,
            new Color(Cream.r, Cream.g, Cream.b, alpha), TextAnchor.MiddleCenter, false, true);
    }

    void DrawFlash()
    {
        if (flash <= 0f) return;
        GUI.color = new Color(1f, 0.84f, 0.44f, flash * 0.24f);
        GUI.DrawTexture(new Rect(0f, 0f, viewW, viewH), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // ---------------------------------------------------------------- drawing helpers

    void Panel(Rect rect, Color fill, Color border, float radius)
    {
        DrawRounded(rect, fill, radius);
        DrawRounded(rect, border, radius, true);
    }

    void DrawRounded(Rect rect, Color color, float radius, bool stroke = false)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        GUIStyle style = stroke ? roundStrokeStyle : roundFillStyle;
        if (style == null) return;
        Color previous = GUI.color;
        GUI.color = color;
        GUI.Box(rect, GUIContent.none, style);
        GUI.color = previous;
    }

    void DrawGlow(Rect rect, Color color)
    {
        if (glowTex == null) return;
        GUI.DrawTexture(rect, glowTex, ScaleMode.StretchToFill, true, 0f, color, Vector4.zero, Vector4.zero);
    }

    void Text(Rect rect, string content, Font font, int size, Color color, TextAnchor anchor,
        bool shadow = true, bool wrap = false)
    {
        if (string.IsNullOrEmpty(content)) return;
        float readability = size <= 10 ? 1.65f : size <= 14 ? 1.4f : size <= 20 ? 1.22f : 1.12f;
        size = Mathf.RoundToInt(size * readability);
        GUIStyle style = TextStyle(font, size, anchor, wrap);
        if (shadow)
        {
            style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.6f);
            GUI.Label(new Rect(rect.x + 1.5f, rect.y + 2.5f, rect.width, rect.height), content, style);
        }
        style.normal.textColor = color;
        GUI.Label(rect, content, style);
    }

    GUIStyle TextStyle(Font font, int size, TextAnchor anchor, bool wrap)
    {
        Font resolved = font != null ? font : GUI.skin.font;
        string key = (resolved != null ? resolved.name : "default") + "|" + size + "|" + anchor + "|" + wrap;
        GUIStyle style;
        if (textStyles.TryGetValue(key, out style)) return style;

        style = new GUIStyle(GUIStyle.none);
        style.font = resolved;
        style.fontSize = Mathf.Max(8, size);
        style.alignment = anchor;
        style.wordWrap = wrap;
        style.clipping = TextClipping.Overflow;
        style.richText = false;
        textStyles[key] = style;
        return style;
    }

    // ---------------------------------------------------------------- assets

    void EnsureAssets()
    {
        if (roundTex != null) return;

        displayFont = Resources.Load<Font>("Fonts/Barlow-Bold");
        bodyFont = Resources.Load<Font>("Fonts/Barlow-SemiBold");
        boldFont = Resources.Load<Font>("Fonts/Barlow-Bold");
        if (bodyFont == null) bodyFont = displayFont;
        if (boldFont == null) boldFont = bodyFont;

        roundTex = Rounded(64, 16, false, 0);
        roundStrokeTex = Rounded(64, 16, true, 2);
        roundFillStyle = RoundStyle(roundTex, 18);
        roundStrokeStyle = RoundStyle(roundStrokeTex, 18);
        coinTex = CoinTexture(96);
        glowTex = GlowTexture(128);
        vignetteTex = VignetteTexture(128);
        topFadeTex = FadeTexture(true);
        bottomFadeTex = FadeTexture(false);
        clickStyle = new GUIStyle { name = "InvisibleClick" };
    }

    static GUIStyle RoundStyle(Texture2D texture, int border)
    {
        var style = new GUIStyle(GUIStyle.none);
        style.normal.background = texture;
        style.border = new RectOffset(border, border, border, border);
        style.stretchWidth = true;
        style.stretchHeight = true;
        return style;
    }

    static Texture2D Rounded(int size, int radius, bool stroke, int strokeWidth)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = stroke ? "HudRoundedStroke" : "HudRoundedFill";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;

        var pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - half) - (half - radius);
                float dy = Mathf.Abs(y - half) - (half - radius);
                float corner = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f))
                               + Mathf.Min(Mathf.Max(dx, dy), 0f);
                float distance = corner - radius;

                float alpha;
                if (stroke)
                    alpha = Mathf.Clamp01(distance + strokeWidth + 0.5f) * Mathf.Clamp01(0.5f - distance);
                else
                    alpha = Mathf.Clamp01(0.5f - distance);

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static Texture2D GlowTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "HudGlow";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;

        var pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static Texture2D VignetteTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "HudVignette";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;

        var pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 0.85f;
                float alpha = Mathf.Clamp01((distance - 0.42f) / 0.62f) * 0.72f;
                pixels[y * size + x] = new Color32(4, 2, 8, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static Texture2D FadeTexture(bool fromTop)
    {
        const int height = 128;
        var texture = new Texture2D(4, height, TextureFormat.RGBA32, false);
        texture.name = fromTop ? "HudTopFade" : "HudBottomFade";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;

        var pixels = new Color32[4 * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            float alpha = (fromTop ? v : 1f - v) * 0.82f;
            var color = new Color32(5, 3, 9, (byte)Mathf.RoundToInt(alpha * 255f));
            for (int x = 0; x < 4; x++) pixels[y * 4 + x] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static Texture2D CoinTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "HudCoin";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;

        var pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        Color rim = new Color(0.62f, 0.40f, 0.10f);
        Color light = new Color(1f, 0.90f, 0.60f);
        Color deep = new Color(0.94f, 0.67f, 0.20f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - half) / half;
                float ny = (y - half) / half;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01((1f - radius) * half * 0.9f);
                if (alpha <= 0f)
                {
                    pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                Color color;
                if (radius > 0.82f) color = rim;
                else if (radius > 0.70f) color = Color.Lerp(light, rim, (radius - 0.70f) / 0.12f);
                else color = Color.Lerp(deep, light, Mathf.Clamp01((y / (float)size) * 1.1f));

                float diamond = Mathf.Abs(nx) * 0.85f + Mathf.Abs(ny * 1.25f);
                if (diamond < 0.24f) color = Color.Lerp(color, light, 0.85f);

                color.a = alpha;
                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}

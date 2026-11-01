using System;
using System.IO;
using MineGrid;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static partial class MineGridSetup
{
    private const string Root = "Assets/MineGrid";
    private static UITheme _theme;
    private static Sprite _rounded;
    private static readonly Color _ink = new Color(.045f, .06f, .14f);
    private static readonly Color _panel = new Color(.105f, .14f, .29f);
    private static readonly Color _cyan = new Color(.35f, .91f, 1);
    private static readonly Color _gold = new Color(1, .81f, .33f);
    private static readonly Color _green = new Color(.13f, .76f, .49f);
    [MenuItem("MineGrid/Generate Complete Game")]
    public static void Generate()
    {
        if (EditorApplication.isPlaying)
        {
            throw new Exception("Leave Play Mode before generating.");
        }

        Directory.CreateDirectory(Root + "/Art");
        Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();
        PrepareTheme();
        PrepareCatalog();
        var config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/GameConfig.asset");
        if (!config)
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, Root + "/GameConfig.asset");
        }

        if (config.wheelCoins.Length != 6)
        {
            throw new Exception("The authored wheel requires six reward segments.");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraGo = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = _ink;
        camera.orthographic = true;
        var canvasGo = new GameObject("MineGrid Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight = .5f;
        var bg = Image("Midnight Cave", canvasGo.transform, 0, 0, 1, 1, Color.white);
        bg.sprite = _theme.backdrop;
        bg.raycastTarget = false;
        var random = new System.Random(39);
        for (int i = 0; i < 24; i++)
        {
            float x = (float)random.NextDouble(), y = (float)random.NextDouble();
            var star = Image("Distant Spark " + i, canvasGo.transform, x, y, x, y, new Color(.4f, .9f, 1, .10f));
            star.rectTransform.sizeDelta = Vector2.one * (3 + i % 4 * 2);
            star.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            star.raycastTarget = false;
        }

        var safe = Rect("Device Safe Area", canvasGo.transform, 0, 0, 1, 1);
        safe.gameObject.AddComponent<SafeArea>();
        var fit = Rect("Portrait Layout", safe, 0, 0, 1, 1);
        var fitter = fit.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 720f / 1280;
        var ui = canvasGo.AddComponent<GameUI>();
        Set(ui, "popupPresenter", canvasGo.AddComponent<PopupPresenter>());
        Set(ui, "navigator", canvasGo.AddComponent<ScreenNavigator>());
        Set(ui, "walletView", canvasGo.AddComponent<WalletView>());
        Set(ui, "progressionView", canvasGo.AddComponent<ProgressionView>());
        Set(ui, "theme", _theme);
        Set(ui, "config", config);
        var audio = canvasGo.AddComponent<AudioService>();
        var source = canvasGo.AddComponent<AudioSource>();
        source.playOnAwake = false;
        Set(audio, "source", source);
        Set(ui, "audioService", audio);
        BuildEventSystem();
        Set(ui, "compactBrand", Icon("Compact Logo", fit, .055f, .925f, .39f, .987f, _theme.logo).gameObject);
        var wallet = Card("Wallet", fit, .59f, .931f, .95f, .98f, _panel);
        Icon("Wallet Coin", wallet, .025f, .08f, .24f, .92f, _theme.coin);
        var balance = Text("Coin Balance", wallet, .25f, .12f, .92f, .88f, "0", 29, TextAlignmentOptions.Right, true);
        balance.color = _gold;
        Set(ui, "balance", balance);
        Text("Footer", fit, .1f, .006f, .9f, .035f, "A little risk. A lot of sparkle.", 17);
        var title = BuildTitle(fit, ui);
        var homeScreen = BuildHome(fit, ui);
        var home = homeScreen.Page;
        var hud = homeScreen.Hud;
        var gameplayScreen = BuildGameplay(fit, ui);
        var game = gameplayScreen.Page;
        var start = gameplayScreen.Start;
        var wheelPage = BuildWheel(config, fit, ui);
        var progressionPage = BuildProgression(config, fit, ui);
        var shopPage = BuildShop(fit, ui);
        var popupScreen = BuildPopup(fit, ui);
        var popup = popupScreen.Overlay;
        var panel = popupScreen.Panel;
        var particles = popupScreen.Particles;
        var coinFx = canvasGo.AddComponent<CoinFeedback>();
        Set(coinFx, "wallet", wallet);
        Set(coinFx, "layer", particles);
        Set(coinFx, "coin", _theme.coin);
        Set(ui, "coinFeedback", coinFx);
        foreach (var page in new[]
        {
            title,
            home,
            game,
            wheelPage,
            progressionPage
        }

        )
        {
            page.AddComponent<CanvasGroup>();
        }

        shopPage.SetActive(false);
        home.SetActive(false);
        game.SetActive(false);
        wheelPage.SetActive(false);
        progressionPage.SetActive(false);
        popup.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(start.gameObject, Root + "/Prefabs/PrimaryButton.prefab");
        PrefabUtility.SaveAsPrefabAsset(hud.gameObject, Root + "/Prefabs/ProgressHUD.prefab");
        PrefabUtility.SaveAsPrefabAsset(panel.gameObject, Root + "/Prefabs/Popup.prefab");
        Set(ui, "config", AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/GameConfig.asset"));
        Set(ui, "theme", AssetDatabase.LoadAssetAtPath<UITheme>(Root + "/UITheme.asset"));
        var refs = new SerializedObject(ui).GetIterator();
        while (refs.NextVisible(true))
        {
            if (refs.propertyType == SerializedPropertyType.ObjectReference && refs.objectReferenceValue == null)
            {
                throw new Exception("Missing UI reference: " + refs.propertyPath);
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MineGrid.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MineGrid.unity", true)
        };
        Verify();
        Debug.Log("MineGrid Sprint 3: polished scene and reusable assets generated.");
    }

    private static void Link(Button b, UnityAction action) => UnityEventTools.AddPersistentListener(b.onClick, action);
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        (so.FindProperty(field) ?? so.FindProperty("_" + field)).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjects(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field) ?? so.FindProperty("_" + field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("MineGrid/Verify Gameplay Rules")]
    public static void Verify() => MineGridVerification.Run();
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Screen = UnityEngine.Device.Screen;

namespace HydraulicPress
{
    public sealed class UIManager : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Sprite pressIcon, walletIcon, settingsIcon, replayIcon, brokenIcon, coinIcon, checkIcon, pauseIcon, playIcon, homeIcon;
        public Sprite rounded;
        private readonly Color ink = Hex("172C37"), muted = Hex("758087"), copper = Hex("E7A15A"), mint = Hex("BFE1C6"), paper = Hex("F3F0E9");
        private GameManager game;
        private Canvas canvas;
        private UnityEngine.UI.CanvasScaler scaler;
        private RectTransform safe, header, multiplierRoot, actions, objectLabel, statusRoot;
        private TextMeshProUGUI multiplierText, bankText, progressText, nameText, rewardText, statusText;
        private UnityEngine.UI.Button crush, collect, settings;
        private UnityEngine.UI.Image[] dots;
        private GameObject result, settingsPanel;
        private TextMeshProUGUI resultTitle, resultMultiplier, resultPoints;
        private UnityEngine.UI.Image resultIcon;
        private TextMeshProUGUI soundLabel, musicLabel, shakeLabel, qualityLabel, statsText;
        private RectTransform crushRect, collectRect;
        private GameObject navigationPanel;
        private RectTransform navigationCard;
        private TextMeshProUGUI navigationTitle, navigationLabel;
        private UnityEngine.UI.Button navigationPlay, navigationSettings, navigationHome;
        private RectTransform settingsCard, soundRow, musicRow;
        private UnityEngine.UI.Button shakeButton, qualityButton;
        private TextMeshProUGUI settingsTitle, rulesText;
        private UnityEngine.UI.Button settingsClose;
        private float pop;
        private int lastWidth, lastHeight;
        private Rect lastSafeArea;
        public bool ResultVisible => result != null && result.activeSelf;
        public int LayoutVersion { get; private set; }
        [Serializable] public sealed class ButtonBounds { public string name; public float x,y,width,height; public bool interactable; }
        private readonly Vector3[] boundsCorners=new Vector3[4];
        public ButtonBounds[] GetButtonBounds()
        {
            Canvas.ForceUpdateCanvases();
            var buttons=GetComponentsInChildren<UnityEngine.UI.Button>(false);var result=new ButtonBounds[buttons.Length];
            for(int i=0;i<buttons.Length;i++)
            {
                var rect=buttons[i].GetComponent<RectTransform>();rect.GetWorldCorners(boundsCorners);
                Vector2 min=RectTransformUtility.WorldToScreenPoint(null,boundsCorners[0]),max=RectTransformUtility.WorldToScreenPoint(null,boundsCorners[2]);
                result[i]=new ButtonBounds{name=buttons[i].name,x=(min.x+max.x)*.5f/Screen.width,y=1-(min.y+max.y)*.5f/Screen.height,width=(max.x-min.x)/Screen.width,height=(max.y-min.y)/Screen.height,interactable=buttons[i].interactable};
            }
            return result;
        }

        public void Initialize(GameManager owner)
        {
            game = owner;
            canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            safe = Rect("Safe area", transform); Fill(safe);
            header = Rect("Header", safe); header.sizeDelta = new Vector2(632, 62);
            Icon(header, pressIcon, new Vector2(-289, 0), 42, ink);
            var brand = Text(header, "HYDRAULIC\nPRESS", 17, ink, new Vector2(-211, 0), new Vector2(122, 58)); brand.alignment = TextAlignmentOptions.Left; brand.characterSpacing = 3; brand.fontStyle = FontStyles.Bold;
            Icon(header, coinIcon, new Vector2(115, 0), 28, ink);
            bankText = Text(header, "0", 25, ink, new Vector2(184, 0), new Vector2(110, 50)); bankText.alignment = TextAlignmentOptions.Left;
            settings = IconButton(header, pauseIcon, new Vector2(282, 0), new Vector2(56, 56), new Color(1, 1, 1, .55f), ink, game.PauseGame);
            settings.gameObject.name = "PauseButton";

            multiplierRoot = Rect("Multiplier", safe); multiplierRoot.sizeDelta = new Vector2(420, 190);
            multiplierText = Text(multiplierRoot, "1.00<size=48>x</size>", 100, ink, new Vector2(0, 28), new Vector2(440, 120)); multiplierText.fontStyle = FontStyles.Bold;
            progressText = Text(multiplierRoot, "0 / 20", 20, muted, new Vector2(0, -43), new Vector2(300, 35)); progressText.characterSpacing = 4;
            dots = new UnityEngine.UI.Image[20];
            for (int i = 0; i < 20; i++) dots[i] = Panel(multiplierRoot, new Vector2((i - 9.5f) * 15, -79), new Vector2(8, 8), new Color(.16f, .22f, .26f, .15f));

            objectLabel = Rect("Object caption", safe); objectLabel.sizeDelta = new Vector2(400, 72);
            nameText = Text(objectLabel, "", 24, ink, new Vector2(0, 12), new Vector2(400, 38)); nameText.fontStyle = FontStyles.Bold;
            statusText = Text(objectLabel, "3 HIDDEN HAZARDS", 14, muted, new Vector2(0, -21), new Vector2(400, 28)); statusText.characterSpacing = 2;
            statusRoot = Rect("Collect preview", safe); statusRoot.sizeDelta = new Vector2(500, 40);
            rewardText = Text(statusRoot, "CRUSH AGAIN OR CASH OUT", 17, muted, Vector2.zero, new Vector2(500, 38)); rewardText.characterSpacing = 2;

            actions = Rect("Actions", safe);
            crush = ActionButton(actions, "CRUSH", pressIcon, copper, ink, game.Crush); crushRect = crush.GetComponent<RectTransform>();
            collect = ActionButton(actions, "CASH OUT", walletIcon, mint, ink, game.Collect); collectRect = collect.GetComponent<RectTransform>();
            crush.name = "Crush"; collect.name = "Collect";
            BuildResult(); BuildNavigation(); BuildSettings(); game.Updated += Refresh; Layout(); Refresh();
        }

        private void Refresh()
        {
            bool playing = game.Navigation == NavigationState.Playing;
            multiplierRoot.gameObject.SetActive(playing); actions.gameObject.SetActive(playing);
            objectLabel.gameObject.SetActive(playing); statusRoot.gameObject.SetActive(playing);
            settings.gameObject.SetActive(playing && !ResultVisible && !game.SettingsOpen);
            settings.interactable = true; // Pausing is valid even during compression or failure.
            navigationPanel.SetActive(!playing && !game.SettingsOpen);
            bool main = game.Navigation == NavigationState.MainMenu;
            navigationTitle.text = main ? "HYDRAULIC\nPRESS" : "PAUSED";
            navigationLabel.text = main ? game.Round == null ? "PLAY" : game.Round.State == RoundState.Ready || game.Round.State == RoundState.Crushing ? "CONTINUE" : "PLAY AGAIN" : "RESUME";
            navigationHome.gameObject.SetActive(!main);
            LayoutNavigation();
            LayoutVersion++;
            bankText.text = Compact(game.Save.bank);
            if (game.Round == null) return;
            multiplierText.text = game.Round.Multiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "<size=48>x</size>";
            bankText.text = Compact(game.Save.bank); progressText.text = game.Round.Crushed + " / 20";
            nameText.text = game.CurrentName;
            statusText.text = "3 HIDDEN  /  " + (20 - game.Round.Crushed) + " REMAIN";
            rewardText.text = game.Round.Crushed > 0 ? "COLLECT  +" + game.Round.Reward.ToString("N0") : "CRUSH AGAIN OR CASH OUT";
            crush.interactable = game.CanCrush; collect.interactable = game.CanCollect;
            for (int i = 0; i < dots.Length; i++) dots[i].color = i < game.Round.Crushed ? ink : new Color(.16f, .22f, .26f, .15f);
        }
        public void PopMultiplier() { pop = 1; Refresh(); }
        private void Update()
        {
            if (game == null) return;
            if (lastWidth != Screen.width || lastHeight != Screen.height || lastSafeArea != Screen.safeArea) Layout();
            pop = Mathf.MoveTowards(pop, 0, Time.unscaledDeltaTime * 3.5f);
            multiplierText.transform.localScale = Vector3.one * (1 + Mathf.Sin(pop * Mathf.PI) * .09f);
        }
        private void Layout()
        {
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafeArea = Screen.safeArea;
            bool wide = Screen.width >= Screen.height;
            float aspect = (float)Screen.width / Mathf.Max(1,Screen.height);
            scaler.referenceResolution = wide ? new Vector2(1280, 720) : new Vector2(720, 1280);
            scaler.matchWidthOrHeight = wide ? 1 : 0;
            safe.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safe.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height); safe.offsetMin = safe.offsetMax = Vector2.zero;
            Anchor(header, new Vector2(.5f, 1), new Vector2(0, -51));
            if (wide)
            {
                float headerWidth = Mathf.Min(1120, 720 * aspect * lastSafeArea.width / Screen.width - 64);
                float half = headerWidth * .5f;
                float compact = Mathf.InverseLerp(1, 1.5f, aspect);
                float actionWidth = Mathf.Min(242, 720 * aspect * .28f);
                header.sizeDelta = new Vector2(headerWidth, 62);
                // Keep the compact brand and bank cluster at the opposite edges.
                header.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-half+27,0);
                header.GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(-half+105,0);
                header.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(half-201,0);
                bankText.rectTransform.anchoredPosition = new Vector2(half-132,0); settings.GetComponent<RectTransform>().anchoredPosition = new Vector2(half-34,0);
                Anchor(multiplierRoot, new Vector2(.17f, .54f), Vector2.zero); multiplierRoot.localScale = Vector3.one * Mathf.Lerp(.62f,.78f,compact);
                Anchor(actions, new Vector2(.84f, .47f), Vector2.zero); actions.sizeDelta = new Vector2(actionWidth, 215);
                crushRect.sizeDelta = collectRect.sizeDelta = new Vector2(actionWidth, 100); crushRect.anchoredPosition = new Vector2(0, 56); collectRect.anchoredPosition = new Vector2(0, -56);
                crushRect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = collectRect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-actionWidth*.33f,0);
                Anchor(statusRoot, new Vector2(.84f, .47f), new Vector2(0, 150)); statusRoot.localScale = Vector3.one * Mathf.Lerp(.64f,.72f,compact);
                Anchor(objectLabel, new Vector2(.5f, 0), new Vector2(0, 58)); objectLabel.localScale = Vector3.one * .85f;
            }
            else
            {
                header.sizeDelta = new Vector2(632, 62);
                header.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-289,0); header.GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(-211,0);
                header.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(115,0); bankText.rectTransform.anchoredPosition = new Vector2(184,0); settings.GetComponent<RectTransform>().anchoredPosition = new Vector2(282,0);
                Anchor(multiplierRoot, new Vector2(.5f, 1), new Vector2(0, -177)); multiplierRoot.localScale = Vector3.one;
                Anchor(actions, new Vector2(.5f, 0), new Vector2(0, 90)); actions.sizeDelta = new Vector2(632, 98);
                crushRect.sizeDelta = new Vector2(340, 100); collectRect.sizeDelta = new Vector2(272, 100); crushRect.anchoredPosition = new Vector2(-146, 0); collectRect.anchoredPosition = new Vector2(180, 0);
                crushRect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = collectRect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-80,0);
                Anchor(statusRoot, new Vector2(.5f, 0), new Vector2(0, 169)); statusRoot.localScale = Vector3.one;
                Anchor(objectLabel, new Vector2(.5f, 0), new Vector2(0, 244)); objectLabel.localScale = Vector3.one;
            }
            settings.GetComponent<RectTransform>().sizeDelta = Vector2.one * (wide && !UnityEngine.Device.Application.isMobilePlatform ? 70 : 100);
            progressText.fontSize = wide ? 22 : 26; nameText.fontSize = 28; statusText.fontSize = 22; rewardText.fontSize = wide ? 20 : 24; bankText.fontSize = 28;
            crush.GetComponentInChildren<TextMeshProUGUI>().fontSize = collect.GetComponentInChildren<TextMeshProUGUI>().fontSize = wide ? 26 : 32;
            LayoutSettings(wide); LayoutNavigation();
            var resultCard = result.transform.GetChild(0).GetComponent<RectTransform>(); resultCard.localScale = Vector3.one * (wide ? .85f : 1);
        }

        private void BuildResult()
        {
            result = Overlay("Result"); var card = Panel(result.transform, Vector2.zero, new Vector2(560, 430), ink).rectTransform;
            resultIcon = Icon(card, walletIcon, new Vector2(0, 132), 66, mint);
            resultTitle = Text(card, "COLLECTED", 17, mint, new Vector2(0, 68), new Vector2(480, 36)); resultTitle.characterSpacing = 4;
            resultMultiplier = Text(card, "1.00x", 75, paper, new Vector2(0, 0), new Vector2(500, 100)); resultMultiplier.fontStyle = FontStyles.Bold;
            resultPoints = Text(card, "+113", 25, paper, new Vector2(0, -66), new Vector2(480, 42));
            IconButton(card, replayIcon, new Vector2(0, -143), new Vector2(180, 100), copper, ink, () => game.NewRound()).name = "Replay";
            result.SetActive(false);
        }
        public void ShowResult(bool success, double multiplier, long points, bool perfect = false)
        {
            result.SetActive(true); resultIcon.sprite = success ? walletIcon : brokenIcon; resultIcon.color = success ? mint : copper;
            resultTitle.text = perfect ? "PERFECT ROUND" : success ? "COLLECTED" : "PRESS BROKEN"; resultTitle.color = success ? mint : copper;
            resultMultiplier.text = multiplier.ToString("0.00") + "x"; resultPoints.text = success ? "+" + points.ToString("N0") + " POINTS" : "UNBANKED POINTS LOST";
        }
        public void HideResult() => result.SetActive(false);

        private void BuildNavigation()
        {
            navigationPanel = Overlay("Navigation"); navigationPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(.95f,.94f,.91f,.05f);
            navigationTitle = Text(navigationPanel.transform, "HYDRAULIC\nPRESS", 64, ink, Vector2.zero, new Vector2(620,190)); navigationTitle.fontStyle = FontStyles.Bold; navigationTitle.characterSpacing = 3;
            navigationCard = Panel(navigationPanel.transform, Vector2.zero, new Vector2(632,300), paper).rectTransform;
            navigationPlay = ActionButton(navigationCard,"PLAY",playIcon,copper,ink,game.PlayOrResume); navigationPlay.name="PlayOrResume";
            navigationLabel = navigationPlay.GetComponentInChildren<TextMeshProUGUI>(); navigationLabel.fontSize=32;
            navigationSettings=ActionButton(navigationCard,"SETTINGS",settingsIcon,mint,ink,()=>game.SetSettings(true)); navigationSettings.name="NavigationSettings";
            navigationHome=ActionButton(navigationCard,"MAIN MENU",homeIcon,Color.white,ink,game.ShowMainMenu); navigationHome.name="MainMenu";
        }
        private void LayoutNavigation()
        {
            if (navigationPanel == null) return;
            bool main=game.Navigation==NavigationState.MainMenu;
            bool side=(float)Screen.width/Mathf.Max(1,Screen.height)>=1.5f;
            navigationTitle.rectTransform.localScale=Vector3.one*(side?.8f:1);
            Anchor(navigationTitle.rectTransform,side?new Vector2(.17f,.60f):new Vector2(.5f,1),side?Vector2.zero:new Vector2(0,-170));
            Anchor(navigationCard,side?new Vector2(.84f,.45f):main?new Vector2(.5f,0):new Vector2(.5f,.43f),side||!main?Vector2.zero:new Vector2(0,190));
            float width=side?380:632;
            navigationCard.sizeDelta=new Vector2(width,main?300:420);
            var primary=navigationPlay.GetComponent<RectTransform>(); primary.sizeDelta=new Vector2(width-48,100); primary.anchoredPosition=new Vector2(0,main?65:120);
            var options=navigationSettings.GetComponent<RectTransform>(); options.sizeDelta=new Vector2(width-48,100); options.anchoredPosition=new Vector2(0,main?-65:0);
            var home=navigationHome.GetComponent<RectTransform>(); home.sizeDelta=new Vector2(width-48,100); home.anchoredPosition=new Vector2(0,-120);
            navigationSettings.GetComponentInChildren<TextMeshProUGUI>().fontSize=28; navigationHome.GetComponentInChildren<TextMeshProUGUI>().fontSize=28;
            foreach (var button in new[] { crush, collect, navigationPlay, navigationSettings, navigationHome }) LayoutButtonContent(button);
        }

        private static void LayoutButtonContent(UnityEngine.UI.Button button)
        {
            var rect = button.GetComponent<RectTransform>();
            var icon = rect.Find("Icon") as RectTransform;
            if (icon == null) return;
            icon.anchoredPosition = new Vector2(-rect.sizeDelta.x * .5f + 42, 0);
            var label = button.GetComponentInChildren<TextMeshProUGUI>().rectTransform;
            label.anchorMin = new Vector2(0, .5f); label.anchorMax = new Vector2(1, .5f);
            label.offsetMin = new Vector2(72, -30); label.offsetMax = new Vector2(-12, 30);
        }

        private void BuildSettings()
        {
            settingsPanel = Overlay("Settings"); settingsCard = Panel(settingsPanel.transform, Vector2.zero, new Vector2(600,840), paper).rectTransform;
            settingsTitle=Text(settingsCard,"SETTINGS",32,ink,Vector2.zero,new Vector2(300,50)); settingsTitle.fontStyle=FontStyles.Bold;
            settingsClose=IconButton(settingsCard,checkIcon,Vector2.zero,new Vector2(100,100),mint,ink,()=>game.SetSettings(false)); settingsClose.name="CloseSettings";
            soundLabel=Text(settingsCard,"SOUND",28,ink,Vector2.zero,new Vector2(200,50));
            soundRow=Slider(settingsCard,Vector2.zero,game.Save.sound,value=>{game.Save.sound=value;game.SettingsChanged();}); soundRow.name="SoundVolume";
            musicLabel=Text(settingsCard,"MUSIC",28,ink,Vector2.zero,new Vector2(200,50));
            musicRow=Slider(settingsCard,Vector2.zero,game.Save.music,value=>{game.Save.music=value;game.SettingsChanged();}); musicRow.name="MusicVolume";
            shakeLabel=Text(settingsCard,"CAMERA SHAKE",28,ink,Vector2.zero,new Vector2(320,50));
            shakeButton=ActionButton(settingsCard,game.Save.shake?"ON":"OFF",null,mint,ink,null); shakeButton.name="CameraShake";
            shakeButton.onClick.AddListener(()=>{game.Save.shake=!game.Save.shake;shakeButton.GetComponentInChildren<TextMeshProUGUI>().text=game.Save.shake?"ON":"OFF";game.SettingsChanged();});
            qualityLabel=Text(settingsCard,"POWER SAVING",28,ink,Vector2.zero,new Vector2(320,50));
            qualityButton=ActionButton(settingsCard,game.Save.lowGraphics?"ON":"OFF",null,mint,ink,null); qualityButton.name="PowerSaving";
            qualityButton.onClick.AddListener(()=>{game.Save.lowGraphics=!game.Save.lowGraphics;qualityButton.GetComponentInChildren<TextMeshProUGUI>().text=game.Save.lowGraphics?"ON":"OFF";game.SettingsChanged();});
            statsText=Text(settingsCard,"",24,ink,Vector2.zero,new Vector2(560,85)); statsText.lineSpacing=6;
            rulesText=Text(settingsCard,"",24,muted,Vector2.zero,new Vector2(560,125)); rulesText.lineSpacing=6;
            settingsPanel.SetActive(false);
        }
        private void LayoutSettings(bool wide)
        {
            settingsCard.sizeDelta=wide?new Vector2(1000,560):new Vector2(600,840);
            float referenceWidth=wide?720f*Screen.width/Mathf.Max(1,Screen.height):720;
            float referenceHeight=wide?720:720f*Screen.height/Mathf.Max(1,Screen.width);
            float safeWidth=referenceWidth*lastSafeArea.width/Mathf.Max(1,Screen.width);
            float safeHeight=referenceHeight*lastSafeArea.height/Mathf.Max(1,Screen.height);
            float scale=Mathf.Min(wide?.95f:1.05f,Mathf.Min((safeWidth-40)/settingsCard.sizeDelta.x,(safeHeight-40)/settingsCard.sizeDelta.y));
            settingsCard.localScale=Vector3.one*Mathf.Max(.3f,scale);
            settingsTitle.rectTransform.anchoredPosition=wide?new Vector2(-295,210):new Vector2(-115,340);
            settingsClose.GetComponent<RectTransform>().anchoredPosition=wide?new Vector2(410,210):new Vector2(235,340);
            soundLabel.rectTransform.anchoredPosition=wide?new Vector2(-340,110):new Vector2(-170,225);
            soundRow.anchoredPosition=wide?new Vector2(-135,110):new Vector2(105,225);
            musicLabel.rectTransform.anchoredPosition=wide?new Vector2(-340,-10):new Vector2(-170,115);
            musicRow.anchoredPosition=wide?new Vector2(-135,-10):new Vector2(105,115);
            shakeLabel.rectTransform.anchoredPosition=wide?new Vector2(170,110):new Vector2(-105,5);
            qualityLabel.rectTransform.anchoredPosition=wide?new Vector2(170,-10):new Vector2(-105,-105);
            var shake=shakeButton.GetComponent<RectTransform>(); var quality=qualityButton.GetComponent<RectTransform>();
            shake.sizeDelta=quality.sizeDelta=new Vector2(112,100);
            shake.anchoredPosition=wide?new Vector2(410,110):new Vector2(195,5);
            quality.anchoredPosition=wide?new Vector2(410,-10):new Vector2(195,-105);
            shakeButton.GetComponentInChildren<TextMeshProUGUI>().fontSize=qualityButton.GetComponentInChildren<TextMeshProUGUI>().fontSize=28;
            statsText.rectTransform.anchoredPosition=new Vector2(0,wide?-125:-225); statsText.rectTransform.sizeDelta=new Vector2(wide?900:560,85);
            rulesText.rectTransform.anchoredPosition=new Vector2(0,wide?-215:-335); rulesText.rectTransform.sizeDelta=new Vector2(wide?900:560,125);
            string payout=game.config.basePoints+" x "+(1-game.config.rewardMargin).ToString("0.00")+" / survival chance";
            rulesText.text=wide?"17 SAFE  /  3 HIDDEN      |      VIRTUAL POINTS\nPayout = "+payout:"17 SAFE  /  3 HIDDEN\nPayout = "+payout+"\nVIRTUAL POINTS";
        }
        public void SetSettings(bool open)
        {
            settingsPanel.SetActive(open);
            if (open) statsText.text = "BEST  " + game.Save.bestMultiplier.ToString("0.00") + "x     /     " + game.Save.bestRound.ToString("N0") + " PTS\n" + game.Save.totalCrushed.ToString("N0") + " OBJECTS CRUSHED";
        }
        private RectTransform Slider(Transform parent, Vector2 position, float value, Action<float> changed)
        {
            var root = Rect("Volume", parent); root.anchoredPosition = position; root.sizeDelta = new Vector2(250,100);
            var bg = Panel(root, Vector2.zero, new Vector2(250,8),new Color(.1f,.2f,.25f,.12f));
            var handle = Panel(root, Vector2.zero, new Vector2(42,42),ink);
            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.targetGraphic = handle; slider.handleRect = handle.rectTransform; slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1; slider.value = value;
            handle.rectTransform.sizeDelta = new Vector2(42,-58);
            var hitArea = root.gameObject.AddComponent<UnityEngine.UI.Image>(); hitArea.color = Color.clear; hitArea.raycastTarget = true;
            bg.raycastTarget = false; handle.raycastTarget = true; slider.onValueChanged.AddListener(v => changed(v));
            return root;
        }
        private GameObject Overlay(string name)
        {
            var rect = Rect(name, safe); Fill(rect); var shade = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); shade.color = new Color(.08f, .13f, .16f, .45f); return rect.gameObject;
        }
        private UnityEngine.UI.Button ActionButton(Transform parent, string label, Sprite icon, Color bg, Color fg, Action action)
        {
            var panel = Panel(parent, Vector2.zero, new Vector2(280, 94), bg); panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = panel;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = Color.white; colors.pressedColor = new Color(.82f,.82f,.82f); colors.disabledColor = new Color(1,1,1,.34f); button.colors = colors;
            if (icon != null) Icon(panel.transform, icon, new Vector2(-80, 0), 42, fg);
            var text = Text(panel.transform, label, 23, fg, new Vector2(icon == null ? 0 : 26, 0), new Vector2(icon == null ? 112 : 170, 60)); text.fontStyle = FontStyles.Bold;
            if (action != null) button.onClick.AddListener(() => { game.audioManager.Unlock(); game.audioManager.Play(game.audioManager.button, .4f); action(); });
            return button;
        }
        private UnityEngine.UI.Button IconButton(Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color bg, Color fg, Action action)
        {
            var panel = Panel(parent, position, size, bg); panel.raycastTarget = true; var button = panel.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = panel;
            Icon(panel.transform, sprite, Vector2.zero, Mathf.Min(size.x,size.y) * .57f, fg);
            button.onClick.AddListener(() => { game.audioManager.Unlock(); game.audioManager.Play(game.audioManager.button,.4f); action(); }); return button;
        }
        private UnityEngine.UI.Image Panel(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect("Panel", parent); rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = rounded; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; image.raycastTarget = false; return image;
        }
        private UnityEngine.UI.Image Icon(Transform parent, Sprite sprite, Vector2 position, float size, Color color)
        {
            var rect = Rect("Icon", parent); rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * size;
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; image.preserveAspect = true; return image;
        }
        private TextMeshProUGUI Text(Transform parent, string content, float size, Color color, Vector2 position, Vector2 dimensions)
        {
            var rect = Rect("Text", parent); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = content; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; return text;
        }
        private static RectTransform Rect(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>(); }
        private static void Fill(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position) { rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; }
        private static string Compact(long value) => value < 1000000 ? value.ToString("N0") : value < 1000000000 ? (value / 1000000d).ToString("0.0") + "M" : (value / 1000000000d).ToString("0.0") + "B";
        private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }
        private void OnDestroy() { if (game != null) game.Updated -= Refresh; }
    }
}

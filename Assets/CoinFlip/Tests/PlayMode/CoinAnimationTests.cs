using System.Collections;
using CoinFlip;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CoinAnimationTests
{
    static Button ButtonNamed(UIManager ui,string name)
    {
        foreach(var button in ui.GetComponentsInChildren<Button>(true))if(button.name==name)return button;
        Assert.Fail("Missing UI button: "+name);return null;
    }

    [UnityTest]
    public IEnumerator MainMenuPauseResumeAndReturnFlowsWork()
    {
        var root=new GameObject("Menu flow test");
        var model=new GameSession(new SaveData(),()=>CoinSide.Heads);
        var sound=root.AddComponent<AudioManager>();sound.Initialize(model.Data);
        var ui=root.AddComponent<UIManager>();ui.Initialize(model,sound,Resources.Load<TMP_FontAsset>("Fonts/Outfit SDF"));
        ui.StartRequested+=ui.ShowGame;ui.MainMenuRequested+=ui.ShowMainMenu;
        bool paused=false;ui.PauseChanged+=value=>paused=value;
        yield return null;
        try
        {
            Assert.True(ui.IsMainMenu,"A fresh launch should show the main menu.");
            ButtonNamed(ui,"Play game").onClick.Invoke();
            Assert.False(ui.IsMainMenu);Assert.False(ui.IsPaused);
            ButtonNamed(ui,"Pause menu").onClick.Invoke();
            Assert.True(ui.IsPaused);Assert.True(paused);
            ButtonNamed(ui,"Resume game").onClick.Invoke();
            Assert.False(ui.IsPaused);Assert.False(paused);
            ButtonNamed(ui,"Pause menu").onClick.Invoke();
            ButtonNamed(ui,"Return to main menu").onClick.Invoke();
            Assert.True(ui.IsMainMenu);Assert.False(ui.IsPaused);Assert.False(paused);
        }
        finally{Object.Destroy(root);}
    }

    [UnityTest]
    public IEnumerator BothPredeterminedFacesSettleOnceWithinTwoSeconds()
    {
        var root=new GameObject("Animation test");var config=ScriptableObject.CreateInstance<GameSettings>();
        var model=new GameSession(new SaveData(),()=>CoinSide.Heads);
        var sound=root.AddComponent<AudioManager>();sound.Initialize(model.Data);
        var ui=root.AddComponent<UIManager>();ui.Initialize(model,sound,Resources.Load<TMP_FontAsset>("Fonts/Outfit SDF"));
        var effects=root.AddComponent<VFXManager>();effects.Initialize(ui,32);
        var animator=root.AddComponent<CoinAnimationController>();animator.Initialize(ui,sound,effects,config);
        yield return null;
        try
        {
            foreach(CoinSide side in new[]{CoinSide.Heads,CoinSide.Tails})
            {
                int calls=0;float start=Time.realtimeSinceStartup;bool done=false;
                var routine=animator.Flip(new Round(1,1,CoinSide.Heads,side),()=>calls++);
                while(Time.realtimeSinceStartup-start<3)
                {
                    if(!routine.MoveNext()){done=true;break;}
                    yield return routine.Current;
                }
                Assert.True(done,$"Animation stuck: {animator.Phase}, t={animator.Progress}, dt={Time.unscaledDeltaTime}, duration={config.flipDuration}, paused={animator.Suspended}");
                Assert.AreEqual(1,calls);Assert.AreEqual(side.ToString(),ui.CoinImage.sprite.name);Assert.AreEqual(Vector2.zero,ui.Coin.anchoredPosition);Assert.AreEqual(Vector3.one,ui.Coin.localScale);
                Assert.AreEqual(10,model.Data.balance,"Presentation must never modify the balance");
            }
        }
        finally{Object.Destroy(root);Object.Destroy(config);}
    }
}

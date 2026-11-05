using System;
using ChickenRush;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class SeededRiskTests
{
    const string Root="Assets/ChickenRush/";
    static RushDifficulty Config(string mode)=>AssetDatabase.LoadAssetAtPath<RushDifficulty>(Root+"Config/"+mode+".asset");
    static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Arcade/"+name+".png");
    [TestCase("EASY")][TestCase("NORMAL")][TestCase("HARD")][TestCase("HARDCORE")]
    public void RiskAndRewardsRemainInternallyConsistent(string mode)
    {
        var c=Config(mode);double survival=1;float previous=1;
        Assert.AreEqual(c.multipliers.Length,c.successChances.Length);
        for(int i=0;i<c.multipliers.Length;i++)
        {
            float p=c.successChances[i];Assert.That(p,Is.InRange(.01f,.99f));survival*=p;
            Assert.Greater(c.multipliers[i],previous);Assert.That(c.multipliers[i],Is.EqualTo(Math.Round(.985/survival,2)).Within(.01f));
            if(i>0)Assert.LessOrEqual(p,c.successChances[i-1]);previous=c.multipliers[i];
        }
    }
    [Test] public void SeededRollsHaveTheConfiguredFrequencyAndIgnoreCallOrder()
    {
        int passes=0;const int samples=100000;
        for(int seed=0;seed<samples;seed++)if(SeededRisk.Survives(seed,4,.7f))passes++;
        Assert.That(passes/(double)samples,Is.EqualTo(.7).Within(.007));
        double original=SeededRisk.Roll(4711,6);
        for(int i=0;i<1000;i++)SeededRisk.Roll(i,i%24+1);
        Assert.AreEqual(original,SeededRisk.Roll(4711,6));Assert.AreNotEqual(original,SeededRisk.Roll(4711,7));
    }
    [Test] public void PlannedOutcomeCannotBeOverriddenByPresentationCallbacks()
    {
        for(int seed=0;seed<64;seed++)
        {
            var r=new RoundRules(10,new[]{1.5f,2.5f},new[]{.65f,.5f});r.SetStake(4);r.Start(seed);r.Ready();r.Hop();
            Assert.IsFalse(r.CashOut());Assert.IsFalse(r.Hop());
            if(r.PlannedSuccess){Assert.IsFalse(r.Hit());Assert.IsTrue(r.Land());Assert.IsFalse(r.Land());Assert.IsTrue(r.CashOut());Assert.IsFalse(r.CashOut());Assert.AreEqual(12,r.Balance);}
            else{Assert.IsFalse(r.Land());Assert.IsTrue(r.Hit());Assert.IsFalse(r.Hit());Assert.IsFalse(r.CashOut());Assert.AreEqual(6,r.Balance);}
        }
    }
    [Test] public void FourModesIncreasePerHopRiskAndReward()
    {
        var names=new[]{"EASY","NORMAL","HARD","HARDCORE"};
        for(int i=1;i<4;i++){Assert.Less(Config(names[i]).successChances[0],Config(names[i-1]).successChances[0]);Assert.Greater(Config(names[i]).multipliers[0],Config(names[i-1]).multipliers[0]);}
    }
    [TestCase("EASY")][TestCase("NORMAL")][TestCase("HARD")][TestCase("HARDCORE")]
    public void VisibleTrafficMatchesSeededResultsAndRecyclesRoadSegments(string mode)
    {
        var parent=new GameObject("Isolated road test");var c=Config(mode);
        var vehicles=new Sprite[10];string[] names={"Compact","Taxi","Van","Truck","Delivery"};
        for(int i=0;i<10;i++)vehicles[i]=Art(names[i%5]+(i<5?"Down":"Up"));
        var world=new RoadWorld(parent.transform,AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Shapes/Rounded.png"),AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Shapes/Disc.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Shapes/Square.png"),null,Art("RoadSurface"),AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"),
            AssetDatabase.LoadAssetAtPath<Material>(Root+"Art/ChickenUnlit.mat"),c,vehicles,Art("LandingPad"),Art("Barrier"),Art("Shrub"),Art("Sidewalk"));
        try
        {
            int successes=0,failures=0,childCount=parent.transform.childCount;
            for(int seed=1;seed<=12;seed++)
            {
                world.Reset(seed);
                // Exercise every lane/outcome independently, including far beyond
                // the initial window, without allocating more road objects.
                for(int lane=1;lane<=c.multipliers.Length;lane++)
                {
                    bool success=SeededRisk.Survives(seed,lane,c.successChances[lane-1]);
                    world.Present(4.4f,lane-1,0,lane*RoadWorld.Step+3,7);world.BeginAttempt(lane,success,c.hopSeconds);
                    Vector2 previous=new Vector2((lane-1)*RoadWorld.Step,0);bool contact=false;
                    for(float t=1f/120;t<c.hopSeconds+1f/120;t+=1f/120)
                    {
                        world.Tick(1f/120,previous,true,true);
                        var foot=new Vector2((lane-1)*RoadWorld.Step+RoadWorld.Step*TrafficPattern.HopProgress(t,c.hopSeconds),0);
                        if(world.Collides(foot,previous)){contact=true;break;}
                        if(success)
                        {
                            world.FillSnapshots();
                            foreach(var car in world.vehicleSnapshots)if(car.visible && Mathf.Abs(car.x-foot.x)<car.width*.5f+.23f)
                                Assert.Greater(Mathf.Abs(car.y-.5f),car.length*.5f+.26f,"Visible collision on a winning hop: "+mode+" lane "+lane);
                        }
                        previous=foot;
                    }
                    Assert.AreEqual(!success,contact,mode+" lane "+lane);if(success){world.Secure(lane);successes++;}else failures++;
                    Assert.AreEqual(childCount,parent.transform.childCount,"Road pool grew");
                }
            }
            Assert.Greater(successes,0);Assert.Greater(failures,0);Assert.AreEqual(36,world.vehicleSnapshots.Length);
        }
        finally{Object.DestroyImmediate(parent);}
    }
}

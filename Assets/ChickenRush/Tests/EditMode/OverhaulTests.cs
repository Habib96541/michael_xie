using System;
using System.Linq;
using ChickenRush;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class OverhaulTests
{
    static RushDifficulty Config(string mode)=>AssetDatabase.LoadAssetAtPath<RushDifficulty>("Assets/ChickenRush/Config/"+mode+".asset");

    [TestCase("EASY")][TestCase("NORMAL")][TestCase("HARD")]
    public void TrafficEscalatesAndKeepsARecoveryGap(string mode)
    {
        var config=Config(mode);Assert.GreaterOrEqual(config.multipliers.Length,12);
        Assert.Less(config.GetLane(3).speed,config.GetLane(6).speed);Assert.Less(config.GetLane(6).speed,config.GetLane(10).speed);Assert.Less(config.GetLane(10).speed,config.GetLane(15).speed);
        Assert.Less(config.GetLane(3).vehicleCount,config.GetLane(10).vehicleCount);
        for(int lane=1;lane<=config.multipliers.Length;lane++)for(int seed=0;seed<32;seed++)
        {
            var tuning=config.GetLane(lane);var pattern=new TrafficPattern(tuning,seed);
            Assert.That(pattern.GapAfter(pattern.vehicles.Length-1),Is.GreaterThanOrEqualTo(tuning.guaranteedGap-.0001f),mode+" lane "+lane);
            for(int i=0;i<pattern.vehicles.Length;i++)Assert.Greater(pattern.GapAfter(i)*pattern.speed,.15f,"Overlapping/near-touching traffic");
            foreach(var car in pattern.vehicles)Assert.Greater(RoadWorld.LaneOffset-TrafficPattern.PlayerCenterX-car.width*.5f-TrafficPattern.PlayerHalfWidth,.05f,"Safe island invaded");
        }
    }
    [TestCase("EASY")][TestCase("NORMAL")][TestCase("HARD")]
    public void EveryLaneHasAReachableLaunchWindow(string mode)
    {
        var config=Config(mode);
        for(int lane=1;lane<=config.multipliers.Length;lane++)for(int seed=0;seed<3;seed++)
        {
            var pattern=new TrafficPattern(config.GetLane(lane),seed*91);int run=0,best=0;
            for(float launch=0;launch<pattern.CycleSeconds;launch+=.04f)
            {
                bool clear=true;
                // Independent sampled AABBs check actual core/body overlap, rather
                // than calling the runtime swept-intersection implementation.
                for(float t=0;t<=config.hopSeconds && clear;t+=1f/240)
                {
                    float player=RoadWorld.Step*TrafficPattern.HopProgress(t,config.hopSeconds)+TrafficPattern.PlayerCenterX;
                    for(int car=0;car<pattern.vehicles.Length;car++)
                    {
                        var vehicle=pattern.vehicles[car];float dy=Mathf.Abs(pattern.Position(car,launch+t)-TrafficPattern.PlayerCenterY);
                        if(Mathf.Abs(player-RoadWorld.LaneOffset)<vehicle.width*.5f+TrafficPattern.PlayerHalfWidth && dy<vehicle.length*.5f+TrafficPattern.PlayerHalfHeight){clear=false;break;}
                    }
                }
                run=clear?run+1:0;best=Math.Max(best,run);
            }
            Assert.GreaterOrEqual(best,2,mode+" lane "+lane+" seed "+seed+" has no usable sampled launch window");
        }
    }
    [Test] public void DifferentLanesHaveDifferentSpeedsDirectionsAndHeadways()
    {
        var c=Config("NORMAL");var first=new TrafficPattern(c.GetLane(1),42);var second=new TrafficPattern(c.GetLane(2),42);var express=new TrafficPattern(c.GetLane(5),42);
        Assert.AreEqual(-1,first.direction);Assert.AreEqual(1,second.direction);Assert.Greater(express.speed,first.speed);
        Assert.AreNotEqual(first.GapAfter(0),first.GapAfter(1));Assert.IsTrue(express.vehicles.All(v=>v.kind==VehicleKind.Express));
        var again=new TrafficPattern(c.GetLane(1),42);Assert.AreEqual(first.Position(0,1.25),again.Position(0,1.25));
    }
    [Test] public void SweptHitCatchesFastVerticalVehiclesButAllowsANearMiss()
    {
        Assert.IsTrue(TrafficPattern.SweptHit(new Vector2(0,4),new Vector2(0,-4),new Vector2(.4f,.8f)));
        Assert.IsFalse(TrafficPattern.SweptHit(new Vector2(.41f,4),new Vector2(.41f,-4),new Vector2(.4f,.8f)));
        Assert.IsFalse(TrafficPattern.SweptHit(new Vector2(2,1),new Vector2(1,1),new Vector2(.4f,.8f)));
    }
    [TestCase(1920,1080,RushDisplay.Desktop)]
    [TestCase(1366,768,RushDisplay.Desktop)]
    [TestCase(1280,720,RushDisplay.Desktop)]
    [TestCase(390,844,RushDisplay.Portrait)]
    [TestCase(360,800,RushDisplay.Portrait)]
    [TestCase(844,390,RushDisplay.Landscape)]
    [TestCase(768,1024,RushDisplay.Tablet)]
    public void ResponsiveRegionsStaySeparateAndTouchable(int width,int height,RushDisplay mode)
    {
        var l=RushLayout.Calculate(width,height);Assert.AreEqual(mode,l.mode);
        var bounds=new Rect(0,0,width,height);
        foreach(var rect in new[]{l.brand,l.wallet,l.best,l.difficulty,l.settings,l.hop,l.cash,l.risk,l.multiplier,l.potential,l.viewport})
        {
            Assert.Greater(rect.width,0);Assert.Greater(rect.height,0);Assert.GreaterOrEqual(rect.x,0);Assert.GreaterOrEqual(rect.y,0);
            Assert.LessOrEqual(rect.xMax,bounds.xMax+.01f);Assert.LessOrEqual(rect.yMax,bounds.yMax+.01f);
        }
        Assert.IsFalse(l.hop.Overlaps(l.cash));Assert.IsFalse(l.viewport.Overlaps(l.hop));Assert.IsFalse(l.viewport.Overlaps(l.wallet));
        Assert.IsFalse(l.multiplier.Overlaps(l.risk));Assert.IsFalse(l.potential.Overlaps(l.risk));
        foreach(var rect in new[]{l.hop,l.cash,l.settings,l.difficulty}){Assert.GreaterOrEqual(rect.width,44);Assert.GreaterOrEqual(rect.height,44);}
        var top=new[]{l.brand,l.wallet,l.best,l.difficulty,l.settings};
        for(int a=0;a<top.Length;a++)for(int b=a+1;b<top.Length;b++)Assert.IsFalse(top[a].Overlaps(top[b]),a+" overlaps "+b);
    }
    [Test] public void UIUsesLosslessSourcesAndExplicitNineSliceBorders()
    {
        string path="Assets/ChickenRush/Art/UI/Card.png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Assert.AreEqual(new Vector4(32,32,32,32),sprite.border);Assert.AreEqual(128,sprite.rect.width);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.AreEqual(TextureImporterFormat.RGBA32,importer.GetPlatformTextureSettings("WebGL").format);Assert.IsFalse(importer.mipmapEnabled);
    }
    public sealed class SliceProbe:Image { public void Fill(VertexHelper helper){base.OnPopulateMesh(helper);} }
    [Test] public void NineSliceKeepsTheSameCornerWidthAtDifferentSizes()
    {
        var canvas=new GameObject("Test Canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().referencePixelsPerUnit=100;
        var go=new GameObject("Slice probe",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);var image=go.AddComponent<SliceProbe>();
        image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ChickenRush/Art/UI/Card.png");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;
        try
        {
            foreach(var size in new[]{new Vector2(240,64),new Vector2(480,90)})
            {
                go.GetComponent<RectTransform>().sizeDelta=size;
                using(var helper=new VertexHelper())
                {
                    image.Fill(helper);var positions=new float[helper.currentVertCount];var vertex=new UIVertex();
                    for(int i=0;i<positions.Length;i++){helper.PopulateUIVertex(ref vertex,i);positions[i]=vertex.position.x;}
                    var columns=positions.Distinct().OrderBy(x=>x).ToArray();Assert.That(columns[1]-columns[0],Is.EqualTo(16).Within(.01f));
                }
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(canvas);}
    }
    [TestCase("EASY")][TestCase("NORMAL")][TestCase("HARD")]
    public void FullConfiguredCrossingPaysTheDisplayedProgressionOnce(string mode)
    {
        var config=Config(mode);var rules=new RoundRules(100,config.multipliers);rules.SetStake(10);rules.Start();rules.Ready();
        for(int lane=0;lane<config.multipliers.Length;lane++){Assert.IsTrue(rules.Hop());Assert.IsTrue(rules.Land());}
        int reward=(int)decimal.Floor(10*(decimal)config.multipliers.Last());Assert.AreEqual(reward,rules.PotentialReward);
        Assert.IsTrue(rules.CashOut());Assert.IsFalse(rules.CashOut());Assert.AreEqual(90+reward,rules.Balance);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrickRush.Editor
{
    public static class RuleValidation
    {
        public static string Run()
        {
            var passed=new List<string>();
            void Check(string name,bool condition){if(!condition)throw new Exception("FAILED: "+name);passed.Add(name);}
            Check("Combo boundaries",GameRules.Multiplier(2)==1&&GameRules.Multiplier(3)==2&&GameRules.Multiplier(5)==2&&GameRules.Multiplier(6)==3&&GameRules.Multiplier(10)==4&&GameRules.Multiplier(15)==5&&GameRules.Multiplier(100)==5);
            var score=new ScoreManager();score.Reset();for(int i=0;i<3;i++)score.Award(BrickKind.Standard,false);
            Check("Third brick earns x2",score.Points==40&&score.Destroyed==3&&score.HighestCombo==2);
            Check("Double-score armored reward",score.Award(BrickKind.Armored,true)==100&&score.Points==140);
            score.Return();Check("Shared paddle reset",score.Combo==0&&score.Multiplier==1&&score.HighestCombo==2);
            Check("Life bonus",GameRules.FinalScore(140,3,100)==440&&GameRules.FinalScore(140,-1,100)==140);
            score.Reset();Check("Replay reset",score.Points==0&&score.Destroyed==0&&score.HighestCombo==1);
            bool valid=true;for(int i=0;i<1000;i++){var d=GameRules.SafeDirection(new Vector2(Mathf.Cos(i),Mathf.Sin(i)));if(Mathf.Abs(d.magnitude-1)>.0001f||Mathf.Abs(d.y)<.3399f||Mathf.Abs(d.x)<.296f)valid=false;}
            Check("No horizontal or vertical loops (1000 headings)",valid);
            Check("Paddle aims left, center, right",GameRules.PaddleBounce(-1).x<-.8f&&GameRules.PaddleBounce(1).x>.8f&&GameRules.PaddleBounce(0).y>.9f);
            Art.Load();var root=new GameObject("Validation (temporary)");
            try
            {
                var waves=new BrickWaveManager(root.transform);waves.Reset();
                for(int i=0;i<8;i++){waves.Generate(60);int alive=0;foreach(var b in waves.Bricks)if(b.Alive&&b.Kind!=BrickKind.Obstacle)alive++;Check("Layout "+i+" has reachable destructible bricks",alive==waves.Remaining&&alive>20&&alive<=63);}
                Check("Wave completion tracking",waves.Wave==8&&waves.Completed==7);
                int count=waves.Remaining;for(int i=0;i<count;i++)waves.Destroyed();Check("Completion counted before transition",waves.Completed==8);
                waves.Reset();waves.Generate(0);count=waves.Remaining;
                Check("20-second evolution adds armor",waves.Evolve(20)&&Array.Exists(waves.Bricks,b=>b.Alive&&b.Kind==BrickKind.Armored));
                Check("45-second evolution adds explosives",waves.Evolve(45)&&Array.Exists(waves.Bricks,b=>b.Alive&&b.Kind==BrickKind.Explosive));
                Check("70-second evolution preserves solvability",waves.Evolve(70)&&waves.Remaining==count);
                Check("Evolution is idempotent",!waves.Evolve(70));
                foreach(var b in waves.Bricks)b.Hide();
                var brick=waves.Bricks[0];brick.Configure(Vector2.zero,BrickKind.Armored,Art.Cyan);Physics2D.SyncTransforms();
                var hit=Physics2D.CircleCast(new Vector2(0,-4),BallController.Radius,Vector2.up,10,1<<6);
                Check("Swept high-speed cast hits brick",hit.collider!=null&&hit.collider.gameObject==brick.gameObject&&hit.distance<4);
                Check("Armor takes two hits",!brick.Damage()&&brick.Alive&&brick.HP==1&&brick.Damage()&&!brick.Alive);
                Check("No duplicate destruction",!brick.Damage());
                Physics2D.SyncTransforms();Check("Destroyed collider removed immediately",Physics2D.CircleCast(new Vector2(0,-4),BallController.Radius,Vector2.up,10,1<<6).collider==null);
                brick.Configure(Vector2.zero,BrickKind.Obstacle,Art.White);Check("Indestructible obstacle",!brick.Damage()&&brick.Alive);
                brick.Configure(Vector2.zero,BrickKind.Standard,Art.Cyan);Physics2D.SyncTransforms();
                var origin=new Vector2(-3,-2);var towardCorner=(new Vector2(-.455f,-.18f)-origin).normalized;
                hit=Physics2D.CircleCast(origin,BallController.Radius,towardCorner,10,1<<6);
                Check("Diagonal corner cast returns a valid normal",hit.collider!=null&&hit.normal.x<-.1f&&hit.normal.y<-.1f);
                brick.Configure(new Vector2(-.46f,0),BrickKind.Standard,Art.Cyan);waves.Bricks[1].Configure(new Vector2(.46f,0),BrickKind.Standard,Art.Pink);Physics2D.SyncTransforms();
                var filter=new ContactFilter2D();filter.SetLayerMask(1<<6);var simultaneous=new RaycastHit2D[4];
                int contacts=Physics2D.CircleCast(new Vector2(0,-4),BallController.Radius,Vector2.up,filter,simultaneous,10);
                Check("Adjacent-brick seam reports both contacts",contacts==2&&simultaneous[0].collider!=simultaneous[1].collider);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/rules.txt",string.Join("\n",passed));Debug.Log("BRICK RUSH: "+passed.Count+" validation checks passed.");return passed.Count+" checks passed";
        }
    }
}

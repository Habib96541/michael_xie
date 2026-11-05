using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths.Editor
{
    public static class WindowAndDailyChecks
    {
        static void Require(bool value,string message){if(!value)throw new Exception("WINDOW/DAILY CHECK FAILED: "+message);}
        public static void Rules()
        {
            long now=20*86400+600;
            var q=new DailyQuestProgress();q.EnsureDay(now);
            Require(q.Claim(0,now)==0&&q.Claim(-1,now)==0&&q.Claim(99,now)==0,"incomplete/invalid quests never pay");
            for(int i=0;i<10;i++)q.RecordFish(i%2==0?"minnow":"perch",now);
            Require(q.Value(0)==10&&q.Value(2)==2,"landed fish and unique species use separate daily counters");
            Require(q.Claim(0,now)==100&&q.Claim(0,now)==0,"fish reward claims exactly once");
            q.RecordDepth(14.9f,now);Require(q.Claim(1,now)==0,"depth target cannot round up early");
            q.RecordDepth(15,now);Require(q.Claim(1,now)==150,"reaching depth completes the depth quest");
            q.RecordFish("clown",now);q.RecordFish("bluefin",now);q.RecordFish("sunfish",now);
            Require(q.Claim(2,now)==125,"five distinct species completes daily variety even if already known");
            for(int i=0;i<5;i++)q.RecordCast(now);
            Require(q.Claim(3,now)==100,"five real cast records complete the cast quest");
            Require(q.EnsureDay(21*86400)&&q.fish==0&&q.casts==0&&q.deepest==0&&q.species.Count==0&&q.claimed.Count==0,"midnight resets progress and claims");
            for(int i=0;i<5;i++)q.RecordCast(21*86400+1);
            q.Claim(3,21*86400+1);
            Require(!q.EnsureDay(now)&&q.day==21&&q.Claim(3,now)==0,"clock rollback cannot reopen claims");
            const string key="LittleDepths.DailyRulesSmokeOnly";
            try
            {
                PlayerPrefs.SetString(key,"{\"coins\":321,\"totalFish\":500,\"totalCasts\":200,\"deepest\":60}");
                var p=GameProgress.Load(key);
                Require(p.coins==321&&p.dailyQuests.Value(0)==0&&p.dailyQuests.Value(1)==0&&p.dailyQuests.Value(3)==0,"old saves migrate without treating lifetime totals as today's progress");
                for(int i=0;i<5;i++)p.dailyQuests.RecordCast(GameProgress.Now);
                p.dailyQuests.Claim(3,GameProgress.Now);p.prizeLastCard=4;p.prizeLastReward=125;p.prizeReadyAt=GameProgress.Now+3600;p.Save(key);
                var loaded=GameProgress.Load(key);
                Require(loaded.dailyQuests.casts==5&&loaded.dailyQuests.Claim(3,GameProgress.Now)==0,"daily claims and progress survive reload");
                Require(loaded.prizeLastCard==4&&loaded.prizeLastReward==125,"chosen event card and reward survive reload");
            }
            finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
            Debug.Log("DAILY_RULES_OK: Migration, daily counters, thresholds, unique species, payout, duplicate claims, persistence, midnight and rollback passed.");
        }
        public static Button Cross(FishingGame game)
        {
            var buttons=game.hud.modalLayer.GetComponentsInChildren<Button>().Where(b=>b.name=="Close window").ToArray();
            Require(buttons.Length==1,"each active window has exactly one close button");
            var button=buttons[0];var rect=(RectTransform)button.transform;var panel=(RectTransform)rect.parent;
            Require(button.image.sprite&&button.image.sprite.name=="ref_icon_close"&&button.image.preserveAspect,"close uses the cross sprite");
            Require(rect.anchoredPosition.x>0&&rect.anchoredPosition.y>0&&rect.anchoredPosition.x+rect.rect.width/2<=panel.rect.width/2&&rect.anchoredPosition.y+rect.rect.height/2<=panel.rect.height/2,"cross is completely inside top-right corner");
            Require(!game.hud.modalLayer.GetComponentsInChildren<Button>().Any(b=>b.name=="Back to lake"),"old bottom home close button is absent");
            return button;
        }
        public static void Close(FishingGame game)
        {
            Cross(game).onClick.Invoke();Require(!game.hud.IsModalOpen,"cross actually dismisses the window");
        }
        public static void Cards(FishingGame game,bool revealed)
        {
            Cross(game);
            var faces=game.hud.modalLayer.GetComponentsInChildren<Image>().Where(i=>i.enabled&&i.sprite&&(i.sprite.name=="ref_prize_back"||i.sprite.name=="ref_prize_front")).ToArray();
            Require(faces.Length==9,"event has exactly nine separate card faces");
            Require(faces.Count(i=>i.sprite.name=="ref_prize_front")== (revealed?1:0),"only selected card displays its front");
            Require(!faces.Any(i=>i.GetComponentsInChildren<Image>().Any(c=>c.name=="ref_white_patch")),"reward text has no rectangular patch over its border");
            var board=new Texture2D(2,2);board.LoadImage(File.ReadAllBytes("Assets/LittleDepths/Art/Provided/ref_prizes.png"));
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {
                Color color=board.GetPixel((int)(board.width*(89+x*72.5f)/463),(int)(board.height*(1-(111+y*72)/308f)));
                Require(color.b>color.r&&color.a>.99f,"event background contains no painted yellow card under the live cards");
            }
            UnityEngine.Object.DestroyImmediate(board);
            var front=new Texture2D(2,2);front.LoadImage(File.ReadAllBytes("Assets/LittleDepths/Art/Provided/ref_prize_front.png"));
            foreach(float x in new[]{.25f,.5f,.75f})
            {
                Color color=front.GetPixel((int)(front.width*x),(int)(front.height*.26f));
                Require(color.a>.99f&&color.r>.94f&&color.g>.94f&&color.b>.94f,"reward face has a clean opaque white amount area");
            }
            UnityEngine.Object.DestroyImmediate(front);
            Debug.Log("EVENT_CARDS_OK: One face per slot, clean background and reward text; revealed="+revealed);
        }
    }
}

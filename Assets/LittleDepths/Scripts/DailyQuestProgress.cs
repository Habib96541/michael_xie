using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleDepths
{
    [Serializable]
    public class DailyQuestProgress
    {
        public const int Count=4;
        public static readonly string[] Titles={"Catch 10 fish","Reach 15m","Catch 5 species","Complete 5 casts"};
        public static readonly int[] Targets={10,15,5,5};
        public static readonly int[] Rewards={100,150,125,100};
        public long day=-1;
        public int fish,casts;
        public float deepest;
        public List<string> species=new List<string>();
        public List<int> claimed=new List<int>();

        public bool EnsureDay(long now)
        {
            species??=new List<string>();claimed??=new List<int>();
            long current=Math.Max(0,now)/86400;
            // A clock rollback cannot reopen yesterday's reward claims.
            if(current<=day)return false;
            day=current;fish=casts=0;deepest=0;species.Clear();claimed.Clear();return true;
        }
        public void RecordCast(long now){EnsureDay(now);casts++;}
        public void RecordDepth(float meters,long now){EnsureDay(now);deepest=Mathf.Max(deepest,meters);}
        public void RecordFish(string id,long now)
        {
            EnsureDay(now);if(string.IsNullOrEmpty(id))return;
            fish++;if(!species.Contains(id))species.Add(id);
        }
        public int Value(int index)=>index switch{0=>fish,1=>Mathf.FloorToInt(deepest),2=>species.Count,3=>casts,_=>0};
        public bool Complete(int index)=>index>=0&&index<Count&&Value(index)>=Targets[index];
        public int Claim(int index,long now)
        {
            EnsureDay(now);
            if(!Complete(index)||claimed.Contains(index))return 0;
            claimed.Add(index);return Rewards[index];
        }
        public long SecondsToReset(long now)=>Math.Max(0,(day+1)*86400-now);
    }
}

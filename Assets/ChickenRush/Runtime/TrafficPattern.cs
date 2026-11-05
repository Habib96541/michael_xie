using System;
using UnityEngine;

namespace ChickenRush
{
    public enum VehicleKind { Compact, Taxi, Van, Express }
    public struct VehiclePlan
    {
        public VehicleKind kind;
        public float width,length,offset;
    }

    // Each lane is a periodic stream with variable headways. A common speed
    // within a lane preserves those headways; lanes have different speeds.
    // The last gap is reserved, so density never removes every crossing window.
    public sealed class TrafficPattern
    {
        public const float TrackLength=26;
        public const float PlayerHalfWidth=.23f,PlayerHalfHeight=.29f,PlayerCenterY=.38f,PlayerCenterX=.06f;
        public readonly VehiclePlan[] vehicles;
        public readonly float speed,phase,guaranteedGap;
        public readonly int direction;
        public float CycleSeconds => TrackLength/speed;

        public TrafficPattern(LaneTuning tuning,int seed)
        {
            var random=new System.Random(seed+tuning.lane*7919);
            direction=tuning.lane%2==0?1:-1;
            speed=tuning.speed*(tuning.express?1.16f:1);
            phase=(float)random.NextDouble()*TrackLength;
            guaranteedGap=tuning.guaranteedGap;
            vehicles=new VehiclePlan[tuning.vehicleCount];
            float totalLength=0;
            for(int i=0;i<vehicles.Length;i++)
            {
                var kind=tuning.express?VehicleKind.Express:(VehicleKind)((i+tuning.lane)%3);
                float length=kind==VehicleKind.Van?1.66f:kind==VehicleKind.Express?1.08f:1.23f;
                float width=kind==VehicleKind.Van?.75f:kind==VehicleKind.Express?.60f:.66f;
                vehicles[i]=new VehiclePlan{kind=kind,length=length,width=width};totalLength+=length;
            }
            float budget=TrackLength-totalLength-guaranteedGap*speed;
            if(budget<.2f*(vehicles.Length-1))throw new ArgumentException("Traffic tier leaves insufficient headway for its vehicle count and speed.");
            var gaps=new float[vehicles.Length-1];float sum=0;
            for(int i=0;i<gaps.Length;i++)
            {
                gaps[i]=Mathf.Lerp(tuning.minGap,tuning.maxGap,(float)random.NextDouble())*speed;
                sum+=gaps[i];
            }
            float scale=Mathf.Min(1,budget/Mathf.Max(sum,.001f));
            float offset=0;
            for(int i=0;i<vehicles.Length;i++)
            {
                var car=vehicles[i];car.offset=offset;vehicles[i]=car;
                if(i<vehicles.Length-1)offset+=(car.length+vehicles[i+1].length)*.5f+gaps[i]*scale;
            }
        }
        public float Position(int car,double time)
        {
            double distance=phase+vehicles[car].offset+time*speed;
            float wrapped=(float)(distance-Math.Floor(distance/TrackLength)*TrackLength)-TrackLength*.5f;
            return direction*wrapped;
        }
        public float GapAfter(int car)
        {
            int next=(car+1)%vehicles.Length;
            float span=next==0?TrackLength-vehicles[car].offset:vehicles[next].offset-vehicles[car].offset;
            return (span-(vehicles[car].length+vehicles[next].length)*.5f)/speed;
        }
        public static float HopProgress(float seconds,float duration)
        {
            float t=Mathf.Clamp01((seconds-.04f)/(duration-.08f));return t*t*(3-2*t);
        }
        public static bool SweptHit(Vector2 from,Vector2 to,Vector2 half)
        {
            float enter=0,exit=1;
            return Slab(from.x,to.x-from.x,half.x,ref enter,ref exit)&&Slab(from.y,to.y-from.y,half.y,ref enter,ref exit);
        }
        static bool Slab(float origin,float delta,float half,ref float enter,ref float exit)
        {
            if(Mathf.Abs(delta)<.00001f)return Mathf.Abs(origin)<=half;
            float a=(-half-origin)/delta,b=(half-origin)/delta;
            if(a>b){float swap=a;a=b;b=swap;}enter=Mathf.Max(enter,a);exit=Mathf.Min(exit,b);return enter<=exit;
        }
    }
}

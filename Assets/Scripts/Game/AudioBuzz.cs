using BullRide.Core;
using UnityEngine;

namespace BullRide
{
    public sealed class AudioBuzz : MonoBehaviour
    {
        AudioSource crowd, effects, hooves;
        AudioClip bull, gate, hoofA, hoofB, whoosh, impact, whistle, cheer, reward;
        double nextSnort;
        bool alternateHoof;

        public void Build()
        {
            crowd = MakeSource();
            crowd.loop = true;
            crowd.clip = Load("Crowd");
            effects = MakeSource();
            hooves = MakeSource();
            bull = Load("Bull");
            gate = Load("Gate");
            hoofA = Load("HoofA");
            hoofB = Load("HoofB");
            whoosh = Load("Whoosh");
            impact = Load("Impact");
            whistle = Load("Whistle");
            cheer = Load("Cheer");
            reward = Load("Reward");
        }

        AudioSource MakeSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        static AudioClip Load(string name) => Resources.Load<AudioClip>("RodeoAudio/" + name);

        static void Cue(AudioSource source, AudioClip clip, float volume)
        {
            if (clip != null) source.PlayOneShot(clip, volume);
        }

        public void StartRide()
        {
            crowd.volume = .16f;
            if (crowd.clip != null) crowd.Play();
            nextSnort = .8;
            Cue(effects, bull, .60f);
        }

        public void ResetRide()
        {
            crowd.Stop();
            effects.Stop();
            hooves.Stop();
        }

        public void Tick(RideSim sim, float dt)
        {
            float level = sim.Phase == RidePhase.Ride ? .23f :
                sim.Phase == RidePhase.Over ? .065f : .16f;
            crowd.volume = Mathf.MoveTowards(crowd.volume, level, .32f * dt);
            if (sim.Phase != RidePhase.Ride || sim.RideTime < nextSnort ||
                sim.Pose.Hop + sim.Pose.Kick * .17f < .31f) return;
            Cue(effects, bull, .66f);
            nextSnort = sim.RideTime + 2.6;
        }

        public void GateOpen() => Cue(effects, gate, .62f);

        public void HoofContact(bool landed)
        {
            hooves.pitch = Random.Range(.88f, 1.07f);
            Cue(hooves, alternateHoof ? hoofA : hoofB, landed ? .78f : .46f);
            alternateHoof = !alternateHoof;
        }

        public void Throw() => Cue(effects, whoosh, .70f);

        public void Land()
        {
            Cue(effects, impact, .82f);
            Cue(effects, cheer, .30f);
        }

        public void Finish()
        {
            Cue(effects, whistle, .60f);
            Cue(effects, cheer, .56f);
        }

        public void Reward() => Cue(effects, reward, .73f);
    }
}

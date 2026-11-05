using System.Collections.Generic;
using UnityEngine;

namespace LittleDepths
{
    public class FishingWorld : MonoBehaviour
    {
        public Transform fishRoot, effectsRoot, boat;
        public Sprite bubbleSprite, sparkleSprite;
        public AnglerRig angler;
        public List<FishActor> Fish { get; } = new List<FishActor>();
        readonly List<Mote> motes = new List<Mote>();
        class Mote { public Transform t; public SpriteRenderer r; public Vector3 velocity; public float life, maxLife, size; }

        public void Populate(FishingConfig config, GameProgress progress, bool full)
        {
            foreach (var f in Fish) if (f && !f.caught) Destroy(f.gameObject);
            Fish.Clear();
            float max = full ? config.MaxDepth(progress) + 1 : 7;
            for (float depth = .8f; depth < max; depth += .58f)
            {
                int count = UnityEngine.Random.Range(1, 3);
                for (int j = 0; j < count; j++)
                {
                    FishSpecies type = PickSpecies(config, depth);
                    bool trash = full && UnityEngine.Random.value < .035f;
                    var obj = new GameObject(trash ? "Discarded boot" : type.displayName);
                    obj.transform.SetParent(fishRoot);
                    var fish = obj.AddComponent<FishActor>();
                    float x = count > 1 ? (j == 0 ? UnityEngine.Random.Range(-2.5f,-.25f) : UnityEngine.Random.Range(.25f,2.5f)) : UnityEngine.Random.Range(-2.5f,2.5f);
                    fish.Initialize(type, trash, trash ? config.trashSprite : type.sprite,
                        new Vector2(x, config.surfaceY - depth * config.metersToWorld), UnityEngine.Random.value > .5f ? 1 : -1);
                    Fish.Add(fish);
                }
            }
        }

        public FishSpecies PickSpecies(FishingConfig config, float depth)
        {
            float sum = 0;
            foreach (var s in config.species) if (depth >= s.minDepth && depth <= s.maxDepth) sum += s.weight;
            float roll = UnityEngine.Random.value * sum;
            foreach (var s in config.species)
            {
                if (depth < s.minDepth || depth > s.maxDepth) continue;
                roll -= s.weight; if (roll <= 0) return s;
            }
            return config.species[0];
        }

        public void Animate(float dt, float time)
        {
            foreach (var f in Fish) if (f) f.Swim(dt, time, 2.65f);
            if (boat) { boat.position = new Vector3(boat.position.x, 2.1f + Mathf.Sin(time * 1.2f) * .035f, 0); boat.rotation = Quaternion.Euler(0,0,Mathf.Sin(time*.9f)*.7f); }
            for (int i = motes.Count - 1; i >= 0; i--)
            {
                var m = motes[i]; m.life -= dt;
                if (m.life <= 0 || !m.t) { if (m.t) Destroy(m.t.gameObject); motes.RemoveAt(i); continue; }
                m.t.position += m.velocity * dt;
                Color c = m.r.color; c.a = Mathf.Min(1, m.life / (m.maxLife * .5f)); m.r.color = c;
                m.t.localScale = Vector3.one * m.size * Mathf.Lerp(.2f,1,m.life/m.maxLife);
            }
        }

        void SpawnMote(Vector3 at, Vector3 velocity, Sprite sprite, Color color, float size, float life)
        {
            var go = new GameObject("Water sparkle"); go.transform.SetParent(effectsRoot);
            go.transform.position = at; go.transform.localScale = Vector3.one * size;
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.color = color; r.sortingOrder = 40;
            motes.Add(new Mote { t=go.transform, r=r, velocity=velocity, size=size, life=life, maxLife=life });
        }
        public void Burst(Vector3 at, Color color, int count)
        {
            for (int i=0;i<count;i++) SpawnMote(at, (Vector3)UnityEngine.Random.insideUnitCircle*1.1f, sparkleSprite, color, UnityEngine.Random.Range(.055f,.14f), UnityEngine.Random.Range(.4f,.85f));
        }
        public void Bubble(Vector3 at) => SpawnMote(at, new Vector3(UnityEngine.Random.Range(-.1f,.1f),.5f,0), bubbleSprite, new Color(.8f,.98f,1,.65f), UnityEngine.Random.Range(.035f,.09f), 1.3f);
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace BottleFlip
{
    /// <summary>
    /// Single entry point. Constructs the entire game in code so the project has no
    /// fragile prefab/scene references to break.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 22f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Pal.Hex("10142C");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Pal.Hex("0A0C20");
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 26f;
            Time.timeScale = 1f;

            Build();
        }

        void Build()
        {
            var world = new GameObject("World").transform;
            var camRig = BuildCamera();
            BuildLights();
            BuildEnvironment(world);

            var bottleGo = new GameObject("Bottle");
            bottleGo.transform.SetParent(world, false);
            var rig = bottleGo.AddComponent<BottleRig>();
            rig.Build();

            var fx = new GameObject("FX").AddComponent<Fx>();
            fx.Build();

            var sfx = new GameObject("Audio").AddComponent<Sfx>();
            sfx.Build();

            var ui = new GameObject("UI").AddComponent<UIRoot>();
            ui.WorldCamera = camRig.Cam;
            ui.Build();

            var esGo = new GameObject("EventSystem");
            var es = esGo.AddComponent<EventSystem>();
            es.sendNavigationEvents = false;   // no keyboard "submit" clicks firing buttons twice
            esGo.AddComponent<StandaloneInputModule>();

            var gm = new GameObject("Game").AddComponent<GameManager>();
            gm.Ui = ui;
            gm.Rig = rig;
            gm.Fx = fx;
            gm.Sfx = sfx;
            gm.Cam = camRig;
            gm.Init();
        }

        GameCamera BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.InkDeep;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 90f;
            cam.allowMSAA = true;
            cam.allowHDR = false;

            camGo.AddComponent<AudioListener>();

            var rig = camGo.AddComponent<GameCamera>();
            rig.Cam = cam;
            rig.Build(33f);
            return rig;
        }

        void BuildLights()
        {
            var keyGo = new GameObject("Key Light");
            keyGo.transform.SetParent(transform, false);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = Pal.Hex("FFF3E2");
            key.intensity = 1.1f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.9f;
            keyGo.transform.rotation = Quaternion.Euler(36f, -28f, 0f);

            var rimGo = new GameObject("Rim Light");
            rimGo.transform.SetParent(transform, false);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Point;
            rim.color = Pal.Hex("8FE9FF");
            rim.intensity = 5.0f;
            rim.range = 9f;
            rimGo.transform.position = new Vector3(1.7f, 2.9f, 2.5f);

            var fillGo = new GameObject("Fill Light");
            fillGo.transform.SetParent(transform, false);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = Pal.Hex("FFB27A");
            fill.intensity = 1.6f;
            fill.range = 9f;
            fillGo.transform.position = new Vector3(-2.3f, 1.5f, -2.6f);
        }

        void BuildEnvironment(Transform parent)
        {
            // Sky gradient backdrop.
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(parent, false);
            backdrop.transform.position = new Vector3(0f, 3.5f, 16f);
            backdrop.transform.localScale = new Vector3(70f, 44f, 1f);
            backdrop.AddComponent<MeshFilter>().sharedMesh = Art.QuadMesh(1f, "BackdropQuad");
            var bmr = backdrop.AddComponent<MeshRenderer>();
            bmr.sharedMaterial = Art.Unlit(Art.Gradient(Pal.Hex("0A0C20"), Pal.Hex("1A1E44")), Color.white, false, "Backdrop");
            bmr.shadowCastingMode = ShadowCastingMode.Off;
            bmr.receiveShadows = false;

            // Warm halo behind the bottle.
            var halo = new GameObject("Halo");
            halo.transform.SetParent(parent, false);
            halo.transform.position = new Vector3(0f, 1.45f, 6.2f);
            halo.transform.localScale = new Vector3(10f, 10f, 1f);
            halo.AddComponent<MeshFilter>().sharedMesh = Art.QuadMesh(1f, "HaloQuad");
            var hmr = halo.AddComponent<MeshRenderer>();
            hmr.sharedMaterial = Art.Unlit(Art.Glow, Pal.Hex("FFB066").Alpha(0.06f), true, "HaloMat");
            hmr.shadowCastingMode = ShadowCastingMode.Off;
            hmr.receiveShadows = false;

            // Cool counter-halo for colour depth.
            var halo2 = new GameObject("HaloCool");
            halo2.transform.SetParent(parent, false);
            halo2.transform.position = new Vector3(-3.2f, 2.6f, 5.0f);
            halo2.transform.localScale = new Vector3(8f, 8f, 1f);
            halo2.AddComponent<MeshFilter>().sharedMesh = Art.QuadMesh(1f, "HaloQuad2");
            var hmr2 = halo2.AddComponent<MeshRenderer>();
            hmr2.sharedMaterial = Art.Unlit(Art.Glow, Pal.Hex("3B7BFF").Alpha(0.035f), true, "HaloCoolMat");
            hmr2.shadowCastingMode = ShadowCastingMode.Off;
            hmr2.receiveShadows = false;

            // Floor.
            var floor = new GameObject("Floor");
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -0.04f, 6f);
            floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(70f, 70f, 1f);
            floor.AddComponent<MeshFilter>().sharedMesh = Art.QuadMesh(1f, "FloorQuad");
            var fmr = floor.AddComponent<MeshRenderer>();
            fmr.sharedMaterial = Art.Textured(Art.White, Pal.Hex("050610"), 0.12f);
            fmr.receiveShadows = true;

            // Stage disc the bottle stands on.
            var stage = new GameObject("Stage");
            stage.transform.SetParent(parent, false);
            stage.transform.position = new Vector3(0f, -0.1f, 0f);
            stage.AddComponent<MeshFilter>().sharedMesh = Shapes.Cylinder(1.5f, 0.1f, 0.02f, 64, "StageMesh");
            var smr = stage.AddComponent<MeshRenderer>();
            smr.sharedMaterial = Art.Glossy(Pal.Hex("111533"), 0.1f, 0.34f, "StageMat");
            smr.receiveShadows = true;

            // Warm light pool on the stage.
            var pool = new GameObject("StagePool");
            pool.transform.SetParent(parent, false);
            pool.transform.position = new Vector3(0f, 0.004f, 0f);
            pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.localScale = new Vector3(4.4f, 4.4f, 1f);
            pool.AddComponent<MeshFilter>().sharedMesh = Art.QuadMesh(1f, "PoolQuad");
            var pmr = pool.AddComponent<MeshRenderer>();
            pmr.sharedMaterial = Art.Unlit(Art.Glow, Pal.Hex("FFC64B").Alpha(0.12f), true, "PoolMat");
            pmr.shadowCastingMode = ShadowCastingMode.Off;

            // Glowing rim around the stage edge.
            var rim = new GameObject("StageRim");
            rim.transform.SetParent(parent, false);
            rim.transform.position = new Vector3(0f, 0.012f, 0f);
            rim.AddComponent<MeshFilter>().sharedMesh = Shapes.Ring(1.51f, 0.05f, 72, "RimMesh");
            var rmr = rim.AddComponent<MeshRenderer>();
            rmr.sharedMaterial = Art.Emissive(Pal.Amber * 3.0f);
            rmr.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}

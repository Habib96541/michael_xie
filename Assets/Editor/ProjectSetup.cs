using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click project setup: materials, the arena scene, rule validation and a
/// responsive WebGL build. Everything is also callable from Unity batch mode.
/// </summary>
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/ChestShuffle3D.unity";
    const string MaterialFolder = "Assets/Resources/Materials";

    [MenuItem("Chest Shuffle/Set Up Scene", priority = 0)]
    public static void Setup()
    {
        BuildScene();
        Validate();
        AssetDatabase.SaveAssets();
        Debug.Log("Chest Shuffle 3D setup complete: " + ScenePath);
    }

    [MenuItem("Chest Shuffle/Validate Rules", priority = 20)]
    public static void Validate()
    {
        Check(MultiplierTable.ChestCount == 3, "Three chests");
        Check(MultiplierTable.Values.Length == MultiplierTable.Weights.Length, "Every multiplier has a weight");
        Check(MultiplierTable.Values[0] == 0.1f, "Lowest multiplier is 0.1x");
        Check(MultiplierTable.Values[MultiplierTable.Values.Length - 1] == 10f, "Highest multiplier is 10x");

        for (int i = 0; i < MultiplierTable.Values.Length; i++)
        {
            float value = MultiplierTable.Values[i];
            Check(value >= MultiplierTable.MinMultiplier && value <= MultiplierTable.MaxMultiplier,
                "Multiplier in range: " + value);
            Check(MultiplierTable.Weights[i] > 0, "Positive weight: " + value);
        }

        var rng = new System.Random(9321);
        var seen = new System.Collections.Generic.HashSet<float>();
        int wildcardBelowOne = 0;
        int wildcardAtLeastOne = 0;
        for (int round = 0; round < 3000; round++)
        {
            float[] roll = MultiplierTable.RollDistinct(rng);
            Check(roll.Length == 3, "Three multipliers per round");
            Check(roll[0] != roll[1] && roll[1] != roll[2] && roll[0] != roll[2], "Multipliers are distinct");
            bool hasLoss = roll[0] < 1f || roll[1] < 1f || roll[2] < 1f;
            bool hasProfit = roll[0] > 1f || roll[1] > 1f || roll[2] > 1f;
            Check(hasLoss, "At least one chest is below 1x each round");
            Check(hasProfit, "At least one chest is above 1x each round");
            float wildcard = -1f;
            for (int i = 0; i < roll.Length; i++)
            {
                Check(roll[i] >= 0f && roll[i] <= 10f, "Rolled multiplier in range");
                bool onLadder = false;
                for (int valueIndex = 0; valueIndex < MultiplierTable.Values.Length; valueIndex++)
                    if (roll[i] == MultiplierTable.Values[valueIndex])
                    {
                        seen.Add(MultiplierTable.Values[valueIndex]);
                        onLadder = true;
                    }
                if (!onLadder) wildcard = roll[i];
            }
            if (wildcard >= 0f && wildcard < 1f) wildcardBelowOne++;
            else if (wildcard >= 1f) wildcardAtLeastOne++;
        }
        Check(wildcardBelowOne > 1200 && wildcardBelowOne < 1800 && wildcardAtLeastOne > 1200 && wildcardAtLeastOne < 1800,
            "Wildcard is split evenly between 0-1x and 1-10x");
        bool allWeightedValuesSeen = true;
        for (int i = 0; i < MultiplierTable.Values.Length; i++)
            if (MultiplierTable.Values[i] != 1f && !seen.Contains(MultiplierTable.Values[i])) allWeightedValuesSeen = false;
        Check(allWeightedValuesSeen, "Every weighted loss/profit ladder multiplier is reachable");

        Check(MultiplierTable.Payout(100, 10f) == 1000, "10x payout");
        Check(MultiplierTable.Payout(100, 0.5f) == 50, "0.5x payout");
        Check(MultiplierTable.Payout(10, 0.1f) == 1, "0.1x payout");
        Check(MultiplierTable.Payout(20, 1.5f) == 30, "1.5x payout");
        Check(MultiplierTable.Format(0.1f) == "0.1\u00D7", "Format 0.1x");
        Check(MultiplierTable.Format(10f) == "10\u00D7", "Format 10x");
        Check(MultiplierTable.Format(1f) == "1\u00D7", "Format 1x");

        for (int i = 0; i < ChestShuffleGame.StakeOptions.Length; i++)
        {
            Check(ChestShuffleGame.StakeOptions[i] > 0, "Positive stake option");
            if (i > 0) Check(ChestShuffleGame.StakeOptions[i] > ChestShuffleGame.StakeOptions[i - 1], "Stake options ascend");
        }
        Check(ChestShuffleGame.StakeOptions[0] == 10, "Smallest stake is 10");
        Check(ChestShuffleGame.StakeOptions[0] <= ChestShuffleGame.StartingBalance, "Bankroll covers the smallest stake");

        Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null, "Scene exists at " + ScenePath);
        Check(AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/ArchivoBlack-Regular.ttf") != null, "Display font present");
        Check(AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Barlow-SemiBold.ttf") != null, "Body font present");
        for (int i = 0; i < MaterialLibrary.Keys.Length; i++)
            Check(AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + MaterialLibrary.Keys[i] + ".mat") != null,
                "Material present: " + MaterialLibrary.Keys[i]);

        Debug.Log("Chest Shuffle 3D rule checks passed.");
    }

    public static void BuildScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EnsureMaterials();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("#2B2142");
        RenderSettings.ambientEquatorColor = Hex("#181023");
        RenderSettings.ambientGroundColor = Hex("#07050B");
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Hex("#0B0714");
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.reflectionIntensity = 0.7f;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("#07050C");
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 80f;
        camera.fieldOfView = 38f;
        camera.allowHDR = true;
        camera.transform.position = new Vector3(0f, 4.75f, -8.2f);
        camera.transform.LookAt(new Vector3(0f, 0.55f, 0.45f));
        cameraObject.AddComponent<AudioListener>();

        BuildLights();
        BuildArena();

        var gameObject = new GameObject("Chest Shuffle");
        gameObject.AddComponent<ChestShuffleGame>();
        gameObject.AddComponent<ChestHud>();

        QualitySettings.shadowDistance = 28f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "Chest Shuffle Studio";
        PlayerSettings.productName = "Chest Shuffle 3D";
        PlayerSettings.defaultScreenWidth = 1440;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;

        AssetDatabase.SaveAssets();
        Debug.Log("Chest Shuffle 3D scene built at " + ScenePath);
    }

    static void BuildLights()
    {
        var keyObject = new GameObject("Key Light");
        var key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = Hex("#FFE3B9");
        key.intensity = 1.2f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.82f;
        key.shadowBias = 0.02f;
        keyObject.transform.rotation = Quaternion.Euler(48f, -34f, 0f);

        for (int i = 0; i < MultiplierTable.ChestCount; i++)
        {
            float x = (i - 1) * 2.75f;
            var spotObject = new GameObject("Lamp Spot " + (i + 1));
            spotObject.transform.position = new Vector3(x, 5.7f, 0.5f);
            spotObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var spot = spotObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = Hex("#FFD79A");
            spot.intensity = 3.2f;
            spot.range = 11f;
            spot.spotAngle = 62f;
            spot.shadows = LightShadows.None;
        }

        var rimObject = new GameObject("Purple Rim");
        rimObject.transform.position = new Vector3(0f, 3.6f, 5.2f);
        var rim = rimObject.AddComponent<Light>();
        rim.type = LightType.Point;
        rim.color = Hex("#8B5BFF");
        rim.intensity = 3.4f;
        rim.range = 17f;
        rim.shadows = LightShadows.None;

        var fillObject = new GameObject("Front Fill");
        fillObject.transform.position = new Vector3(0f, 1.7f, -4.8f);
        var fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = Hex("#FFC98A");
        fill.intensity = 1.8f;
        fill.range = 15f;
        fill.shadows = LightShadows.None;
    }

    static void BuildArena()
    {
        var arena = new GameObject("Arena").transform;

        Primitive(PrimitiveType.Plane, "Floor", arena, new Vector3(0f, 0f, 0f), new Vector3(4f, 1f, 4f), "Floor");
        Primitive(PrimitiveType.Plane, "Dais", arena, new Vector3(0f, 0.012f, 0f), new Vector3(1.05f, 1f, 1.05f), "Stone");
        Ring("Outer Inlay", arena, 5.15f, 5.42f, 0.022f, "Gold");
        Ring("Inner Inlay", arena, 3.95f, 4.05f, 0.026f, "PurpleGlow");

        Primitive(PrimitiveType.Cube, "Back Wall", arena, new Vector3(0f, 5.8f, 9.2f), new Vector3(34f, 12f, 0.5f), "Wall");
        Primitive(PrimitiveType.Cube, "Wall Trim", arena, new Vector3(0f, 5.6f, 8.9f), new Vector3(30f, 0.07f, 0.08f), "Gold");
        Primitive(PrimitiveType.Cube, "Skirting", arena, new Vector3(0f, 0.16f, 8.9f), new Vector3(30f, 0.32f, 0.3f), "WoodDark");

        float[] stripX = { -7.8f, -5.4f, 5.4f, 7.8f };
        for (int i = 0; i < stripX.Length; i++)
            Primitive(PrimitiveType.Cube, "Light Strip " + i, arena, new Vector3(stripX[i], 3.3f, 8.9f),
                new Vector3(0.12f, 4.4f, 0.08f), "GoldGlow");

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 10.8f;
            Primitive(PrimitiveType.Cube, "Column", arena, new Vector3(x, 4.5f, 7.6f), new Vector3(1.3f, 9f, 1.3f), "WoodDark");
            Primitive(PrimitiveType.Cube, "Column Capital", arena, new Vector3(x, 8.95f, 7.6f), new Vector3(1.7f, 0.2f, 1.7f), "Gold");
            Primitive(PrimitiveType.Cube, "Column Base", arena, new Vector3(x, 0.14f, 7.6f), new Vector3(1.8f, 0.28f, 1.8f), "Gold");
        }

        for (int i = 0; i < MultiplierTable.ChestCount; i++)
        {
            float x = (i - 1) * 2.75f;
            Primitive(PrimitiveType.Cylinder, "Pedestal " + (i + 1), arena, new Vector3(x, 0.15f, 0f),
                new Vector3(2.16f, 0.15f, 2.16f), "Stone");
            RingAt("Pedestal Rim " + (i + 1), arena, new Vector3(x, 0.305f, 0f), 0.98f, 1.07f, "Gold");

            Primitive(PrimitiveType.Cube, "Lamp Rod " + (i + 1), arena, new Vector3(x, 6.9f, 0.5f),
                new Vector3(0.06f, 3.4f, 0.06f), "Iron");
            Primitive(PrimitiveType.Cylinder, "Lamp Shade " + (i + 1), arena, new Vector3(x, 5.92f, 0.5f),
                new Vector3(0.62f, 0.16f, 0.62f), "Iron");
            Primitive(PrimitiveType.Cylinder, "Lamp Bulb " + (i + 1), arena, new Vector3(x, 5.76f, 0.5f),
                new Vector3(0.34f, 0.12f, 0.34f), "GoldGlow");
        }

        BuildDust(arena);
    }

    static void BuildDust(Transform arena)
    {
        var dustObject = new GameObject("Dust Motes");
        dustObject.transform.SetParent(arena, false);
        dustObject.transform.localPosition = new Vector3(0f, 2.2f, 1.8f);

        var particles = dustObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.055f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.84f, 0.55f, 0.5f), new Color(0.72f, 0.52f, 1f, 0.42f));
        main.maxParticles = 110;
        main.gravityModifier = -0.004f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 11f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(11f, 3.6f, 4.5f);

        var renderer = dustObject.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Mat("Dust");
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        particles.Stop();
    }

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position,
        Vector3 scaleValue, string materialKey, bool keepCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (!keepCollider)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scaleValue;
        go.GetComponent<Renderer>().sharedMaterial = Mat(materialKey);
        return go;
    }

    static void Ring(string name, Transform parent, float inner, float outer, float y, string materialKey)
    {
        RingAt(name, parent, new Vector3(0f, y, 0f), inner, outer, materialKey);
    }

    static void RingAt(string name, Transform parent, Vector3 position, float inner, float outer, string materialKey)
    {
        if (inner <= 0f) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = MeshUtils.Ring(inner, outer, 96);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Mat(materialKey);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static Material Mat(string key)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + key + ".mat");
        return material != null ? material : MaterialLibrary.BuildDefault(key);
    }

    static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Resources", "Materials");

        for (int i = 0; i < MaterialLibrary.Keys.Length; i++)
        {
            string key = MaterialLibrary.Keys[i];
            string path = MaterialFolder + "/" + key + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;

            Material material = MaterialLibrary.BuildDefault(key);
            material.name = key;
            AssetDatabase.CreateAsset(material, path);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Chest Shuffle/Build WebGL", priority = 40)]
    public static void BuildWebGL()
    {
        Validate();

        PlayerSettings.stripEngineCode = false;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.memorySize = 256;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("WebGL build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors)");

        PatchResponsivePage(options.locationPathName);
        BuildStandaloneHtml(options.locationPathName);
        Debug.Log("WebGL build complete: " + report.summary.outputPath + " (" + report.summary.totalSize + " bytes)");
    }

    [MenuItem("Chest Shuffle/Export iOS for Device", priority = 41)]
    public static void ExportIOSDevice() => ExportIOS(iOSSdkVersion.DeviceSDK, "Build/iOS");

    [MenuItem("Chest Shuffle/Export iOS for Simulator", priority = 42)]
    public static void ExportIOSSimulator() => ExportIOS(iOSSdkVersion.SimulatorSDK, "Build/iOS-Simulator");

    static void ExportIOS(iOSSdkVersion sdk, string outputPath)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            throw new InvalidOperationException("Install iOS Build Support for Unity " + Application.unityVersion + ".");

        Validate();
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        iOSSdkVersion previousSdk = PlayerSettings.iOS.sdkVersion;
        try
        {
            PlayerSettings.iOS.sdkVersion = sdk;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.iOS
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("iOS export failed: " + report.summary.result);
            Debug.Log("Xcode project exported to " + report.summary.outputPath);
        }
        finally
        {
            PlayerSettings.iOS.sdkVersion = previousSdk;
        }
    }

    static void BuildStandaloneHtml(string outputPath)
    {
        string buildPath = Path.Combine(outputPath, "Build");
        string loader = File.ReadAllText(Path.Combine(buildPath, "WebGL.loader.js"));
        string framework = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(buildPath, "WebGL.framework.js")));
        string data = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(buildPath, "WebGL.data")));
        string wasm = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(buildPath, "WebGL.wasm")));

        var html = new StringBuilder(data.Length + wasm.Length + framework.Length + loader.Length + 8192);
        html.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no,viewport-fit=cover\"><meta name=\"theme-color\" content=\"#07050c\"><title>Chest Shuffle 3D</title>");
        html.AppendLine("<style>html,body{width:100%;height:100%;height:100dvh;margin:0;overflow:hidden;background:#07050c;touch-action:none;overscroll-behavior:none;-webkit-user-select:none;user-select:none;-webkit-touch-callout:none}#unity-container{position:fixed;left:var(--viewport-left,0px);top:var(--viewport-top,0px);width:var(--viewport-width,100vw);height:var(--viewport-height,100dvh);overflow:hidden}#unity-canvas{position:absolute;left:env(safe-area-inset-left,0px);top:env(safe-area-inset-top,0px);width:calc(100% - env(safe-area-inset-left,0px) - env(safe-area-inset-right,0px));height:calc(100% - env(safe-area-inset-top,0px) - env(safe-area-inset-bottom,0px));display:block;background:#07050c;outline:none;touch-action:none}#unity-loading-container{position:absolute;inset:0;display:flex;align-items:center;justify-content:center;background:#07050caa;backdrop-filter:blur(5px)}#unity-loading-bar{width:min(340px,70vw);color:#ffe39c;text-align:center;font:600 16px Arial,sans-serif}#unity-progress-bar-empty{height:12px;margin-top:16px;border:1px solid #c3a66d;border-radius:9px;background:#292032;overflow:hidden}#unity-progress-bar-full{height:100%;width:0;background:#ffe39c;transition:width .15s}#unity-warning{position:absolute;z-index:2;top:12px;left:50%;transform:translateX(-50%);max-width:90vw;padding:10px;border-radius:8px;background:#33273d;color:white;font:14px Arial,sans-serif;display:none}</style></head><body><div id=\"unity-container\"><canvas id=\"unity-canvas\" tabindex=\"-1\"></canvas><div id=\"unity-loading-container\"><div id=\"unity-loading-bar\">Loading Chest Shuffle 3D<div id=\"unity-progress-bar-empty\"><div id=\"unity-progress-bar-full\"></div></div></div></div></div><div id=\"unity-warning\"></div>");
        html.Append("<script id=\"unity-framework\" type=\"application/octet-stream\">").Append(framework).AppendLine("</script>");
        html.Append("<script id=\"unity-data\" type=\"application/octet-stream\">").Append(data).AppendLine("</script>");
        html.Append("<script id=\"unity-wasm\" type=\"application/octet-stream\">").Append(wasm).AppendLine("</script>");
        html.Append("<script>").AppendLine(loader).AppendLine("</script>");
        html.AppendLine(@"<script>
function embeddedBlob(id,type){const encoded=document.getElementById(id).textContent.trim();const size=768*1024;const parts=[];for(let p=0;p<encoded.length;p+=size){const binary=atob(encoded.slice(p,p+size));const bytes=new Uint8Array(binary.length);for(let i=0;i<binary.length;i++)bytes[i]=binary.charCodeAt(i);parts.push(bytes)}return URL.createObjectURL(new Blob(parts,{type}))}
const canvas=document.getElementById('unity-canvas'),loading=document.getElementById('unity-loading-container'),progress=document.getElementById('unity-progress-bar-full');
function updateViewport(){const v=window.visualViewport;const root=document.documentElement;root.style.setProperty('--viewport-width',(v?v.width:window.innerWidth)+'px');root.style.setProperty('--viewport-height',(v?v.height:window.innerHeight)+'px');root.style.setProperty('--viewport-left',(v?v.offsetLeft:0)+'px');root.style.setProperty('--viewport-top',(v?v.offsetTop:0)+'px')}
updateViewport();window.addEventListener('resize',updateViewport);window.addEventListener('orientationchange',updateViewport);if(window.visualViewport){window.visualViewport.addEventListener('resize',updateViewport);window.visualViewport.addEventListener('scroll',updateViewport)}
function unityShowBanner(message,type){const banner=document.getElementById('unity-warning');banner.textContent=message;banner.style.display='block';banner.style.background=type==='error'?'#8f2537':type==='warning'?'#70531c':'#33273d';if(type!=='error')setTimeout(()=>banner.style.display='none',5000)}
const frameworkUrl=embeddedBlob('unity-framework','text/javascript'),dataUrl=embeddedBlob('unity-data','application/octet-stream'),codeUrl=embeddedBlob('unity-wasm','application/wasm');
const mobile=window.matchMedia('(pointer: coarse)').matches||/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)||(navigator.maxTouchPoints>1&&/Macintosh/i.test(navigator.userAgent));
const dpr=mobile?Math.min(window.devicePixelRatio||1,1.5):(window.devicePixelRatio||1);
createUnityInstance(canvas,{dataUrl,frameworkUrl,codeUrl,devicePixelRatio:dpr,streamingAssetsUrl:'',companyName:'Chest Shuffle Studio',productName:'Chest Shuffle 3D',productVersion:'1.0',showBanner:unityShowBanner},value=>{progress.style.width=(value*100)+'%';if(value>=1)loading.style.display='none'}).catch(error=>{loading.style.display='none';unityShowBanner(String(error),'error');console.error(error)});
</script></body></html>");
        string standalonePath = Path.Combine(outputPath, "ChestShuffle3D.html");
        File.WriteAllText(standalonePath, html.ToString(), new UTF8Encoding(false));
        Debug.Log("Standalone single-file WebGL build complete: " + standalonePath + " (" + new FileInfo(standalonePath).Length + " bytes)");
    }

    static void PatchResponsivePage(string outputPath)
    {
        string indexPath = Path.Combine(outputPath, "index.html");
        if (!File.Exists(indexPath)) throw new FileNotFoundException("Missing WebGL index.html", indexPath);
        string html = File.ReadAllText(indexPath);
        html = html.Replace("fullscreenContainer.style.width = \"960px\";", "fullscreenContainer.style.width = window.innerWidth + \"px\";");
        html = html.Replace("fullscreenContainer.style.height = \"600px\";", "fullscreenContainer.style.height = window.innerHeight + \"px\";");
        html = html.Replace("<title>Unity Web Player | Chest Shuffle 3D</title>",
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover\">" +
            "<meta name=\"theme-color\" content=\"#07050c\"><title>Chest Shuffle 3D</title>");
        html = html.Replace(
            "let fullscreenContainer = document.querySelector(\"#unity-fullscreen-container\");",
            "let fullscreenContainer = document.querySelector(\"#unity-fullscreen-container\");\n" +
            "      function updateUnityViewport() { const v = window.visualViewport; const root = document.documentElement; root.style.setProperty('--viewport-width', (v ? v.width : window.innerWidth) + 'px'); root.style.setProperty('--viewport-height', (v ? v.height : window.innerHeight) + 'px'); root.style.setProperty('--viewport-left', (v ? v.offsetLeft : 0) + 'px'); root.style.setProperty('--viewport-top', (v ? v.offsetTop : 0) + 'px'); }\n" +
            "      updateUnityViewport(); window.addEventListener('resize', updateUnityViewport); window.addEventListener('orientationchange', updateUnityViewport); if (window.visualViewport) { window.visualViewport.addEventListener('resize', updateUnityViewport); window.visualViewport.addEventListener('scroll', updateUnityViewport); }");
        html = html.Replace("if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {",
            "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && /Macintosh/i.test(navigator.userAgent))) {");
        html = html.Replace("var meta = document.createElement('meta');", "")
            .Replace("meta.name = 'viewport';", "")
            .Replace("meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';", "")
            .Replace("document.getElementsByTagName('head')[0].appendChild(meta);", "");
        html = html.Replace(
            "        canvas.className = \"unity-mobile\";",
            "        canvas.className = \"unity-mobile\";\n        config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);");
        File.WriteAllText(indexPath, html);

        string cssPath = Path.Combine(outputPath, "TemplateData", "style.css");
        if (!File.Exists(cssPath)) return;
        File.AppendAllText(cssPath,
            "\nhtml,body{width:100%;height:100%;height:100dvh;margin:0;padding:0;overflow:hidden;background:#07050c;touch-action:none;overscroll-behavior:none;-webkit-user-select:none;user-select:none;-webkit-touch-callout:none}" +
            "#unity-container,#unity-container.unity-desktop,#unity-container.unity-mobile{position:fixed;left:var(--viewport-left,0px)!important;top:var(--viewport-top,0px)!important;right:auto!important;bottom:auto!important;width:var(--viewport-width,100vw)!important;height:var(--viewport-height,100dvh)!important;transform:none;overflow:hidden}" +
            "#unity-fullscreen-container,.unity-mobile #unity-fullscreen-container{position:absolute;inset:0;width:100%!important;height:100%!important}" +
            "#unity-canvas,#unity-canvas.unity-mobile{position:absolute;left:env(safe-area-inset-left,0px);top:env(safe-area-inset-top,0px);width:calc(100% - env(safe-area-inset-left,0px) - env(safe-area-inset-right,0px));height:calc(100% - env(safe-area-inset-top,0px) - env(safe-area-inset-bottom,0px));display:block;background:#07050c;outline:none;touch-action:none}" +
            "#unity-footer{display:none}#unity-warning{z-index:20}\n");
    }

    static Color Hex(string hex)
    {
        Color color;
        if (!ColorUtility.TryParseHtmlString(hex, out color)) return Color.magenta;
        return color;
    }

    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("Check failed: " + name);
        Debug.Log("PASS: " + name);
    }
}

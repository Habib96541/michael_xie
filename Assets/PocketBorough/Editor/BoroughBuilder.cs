using System;
using System.IO;
using System.Linq;
using System.Text;
using PocketBorough;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BoroughBuilder
{
    const string Root="Assets/PocketBorough/Generated";
    static void Folder(string p){Directory.CreateDirectory(p);}
    static T Save<T>(T asset,string path) where T:UnityEngine.Object
    {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);return old;}AssetDatabase.CreateAsset(asset,path);return asset;}
    [MenuItem("Pocket Borough/1. Import typography")]
    public static void ImportTypography()=>TMP_PackageResourceImporter.ImportResources(true,false,false);
    [MenuItem("Pocket Borough/2. Bake game and original assets")]
    public static void CreateGame()
    {
        foreach(string dir in new[]{Root,Root+"/Meshes",Root+"/Materials",Root+"/Prefabs",Root+"/Audio","Assets/Resources","Assets/PocketBorough/Scenes","Reports"})Folder(dir);
        AssetDatabase.Refresh();
        var shader=Shader.Find("PocketBorough/City");if(!shader)throw new Exception("City shader missing");
        Material Mat(string name,float ground,float clip){var mat=new Material(shader){name=name,enableInstancing=true};mat.SetFloat("_Ground",ground);mat.SetFloat("_ClipBelow",clip);return Save(mat,Root+"/Materials/"+name+".mat");}
        var catalog=AssetDatabase.LoadAssetAtPath<BoroughCatalog>("Assets/Resources/BoroughCatalog.asset");if(!catalog){catalog=ScriptableObject.CreateInstance<BoroughCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/BoroughCatalog.asset");}
        catalog.cityMaterial=Mat("Shared vertex palette",0,1);catalog.groundMaterial=Mat("Ground with eight circular openings",1,0);catalog.bowlMaterial=Mat("Interior and plinth",0,0);catalog.rimMaterial=Mat("Tintable rims",0,0);
        catalog.bowlMesh=Save(MeshWorkshop.HoleBowl(),Root+"/Meshes/Hole cavity.asset");catalog.rimMesh=Save(MeshWorkshop.Ring(),Root+"/Meshes/Hole rim.asset");
        catalog.models=new ModelDefinition[45];var report=new StringBuilder("category,name,vertices,triangles,footprint,mass,points\n");
        for(int i=0;i<45;i++)
        {
            bool building=i<15,vehicle=i>=15&&i<25;int id=building?i:vehicle?i-15:i-25;
            var mesh=building?MeshWorkshop.Building(id):vehicle?MeshWorkshop.Vehicle(id):MeshWorkshop.Prop(id);
            string name=mesh.name;mesh=Save(mesh,Root+"/Meshes/"+name+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=catalog.cityMaterial;renderer.shadowCastingMode=building||vehicle||id==6?ShadowCastingMode.On:ShadowCastingMode.Off;
            var bounds=mesh.bounds;go.GetComponent<BoxCollider>().center=bounds.center;go.GetComponent<BoxCollider>().size=bounds.size;go.GetComponent<BoxCollider>().isTrigger=true;
            string prefab=Root+"/Prefabs/"+name+".prefab";var saved=PrefabUtility.SaveAsPrefabAsset(go,prefab);UnityEngine.Object.DestroyImmediate(go);
            // Footprint uses both horizontal dimensions, with a little forgiveness for slender furniture.
            float footprint=Mathf.Sqrt(bounds.size.x*bounds.size.x+bounds.size.z*bounds.size.z)*.47f;
            float mass=building?65+i*13:vehicle?14+id*3: id==6?10:id==19?18:id==12?10:id==16?12:2+id%4;
            int points=building?140+i*45:vehicle?40+id*8:8+id*2;
            catalog.models[i]=new ModelDefinition{title=name,prefab=saved,footprint=footprint,mass=mass,points=points,category=building?0:vehicle?1:2};
            report.AppendLine($"{(building?"building":vehicle?"vehicle":"prop")},{name},{mesh.vertexCount},{mesh.triangles.Length/3},{footprint:F3},{mass},{points}");
        }
        catalog.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if(!catalog.font)throw new Exception("Import TMP Essentials first (Pocket Borough/1. Import typography)");
        CreatePanel();catalog.panelSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Rounded panel.png");
        CreateAudio();catalog.sounds=Enumerable.Range(0,9).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Sound "+i+".wav")).ToArray();catalog.music=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Borough groove.wav");
        var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        var desktop=Save(UnityEngine.Object.Instantiate(mobile),Root+"/Materials/Borough Desktop URP.asset");desktop.name="Borough Desktop URP";desktop.msaaSampleCount=2;desktop.renderScale=1;desktop.shadowDistance=45;desktop.shadowCascadeCount=1;desktop.mainLightShadowmapResolution=1024;desktop.supportsHDR=true;desktop.supportsCameraDepthTexture=false;desktop.supportsCameraOpaqueTexture=false;
        var low=Save(UnityEngine.Object.Instantiate(desktop),Root+"/Materials/Borough Mobile URP.asset");low.name="Borough Mobile URP";low.renderScale=.8f;low.shadowDistance=28;low.mainLightShadowmapResolution=512;
        catalog.desktopPipeline=desktop;catalog.mobilePipeline=low;
        desktop.shadowDistance=85;low.shadowDistance=75;
        var soft=new SerializedObject(desktop);soft.FindProperty("m_SoftShadowsSupported").boolValue=true;soft.ApplyModifiedPropertiesWithoutUndo();
        var hard=new SerializedObject(low);hard.FindProperty("m_SoftShadowsSupported").boolValue=false;hard.ApplyModifiedPropertiesWithoutUndo();
        GraphicsSettings.defaultRenderPipeline=desktop;
        for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=i==0?low:desktop;QualitySettings.antiAliasing=2;QualitySettings.pixelLightCount=1;QualitySettings.vSyncCount=0;}
        QualitySettings.SetQualityLevel(Math.Min(1,QualitySettings.names.Length-1),false);
        EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(desktop);EditorUtility.SetDirty(low);AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        catalog=AssetDatabase.LoadAssetAtPath<BoroughCatalog>("Assets/Resources/BoroughCatalog.asset");
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=30;camera.nearClipPlane=.1f;camera.farClipPlane=220;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=MeshWorkshop.C("AACBCD");camera.transform.SetPositionAndRotation(new(0,55,-38),Quaternion.Euler(56,0,0));camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var light=new GameObject("Warm afternoon sun",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.color=new(1,.95f,.84f);light.shadows=LightShadows.Soft;light.shadowBias=.035f;light.shadowNormalBias=.22f;light.transform.rotation=Quaternion.Euler(47,-32,0);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=MeshWorkshop.C("B9D1D5");RenderSettings.fog=false;
        var profile=Save(ScriptableObject.CreateInstance<VolumeProfile>(),Root+"/Materials/Borough colour.asset");
        foreach(var c in profile.components.ToArray()){profile.Remove(c.GetType());UnityEngine.Object.DestroyImmediate(c,true);}
        var tonemap=profile.Add<Tonemapping>(true);tonemap.mode.value=TonemappingMode.Neutral;AssetDatabase.AddObjectToAsset(tonemap,profile);
        var color=profile.Add<ColorAdjustments>(true);color.saturation.value=7;color.contrast.value=3;AssetDatabase.AddObjectToAsset(color,profile);
        var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.18f;bloom.threshold.value=1.1f;AssetDatabase.AddObjectToAsset(bloom,profile);
        var volume=new GameObject("Mobile-friendly colour grade",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
        new GameObject("BoroughGame",typeof(BoroughGame)).GetComponent<BoroughGame>().catalog=catalog;
        EditorSceneManager.SaveScene(scene,"Assets/PocketBorough/Scenes/PocketBorough.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/PocketBorough/Scenes/PocketBorough.unity",true)};
        ConfigureWeb();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        File.WriteAllText("Reports/asset-inventory.csv",report.ToString());
        Debug.Log("BOROUGH_BAKE_OK: 15 buildings, 10 vehicles, 20 props; scene, sound, typography, materials and prefabs baked.");
    }
    static void CreatePanel()
    {
        const int n=64;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++){float dx=Mathf.Max(0,16-x,x-47),dy=Mathf.Max(0,16-y,y-47);float a=Mathf.Clamp01(16.2f-Mathf.Sqrt(dx*dx+dy*dy));pixels[x+y*n]=new(1,1,1,a);}
        tex.SetPixels(pixels);tex.Apply();string p=Root+"/Rounded panel.png";File.WriteAllBytes(p,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(p);
        var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=new(20,20,20,20);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    }
    static void Wav(string path,int length,Func<float,int,float> sample)
    {
        const int rate=22050;using var file=File.Create(path);using var w=new BinaryWriter(file);w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+length*2);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(length*2);for(int i=0;i<length;i++)w.Write((short)(Mathf.Clamp(sample(i/(float)rate,i),-.95f,.95f)*32767));
    }
    static void CreateAudio()
    {
        const int rate=22050;
        for(int fx=0;fx<9;fx++)
        {
            int id=fx;float duration=fx==6||fx==7?1.2f:fx==3?.65f:.26f;
            Wav(Root+"/Audio/Sound "+fx+".wav",(int)(rate*duration),(t,i)=>
            {float u=t/duration,env=Mathf.Sin(Mathf.PI*.5f*Mathf.Min(1,u*14))*Mathf.Pow(1-u,1.6f);float freq=id==0?280-190*u:id==1?160-110*u:id==2?130+500*u:id==3?390+660*u:id==4?720:id==5?520:id==6?440*Mathf.Pow(2,Mathf.Floor(u*5)/4):id==7?420-200*u:160;return (Mathf.Sin(t*freq*6.283f)+.2f*Mathf.Sin(t*freq*12.566f))*.36f*env;});
        }
        // Original 110 BPM, four-bar pentatonic loop: rounded bass, soft keys, kick and shaker.
        float beat=60f/110,seconds=beat*32;int[] notes={0,4,7,9,7,4,2,4,0,4,7,12,9,7,4,2};
        Wav(Root+"/Audio/Borough groove.wav",(int)(rate*seconds),(t,i)=>
        {
            int step=Mathf.FloorToInt(t/(beat*.5f));float nt=t%(beat*.5f);float hz=220*Mathf.Pow(2,notes[step%notes.Length]/12f);float keys=Mathf.Sin(nt*hz*6.283f)*Mathf.Exp(-nt*12)*.1f;
            int chord=(int)(t/(beat*8))%4;float bassHz=chord==0?110:chord==1?82.4f:chord==2?98:73.4f;float bt=t%beat;float bass=Mathf.Sin(bt*bassHz*6.283f)*Mathf.Exp(-bt*8)*.16f;
            float kick=Mathf.Sin(bt*(55+60*Mathf.Exp(-bt*25))*6.283f)*Mathf.Exp(-bt*35)*.2f;
            float noise=Mathf.Sin(i*12.9898f)*Mathf.Sin(i*78.233f);float shaker=noise*Mathf.Exp(-nt*90)*.03f;return keys+bass+kick+shaker;
        });
        AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles(Root+"/Audio","*.wav"))
        {var importer=(AudioImporter)AssetImporter.GetAtPath(path);var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=path.Contains("groove")?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;sample.quality=.55f;importer.defaultSampleSettings=sample;importer.forceToMono=true;importer.SaveAndReimport();}
    }
    public static void ConfigureWeb()
    {
        PlayerSettings.companyName="Pocket Borough Studio";PlayerSettings.productName="Pocket Borough";PlayerSettings.bundleVersion="1.0.0";
        PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=720;PlayerSettings.runInBackground=true;
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.WebGL.maximumMemorySize=512;PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;PlayerSettings.WebGL.template="PROJECT:Borough";
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL,ManagedStrippingLevel.Low);
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.WebGL.threadsSupport=false;
        AssetDatabase.SaveAssets();
    }
    public static void RenderGallery()
    {
        Directory.CreateDirectory("Reports");var catalog=Resources.Load<BoroughCatalog>("BoroughCatalog");
        var go=new GameObject("Asset validation camera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=MeshWorkshop.C("E8E6D8");camera.cullingMask=1<<30;camera.nearClipPlane=.1f;camera.farClipPlane=100;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
        for(int category=0;category<3;category++)for(int view=0;view<2;view++)
        {
            int start=category==0?0:category==1?15:25,count=category==0?15:category==1?10:20;int rows=(count+4)/5;var sheet=new Texture2D(1280,rows*256,TextureFormat.RGB24,false);var cell=new Texture2D(256,256,TextureFormat.RGB24,false);
            for(int i=0;i<count;i++)
            {
                var item=UnityEngine.Object.Instantiate(catalog.models[start+i].prefab);item.layer=30;var bounds=item.GetComponent<MeshRenderer>().bounds;var target=bounds.center;float size=Mathf.Max(bounds.size.y,Mathf.Max(bounds.size.x,bounds.size.z));camera.orthographicSize=size*.76f;
                camera.transform.position=target+new Vector3(view==0?1.1f:-1.3f,.85f,view==0?-1.6f:1.7f).normalized*size*3;camera.transform.LookAt(target);camera.Render();RenderTexture.active=rt;cell.ReadPixels(new Rect(0,0,256,256),0,0);cell.Apply();sheet.SetPixels((i%5)*256,(rows-1-i/5)*256,256,256,cell.GetPixels());UnityEngine.Object.DestroyImmediate(item);
            }
            sheet.Apply();File.WriteAllBytes("Reports/"+new[]{"buildings","vehicles","props"}[category]+"-view-"+view+".png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(cell);
        }
        RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);Debug.Log("BOROUGH_GALLERY_OK: all 45 models rendered from two angles.");
    }
    [MenuItem("Pocket Borough/3. Build WebGL release")]
    public static void BuildWeb()
    {
        ConfigureWeb();Directory.CreateDirectory("Builds");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/PocketBorough/Scenes/PocketBorough.unity"},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
        Directory.CreateDirectory("Reports");File.WriteAllText("Reports/build-report.json",JsonUtility.ToJson(new BuildReceipt{result=report.summary.result.ToString(),bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
        Debug.Log("BOROUGH_WEBGL_BUILD_OK "+report.summary.totalSize+" bytes");
    }
    [Serializable] class BuildReceipt{public string result;public ulong bytes;public double seconds;public int errors,warnings;}
}

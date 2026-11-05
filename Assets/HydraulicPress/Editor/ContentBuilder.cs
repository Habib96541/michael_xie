using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HydraulicPress.Editor
{
    public static class ContentBuilder
    {
        private const string Root = "Assets/HydraulicPress/";
        private static int meshIndex;
        private static Material steel, dark, silver, copper, cream, black, red, green, yellow, blue, pink, white, glass, brown;
        private static readonly string[] Names = { "Soda can", "Watermelon", "Rubber duck", "Glass bottle", "Smartphone", "Toy car", "Toothpaste tube", "Ceramic mug", "Tennis ball", "Metal spring", "Chocolate bar", "Alarm clock", "Paint can", "Plastic bottle", "Toy robot", "Jelly cube", "Light bulb", "Metal lunchbox", "Small safe", "Steel coupling" };

        [MenuItem("Hydraulic Press/Create or refresh game content")]
        public static void CreateGame()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding content.");
            foreach (string path in new[] { "Generated/Meshes", "Generated/Materials", "Generated/Prefabs", "Generated/Definitions", "Scenes", "Art/Fonts" }) Directory.CreateDirectory(Root + path);
            AssetDatabase.Refresh(); meshIndex = 0; Palette(); ImportIcons();
            var config = LoadOrCreate<GameplayConfig>(Root + "Generated/Gameplay.asset"); config.objects = new CrushableObjectDefinition[20];
            for (int i = 0; i < 20; i++) config.objects[i] = CreateObject(i);
            EditorUtility.SetDirty(config);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Hydraulic Press").AddComponent<GameManager>(); game.config = config;
            game.gameObject.AddComponent<WebGLResponsiveManager>().game=game;
            var rig = new GameObject("Press assembly").transform;
            game.press = BuildPress(rig);
            var anchor = new GameObject("Object arrival").transform; anchor.SetParent(rig); anchor.localPosition = new Vector3(0, 1.08f, 0); game.objectAnchor = anchor;
            var floor = Box("Studio floor", null, new Vector3(0, -.31f, 0), new Vector3(100, .25f, 100), cream); floor.AddComponent<BoxCollider>();
            Part("Presentation plinth", null, ModelGeometry.Lathe(new[] { V(0,-.15f), V(2.5f,-.15f), V(2.6f,-.06f), V(2.6f,.02f), V(2.5f,.08f), V(0,.08f) },64), Vector3.zero, dark);
            for (int i = 0; i < 12; i++)
            {
                float a = (i * 30 + 15) * Mathf.Deg2Rad;
                var mark = Box("Plinth registration", null, new Vector3(Mathf.Cos(a)*2.32f,.085f,Mathf.Sin(a)*2.32f), new Vector3(.14f,.015f,.035f), copper,.006f); mark.transform.localRotation = Quaternion.Euler(0,-i*30-15,0);
            }
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData)); cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(7.2f,6.2f,-10.8f); cameraGo.transform.LookAt(new Vector3(0,2.17f,0));
            var view = cameraGo.GetComponent<Camera>(); view.orthographic = true; view.orthographicSize = 4.9f; view.nearClipPlane = .1f; view.farClipPlane = 60; view.backgroundColor = cream.color; view.clearFlags = CameraClearFlags.SolidColor;
            var cameraData = cameraGo.GetComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing = true;
            game.cameraRig = cameraGo.AddComponent<CameraController>();
            Light("Large softbox", new Vector3(48,-34,0), new Color(1,.92f,.82f), 2.2f, true);
            Light("Cool rim", new Vector3(58,140,0), new Color(.70f,.81f,1), .8f, false);
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.68f,.72f,.75f); RenderSettings.ambientEquatorColor = new Color(.47f,.48f,.47f); RenderSettings.ambientGroundColor = new Color(.28f,.27f,.25f);
            StudioReflection();
            var volume = new GameObject("Studio grading").AddComponent<Volume>(); volume.isGlobal = true;
            var profile = LoadOrCreate<VolumeProfile>(Root + "Generated/StudioVolume.asset");
            if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            volume.sharedProfile = profile; EditorUtility.SetDirty(profile);
            game.effects = new GameObject("Pooled physics and particles").AddComponent<PhysicsEffectManager>(); game.effects.shardMaterial = white;
            string particlePath=Root+"Generated/Materials/Dust.mat";
            var particleMat=AssetDatabase.LoadAssetAtPath<Material>(particlePath);
            if(particleMat==null){particleMat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(particleMat,particlePath);}
            game.effects.particleMaterial=particleMat;
            game.effects.shardMeshes = new Mesh[8]; for (int i = 0; i < 8; i++) game.effects.shardMeshes[i] = SaveMesh(ModelGeometry.Shard(i+300));
            game.audioManager = new GameObject("Audio").AddComponent<AudioManager>();
            foreach (var field in typeof(AudioManager).GetFields()) if (field.FieldType == typeof(AudioClip)) field.SetValue(game.audioManager, AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Art/Audio/" + field.Name + ".wav"));
            game.hud = new GameObject("HUD", typeof(RectTransform)).AddComponent<UIManager>(); game.hud.font = MakeFont();
            game.hud.pressIcon = Icon("press"); game.hud.walletIcon = Icon("wallet"); game.hud.settingsIcon = Icon("settings"); game.hud.replayIcon = Icon("replay"); game.hud.brokenIcon = Icon("broken"); game.hud.coinIcon = Icon("coin"); game.hud.checkIcon = Icon("check"); game.hud.rounded = MakeRounded();
            game.hud.pauseIcon=Icon("pause");game.hud.playIcon=Icon("play");game.hud.homeIcon=Icon("home");
            ConfigurePipeline(); ConfigurePlayer();
            config.graphics=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"Generated/PressPipeline.asset");config.rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"Generated/PressRenderer.asset");EditorUtility.SetDirty(config);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Root + "Scenes/HydraulicPress.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "Scenes/HydraulicPress.unity", true) };
            AssetDatabase.SaveAssets(); Debug.Log("Hydraulic Press content created: 20 definitions, 20 original object prefabs, complete scene.");
        }

        private static HydraulicPressController BuildPress(Transform root)
        {
            var controller = root.gameObject.AddComponent<HydraulicPressController>(); controller.machine = root;
            Box("Foundation",root,new Vector3(0,.35f,0),new Vector3(3.65f,.55f,2.22f),dark,.14f);
            Box("Copper toe",root,new Vector3(0,.54f,-1.105f),new Vector3(3.05f,.075f,.025f),copper,.01f);
            var deck = Box("Machined anvil",root,new Vector3(0,.92f,0),new Vector3(3.2f,.32f,1.92f),steel,.085f); controller.deck = deck.transform;
            deck.AddComponent<BoxCollider>();
            Box("Press bed insert",root,new Vector3(0,1.075f,0),new Vector3(2.4f,.025f,1.35f),silver,.012f);
            for(int i=-5;i<=5;i++) Box("Bed tooling groove",root,new Vector3(i*.21f,1.092f,0),new Vector3(.014f,.007f,1.25f),dark,.002f);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Cast frame column",root,new Vector3(side*1.52f,2.52f,.32f),new Vector3(.52f,4.0f,1.12f),dark,.075f);
                Box("Painted column inset",root,new Vector3(side*1.52f,2.48f,-.255f),new Vector3(.30f,3.38f,.045f),steel,.04f);
                Box("Copper guide rail",root,new Vector3(side*1.32f,2.55f,-.33f),new Vector3(.075f,3.3f,.10f),copper,.02f);
                for(int j=0;j<3;j++)
                {
                    var bolt=Cylinder("Frame hex bolt",root,new Vector3(side*1.52f,1.1f+j*1.43f,-.33f),.085f,.065f,silver,6); bolt.transform.localRotation=Quaternion.Euler(90,0,0);
                }
                Cylinder("Rear tie rod",root,new Vector3(side*1.10f,2.55f,.68f),.07f,3.2f,silver);
                Cylinder("Anvil fastener",root,new Vector3(side*1.3f,1.09f,-.65f),.085f,.035f,dark,6);
            }
            Box("Crown casting",root,new Vector3(0,4.45f,.22f),new Vector3(3.70f,.68f,1.45f),steel,.12f);
            Box("Crown front trim",root,new Vector3(0,4.44f,-.535f),new Vector3(3.30f,.28f,.08f),dark,.04f);
            for(int i=-1;i<=1;i+=2) Box("Crown badge",root,new Vector3(i*1.34f,4.44f,-.59f),new Vector3(.18f,.12f,.025f),copper,.015f);
            var cylinder = Cylinder("Hydraulic cylinder",root,new Vector3(0,4.02f,0),.43f,1.05f,dark); controller.cylinder=cylinder.transform;
            Cylinder("Upper gland",root,new Vector3(0,4.50f,0),.49f,.16f,silver);
            Cylinder("Lower gland",root,new Vector3(0,3.51f,0),.47f,.16f,copper);
            for(int i=0;i<4;i++) { float a=i*Mathf.PI*.5f+.6f; Cylinder("Cylinder stud",root,new Vector3(Mathf.Cos(a)*.38f,3.49f,Mathf.Sin(a)*.38f),.045f,.12f,silver,6); }
            controller.shaft=Cylinder("Polished moving ram",root,new Vector3(0,3.7f,0),.24f,1,silver).transform;
            controller.plate=Box("Heavy moving platen",root,new Vector3(0,3.20f,0),new Vector3(2.45f,.30f,1.50f),steel,.07f).transform;
            controller.plate.gameObject.AddComponent<BoxCollider>();
            controller.plate.gameObject.AddComponent<Rigidbody>().isKinematic=true;
            Box("Platen underside",controller.plate,new Vector3(0,-.145f,0),new Vector3(2.25f,.055f,1.32f),silver,.025f);
            Box("Platen copper face",controller.plate,new Vector3(0,0,-.76f),new Vector3(2.12f,.16f,.025f),copper,.015f);
            for(int i=-4;i<=4;i++)
            {
                var stripe=Box("Safety inlay",controller.plate,new Vector3(i*.235f,0,-.779f),new Vector3(.075f,.18f,.011f),dark,.003f); stripe.transform.localRotation=Quaternion.Euler(0,0,-25);
            }
            var broken=new List<Transform>();
            for(int side=-1;side<=1;side+=2)
            {
                var clamp=Box("Moving rail collar",controller.plate,new Vector3(side*1.32f,0,.1f),new Vector3(.20f,.36f,.45f),silver,.035f); broken.Add(clamp.transform);
                Vector3[] path={new Vector3(side*.42f,4.05f,.22f),new Vector3(side*.8f,4.18f,.30f),new Vector3(side*1.0f,3.85f,.38f),new Vector3(side*1.03f,3.1f,.42f),new Vector3(side*1.22f,2.8f,.45f)};
                var hose=Part("Reinforced hydraulic hose",root,ModelGeometry.Tube(path,.055f),Vector3.zero,black); broken.Add(hose.transform);
                Cylinder("Hydraulic hose coupling",root,new Vector3(side*.43f,4.05f,.22f),.095f,.2f,copper,6).transform.localRotation=Quaternion.Euler(0,0,90);
            }
            var gauge=Cylinder("Pressure gauge",root,new Vector3(-.82f,4.45f,-.62f),.20f,.075f,silver); gauge.transform.localRotation=Quaternion.Euler(90,0,0);
            var face=Cylinder("Gauge dial",root,new Vector3(-.82f,4.45f,-.67f),.165f,.015f,cream); face.transform.localRotation=Quaternion.Euler(90,0,0);
            var needle=Box("Gauge needle",root,new Vector3(-.81f,4.49f,-.69f),new Vector3(.018f,.12f,.012f),red,.004f); needle.transform.localRotation=Quaternion.Euler(0,0,-35);
            var lamp=Part("Status jewel",root,ModelGeometry.Ellipsoid(new Vector3(.15f,.15f,.08f)),new Vector3(.84f,4.44f,-.60f),green); controller.warningLamp=lamp.GetComponent<Renderer>(); controller.readyLamp=green; controller.failureLamp=red;
            controller.breakableParts=broken.ToArray();
            return controller;
        }

        private static CrushableObjectDefinition CreateObject(int id)
        {
            var root=new GameObject(Names[id]); var t=root.transform;
            CrushMaterial response=CrushMaterial.Metal; Color color=copper.color; float height=.8f;
            switch(id)
            {
                case 0:
                    Cylinder("Aluminum body",t,new Vector3(0,.43f,0),.30f,.82f,red);
                    foreach(float y in new[]{.06f,.80f}) Part("Rolled lip",t,ModelGeometry.Ring(.283f,.022f),new Vector3(0,y,0),silver);
                    Cylinder("Lid",t,new Vector3(0,.827f,0),.275f,.018f,silver);
                    var tab=Part("Pull tab",t,ModelGeometry.Ring(.075f,.018f),new Vector3(0,.847f,0),dark); tab.transform.localScale=new Vector3(.6f,1,1);
                    Part("Cream label",t,ModelGeometry.Lathe(new[]{V(.303f,.24f),V(.303f,.44f)}),Vector3.zero,cream);
                    Box("Cola emblem",t,new Vector3(0,.34f,-.31f),new Vector3(.19f,.13f,.015f),red,.04f); height=.86f; color=red.color; break;
                case 1:
                    Part("Striped rind",t,ModelGeometry.Ellipsoid(new Vector3(1.02f,.84f,.85f)),new Vector3(0,.43f,0),green);
                    for(int i=0;i<8;i++)
                    { var path=new Vector3[17]; float a=i*Mathf.PI/4; for(int j=0;j<17;j++){float b=j*Mathf.PI/16;path[j]=new Vector3(Mathf.Cos(a)*Mathf.Sin(b)*.512f,.43f-Mathf.Cos(b)*.422f,Mathf.Sin(a)*Mathf.Sin(b)*.427f);} Part("Rind vein",t,ModelGeometry.Tube(path,.018f,5),Vector3.zero,dark); }
                    response=CrushMaterial.Liquid; color=new Color(.85f,.24f,.26f); height=.86f; break;
                case 2:
                    Part("Duck body",t,ModelGeometry.Ellipsoid(new Vector3(.92f,.55f,.64f)),new Vector3(0,.3f,0),yellow);
                    Part("Duck head",t,ModelGeometry.Ellipsoid(new Vector3(.46f,.47f,.44f)),new Vector3(-.22f,.65f,-.05f),yellow);
                    Box("Beak",t,new Vector3(-.35f,.61f,-.28f),new Vector3(.28f,.11f,.26f),copper,.05f);
                    Part("Wing",t,ModelGeometry.Ellipsoid(new Vector3(.46f,.27f,.08f)),new Vector3(.10f,.37f,-.30f),copper);
                    Part("Eye",t,ModelGeometry.Ellipsoid(Vector3.one*.055f),new Vector3(-.31f,.71f,-.235f),black);
                    response=CrushMaterial.Elastic; color=yellow.color; height=.89f; break;
                case 3:
                    Part("Glass silhouette",t,ModelGeometry.Lathe(new[]{V(0,0),V(.23f,.025f),V(.25f,.12f),V(.25f,.52f),V(.12f,.67f),V(.09f,.84f),V(.12f,.85f),V(.12f,.91f),V(0,.91f)}),Vector3.zero,glass);
                    Cylinder("Bottle label",t,new Vector3(0,.35f,0),.255f,.22f,cream); Cylinder("Cork",t,new Vector3(0,.94f,0),.09f,.08f,brown);
                    response=CrushMaterial.Brittle; color=glass.color; height=.98f; break;
                case 4:
                    Box("Rounded phone chassis",t,new Vector3(0,.10f,0),new Vector3(.64f,.16f,1.03f),dark,.07f);
                    Box("Screen",t,new Vector3(0,.187f,0),new Vector3(.55f,.015f,.91f),blue,.04f);
                    Box("Screen reflection",t,new Vector3(.11f,.197f,.1f),new Vector3(.16f,.008f,.65f),glass,.015f);
                    Box("Earpiece",t,new Vector3(0,.2f,.39f),new Vector3(.14f,.009f,.025f),black,.006f);
                    response=CrushMaterial.Brittle; color=blue.color; height=.21f; break;
                case 5:
                    Box("Toy chassis",t,new Vector3(0,.22f,0),new Vector3(.62f,.24f,1.04f),red,.09f);
                    Box("Toy cab",t,new Vector3(0,.41f,.05f),new Vector3(.53f,.29f,.55f),red,.08f);
                    Box("Windshield",t,new Vector3(0,.44f,-.235f),new Vector3(.40f,.15f,.025f),glass,.03f);
                    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){var wheel=Cylinder("Rubber tire",t,new Vector3(x*.32f,.16f,z*.31f),.15f,.12f,black,20);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}
                    response=CrushMaterial.Plastic; color=red.color; height=.58f; break;
                case 6:
                    Box("Toothpaste tube",t,new Vector3(0,.20f,.02f),new Vector3(.48f,.30f,.88f),cream,.12f);
                    Box("Fresh stripe",t,new Vector3(0,.355f,.02f),new Vector3(.30f,.02f,.65f),blue,.04f);
                    var cap=Cylinder("Screw cap",t,new Vector3(0,.19f,-.49f),.14f,.16f,red);cap.transform.localRotation=Quaternion.Euler(90,0,0);
                    response=CrushMaterial.Liquid;color=blue.color;height=.37f;break;
                case 7:
                    Part("Ceramic cup",t,ModelGeometry.Lathe(new[]{V(0,.02f),V(.28f,.02f),V(.33f,.08f),V(.35f,.64f),V(.31f,.67f),V(.285f,.62f),V(.27f,.12f),V(0,.12f)}),Vector3.zero,blue);
                    var handle=Part("Mug handle",t,ModelGeometry.Ring(.22f,.065f),new Vector3(.42f,.37f,0),blue);handle.transform.localRotation=Quaternion.Euler(90,0,0);
                    response=CrushMaterial.Brittle;color=blue.color;height=.70f;break;
                case 8:
                    Part("Felt ball",t,ModelGeometry.Ellipsoid(Vector3.one*.72f),new Vector3(0,.36f,0),yellow);
                    for(int i=0;i<2;i++){var seam=Part("Tennis seam",t,ModelGeometry.Ring(.357f,.016f),new Vector3(0,.36f,0),cream);seam.transform.localRotation=Quaternion.Euler(i==0?58:-58,0,25);}
                    response=CrushMaterial.Elastic;color=yellow.color;height=.73f;break;
                case 9:
                    var spring=new Vector3[145];for(int i=0;i<spring.Length;i++){float p=(float)i/(spring.Length-1),a=p*Mathf.PI*10;spring[i]=new Vector3(Mathf.Cos(a)*.30f,.07f+p*.8f,Mathf.Sin(a)*.30f);}Part("Steel spring",t,ModelGeometry.Tube(spring,.045f),Vector3.zero,silver);
                    response=CrushMaterial.Elastic;color=silver.color;height=.92f;break;
                case 10:
                    Box("Chocolate slab",t,new Vector3(0,.08f,0),new Vector3(.72f,.12f,1.02f),brown,.035f);
                    for(int x=0;x<3;x++)for(int z=0;z<4;z++)Box("Chocolate square",t,new Vector3((x-1)*.23f,.18f,(z-1.5f)*.25f),new Vector3(.21f,.15f,.23f),brown,.035f);
                    response=CrushMaterial.Soft;color=brown.color;height=.27f;break;
                case 11:
                    var body=Cylinder("Clock case",t,new Vector3(0,.43f,0),.40f,.22f,copper);body.transform.localRotation=Quaternion.Euler(90,0,0);
                    var dial=Cylinder("Clock dial",t,new Vector3(0,.43f,-.12f),.355f,.02f,cream);dial.transform.localRotation=Quaternion.Euler(90,0,0);
                    Box("Hour hand",t,new Vector3(-.075f,.46f,-.14f),new Vector3(.16f,.025f,.015f),dark,.006f);
                    Box("Minute hand",t,new Vector3(0,.54f,-.14f),new Vector3(.024f,.24f,.015f),dark,.006f);
                    for(int s=-1;s<=1;s+=2){Part("Alarm bell",t,ModelGeometry.Ellipsoid(new Vector3(.32f,.19f,.30f)),new Vector3(s*.27f,.79f,0),silver);Box("Clock foot",t,new Vector3(s*.24f,.07f,0),new Vector3(.12f,.15f,.20f),dark,.03f);}
                    color=copper.color;height=.9f;break;
                case 12:
                    Cylinder("Paint tin",t,new Vector3(0,.37f,0),.34f,.70f,silver);
                    Cylinder("Paint label",t,new Vector3(0,.37f,0),.345f,.40f,pink);
                    Part("Paint rim",t,ModelGeometry.Ring(.33f,.025f),new Vector3(0,.73f,0),dark);
                    Cylinder("Wet paint",t,new Vector3(0,.72f,0),.31f,.014f,pink);
                    var bail=Part("Wire handle",t,ModelGeometry.Ring(.39f,.019f),new Vector3(0,.48f,0),silver);bail.transform.localRotation=Quaternion.Euler(90,0,0);
                    response=CrushMaterial.Liquid;color=pink.color;height=.89f;break;
                case 13:
                    Part("PET bottle",t,ModelGeometry.Lathe(new[]{V(0,0),V(.22f,.02f),V(.25f,.12f),V(.23f,.24f),V(.25f,.32f),V(.23f,.50f),V(.22f,.65f),V(.10f,.79f),V(.10f,.87f),V(0,.87f)}),Vector3.zero,glass);
                    Cylinder("Bottle cap",t,new Vector3(0,.89f,0),.12f,.09f,blue);
                    Cylinder("Bottle band",t,new Vector3(0,.44f,0),.255f,.20f,blue);response=CrushMaterial.Plastic;color=glass.color;height=.95f;break;
                case 14:
                    Box("Robot body",t,new Vector3(0,.40f,0),new Vector3(.44f,.43f,.30f),blue,.06f);
                    Box("Robot head",t,new Vector3(0,.76f,0),new Vector3(.50f,.26f,.32f),silver,.045f);
                    for(int s=-1;s<=1;s+=2){Box("Robot arm",t,new Vector3(s*.33f,.39f,0),new Vector3(.15f,.39f,.19f),copper,.04f);Box("Robot boot",t,new Vector3(s*.14f,.10f,-.04f),new Vector3(.20f,.19f,.36f),dark,.035f);Part("Robot eye",t,ModelGeometry.Ellipsoid(Vector3.one*.07f),new Vector3(s*.13f,.78f,-.16f),dark);}
                    Box("Robot heart",t,new Vector3(0,.45f,-.16f),new Vector3(.13f,.13f,.025f),red,.02f);response=CrushMaterial.Plastic;color=blue.color;height=.91f;break;
                case 15:
                    Box("Jelly",t,new Vector3(0,.37f,0),new Vector3(.80f,.74f,.80f),pink,.18f);
                    for(int i=0;i<4;i++)Part("Jelly highlight",t,ModelGeometry.Ellipsoid(new Vector3(.07f,.035f,.10f)),new Vector3((i%2-.5f)*.37f,.75f,(i/2-.5f)*.33f),cream);
                    response=CrushMaterial.Soft;color=pink.color;height=.76f;break;
                case 16:
                    Part("Bulb globe",t,ModelGeometry.Lathe(new[]{V(0,0),V(.14f,.02f),V(.16f,.25f),V(.30f,.39f),V(.35f,.59f),V(.29f,.77f),V(.12f,.88f),V(0,.91f)}),Vector3.zero,cream);
                    Cylinder("Bulb screw",t,new Vector3(0,.13f,0),.16f,.24f,silver);
                    for(int i=0;i<4;i++)Part("Screw thread",t,ModelGeometry.Ring(.16f,.022f),new Vector3(0,.04f+i*.055f,0),dark);
                    response=CrushMaterial.Brittle;color=cream.color;height=.93f;break;
                case 17:
                    Box("Lunchbox",t,new Vector3(0,.27f,0),new Vector3(.94f,.50f,.67f),green,.085f);
                    Box("Lid seam",t,new Vector3(0,.43f,0),new Vector3(.96f,.045f,.69f),silver,.022f);
                    var grip=Part("Carry handle",t,ModelGeometry.Ring(.16f,.035f),new Vector3(0,.57f,0),dark);grip.transform.localRotation=Quaternion.Euler(90,0,0);
                    Box("Latch",t,new Vector3(0,.36f,-.35f),new Vector3(.12f,.18f,.04f),copper,.02f);color=green.color;height=.76f;break;
                case 18:
                    Box("Safe housing",t,new Vector3(0,.42f,0),new Vector3(.84f,.82f,.68f),steel,.065f);
                    Box("Safe inset door",t,new Vector3(0,.42f,-.36f),new Vector3(.68f,.65f,.07f),dark,.04f);
                    var lockDial=Cylinder("Combination dial",t,new Vector3(-.12f,.49f,-.42f),.13f,.07f,silver,24);lockDial.transform.localRotation=Quaternion.Euler(90,0,0);
                    Box("Safe handle",t,new Vector3(.18f,.37f,-.43f),new Vector3(.06f,.22f,.08f),copper,.02f);color=steel.color;height=.85f;break;
                case 19:
                    Cylinder("Coupling body",t,new Vector3(0,.30f,0),.30f,.54f,steel,24);
                    for(int j=0;j<2;j++){Cylinder("Flange",t,new Vector3(0,.08f+j*.46f,0),.44f,.13f,silver,24);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Cylinder("Flange bolt",t,new Vector3(Mathf.Cos(a)*.34f,.16f+j*.46f,Mathf.Sin(a)*.34f),.055f,.055f,dark,6);}}
                    Cylinder("Axle socket",t,new Vector3(0,.62f,0),.19f,.09f,dark,12);color=silver.color;height=.68f;break;
            }
            var bounds=new Bounds(new Vector3(0,height*.5f,0),new Vector3(.85f,height,.85f));
            var collider=root.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;
            root.AddComponent<Rigidbody>().isKinematic=true;
            string prefabPath=Root+"Generated/Prefabs/"+id.ToString("00")+".prefab"; var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath); UnityEngine.Object.DestroyImmediate(root);
            var def=LoadOrCreate<CrushableObjectDefinition>(Root+"Generated/Definitions/"+id.ToString("00")+".asset");def.displayName=Names[id];def.response=response;def.prefab=prefab;def.height=height;def.fragmentColor=color;EditorUtility.SetDirty(def);return def;
        }

        private static void Palette()
        {
            steel=Mat("Steel","475F6B",.75f,.38f);dark=Mat("Graphite","172C37",.55f,.35f);silver=Mat("Machined aluminum","A5B9BF",.88f,.64f);copper=Mat("Copper enamel","D9934E",.48f,.45f);
            cream=Mat("Warm porcelain","DDDAD2",.05f,.32f);black=Mat("Rubber","182227",0,.22f);red=Mat("Vermilion","C95548",.15f,.45f);green=Mat("Sage","76A57B",.08f,.38f);
            yellow=Mat("Marigold","E5C15E",.03f,.40f);blue=Mat("Pool blue","5B9FAE",.08f,.50f);pink=Mat("Guava","CD8094",.02f,.62f);white=Mat("Debris","EAE2CF",.08f,.34f);glass=Mat("Sea glass","9EC8BE",.18f,.80f);brown=Mat("Chocolate","664334",0,.40f);
        }
        private static Material Mat(string name,string hex,float metallic,float smoothness)
        {
            string path=Root+"Generated/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            ColorUtility.TryParseHtmlString("#"+hex,out Color color);mat.color=color;mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smoothness);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        private static Mesh SaveMesh(Mesh mesh)
        {
            string path=Root+"Generated/Meshes/"+(meshIndex++).ToString("0000")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name=Path.GetFileNameWithoutExtension(path);
            if(old!=null){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static GameObject Part(string name,Transform parent,Mesh mesh,Vector3 position,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        private static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 size,Material material,float bevel=.04f)=>Part(name,parent,ModelGeometry.RoundedBox(size,bevel),pos,material);
        private static GameObject Cylinder(string name,Transform parent,Vector3 pos,float radius,float height,Material material,int sides=32)=>Part(name,parent,ModelGeometry.Lathe(new[]{V(0,-height*.5f),V(radius*.94f,-height*.5f),V(radius,-height*.5f+.02f),V(radius,height*.5f-.02f),V(radius*.94f,height*.5f),V(0,height*.5f)},sides),pos,material);
        private static Vector2 V(float r,float y)=>new Vector2(r,y);
        private static T LoadOrCreate<T>(string path) where T:ScriptableObject {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;}
        private static void Light(string name,Vector3 rotation,Color color,float intensity,bool shadows){var light=new GameObject(name).AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;light.transform.rotation=Quaternion.Euler(rotation);light.shadows=shadows?LightShadows.Soft:LightShadows.None;light.shadowBias=.035f;light.shadowNormalBias=.2f;}
        private static void ImportIcons()
        {
            foreach(string path in Directory.GetFiles(Root+"Art/Icons","*.png")){var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)continue;importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        }
        private static Sprite Icon(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Icons/"+name+".png");
        [MenuItem("Hydraulic Press/Update menu icons")]
        public static void UpdateMenuIcons()
        {
            ImportIcons();
            var hud=UnityEngine.Object.FindFirstObjectByType<UIManager>();
            if(hud==null)throw new InvalidOperationException("Open the HydraulicPress gameplay scene first.");
            hud.pauseIcon=Icon("pause");hud.playIcon=Icon("play");hud.homeIcon=Icon("home");EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);AssetDatabase.SaveAssets();
        }
        private static Sprite MakeRounded()
        {
            string path=Root+"Art/Icons/rounded.png";var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(0,Mathf.Abs(x-31.5f)-16),dy=Mathf.Max(0,Mathf.Abs(y-31.5f)-16);float alpha=Mathf.Clamp01(15.5f-Mathf.Sqrt(dx*dx+dy*dy));pixels[y*64+x]=new Color(1,1,1,alpha);}texture.SetPixels(pixels);File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.spriteBorder=Vector4.one*20;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static TMP_FontAsset MakeFont()
        {
            string path=Root+"Art/Fonts/Press Sans.asset";var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing!=null)return existing;
            string source=Root+"Art/Fonts/Press Sans.ttf";File.Copy("Assets/TextMesh Pro/Fonts/LiberationSans.ttf",source,true);AssetDatabase.ImportAsset(source);
            var font=AssetDatabase.LoadAssetAtPath<Font>(source);var asset=TMP_FontAsset.CreateFontAsset(font,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
            asset.name="Press Sans";asset.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
            asset.atlasPopulationMode=AtlasPopulationMode.Static;AssetDatabase.CreateAsset(asset,path);AssetDatabase.AddObjectToAsset(asset.material,asset);foreach(var atlas in asset.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,asset);EditorUtility.SetDirty(asset);return asset;
        }
        private static void StudioReflection()
        {
            string path=Root+"Generated/StudioReflection.asset";var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);if(cube==null){cube=new Cubemap(32,TextureFormat.RGB24,false);AssetDatabase.CreateAsset(cube,path);}
            for(int face=0;face<6;face++){var colors=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float softbox=Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(x-12)/12f),4)*.65f;float value=face==2?.85f:face==3?.18f:.24f+y/64f+softbox;colors[y*32+x]=new Color(value,value*.98f,value*.94f);}cube.SetPixels(colors,(CubemapFace)face);}cube.Apply();EditorUtility.SetDirty(cube);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=cube;
        }
        private static void ConfigurePipeline()
        {
            var renderer=LoadOrCreate<UniversalRendererData>(Root+"Generated/PressRenderer.asset");renderer.renderingMode=RenderingMode.Forward;
            if(renderer.rendererFeatures.Count==0)
            {
                var source=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
                if(source!=null && source.rendererFeatures.Count>0)
                {
                    var ao=UnityEngine.Object.Instantiate(source.rendererFeatures[0]);ao.name="Lightweight studio occlusion";
                    AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);
                    var serialized=new SerializedObject(ao);serialized.FindProperty("m_Settings.Downsample").boolValue=true;serialized.FindProperty("m_Settings.Samples").enumValueIndex=2;serialized.FindProperty("m_Settings.BlurQuality").enumValueIndex=1;serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            EditorUtility.SetDirty(renderer);
            string path=Root+"Generated/PressPipeline.asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,path);}
            pipeline.msaaSampleCount=4;pipeline.renderScale=1;pipeline.shadowDistance=22;pipeline.mainLightShadowmapResolution=2048;pipeline.shadowCascadeCount=1;pipeline.supportsHDR=true;
            var pipelineSettings=new SerializedObject(pipeline);pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue=true;pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            foreach(var feature in renderer.rendererFeatures)if(feature!=null)feature.SetActive(true);
            pipeline.supportsCameraOpaqueTexture=false;
            GraphicsSettings.defaultRenderPipeline=pipeline;
            for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i);QualitySettings.renderPipeline=pipeline;}QualitySettings.SetQualityLevel(0);
            EditorUtility.SetDirty(pipeline);
            QualitySettings.vSyncCount=0;Time.fixedDeltaTime=1f/50;Time.maximumDeltaTime=.1f;
        }
        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName="Copperline Games";PlayerSettings.productName="Hydraulic Press";PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=false;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.WebGL.maximumMemorySize=512;
            PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;PlayerSettings.WebGL.template="PROJECT:HydraulicPress";
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL,ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL,UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            UnityEditor.WebGL.UserBuildSettings.codeOptimization=UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        }
        [MenuItem("Hydraulic Press/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigurePipeline();ConfigurePlayer();AssetDatabase.SaveAssets();Directory.CreateDirectory("Builds/WebGL");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"Scenes/HydraulicPress.unity"},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
            Directory.CreateDirectory("Reports");File.WriteAllText("Reports/build-summary.txt",report.summary.result+"\nBytes: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace HydraulicPress.Editor
{
    // Editor-only adapter for the installed Unity 6.3 Device Simulator.
    public static class PhoneSimulatorQA
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static Type WindowType => typeof(UnityEditor.DeviceSimulation.DeviceSimulator).Assembly.GetTypes().First(t => t.Name == "SimulatorWindow");
        private static EditorWindow Window => EditorWindow.GetWindow(WindowType);
        private static object Main => WindowType.GetProperty("main", Flags).GetValue(Window);
        public static string Select(string name, bool landscape = false)
        {
            var window = Window; window.Show(); window.Focus();
            WindowType.BaseType.GetMethod("SetFocus",Flags).Invoke(window,new object[]{true});
            var main = Main; var type = main.GetType();
            var devices = ((Array)type.GetProperty("devices", Flags).GetValue(main)).Cast<UnityEngine.Object>().ToArray();
            int index = Array.FindIndex(devices, d => d.name == name);
            if (index < 0) throw new ArgumentException("Unknown installed simulator profile: " + name);
            type.GetProperty("deviceIndex", Flags).SetValue(main, index);
            type.GetMethod("Enable", Flags).Invoke(main, null);
            var screen = type.GetProperty("ScreenSimulation", Flags).GetValue(main);
            screen.GetType().GetProperty("orientation", Flags).SetValue(screen, landscape ? ScreenOrientation.LandscapeLeft : ScreenOrientation.Portrait);
            screen.GetType().GetMethod("ApplyChanges",Flags).Invoke(screen,null);
            WindowType.GetProperty("targetSize",Flags).SetValue(window,type.GetProperty("targetSize",Flags).GetValue(main));
            window.Repaint();
            return devices[index].name;
        }
        public static string Capture(string label)
        {
            var window = Window; window.Focus(); window.Repaint();
            // Repaint can be deferred while automation owns focus. Read a fresh game frame.
            WindowType.BaseType.GetMethod("RenderView", Flags).Invoke(window, new object[] { Vector2.zero, false });
            var rt = (RenderTexture)WindowType.BaseType.GetField("m_TargetTexture", Flags).GetValue(window);
            if (rt == null) throw new InvalidOperationException("Simulator has not rendered a frame yet.");
            string folder = "Reports/2026-11-05"; Directory.CreateDirectory(folder);
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
            if(SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels=texture.GetPixels32(); var flipped=new Color32[pixels.Length];
                for(int y=0;y<rt.height;y++)Array.Copy(pixels,y*rt.width,flipped,(rt.height-1-y)*rt.width,rt.width);
                texture.SetPixels32(flipped);texture.Apply();
            }
            string file = folder + "/" + label + ".png"; File.WriteAllBytes(file, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture); RenderTexture.active = previous;
            return file;
        }
        public static object Inspect()
        {
            var main = Main;
            var device = (UnityEngine.Object)main.GetType().GetProperty("currentDevice", Flags).GetValue(main);
            return new { device = device.name, width = Screen.width, height = Screen.height, safeArea = Screen.safeArea.ToString(), dpi = Screen.dpi, orientation = Screen.orientation.ToString(), timeScale = Time.timeScale };
        }
        [Serializable] private sealed class Audit
        {
            public string label, device, orientation, navigation, screenshot;
            public int width, height;
            public Rect safeArea;
            public UIManager.ButtonBounds[] buttons;
        }
        public static string Record(string label)
        {
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Canvas.ForceUpdateCanvases();
            var main=Main;
            string device=((UnityEngine.Object)main.GetType().GetProperty("currentDevice",Flags).GetValue(main)).name;
            var bounds=game.hud.GetButtonBounds(); var area=Screen.safeArea;
            float pointsScale=device.Contains("SE")?2:3;
            foreach(var b in bounds)
            {
                float left=(b.x-b.width/2)*Screen.width, right=(b.x+b.width/2)*Screen.width;
                float bottom=(1-b.y-b.height/2)*Screen.height, top=(1-b.y+b.height/2)*Screen.height;
                if(left<area.xMin-2 || right>area.xMax+2 || bottom<area.yMin-2 || top>area.yMax+2)
                    throw new InvalidOperationException(label+": control outside safe area: "+b.name);
                if(b.width*Screen.width/pointsScale<44 || b.height*Screen.height/pointsScale<44)
                    throw new InvalidOperationException(label+": touch target below 44 points: "+b.name);
            }
            var audit=new Audit{label=label,device=device,orientation=Screen.orientation.ToString(),navigation=game.Navigation.ToString(),width=Screen.width,height=Screen.height,safeArea=area,buttons=bounds,screenshot=Capture(label)};
            string json=JsonUtility.ToJson(audit); File.AppendAllText("Reports/2026-11-05/simulator.jsonl",json+Environment.NewLine); return json;
        }
    }
}

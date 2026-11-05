using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrickRush.Editor
{
    /// <summary>Editor-only adapter to Unity 6's built-in Device Simulator; never included in a player.</summary>
    public static class IPhoneValidation
    {
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        static Type WindowType=>typeof(UnityEditor.DeviceSimulation.DeviceSimulator).Assembly.GetType("UnityEditor.DeviceSimulation.SimulatorWindow");
        static EditorWindow Window=>EditorWindow.GetWindow(WindowType);
        static object Main=>WindowType.GetField("m_Main",Flags).GetValue(Window);
        public static string[] Devices()
        {
            var main=Main;var devices=(Array)main.GetType().GetProperty("devices",Flags).GetValue(main);var names=new string[devices.Length];
            for(int i=0;i<names.Length;i++)names[i]=((UnityEngine.Object)devices.GetValue(i)).name;return names;
        }
        public static void Select(string name,bool landscape)
        {
            var main=Main;var names=Devices();int index=Array.IndexOf(names,name);if(index<0)throw new Exception("Simulator profile unavailable: "+name);
            main.GetType().GetProperty("deviceIndex",Flags).SetValue(main,index);Window.Focus();main.GetType().GetMethod("Enable",Flags).Invoke(main,null);
            UnityEngine.Device.Screen.orientation=landscape?ScreenOrientation.LandscapeLeft:ScreenOrientation.Portrait;
            var simulation=main.GetType().GetProperty("ScreenSimulation",Flags).GetValue(main);simulation.GetType().GetMethod("ApplyChanges",Flags).Invoke(simulation,null);Window.Repaint();
        }
        public static string Audit(string filename)
        {
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(game==null)throw new Exception("Enter Play Mode first.");
            var layout=game.UI.InspectLayout();
            if(!layout.mobile)throw new Exception("Device Simulator must be active.");
            float expected=9.9f/14.6f;if(Mathf.Abs(layout.arena.width/layout.arena.height-expected)>.001f)throw new Exception("Arena aspect ratio changed.");
            foreach(var button in layout.buttons)if(button.rect.xMin<layout.safe.xMin-2||button.rect.yMin<layout.safe.yMin-2||button.rect.xMax>layout.safe.xMax+2||button.rect.yMax>layout.safe.yMax+2)throw new Exception("Button outside safe area: "+button.text);
            var json=JsonUtility.ToJson(layout,true);Directory.CreateDirectory("TestResults/iPhone/unity");File.WriteAllText("TestResults/iPhone/unity/"+filename+".json",json);
            ScreenCapture.CaptureScreenshot("TestResults/iPhone/unity/"+filename+".png");return json;
        }
        public static void Press(string text)
        {
            foreach(var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if(!b.isActiveAndEnabled)continue;
                foreach(var label in b.GetComponentsInChildren<TextMeshProUGUI>())if(label.text.Trim()==text){b.onClick.Invoke();return;}
            }
            throw new Exception("Visible button unavailable: "+text);
        }
    }
}

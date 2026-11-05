#if DEVELOPMENT_BUILD && UNITY_STANDALONE
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BloomDoom
{
    public sealed class GardenRuntimeSmoke : MonoBehaviour
    {
        bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--garden-smoke")>=0)new GameObject("Native game smoke check").AddComponent<GardenRuntimeSmoke>();}
        IEnumerator Start()
        {
            Application.logMessageReceived+=OnLog;yield return new WaitForSecondsRealtime(2);
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","..","Logs","QA"));Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"unity-main-menu.png"));yield return new WaitForSecondsRealtime(1);
            var game=FindFirstObjectByType<BloomDoomGame>();if(game==null){failed=true;}else{
                game.StartDay(0);var sim=game.Simulation;sim.Sunshine=2000;
                for(int row=0;row<5;row++){sim.Cooldowns[0]=sim.Cooldowns[1]=sim.Cooldowns[2]=0;sim.Place(0,row,0,out _);sim.Place(1,row,2,out _);sim.Place(2,row,6,out _);}
                sim.SendWave();sim.Spawn(1,1,1120);sim.Spawn(3,3,1190);yield return new WaitForSecondsRealtime(4);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"unity-gameplay.png"));yield return new WaitForSecondsRealtime(1);
                if(sim.Plants.Count<10||sim.Phase!=GardenPhase.Wave)failed=true;
            }
            File.WriteAllText(Path.Combine(directory,"native-smoke-result.json"),"{\"passed\":"+(!failed?"true":"false")+",\"nativeUnity\":true}");Application.logMessageReceived-=OnLog;Application.Quit(failed?1:0);
        }
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failed=true;}
    }
}
#endif

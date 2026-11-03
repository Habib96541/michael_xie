using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BackyardDojo.Editor
{
    [InitializeOnLoad]
    public static class MobileVerification
    {
        const string Pending="Dojo.MobileVerification.Pending";
        static bool running;
        static MobileVerification() { EditorApplication.update+=Tick; }
        public static void Run()
        {
            SessionState.SetBool(Pending,true);
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Pending,false) || running)return;
            if(EditorApplication.timeSinceStartup>180){Finish("Verification timed out.",1);return;}
            var game=FruitNinjaGame.Instance;
            if(!EditorApplication.isPlaying || game==null || !game.IsReady)return;
            running=true;
            try
            {
                game.RunMobileSmokeTest(mobile=>game.RunRecutSmokeTest(recut=>game.RunSmokeTest(gameplay=>game.RunUiSmokeTest(ui=>
                {
                    string result=mobile+"\n"+recut+"\n"+gameplay+"\n"+ui;
                    Finish(result,result.Contains("=False")?1:0);
                }))));
            }
            catch(Exception error){Finish(error.ToString(),1);}
        }
        static void Finish(string result,int code)
        {
            SessionState.SetBool(Pending,false);
            string artifacts=Environment.GetEnvironmentVariable("DOJO_VERIFY_ARTIFACTS") ?? Path.GetFullPath("Artifacts");
            Directory.CreateDirectory(artifacts);
            File.WriteAllText(Path.Combine(artifacts,"mobile-result.txt"),result);
            Debug.Log("JUICE_DOJO_MOBILE_VERIFICATION: "+result);
            EditorApplication.Exit(code);
        }
    }
}

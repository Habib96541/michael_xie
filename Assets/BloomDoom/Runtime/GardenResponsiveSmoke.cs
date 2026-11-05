#if DEVELOPMENT_BUILD && UNITY_STANDALONE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BloomDoom
{
    public sealed class GardenResponsiveSmoke : MonoBehaviour
    {
        int checks;bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Run(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--garden-layout-smoke")>=0)new GameObject("Client landscape checks").AddComponent<GardenResponsiveSmoke>();}
        void Check(bool result,string label){checks++;if(!result){failed=true;Debug.LogError("[ClientLayout] "+label);}}
        Rect ScreenRect(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
        IEnumerator Start()
        {
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","..","Logs","QA"));Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(3);
            var game=FindFirstObjectByType<BloomDoomGame>();var layout=game.GetComponent<GardenResponsiveLayout>();
            var safe=GardenResponsiveLayout.SafeFrame(new Vector2(2170,1000),new Vector2(868,400),new Rect(47,21,774,379));
            Check(Mathf.Abs(safe.width-1935)<.01f&&Mathf.Abs(safe.height-947.5f)<.01f,"Notch and home indicator safe-area dimensions");
            Check(Mathf.Abs(safe.center.y-26.25f)<.01f&&Mathf.Abs(safe.center.x)<.01f,"Asymmetric safe-area centering");
            foreach(var size in new[]{new Vector2Int(868,400),new Vector2Int(568,262),new Vector2Int(932,430),new Vector2Int(1440,1010),new Vector2Int(868,400)}){
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);yield return new WaitForEndOfFrame();
                bool wide=size.x/(float)size.y>=1.7f;
                Check(layout.Wide==wide,"Native responsive breakpoint "+size);
                if(!wide){Check(Mathf.Abs(layout.Stage.rect.width-1480)<.1f,"Original layout restores after rotation");continue;}
                var stage=ScreenRect(layout.Stage);var board=ScreenRect(layout.Field);
                Check(Mathf.Abs(stage.width-Screen.width)<2&&Mathf.Abs(stage.height-Screen.height)<2,"Stage fills viewport "+size);
                Check(stage.xMin>=-1&&stage.xMax<=Screen.width+1&&stage.yMin>=-1&&stage.yMax<=Screen.height+1,"Stage is not cropped");
                Check(board.width/Screen.width>.97f,"Board uses at least 97% of screen width");
                game.StartDay(0);game.Simulation.Sunshine=10000;
                game.GetComponentsInChildren<Button>().First(b=>b.name=="Seed "+GardenData.Seeds[1].Name).onClick.Invoke();
                var pointer=game.GetComponentInChildren<GardenPointer>();
                for(int row=0;row<5;row++)for(int column=0;column<9;column++){
                    var point=GardenData.Center(row,column);var screen=RectTransformUtility.WorldToScreenPoint(null,pointer.transform.TransformPoint(new Vector3(point.x,-point.y,0)));
                    game.Simulation.Cooldowns[1]=0;
                    pointer.OnPointerDown(new PointerEventData(EventSystem.current){position=screen,button=PointerEventData.InputButton.Left});
                    Check(game.Simulation.At(row,column)?.Kind==1,"Click maps to widened patch "+row+","+column);
                }
                yield return null;yield return new WaitForEndOfFrame();
                foreach(var image in game.GetComponentsInChildren<Image>().Where(i=>i.name=="Animated illustration"))Check(Mathf.Abs(image.transform.parent.lossyScale.x/image.transform.parent.lossyScale.y-1)<.01f,"Resident remains proportional");
                game.StartDay(0);game.Simulation.Sunshine=2000;
                for(int row=0;row<5;row++){game.Simulation.Cooldowns[0]=game.Simulation.Cooldowns[1]=0;game.Simulation.Place(0,row,0,out _);game.Simulation.Place(1,row,2,out _);}
                game.Simulation.Spawn(0,1,1150);game.Simulation.Spawn(1,3,1200);
                yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"client-fullscreen-"+size.x+"x"+size.y+".png"));yield return new WaitForSecondsRealtime(.3f);
            }
            File.WriteAllText(Path.Combine(directory,"client-native-layout.json"),"{\"passed\":"+(!failed?"true":"false")+",\"checks\":"+checks+",\"nativeUnity\":true}");
            Debug.Log("[ClientLayout] "+(failed?"FAIL":"PASS")+": "+checks+" native layout, input and aspect checks.");Application.Quit(failed?1:0);
        }
    }
}
#endif

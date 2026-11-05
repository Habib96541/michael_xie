#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BullRide.EditorTools
{
    public static class BuildiOS
    {
        [MenuItem("Bull Ride/Export iOS for Device")]
        public static void Device() => Export(iOSSdkVersion.DeviceSDK, "Deliverables/BullRide-iOS");

        [MenuItem("Bull Ride/Export iOS for Simulator")]
        public static void Simulator() => Export(iOSSdkVersion.SimulatorSDK, "Deliverables/BullRide-iOS-Simulator");

        static void Export(iOSSdkVersion sdk, string destination)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new InvalidOperationException("Install iOS Build Support for this Unity Editor to export an Xcode project.");

            ProjectSetup.EnsureScene();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            iOSSdkVersion previousSdk = PlayerSettings.iOS.sdkVersion;
            try
            {
                PlayerSettings.iOS.sdkVersion = sdk;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ProjectSetup.ScenePath },
                    locationPathName = destination,
                    target = BuildTarget.iOS
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("iOS export failed: " + report.summary.result);
                UnityEngine.Debug.Log("Xcode project exported to " + destination);
            }
            finally
            {
                PlayerSettings.iOS.sdkVersion = previousSdk;
            }
        }
    }
}
#endif

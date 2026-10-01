using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;

namespace OmniScan.Editor
{

    public static class OmniScanBuild
    {
        const string MobileProfile = "Assets/Settings/Build Profiles/Android™.asset";
        const string QuestProfile = "Assets/Settings/Build Profiles/Quest3.asset";
        const string MobileScene = "Assets/Scenes/SampleScene.unity";
        const string ARCoreLoader = "UnityEngine.XR.ARCore.ARCoreLoader";
        const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

        [MenuItem("OmniScan/Build/Mobile APK (ARCore)")]
        public static void BuildMobile() =>
            Build(MobileProfile, MobileScene, ARCoreLoader, OpenXRLoader, "Builds/omniscan-mobile.apk");

        [MenuItem("OmniScan/Build/Quest 3 APK (OpenXR)")]
        public static void BuildQuest()
        {
            if (!File.Exists(QuestSceneBuilder.ScenePath)) QuestSceneBuilder.Create();
            Build(QuestProfile, QuestSceneBuilder.ScenePath, OpenXRLoader, ARCoreLoader, "Builds/omniscan-quest3.apk");
        }

        static void Build(string profilePath, string scene, string enable, string disable, string output)
        {
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath)
                ?? throw new InvalidOperationException($"Build profile not found: {profilePath}");

            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            XRPackageMetadataStore.RemoveLoader(settings.AssignedSettings, disable, BuildTargetGroup.Android);
            if (!XRPackageMetadataStore.AssignLoader(settings.AssignedSettings, enable, BuildTargetGroup.Android))
                throw new InvalidOperationException($"Could not assign XR loader {enable}. Is its package installed?");

            profile.overrideGlobalScenes = true;
            profile.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
            EditorUtility.SetDirty(profile);

            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = output,
                options = BuildOptions.None,
            });
            Debug.Log($"[OmniScanBuild] {output}: {report.summary.result}");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException($"Build failed: {report.summary.result}");
        }
    }
}


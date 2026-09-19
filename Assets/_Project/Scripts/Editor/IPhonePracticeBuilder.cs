using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BarrelRivals.Editor
{
    /// <summary>Exports the practice scene. Xcode compilation, signing and installation are separate steps.</summary>
    public static class IPhonePracticeBuilder
    {
        public const string ExportPath = "Builds/iOS/BarrelRivals-Practice";

        [MenuItem("Barrel Rivals/Build iPhone Practice (Xcode Export)")]
        public static void Export()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new InvalidOperationException("Install iOS Build Support for this Unity Editor before exporting.");

            PracticeBuilder.Validate();
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.iOS.buildNumber = "3";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });

            // Keep the existing development bundle identifier and any user-selected team.
            // An Apple account/team is selected in Xcode; no credentials belong in this project.
            Directory.CreateDirectory("Builds/iOS");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PracticeBuilder.ScenePath },
                locationPathName = ExportPath,
                target = BuildTarget.iOS,
                options = BuildOptions.Development
            });
            Debug.Log($"BARREL_IOS: Xcode export {report.summary.result}; errors={report.summary.totalErrors}.");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("iPhone practice Xcode export failed.");
            if (!File.Exists(Path.Combine(ExportPath, "Unity-iPhone.xcodeproj/project.pbxproj")))
                throw new InvalidOperationException("Unity did not produce the expected Xcode project.");
            Debug.Log("BARREL_IOS: Export only. Native compilation, signing and iPhone installation have not run.");
        }
    }
}

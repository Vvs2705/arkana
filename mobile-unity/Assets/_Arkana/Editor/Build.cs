using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arkana.EditorTools
{
    /// <summary>
    /// Build Android por linha de comando (build_apk.ps1 -> -executeMethod Arkana.EditorTools.Build.Android).
    /// Receita herdada do Limiar (08/09/2026): settings por script, cena montada por codigo, APK em Builds/.
    /// </summary>
    public static class Build
    {
        public const string SettingsDir = "Assets/_Arkana/Settings";
        const string UrpPath = SettingsDir + "/URP_Base.asset";
        public const string ScenePath = "Assets/_Arkana/Scenes/Main.unity";

        [MenuItem("Arkana/Aplicar settings do projeto")]
        public static void ApplySettings()
        {
            UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath);
            if (urp != null)
            {
                GraphicsSettings.defaultRenderPipeline = urp;
                int current = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = urp;
                    QualitySettings.vSyncCount = 0;
                }
                QualitySettings.SetQualityLevel(current, false);
            }

            PlayerSettings.companyName = "V-STACK";
            PlayerSettings.productName = "Arkana";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "br.com.vstack.arkana");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Arkana.Build: settings aplicados (URP, Android IL2CPP/ARM64, minSdk 26, landscape).");
        }

        [MenuItem("Arkana/Build Android")]
        public static void Android()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string apk = Path.Combine(root, "Builds", "arkana.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(apk));

            ApplySettings();
            MainSceneBuilder.Build();

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apk,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            BuildSummary s = report.summary;
            Debug.Log(string.Format("BuildSummary: result={0} time={1} errors={2} warnings={3} output={4}",
                s.result, s.totalTime, s.totalErrors, s.totalWarnings, s.outputPath));
            if (s.result == BuildResult.Succeeded) return;

            foreach (BuildStep step in report.steps)
                foreach (BuildStepMessage msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError("[" + step.name + "] " + msg.content);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}

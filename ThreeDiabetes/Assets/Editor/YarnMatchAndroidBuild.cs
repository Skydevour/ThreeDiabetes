#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class YarnMatchAndroidBuild
{
    private const string ApplicationIdentifier = "com.yarnmatch.offline";
    private const string OutputDirectoryName = "build";
    private const string OutputFileName = "YarnMatch.apk";

    [MenuItem("Yarn Match/Build Android APK")]
    public static void BuildAndroidApk()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDirectory = Path.Combine(projectRoot, OutputDirectoryName);
        string outputPath = Path.Combine(outputDirectory, OutputFileName);
        Directory.CreateDirectory(outputDirectory);

        List<string> scenePaths = GetEnabledScenePaths();
        if (scenePaths.Count == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");
        }

        PlayerSettings.companyName = "Yarn Match";
        PlayerSettings.productName = "Yarn Match";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, ApplicationIdentifier);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        YarnMatchAndroidIcons.Apply();
        EditorUserBuildSettings.buildAppBundle = false;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenePaths.ToArray(),
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Android APK build failed: " + report.summary.result + ". See the Unity Editor log for details.");
        }

        Debug.Log("Yarn Match APK built: " + outputPath);
    }

    public static void BuildAndroidApkFromCommandLine()
    {
        BuildAndroidApk();
    }

    private static List<string> GetEnabledScenePaths()
    {
        List<string> scenePaths = new List<string>();
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int index = 0; index < scenes.Length; index++)
        {
            if (scenes[index].enabled && !string.IsNullOrEmpty(scenes[index].path))
            {
                scenePaths.Add(scenes[index].path);
            }
        }

        return scenePaths;
    }
}
#endif

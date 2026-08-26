#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuildTools
{
    private const string PackageName = "com.lootbugs.game";
    private const string GameTitle = "Lootbugs";
    private const string BuildOutputDir = "Builds/Android";
    private const string OutputApkName = "Lootbugs.apk";

    [MenuItem("Lootbugs/Android/1. Configure Android Project Settings", false, 100)]
    public static void ConfigureAndroidSettings()
    {
        Debug.Log("[AndroidBuildTools] Configuring PlayerSettings for Android...");

        // 1. Identification & Naming
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.productName = GameTitle;
        PlayerSettings.companyName = "Lootbugs";

        // 2. Orientation Settings (Landscape Left & Right for dual-grip motion gaming)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // 3. Sensor & Performance Settings
        PlayerSettings.accelerometerFrequency = 60;

        // 4. Target Architecture & API Levels
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0+
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34; // Android 14
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // 5. Fullscreen and Rendering
        PlayerSettings.Android.renderOutsideSafeArea = true;

        AssetDatabase.SaveAssets();
        Debug.Log($"[AndroidBuildTools] Successfully configured Android PlayerSettings for {GameTitle} ({PackageName})!");
        EditorUtility.DisplayDialog("Lootbugs Android Config", "Android Project Settings successfully configured!\n\n• Package: com.lootbugs.game\n• Orientation: Landscape (Motion Assisted)\n• Sensor Frequency: 60Hz\n• Architecture: ARM64\n• Min SDK: API 24 (Android 7.0)", "OK");
    }

    [MenuItem("Lootbugs/Android/2. Build Android APK", false, 101)]
    public static void BuildAndroidApk()
    {
        ConfigureAndroidSettings();

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[AndroidBuildTools] No enabled scenes found in EditorBuildSettings!");
            EditorUtility.DisplayDialog("Build Error", "No scenes are enabled in Build Settings!", "OK");
            return;
        }

        string fullOutputDir = Path.Combine(Directory.GetCurrentDirectory(), BuildOutputDir);
        if (!Directory.Exists(fullOutputDir))
        {
            Directory.CreateDirectory(fullOutputDir);
        }

        string apkPath = Path.Combine(fullOutputDir, OutputApkName);
        Debug.Log($"[AndroidBuildTools] Starting Android build targeting: {apkPath}");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AndroidBuildTools] Android Build Succeeded! Size: {summary.totalSize / (1024 * 1024)} MB | Path: {apkPath}");
            EditorUtility.RevealInFinder(apkPath);
            EditorUtility.DisplayDialog("Build Succeeded", $"Android APK built successfully!\n\nOutput: {apkPath}\nTotal Size: {summary.totalSize / (1024 * 1024)} MB", "OK");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[AndroidBuildTools] Android Build Failed! Total Errors: {summary.totalErrors}");
            EditorUtility.DisplayDialog("Build Failed", $"Android build failed with {summary.totalErrors} error(s). Check Console for details.", "OK");
        }
    }
}
#endif

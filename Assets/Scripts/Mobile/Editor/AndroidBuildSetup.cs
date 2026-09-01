#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AndroidBuildSetup
{
    [MenuItem("Lootbugs/Mobile/Configure Android Settings")]
    public static void ConfigureAndroidSettings()
    {
        // 1. Orientation Settings
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // 2. Mobile Presentation & Resolution
        PlayerSettings.statusBarHidden = true;
        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // 3. Android Graphics APIs (Vulkan primary, OpenGLES3 fallback - No HDRP compute requirements)
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[]
        {
            GraphicsDeviceType.Vulkan,
            GraphicsDeviceType.OpenGLES3
        });

        // 4. Android Texture Compression & Frame Rate
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        Debug.Log("<color=#00FF88>[AndroidBuildSetup]</color> Android settings configured successfully! (URP/Mobile-safe, Vulkan/GLES3, Landscape, 60 FPS)");
        EditorUtility.DisplayDialog(
            "Android Configuration",
            "Android settings have been successfully configured for Mobile (Non-HDRP)!\n\n" +
            "• Render Pipeline: Universal Render Pipeline (URP) / Mobile Safe\n" +
            "• Graphics APIs: Vulkan & OpenGLES3\n" +
            "• Texture Compression: ASTC\n" +
            "• Orientation: Landscape Left & Right\n" +
            "• Target Frame Rate: 60 FPS",
            "OK"
        );
    }

    [MenuItem("Lootbugs/Mobile/Switch to Android Build Target")]
    public static void SwitchToAndroidTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            Debug.Log("[AndroidBuildSetup] Project is already on Android build target.");
            EditorUtility.DisplayDialog("Android Target", "Project is already targeting Android.", "OK");
            return;
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        ConfigureAndroidSettings();
    }

    [MenuItem("Lootbugs/Mobile/Upgrade Materials to URP (Mobile Safe)")]
    public static void UpgradeMaterialsToURP()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            urpLitShader = Shader.Find("Standard");
        }

        if (urpLitShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Universal Render Pipeline/Lit' or 'Standard' shader.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int convertedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader != null)
            {
                string shaderName = mat.shader.name;
                // If material is using HDRP or has missing shader, switch to URP Lit
                if (shaderName.StartsWith("HDRP") || shaderName.Contains("High Definition") || shaderName == "Hidden/InternalErrorShader")
                {
                    mat.shader = urpLitShader;
                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<color=#00FF88>[AndroidBuildSetup]</color> Converted {convertedCount} materials to URP Lit shader.");
        EditorUtility.DisplayDialog("Material Upgrade", $"Successfully converted {convertedCount} materials to URP/Mobile-safe shader!", "OK");
    }
}
#endif

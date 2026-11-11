using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build bản phát hành Android (APK + AAB) từ dòng lệnh, ký bằng duckDungeon.keystore:
/// DR_KEYSTORE_PASS=... DR_KEYALIAS_PASS=... Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod AndroidBuilder.BuildRelease
/// Mật khẩu chỉ đọc từ biến môi trường, không lưu vào project. Kết quả nằm ở Builds/ (đã gitignore).
/// Tool test (EquipmentTester, BossRushTester, phím cheat) nằm trong #if UNITY_EDITOR nên tự bị loại khỏi bản build.
/// </summary>
public static class AndroidBuilder
{
    public static void BuildRelease()
    {
        string keystorePass = System.Environment.GetEnvironmentVariable("DR_KEYSTORE_PASS");
        string aliasPass = System.Environment.GetEnvironmentVariable("DR_KEYALIAS_PASS") ?? keystorePass;
        if (string.IsNullOrEmpty(keystorePass))
        {
            Debug.LogError("[AndroidBuilder] Thiếu biến môi trường DR_KEYSTORE_PASS");
            EditorApplication.Exit(1);
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasPass = aliasPass;
        EditorUserBuildSettings.development = false;

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string name = "DungeonDuck_" + PlayerSettings.bundleVersion + "_" + PlayerSettings.Android.bundleVersionCode;
        Directory.CreateDirectory("Builds");

        bool ok = Build(scenes, "Builds/" + name + ".apk", false)
               && Build(scenes, "Builds/" + name + ".aab", true);

        EditorUserBuildSettings.buildAppBundle = false;
        if (!ok)
            EditorApplication.Exit(1);
    }

    static bool Build(string[] scenes, string output, bool appBundle)
    {
        EditorUserBuildSettings.buildAppBundle = appBundle;
        Debug.Log("[AndroidBuilder] Build " + output + " Scenes=" + string.Join(", ", scenes));
        BuildReport report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.Android, BuildOptions.None);
        Debug.Log("[AndroidBuilder] " + output + " Result=" + report.summary.result
                  + " Errors=" + report.summary.totalErrors + " Size=" + report.summary.totalSize);
        return report.summary.result == BuildResult.Succeeded;
    }
}

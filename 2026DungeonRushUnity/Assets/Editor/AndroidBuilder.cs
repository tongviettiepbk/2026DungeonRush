using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 2 kiểu build Android (xem DevBuild.cs):
/// - Product (BuildRelease): APK + AAB lên store, ký duckDungeon.keystore, KHÔNG có DEV_BUILD → tool/cheat bị loại, mua/ads thật.
/// - Dev (BuildDev): 1 APK thêm define DEV_BUILD → bảng cheat "Đồ"/"Boss"/"Cheat", mua IAP + xem ads luôn thành công,
///   tắt Analytics/AppsFlyer. Có mật khẩu keystore thì ký giống Product (cài đè qua lại không phải gỡ), không thì ký debug.
/// Dòng lệnh (Unity phải đóng project):
/// DR_KEYSTORE_PASS=... DR_KEYALIAS_PASS=... Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod AndroidBuilder.BuildRelease
/// Trong Editor: menu Tools/DungeonRush/Build. Mật khẩu lấy từ biến môi trường, không có thì lấy ô Player Settings > Publishing.
/// Mật khẩu không lưu vào project. Kết quả nằm ở Builds/ (đã gitignore).
/// </summary>
public static class AndroidBuilder
{
    private const string DEV_DEFINE = "DEV_BUILD";

    [MenuItem("Tools/DungeonRush/Build/Android Product (APK + AAB)")]
    public static void BuildRelease()
    {
        // DEV_BUILD lọt vào define của project là bản store có cheat + mua free → chặn hẳn.
        string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        if (defines.Split(';').Contains(DEV_DEFINE))
        {
            Fail("Player Settings đang có define " + DEV_DEFINE + " — gỡ ra rồi mới build Product");
            return;
        }

        if (ApplyKeystore() == false)
        {
            Fail("Thiếu mật khẩu keystore (DR_KEYSTORE_PASS hoặc Player Settings > Publishing)");
            return;
        }

        string name = BaseName();
        bool ok = Build("Builds/" + name + ".apk", false, null)
               && Build("Builds/" + name + ".aab", true, null);

        EditorUserBuildSettings.buildAppBundle = false;
        if (!ok)
            Fail("Build Product lỗi");
    }

    [MenuItem("Tools/DungeonRush/Build/Android Dev (APK cheat)")]
    public static void BuildDev()
    {
        bool hasKeystore = ApplyKeystore();
        bool oldCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        if (hasKeystore == false)
        {
            Debug.LogWarning("[AndroidBuilder] Không có mật khẩu keystore → ký debug; cài lên máy đang có bản Product phải gỡ trước.");
            PlayerSettings.Android.useCustomKeystore = false;
        }

        bool ok = Build("Builds/" + BaseName() + "_DEV.apk", false, new[] { DEV_DEFINE });

        PlayerSettings.Android.useCustomKeystore = oldCustomKeystore;
        EditorUserBuildSettings.buildAppBundle = false;
        if (!ok)
            Fail("Build Dev lỗi");
    }

    // Gán mật khẩu ký: ưu tiên biến môi trường, không có thì giữ ô đã nhập ở Player Settings (chỉ còn trong phiên Editor).
    private static bool ApplyKeystore()
    {
        string keystorePass = System.Environment.GetEnvironmentVariable("DR_KEYSTORE_PASS");
        string aliasPass = System.Environment.GetEnvironmentVariable("DR_KEYALIAS_PASS") ?? keystorePass;
        if (string.IsNullOrEmpty(keystorePass) == false)
        {
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasPass = aliasPass;
        }

        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
            return false;

        PlayerSettings.Android.useCustomKeystore = true;
        return true;
    }

    private static string BaseName()
    {
        return "DungeonDuck_" + PlayerSettings.bundleVersion + "_" + PlayerSettings.Android.bundleVersionCode;
    }

    private static bool Build(string output, bool appBundle, string[] extraDefines)
    {
        Directory.CreateDirectory("Builds");
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.buildAppBundle = appBundle;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None,
            extraScriptingDefines = extraDefines,
        };

        Debug.Log("[AndroidBuilder] Build " + output + " Defines=" + (extraDefines == null ? "-" : string.Join(";", extraDefines))
                  + " Scenes=" + string.Join(", ", options.scenes));
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("[AndroidBuilder] " + output + " Result=" + report.summary.result
                  + " Errors=" + report.summary.totalErrors + " Size=" + report.summary.totalSize);
        return report.summary.result == BuildResult.Succeeded;
    }

    // Batch thì thoát mã 1 cho script biết; trong Editor chỉ báo lỗi, không được đóng Editor.
    private static void Fail(string message)
    {
        Debug.LogError("[AndroidBuilder] " + message);
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}

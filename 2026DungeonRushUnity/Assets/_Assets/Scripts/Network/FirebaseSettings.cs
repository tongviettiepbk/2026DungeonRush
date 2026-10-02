// Cấu hình kết nối Firebase cho DungeonRush. Khi đã tạo Firebase project thật:
//   1. Đổi PROJECT_ID + WEB_API_KEY (Firebase console → Project settings → General → Web API Key).
//   2. USE_EMULATOR = false để gọi server thật (Cloud Functions us-central1).
// Emulator local (cd server && firebase emulators:start): project "demo-dungeonrush", không cần API key thật.
public static class FirebaseSettings
{
    public const string PROJECT_ID = "demo-dungeonrush";
    public const string WEB_API_KEY = "";
    public const string REGION = "us-central1";

#if UNITY_EDITOR
    public const bool USE_EMULATOR = true;
#else
    public const bool USE_EMULATOR = false;
#endif
    public const string EMULATOR_HOST = "127.0.0.1";
    public const int EMULATOR_AUTH_PORT = 9099;
    public const int EMULATOR_FUNCTIONS_PORT = 5001;

    public const int REQUEST_TIMEOUT_SECONDS = 20;

    public static string FunctionsBaseUrl => USE_EMULATOR
        ? "http://" + EMULATOR_HOST + ":" + EMULATOR_FUNCTIONS_PORT + "/" + PROJECT_ID + "/" + REGION
        : "https://" + REGION + "-" + PROJECT_ID + ".cloudfunctions.net";

    // Auth REST (Identity Toolkit). Emulator nhận mọi key.
    public static string AuthSignUpUrl => AuthHost + "identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + ApiKey;
    public static string AuthRefreshUrl => SecureTokenHost + "securetoken.googleapis.com/v1/token?key=" + ApiKey;

    private static string AuthHost => USE_EMULATOR ? "http://" + EMULATOR_HOST + ":" + EMULATOR_AUTH_PORT + "/" : "https://";
    private static string SecureTokenHost => AuthHost;
    private static string ApiKey => USE_EMULATOR ? "emulator-key" : WEB_API_KEY;
}

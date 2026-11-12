// Cấu hình kết nối Firebase cho DungeonRush: project thật dungeon-duck-quack-slash (Cloud Functions us-central1).
// Emulator local (cd server && firebase emulators:start --project demo-dungeonrush): không cần API key thật.
public static class FirebaseSettings
{
    public const string PROJECT_ID = "dungeon-duck-quack-slash";
    public const string WEB_API_KEY = "AIzaSyBcv3df1dRysLRq8b_YX90tYt8iMLdQ5Gg";
    public const string REGION = "us-central1";

    // Editor + build đều gọi server thật. Muốn test emulator local thì tạm đổi true (phiên đăng nhập lưu riêng theo đuôi _emu).
    public const bool USE_EMULATOR = false;
    public const string EMULATOR_PROJECT_ID = "demo-dungeonrush";
    public const string EMULATOR_HOST = "127.0.0.1";
    public const int EMULATOR_AUTH_PORT = 9099;
    public const int EMULATOR_FUNCTIONS_PORT = 5001;
    public const int EMULATOR_DATABASE_PORT = 9000;

    // Realtime Database (chat): instance mặc định của project. Bật ở Firebase console → Realtime Database → Create Database
    // (chọn us-central1 thì URL đúng như dưới; vùng khác URL dạng https://<project>-default-rtdb.<vùng>.firebasedatabase.app).
    public const string DATABASE_URL = "https://" + PROJECT_ID + "-default-rtdb.firebaseio.com";

    public const int REQUEST_TIMEOUT_SECONDS = 20;

    // Trường "server" gửi lên (clan lọc theo nó) — emulator dùng tên project demo như data seed.
    public static string ServerId => USE_EMULATOR ? EMULATOR_PROJECT_ID : PROJECT_ID;

    public static string FunctionsBaseUrl => USE_EMULATOR
        ? "http://" + EMULATOR_HOST + ":" + EMULATOR_FUNCTIONS_PORT + "/" + EMULATOR_PROJECT_ID + "/" + REGION
        : "https://" + REGION + "-" + PROJECT_ID + ".cloudfunctions.net";

    public static string DatabaseUrl => USE_EMULATOR
        ? "http://" + EMULATOR_HOST + ":" + EMULATOR_DATABASE_PORT
        : DATABASE_URL;

    // Emulator chọn instance qua ?ns= (database.rules.json nạp vào <project>-default-rtdb).
    public static string DatabaseNamespaceQuery => USE_EMULATOR ? "ns=" + EMULATOR_PROJECT_ID + "-default-rtdb" : string.Empty;

    // Auth REST (Identity Toolkit). Emulator nhận mọi key.
    public static string AuthSignUpUrl => AuthHost + "identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + ApiKey;
    public static string AuthRefreshUrl => SecureTokenHost + "securetoken.googleapis.com/v1/token?key=" + ApiKey;
    // Tài khoản email: đăng nhập / gửi thư đặt lại mật khẩu (gắn email vào khách dùng AuthSignUpUrl kèm idToken).
    public static string AuthSignInPasswordUrl => AuthHost + "identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + ApiKey;
    public static string AuthSendOobCodeUrl => AuthHost + "identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + ApiKey;

    private static string AuthHost => USE_EMULATOR ? "http://" + EMULATOR_HOST + ":" + EMULATOR_AUTH_PORT + "/" : "https://";
    private static string SecureTokenHost => AuthHost;
    private static string ApiKey => USE_EMULATOR ? "emulator-key" : WEB_API_KEY;
}

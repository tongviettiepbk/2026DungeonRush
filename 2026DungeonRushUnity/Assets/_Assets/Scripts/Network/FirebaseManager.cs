using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
#if FIREBASE_SDK
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
#endif

// Cổng vào Firebase của game: đăng nhập ẩn danh lấy ID token + gọi Cloud Functions (callable, qua HTTP như
// FunctionsController gốc gọi https://{fn}-...run.app bằng BestHTTP).
//   • Có Firebase Unity SDK (thêm Scripting Define FIREBASE_SDK sau khi import) → FirebaseAuth như StickIdle.
//   • Chưa có SDK / chạy emulator → Auth REST API (UnityWebRequest), refresh token lưu PlayerPrefs.
// Tài khoản email (FirebaseAuthController gốc: LinkWithCredential / SignInWithEmailAndPassword / SendPasswordResetEmail /
// SignOut) mới làm ở nhánh REST — bật FIREBASE_SDK thì phải viết thêm 4 lời gọi SDK tương ứng.
public class FirebaseManager : Singleton<FirebaseManager>
{
    private const string PREF_REFRESH_TOKEN = "fb_refresh_token";
    private const string PREF_UID = "fb_uid";
    private const string PREF_EMAIL = "fb_email";
    private const string ERROR_NETWORK = "NETWORK";

    public string Uid { get; private set; }
    public bool IsSignedIn => string.IsNullOrEmpty(Uid) == false;
    // Email đã gắn vào tài khoản; rỗng = tài khoản khách (ẩn danh).
    public string Email { get; private set; }
    public bool IsAnonymous => string.IsNullOrEmpty(Email);
    // Phiên của tài khoản email bị máy chủ từ chối (đổi mật khẩu ở máy khác, bị thu hồi...) → chờ người chơi đăng nhập lại,
    // KHÔNG tự tạo tài khoản khách đè lên (gốc: "Was-linked install with no session; awaiting sign-in").
    public bool NeedsSignIn { get; private set; }

    private string idToken;
    private DateTime idTokenExpireUtc;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Uid = PlayerPrefs.GetString(PrefKey(PREF_UID), string.Empty);
        Email = PlayerPrefs.GetString(PrefKey(PREF_EMAIL), string.Empty);
    }

    // ===== Gọi Cloud Function (callable protocol: body {"data":..} → {"result":..} | {"error":..}) =====

    // tokenOverride: gọi thay cho một tài khoản khác tài khoản đang đăng nhập (dọn tài khoản khách vừa bị bỏ lại).
    public void Call<TResponse>(string functionName, object data, Action<bool, TResponse, string> callback, string tokenOverride = null)
    {
        StartCoroutine(RoutineCall(functionName, data, callback, tokenOverride));
    }

    private IEnumerator RoutineCall<TResponse>(string functionName, object data, Action<bool, TResponse, string> callback, string tokenOverride)
    {
        string token = tokenOverride;
        if (string.IsNullOrEmpty(token))
        {
            yield return GetIdToken(t => token = t);
        }
        if (string.IsNullOrEmpty(token))
        {
            callback?.Invoke(false, default(TResponse), "Not signed in");
            yield break;
        }

        string url = FirebaseSettings.FunctionsBaseUrl + "/" + functionName;
        string body = JsonConvert.SerializeObject(new { data = data ?? new object() });

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = FirebaseSettings.REQUEST_TIMEOUT_SECONDS;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();

            string text = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                DebugCustom.LogWarning("[Firebase] " + functionName + " failed: " + request.error + " | " + text);
                callback?.Invoke(false, default(TResponse), string.IsNullOrEmpty(text) ? request.error : text);
                yield break;
            }

            TResponse result;
            try
            {
                JObject json = JObject.Parse(text);
                JToken resultToken = json["result"];
                if (resultToken == null)
                {
                    callback?.Invoke(false, default(TResponse), text);
                    yield break;
                }
                result = resultToken.ToObject<TResponse>();
            }
            catch (Exception e)
            {
                DebugCustom.LogError("[Firebase] Parse " + functionName + ": " + e.Message + " | " + text);
                callback?.Invoke(false, default(TResponse), e.Message);
                yield break;
            }

            callback?.Invoke(true, result, text);
        }
    }

    // ===== Auth =====

    public IEnumerator GetIdToken(Action<string> callback)
    {
        if (string.IsNullOrEmpty(idToken) == false && DateTime.UtcNow < idTokenExpireUtc)
        {
            callback(idToken);
            yield break;
        }

#if FIREBASE_SDK
        if (FirebaseSettings.USE_EMULATOR == false)
        {
            yield return SdkGetToken(callback);
            yield break;
        }
#endif
        yield return RestGetToken(callback);
    }

#if FIREBASE_SDK
    // Bám StickIdle (Login.cs): CheckAndFixDependencies → FirebaseAuth đăng nhập ẩn danh → lấy ID token.
    private IEnumerator SdkGetToken(Action<string> callback)
    {
        bool done = false;
        string token = null;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(depTask =>
        {
            if (depTask.Result != DependencyStatus.Available)
            {
                DebugCustom.LogError("[Firebase] Dependencies: " + depTask.Result);
                done = true;
                return;
            }

            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            Action<FirebaseUser> fetchToken = user =>
            {
                user.TokenAsync(false).ContinueWithOnMainThread(tokenTask =>
                {
                    if (tokenTask.IsCompleted && !tokenTask.IsFaulted && !tokenTask.IsCanceled)
                    {
                        token = tokenTask.Result;
                        Uid = user.UserId;
                    }
                    done = true;
                });
            };

            if (auth.CurrentUser != null)
            {
                fetchToken(auth.CurrentUser);
                return;
            }

            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(signTask =>
            {
                if (signTask.IsFaulted || signTask.IsCanceled)
                {
                    DebugCustom.LogError("[Firebase] SignInAnonymously failed: " + signTask.Exception);
                    done = true;
                    return;
                }
                fetchToken(signTask.Result.User);
            });
        });

        while (!done)
        {
            yield return null;
        }

        SetToken(token, 3000);
        callback(token);
    }
#endif

    // REST: có refresh token → đổi lấy ID token mới; chưa có → đăng ký tài khoản khách mới.
    // Refresh token bị từ chối: tài khoản khách thì tạo khách mới, tài khoản email thì dừng lại chờ đăng nhập (NeedsSignIn).
    private IEnumerator RestGetToken(Action<string> callback)
    {
        if (NeedsSignIn)
        {
            callback(null);
            yield break;
        }

        string refreshToken = PlayerPrefs.GetString(PrefKey(PREF_REFRESH_TOKEN), string.Empty);
        if (string.IsNullOrEmpty(refreshToken) == false)
        {
            string form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(refreshToken);
            JObject json = null;
            string error = null;
            yield return PostAuth(FirebaseSettings.AuthRefreshUrl, form, "application/x-www-form-urlencoded", (j, e) => { json = j; error = e; });
            if (json != null && json["id_token"] != null)
            {
                Uid = (string)json["user_id"];
                SaveSession((string)json["refresh_token"], Uid, Email);
                SetToken((string)json["id_token"], ParseSeconds(json["expires_in"]));
                callback(idToken);
                yield break;
            }

            // Mất mạng / máy chủ lỗi: giữ nguyên phiên, lần sau thử lại.
            if (error == ERROR_NETWORK)
            {
                callback(null);
                yield break;
            }

            if (IsAnonymous == false)
            {
                NeedsSignIn = true;
                callback(null);
                yield break;
            }
        }

        yield return RestSignUpAnonymous(callback);
    }

    private IEnumerator RestSignUpAnonymous(Action<string> callback)
    {
        JObject signUp = null;
        yield return PostAuth(FirebaseSettings.AuthSignUpUrl, "{\"returnSecureToken\":true}", "application/json", (j, e) => signUp = j);
        if (signUp == null || signUp["idToken"] == null)
        {
            callback(null);
            yield break;
        }

        AdoptSession(signUp, string.Empty);
        callback(idToken);
    }

    // Nhận phiên mới từ phản hồi signUp / update / signInWithPassword.
    private void AdoptSession(JObject json, string email)
    {
        Uid = (string)json["localId"];
        SaveSession((string)json["refreshToken"], Uid, email);
        SetToken((string)json["idToken"], ParseSeconds(json["expiresIn"]));
        NeedsSignIn = false;
    }

    // callback(json, error): thành công → error = null; thất bại → json = null, error = mã lỗi Firebase (EMAIL_EXISTS...)
    // hoặc ERROR_NETWORK khi không tới được máy chủ.
    private IEnumerator PostAuth(string url, string body, string contentType, Action<JObject, string> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = FirebaseSettings.REQUEST_TIMEOUT_SECONDS;
            request.SetRequestHeader("Content-Type", contentType);
            yield return request.SendWebRequest();

            string text = request.downloadHandler.text;
            JObject json = null;
            try
            {
                json = string.IsNullOrEmpty(text) ? null : JObject.Parse(text);
            }
            catch (Exception e)
            {
                DebugCustom.LogError("[Firebase] Auth parse: " + e.Message);
            }

            if (request.result == UnityWebRequest.Result.Success && json != null)
            {
                callback(json, null);
                yield break;
            }

            DebugCustom.LogWarning("[Firebase] Auth failed: " + request.error + " | " + text);
            // Máy chủ từ chối (HTTP 4xx) thì thân phản hồi có error.message; còn lại coi là lỗi mạng.
            string message = request.responseCode >= 400 && request.responseCode < 500 && json != null
                ? (string)json.SelectToken("error.message")
                : null;
            callback(null, string.IsNullOrEmpty(message) ? ERROR_NETWORK : message);
        }
    }

    // ===== Tài khoản email (FirebaseAuthController gốc) =====

    // Đăng ký = GẮN email + mật khẩu vào tài khoản khách đang dùng (cor gốc: LinkWithCredentialAsync) → giữ nguyên uid và tiến trình.
    public void LinkWithEmail(string email, string password, Action<bool, AccountAuthError, string> callback)
    {
        if (IsAnonymous == false)
        {
            callback(false, AccountAuthError.ProviderAlreadyLinked, "Already signed in");
            return;
        }
        StartCoroutine(RoutineLinkWithEmail(email, password, callback));
    }

    private IEnumerator RoutineLinkWithEmail(string email, string password, Action<bool, AccountAuthError, string> callback)
    {
        string token = null;
        yield return GetIdToken(t => token = t);
        if (string.IsNullOrEmpty(token))
        {
            callback(false, AccountAuthError.NetworkRequestFailed, null);
            yield break;
        }

        string body = JsonConvert.SerializeObject(new { idToken = token, email, password, returnSecureToken = true });
        JObject json = null;
        string error = null;
        yield return PostAuth(FirebaseSettings.AuthUpdateUrl, body, "application/json", (j, e) => { json = j; error = e; });
        if (json == null || json["idToken"] == null)
        {
            callback(false, ToAuthError(error), null);
            yield break;
        }

        AdoptSession(json, email);
        callback(true, AccountAuthError.None, null);
    }

    // Đăng nhập tài khoản email đã có (cos gốc: SignInWithEmailAndPasswordAsync) → uid đổi sang tài khoản đó.
    public void SignInWithEmail(string email, string password, Action<bool, AccountAuthError, string> callback)
    {
        StartCoroutine(RoutineSignInWithEmail(email, password, callback));
    }

    private IEnumerator RoutineSignInWithEmail(string email, string password, Action<bool, AccountAuthError, string> callback)
    {
        string body = JsonConvert.SerializeObject(new { email, password, returnSecureToken = true });
        JObject json = null;
        string error = null;
        yield return PostAuth(FirebaseSettings.AuthSignInPasswordUrl, body, "application/json", (j, e) => { json = j; error = e; });
        if (json == null || json["idToken"] == null)
        {
            callback(false, ToAuthError(error), null);
            yield break;
        }

        AdoptSession(json, email);
        callback(true, AccountAuthError.None, null);
    }

    // Gửi thư đặt lại mật khẩu (cot gốc: SendPasswordResetEmailAsync).
    public void SendPasswordResetEmail(string email, Action<bool, AccountAuthError, string> callback)
    {
        StartCoroutine(RoutineSendPasswordResetEmail(email, callback));
    }

    private IEnumerator RoutineSendPasswordResetEmail(string email, Action<bool, AccountAuthError, string> callback)
    {
        string body = JsonConvert.SerializeObject(new { requestType = "PASSWORD_RESET", email });
        JObject json = null;
        string error = null;
        yield return PostAuth(FirebaseSettings.AuthSendOobCodeUrl, body, "application/json", (j, e) => { json = j; error = e; });
        callback(json != null, json != null ? AccountAuthError.None : ToAuthError(error), null);
    }

    // Đăng xuất (cou gốc: SignOut rồi SignInAnonymouslyAsync) → máy quay về một tài khoản khách MỚI.
    // Cũng dùng khi phiên tài khoản email đã mất mà người chơi không đăng nhập lại (chơi tiếp bằng khách).
    public void SignOut(Action<bool, string> callback)
    {
        StartCoroutine(RoutineSignOut(callback));
    }

    private IEnumerator RoutineSignOut(Action<bool, string> callback)
    {
        string token = null;
        yield return RestSignUpAnonymous(t => token = t);
        if (string.IsNullOrEmpty(token))
        {
            callback(false, "Failed to re-sign in");
            yield break;
        }
        callback(true, null);
    }

    // Mã lỗi Auth REST → AuthError của Firebase SDK (bản gốc phân nhánh thông báo theo enum này).
    private static AccountAuthError ToAuthError(string error)
    {
        if (string.IsNullOrEmpty(error)) return AccountAuthError.Failure;
        if (error == ERROR_NETWORK) return AccountAuthError.NetworkRequestFailed;
        if (error.StartsWith("EMAIL_EXISTS")) return AccountAuthError.EmailAlreadyInUse;
        if (error.StartsWith("INVALID_EMAIL") || error.StartsWith("MISSING_EMAIL")) return AccountAuthError.InvalidEmail;
        if (error.StartsWith("WEAK_PASSWORD")) return AccountAuthError.WeakPassword;
        if (error.StartsWith("TOO_MANY_ATTEMPTS_TRY_LATER")) return AccountAuthError.TooManyRequests;
        if (error.StartsWith("EMAIL_NOT_FOUND")) return AccountAuthError.UserNotFound;
        if (error.StartsWith("INVALID_PASSWORD") || error.StartsWith("INVALID_LOGIN_CREDENTIALS")) return AccountAuthError.WrongPassword;
        if (error.StartsWith("USER_DISABLED")) return AccountAuthError.UserDisabled;
        return AccountAuthError.Failure;
    }

    private void SetToken(string token, int expiresInSeconds)
    {
        idToken = token;
        // Làm mới sớm 5 phút trước khi hết hạn.
        idTokenExpireUtc = DateTime.UtcNow.AddSeconds(Mathf.Max(60, expiresInSeconds - 300));
    }

    private void SaveSession(string refreshToken, string uid, string email)
    {
        Email = email ?? string.Empty;
        PlayerPrefs.SetString(PrefKey(PREF_REFRESH_TOKEN), refreshToken ?? string.Empty);
        PlayerPrefs.SetString(PrefKey(PREF_UID), uid ?? string.Empty);
        PlayerPrefs.SetString(PrefKey(PREF_EMAIL), Email);
        PlayerPrefs.Save();
    }

    // Tách token emulator / server thật để đổi qua lại không lẫn tài khoản.
    public static string PrefKey(string key)
    {
        return key + (FirebaseSettings.USE_EMULATOR ? "_emu" : "_" + FirebaseSettings.PROJECT_ID);
    }

    private static int ParseSeconds(JToken token)
    {
        return token != null && int.TryParse((string)token, out int s) ? s : 3600;
    }
}

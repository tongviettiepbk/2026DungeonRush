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
public class FirebaseManager : Singleton<FirebaseManager>
{
    private const string PREF_REFRESH_TOKEN = "fb_refresh_token";
    private const string PREF_UID = "fb_uid";

    public string Uid { get; private set; }
    public bool IsSignedIn => string.IsNullOrEmpty(Uid) == false;

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
    }

    // ===== Gọi Cloud Function (callable protocol: body {"data":..} → {"result":..} | {"error":..}) =====

    public void Call<TResponse>(string functionName, object data, Action<bool, TResponse, string> callback)
    {
        StartCoroutine(RoutineCall(functionName, data, callback));
    }

    private IEnumerator RoutineCall<TResponse>(string functionName, object data, Action<bool, TResponse, string> callback)
    {
        string token = null;
        yield return GetIdToken(t => token = t);
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

    // REST: có refresh token → đổi lấy ID token mới; chưa có/hỏng → đăng ký tài khoản ẩn danh mới.
    private IEnumerator RestGetToken(Action<string> callback)
    {
        string refreshToken = PlayerPrefs.GetString(PrefKey(PREF_REFRESH_TOKEN), string.Empty);
        if (string.IsNullOrEmpty(refreshToken) == false)
        {
            string form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(refreshToken);
            JObject json = null;
            yield return PostAuth(FirebaseSettings.AuthRefreshUrl, form, "application/x-www-form-urlencoded", j => json = j);
            if (json != null && json["id_token"] != null)
            {
                Uid = (string)json["user_id"];
                SaveRefresh((string)json["refresh_token"], Uid);
                SetToken((string)json["id_token"], ParseSeconds(json["expires_in"]));
                callback(idToken);
                yield break;
            }
        }

        JObject signUp = null;
        yield return PostAuth(FirebaseSettings.AuthSignUpUrl, "{\"returnSecureToken\":true}", "application/json", j => signUp = j);
        if (signUp == null || signUp["idToken"] == null)
        {
            callback(null);
            yield break;
        }

        Uid = (string)signUp["localId"];
        SaveRefresh((string)signUp["refreshToken"], Uid);
        SetToken((string)signUp["idToken"], ParseSeconds(signUp["expiresIn"]));
        callback(idToken);
    }

    private IEnumerator PostAuth(string url, string body, string contentType, Action<JObject> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = FirebaseSettings.REQUEST_TIMEOUT_SECONDS;
            request.SetRequestHeader("Content-Type", contentType);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                DebugCustom.LogWarning("[Firebase] Auth failed: " + request.error + " | " + request.downloadHandler.text);
                callback(null);
                yield break;
            }

            try
            {
                callback(JObject.Parse(request.downloadHandler.text));
            }
            catch (Exception e)
            {
                DebugCustom.LogError("[Firebase] Auth parse: " + e.Message);
                callback(null);
            }
        }
    }

    private void SetToken(string token, int expiresInSeconds)
    {
        idToken = token;
        // Làm mới sớm 5 phút trước khi hết hạn.
        idTokenExpireUtc = DateTime.UtcNow.AddSeconds(Mathf.Max(60, expiresInSeconds - 300));
    }

    private void SaveRefresh(string refreshToken, string uid)
    {
        PlayerPrefs.SetString(PrefKey(PREF_REFRESH_TOKEN), refreshToken ?? string.Empty);
        PlayerPrefs.SetString(PrefKey(PREF_UID), uid ?? string.Empty);
    }

    // Tách token emulator / server thật để đổi qua lại không lẫn tài khoản.
    private static string PrefKey(string key)
    {
        return key + (FirebaseSettings.USE_EMULATOR ? "_emu" : "_" + FirebaseSettings.PROJECT_ID);
    }

    private static int ParseSeconds(JToken token)
    {
        return token != null && int.TryParse((string)token, out int s) ? s : 3600;
    }
}

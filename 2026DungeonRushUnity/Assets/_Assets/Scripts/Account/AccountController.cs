using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tài khoản người chơi (AccountController gốc).
//  - Mở game lần đầu: tự có tài khoản KHÁCH (Firebase ẩn danh), save chỉ nằm trên máy.
//  - Sign Up = gắn email + mật khẩu vào chính tài khoản khách đó (giữ uid, giữ tiến trình) rồi đẩy save lên cloud.
//  - Sign In = chuyển sang tài khoản email đã có: dọn tài khoản khách bị bỏ lại, kéo save cloud về, lệch thì hỏi Local / Cloud.
//  - Forgot Password = Firebase gửi thư đặt lại mật khẩu về email.
//  - Sign Out = đẩy save lần cuối, máy quay về một tài khoản khách MỚI với save trắng.
public class AccountController : Singleton<AccountController>
{
    public const string FN_LEAVE_CLAN = "leaveclan";
    public const string FN_DELETE_USER_DOC = "deleteuserdoc";

    private const string MSG_CLOUD_SYNC_FAILED = "Signed in, but couldn't sync cloud save. Try again later.";   // Errors.Account.CloudSyncFailed
    private const string MSG_SAVE_FAILED_SIGNING_OUT = "Couldn't save latest progress. Signing out anyway.";       // Errors.Account.SaveFailedSigningOut
    private const string MSG_PLAYING_AS_GUEST = "You're playing as guest. Sign in later to save across devices."; // Errors.Account.PlayingAsGuest
    private const string MSG_NETWORK = "Network error. Check your connection and try again.";
    private const string MSG_TOO_MANY = "Too many attempts. Please try again later.";
    private const string MSG_INVALID_EMAIL = "Please enter a valid email address.";

    // Màn loading chờ đăng nhập + đối chiếu save cloud tối đa ngần này giây rồi cho vào game bằng save trên máy (tua gốc).
    private const float BOOT_TIMEOUT = 7f;

    private static FirebaseManager Firebase => FirebaseManager.Instance;
    private static CloudSaveController Cloud => CloudSaveController.Instance;

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

    // ===== Trạng thái =====

    // clz gốc: đã gắn email (không phải khách).
    public bool IsLinked => Firebase.IsAnonymous == false && Firebase.NeedsSignIn == false;
    public string Email => Firebase.Email;

    // cmk gốc: dòng "Last sync" ở Profile.
    public string GetLastSyncText()
    {
        DateTime? lastSync = Cloud.LastSyncUtc;
        if (lastSync.HasValue == false)
        {
            return "Never";
        }

        TimeSpan elapsed = DateTime.UtcNow - lastSync.Value;
        if (elapsed.TotalSeconds < 60)
        {
            return "just now";
        }
        if (elapsed.TotalMinutes < 60)
        {
            return string.Format("{0} min ago", (int)elapsed.TotalMinutes);
        }
        if (elapsed.TotalHours < 24)
        {
            return string.Format("{0} h ago", (int)elapsed.TotalHours);
        }
        return lastSync.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    // ===== Mở game =====

    // Login gọi lúc loading: đăng nhập (chưa có tài khoản thì tạo khách), tài khoản email thì đối chiếu save cloud.
    // Quá BOOT_TIMEOUT thì thôi chờ — phần còn lại chạy nền, vào game bằng save trên máy.
    public IEnumerator BootRoutine()
    {
        bool isDone = false;
        StartCoroutine(RoutineBoot(() => isDone = true));

        float deadline = Time.unscaledTime + BOOT_TIMEOUT;
        while (isDone == false && Time.unscaledTime < deadline)
        {
            yield return null;
        }
    }

    private IEnumerator RoutineBoot(Action onDone)
    {
        string token = null;
        yield return Firebase.GetIdToken(t => token = t);

        // Phiên tài khoản email đã mất → mời đăng nhập lại (cnb / cna gốc).
        if (Firebase.NeedsSignIn)
        {
            onDone();
            PromptSignIn();
            yield break;
        }
        if (string.IsNullOrEmpty(token) || IsLinked == false)
        {
            onDone();
            yield break;
        }

        Cloud.BindAccount();
        Cloud.Download((ok, cloud, error) =>
        {
            if (ok == false)
            {
                onDone();
                return;
            }
            Reconcile(cloud, onDone);
        });
    }

    // Đối chiếu save cloud với save trên máy theo bảng quyết định của bản gốc (CloudSaveController.Decide).
    public void Reconcile(UserDto cloud, Action onDone)
    {
        switch (Cloud.Decide(cloud))
        {
            case CloudSaveController.Decision.KeepLocal:
                Cloud.KeepLocal(cloud, null);
                onDone?.Invoke();
                break;
            case CloudSaveController.Decision.ApplyCloud:
                ApplyCloud(cloud);
                onDone?.Invoke();
                break;
            case CloudSaveController.Decision.Ask:
                ShowAccountSelect(cloud, onDone);
                break;
            default:
                onDone?.Invoke();
                break;
        }
    }

    // ===== Sign Up (cml) =====

    public void SignUp(string email, string password, Action<bool, string> callback)
    {
        Firebase.LinkWithEmail(email, password, (ok, error, message) =>
        {
            if (ok == false)
            {
                callback(false, GetSignUpError(error, message));
                return;
            }

            this.PostEvent(EventID.AccountChanged);
            Cloud.BindAccount();
            // Tài khoản vừa tạo chưa có gì trên cloud → đẩy save hiện tại lên ngay.
            Cloud.Upload((uploaded, uploadError) =>
            {
                if (uploaded == false)
                {
                    UIManager.Instance.ShowToastMessage(MSG_CLOUD_SYNC_FAILED, isLocalize: false);
                }
                this.PostEvent(EventID.AccountChanged);
                callback(true, null);
            });
        });
    }

    // ===== Sign In (cmm → CaptureAnonTokenThenSignIn) =====

    public void SignIn(string email, string password, Action<bool, string> callback)
    {
        StartCoroutine(RoutineSignIn(email, password, callback));
    }

    private IEnumerator RoutineSignIn(string email, string password, Action<bool, string> callback)
    {
        // Đang là khách: giữ lại token của nó để lát nữa dọn tài khoản khách bị bỏ lại.
        string guestUid = null;
        string guestToken = null;
        bool guestHasClan = false;
        if (Firebase.IsSignedIn && Firebase.IsAnonymous)
        {
            guestUid = Firebase.Uid;
            guestHasClan = string.IsNullOrEmpty(GameData.userData.clan.clanId) == false;
            yield return Firebase.GetIdToken(t => guestToken = t);
        }

        bool isDone = false;
        bool isOk = false;
        AccountAuthError authError = AccountAuthError.None;
        string authMessage = null;
        Firebase.SignInWithEmail(email, password, (ok, error, message) =>
        {
            isOk = ok;
            authError = error;
            authMessage = message;
            isDone = true;
        });
        while (isDone == false)
        {
            yield return null;
        }

        if (isOk == false)
        {
            callback(false, GetSignInError(authError, authMessage));
            yield break;
        }

        if (string.IsNullOrEmpty(guestToken) == false && Firebase.Uid != guestUid)
        {
            CleanUpOrphanGuest(guestToken, guestHasClan);
        }

        this.PostEvent(EventID.AccountChanged);
        callback(true, null);
        PullCloudAfterSignIn();
    }

    // Tài khoản khách vừa bị bỏ lại: cho rời clan rồi xoá dữ liệu của nó trên server (gốc: "Orphan anon doc cleanup").
    private void CleanUpOrphanGuest(string guestToken, bool hasClan)
    {
        Action deleteDoc = () => Firebase.Call<AccountResponse>(FN_DELETE_USER_DOC, null, (ok, response, raw) =>
        {
            DebugCustom.Log("[AccountController] Orphan anon doc cleanup: " + (ok ? "deleted" : "skipped: " + raw));
        }, guestToken);

        if (hasClan)
        {
            Firebase.Call<AccountResponse>(FN_LEAVE_CLAN, null, (ok, response, raw) => deleteDoc(), guestToken);
        }
        else
        {
            deleteDoc();
        }
    }

    // Vừa đăng nhập xong: kéo save cloud của tài khoản về (cmo + bv.clo).
    private void PullCloudAfterSignIn()
    {
        Cloud.BindAccount();
        Cloud.SetSwitchPending(true);
        Cloud.Download((ok, cloud, error) =>
        {
            if (ok == false)
            {
                UIManager.Instance.ShowToastMessage(MSG_CLOUD_SYNC_FAILED, isLocalize: false);
                return;
            }

            // Cloud trống, hoặc không mới hơn bản máy này từng đẩy → đẩy save trên máy lên.
            if (cloud == null || Cloud.LastUploadedVersion >= cloud.SaveVersion)
            {
                Cloud.Upload(OnKeepLocalUploaded, true);
                return;
            }
            // Máy này chưa chơi gì và chưa từng đẩy → nhận luôn save cloud.
            if (Cloud.LastUploadedVersion <= 0 && Cloud.IsLocalFresh())
            {
                ApplyCloud(cloud);
                return;
            }
            ShowAccountSelect(cloud, null);
        });
    }

    // ===== Chọn Local / Cloud (cmq + bw.clp) =====

    private void ShowAccountSelect(UserDto cloud, Action onDone)
    {
        UserData cloudUser;
        try
        {
            cloudUser = UserData.ParseJson(cloud.UserJson);
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[AccountController] Corrupt cloud payload, keeping local: " + e.Message);
            Cloud.KeepLocal(cloud, null);
            onDone?.Invoke();
            return;
        }

        long localSaveMs = Cloud.LastSyncUtc.HasValue ? new DateTimeOffset(Cloud.LastSyncUtc.Value).ToUnixTimeMilliseconds() : 0;
        UIAccountSelectPopup popup = UIManager.Instance.LoadUI(UIKey.AccountSelectPopup, isBackable: false) as UIAccountSelectPopup;
        popup.Show(ToPreview(GameData.userData, localSaveMs), ToPreview(cloudUser, cloud.LastSyncUtcMs), choice =>
        {
            if (choice == UIAccountSelectPopup.Choice.UseCloud)
            {
                ApplyCloud(cloud);
            }
            else
            {
                Cloud.KeepLocal(cloud, OnKeepLocalUploaded);
            }
            onDone?.Invoke();
        });
    }

    private static UIAccountSelectPopup.PreviewData ToPreview(UserData user, long lastSaveUtcMs)
    {
        return new UIAccountSelectPopup.PreviewData
        {
            Nickname = user.profile.isNameSet ? user.profile.userName : string.Empty,
            HighestStageId = user.campaign.passedStageId,
            PlayerLevel = user.player.playerLevel,
            LastSaveUtcMs = lastSaveUtcMs,
        };
    }

    // bw.clq gốc.
    private void OnKeepLocalUploaded(bool ok, string error)
    {
        this.PostEvent(EventID.AccountChanged);
        if (ok == false)
        {
            UIManager.Instance.ShowToastMessage(MSG_CLOUD_SYNC_FAILED, isLocalize: false);
        }
    }

    // Lấy save cloud làm save hiện hành; đang ở trong game thì nạp lại game để mọi thứ đọc save mới.
    private void ApplyCloud(UserDto cloud)
    {
        try
        {
            UserData.ApplyJson(cloud.UserJson);
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[AccountController] Corrupt cloud payload, keeping local: " + e.Message);
            Cloud.KeepLocal(cloud, null);
            return;
        }

        Cloud.MarkSynced(cloud);
        this.PostEvent(EventID.AccountChanged);
        if (SceneManager.GetActiveScene().name == StaticValue.SCENE_MAINGAME)
        {
            RestartGame();
        }
    }

    // ===== Forgot Password (cmr) =====

    public void SendPasswordReset(string email, Action<bool, string> callback)
    {
        Firebase.SendPasswordResetEmail(email, (ok, error, message) =>
        {
            // Email chưa đăng ký vẫn báo "đã gửi" như bản gốc — không để lộ email nào có tài khoản.
            if (ok || error == AccountAuthError.UserNotFound)
            {
                callback(true, null);
                return;
            }
            callback(false, GetResetError(error, message));
        });
    }

    // ===== Sign Out (cms) =====

    public void SignOut(Action<bool, string> callback)
    {
        GameData.Save(true);
        if (Cloud.IsDirty == false)
        {
            DoSignOut(callback);
            return;
        }

        // Đẩy tiến trình mới nhất lên cloud trước khi rời tài khoản.
        Cloud.Upload((ok, error) =>
        {
            if (ok == false)
            {
                UIManager.Instance.ShowToastMessage(MSG_SAVE_FAILED_SIGNING_OUT, isLocalize: false);
            }
            DoSignOut(callback);
        });
    }

    private void DoSignOut(Action<bool, string> callback)
    {
        Firebase.SignOut((ok, error) =>
        {
            if (ok == false)
            {
                callback(false, string.IsNullOrEmpty(error) ? "Sign out failed" : error);
                return;
            }

            Cloud.ResetAccount();
            GameData.userData.ResetLocal();
            this.PostEvent(EventID.AccountChanged);
            callback(true, null);
            RestartGame();
        });
    }

    // ===== Sync Now / Cloud Backup (cmt) =====

    public void SyncNow(Action<bool, string> callback)
    {
        GameData.Save(true);
        Cloud.Upload((ok, error) =>
        {
            this.PostEvent(EventID.AccountChanged);
            callback(ok, error);
        });
    }

    // ===== Phiên đã mất (cna) =====

    // Mở sẵn Sign In với email cũ. Đóng mà không đăng nhập → chơi tiếp bằng một tài khoản khách mới, save trên máy giữ nguyên.
    // (Phần "đóng thì thành khách" là suy từ câu thông báo PlayingAsGuest của bản gốc — thân hàm gốc không đọc được.)
    private void PromptSignIn()
    {
        UISignInPopup popup = UIManager.Instance.LoadUI(UIKey.SignInPopup) as UISignInPopup;
        popup.Show(Firebase.Email, () =>
        {
            if (Firebase.NeedsSignIn == false)
            {
                return;
            }
            Firebase.SignOut((ok, error) =>
            {
                Cloud.ResetAccount();
                this.PostEvent(EventID.AccountChanged);
                UIManager.Instance.ShowToastMessage(MSG_PLAYING_AS_GUEST, isLocalize: false);
            });
        });
    }

    // ===== Nạp lại game (ControllerManager.dam gốc) =====

    // Save vừa bị thay cả loạt (lấy save cloud / đăng xuất): bỏ các controller online đang giữ dữ liệu của tài khoản cũ
    // rồi quay về màn Login để nạp lại từ đầu.
    private static void RestartGame()
    {
        UIManager.Instance.HideAllActiveUI();
        if (BossRushController.Exists()) Destroy(BossRushController.Instance.gameObject);
        if (PvPController.Exists()) Destroy(PvPController.Instance.gameObject);
        if (ClanController.Exists()) Destroy(ClanController.Instance.gameObject);
        if (ClanWarController.Exists()) Destroy(ClanWarController.Instance.gameObject);
        SceneManager.LoadScene(StaticValue.SCENE_LOGIN);
    }

    // ===== Thông báo lỗi (cnd / cne / cnf gốc) =====

    private static string GetSignUpError(AccountAuthError error, string message)
    {
        switch (error)
        {
            case AccountAuthError.EmailAlreadyInUse: return "This email is already registered. Try signing in instead.";
            case AccountAuthError.InvalidEmail: return MSG_INVALID_EMAIL;
            case AccountAuthError.TooManyRequests: return MSG_TOO_MANY;
            case AccountAuthError.ProviderAlreadyLinked: return "An account is already linked.";
            case AccountAuthError.NetworkRequestFailed: return MSG_NETWORK;
            case AccountAuthError.WeakPassword: return "Password is too weak. Use at least 6 characters.";
            default: return string.IsNullOrEmpty(message) ? "Sign up failed. Please try again." : message;
        }
    }

    private static string GetSignInError(AccountAuthError error, string message)
    {
        switch (error)
        {
            case AccountAuthError.UserDisabled: return "This account has been disabled.";
            case AccountAuthError.Failure:
            case AccountAuthError.InvalidEmail:
            case AccountAuthError.WrongPassword:
            case AccountAuthError.UserNotFound: return "Invalid email or password.";
            case AccountAuthError.TooManyRequests: return MSG_TOO_MANY;
            case AccountAuthError.NetworkRequestFailed: return MSG_NETWORK;
            default: return string.IsNullOrEmpty(message) ? "Sign in failed. Please try again." : message;
        }
    }

    private static string GetResetError(AccountAuthError error, string message)
    {
        switch (error)
        {
            case AccountAuthError.InvalidEmail: return MSG_INVALID_EMAIL;
            case AccountAuthError.NetworkRequestFailed: return MSG_NETWORK;
            case AccountAuthError.TooManyRequests: return MSG_TOO_MANY;
            default: return string.IsNullOrEmpty(message) ? "Could not send reset email." : message;
        }
    }
}

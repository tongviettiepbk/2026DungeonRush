using System;
using UnityEngine;

// Đồng bộ save lên cloud (CloudSaveController gốc). CHỈ tài khoản đã gắn email mới đồng bộ — khách lưu trên máy.
//  - Tự đẩy lên: sau lần lưu trên máy cuối cùng 30 giây, lúc app xuống nền, lúc thoát.
//    Project lưu xuống máy dày hơn bản gốc (đánh liên tục thì không bao giờ có 30 giây "yên") nên thêm trần:
//    có thay đổi chưa đẩy quá 5 phút thì đẩy luôn.
//  - Mỗi bản có SaveVersion tăng dần; server từ chối bản không mới hơn (VERSION_NOT_ADVANCING) → tải bản cloud về đối chiếu.
//  - Trạng thái đồng bộ (số phiên bản đã đẩy, giờ sync, cờ chưa đẩy) lưu PlayerPrefs, thuộc về MỘT tài khoản.
public class CloudSaveController : Singleton<CloudSaveController>
{
    public enum Decision
    {
        Nothing,        // cloud không mới hơn
        KeepLocal,      // giữ save trên máy, đẩy đè lên cloud
        ApplyCloud,     // lấy save cloud
        Ask,            // hỏi người chơi (popup chọn Local / Cloud)
    }

    public const string FN_SAVE = "saveuserdoc";
    public const string FN_LOAD = "loaduserdoc";
    public const string ERROR_RATE_LIMITED = "RATE_LIMITED";
    private const string ERROR_VERSION_NOT_ADVANCING = "VERSION_NOT_ADVANCING";

    // Tên key như bản gốc.
    private const string PREF_LAST_UPLOADED_VERSION = "last_uploaded_save_version";
    private const string PREF_LAST_SYNC_UTC_MS = "last_sync_utc_ms";
    private const string PREF_DEVICE_ID = "cloud_device_id";
    private const string PREF_UNSYNCED_LOCAL = "cloud_unsynced_local";
    private const string PREF_SYNC_UID = "cloud_sync_uid";
    private const string PREF_SWITCH_PENDING = "cloud_switch_pending";

    private const float AUTO_SYNC_DELAY = 30f;      // ttw gốc
    private const float AUTO_SYNC_MAX_WAIT = 300f;  // trần của project, bản gốc không có
    // Đẩy lỗi thì giãn dần lần thử lại như bản gốc: 60s → nhân đôi mỗi lần lỗi, tối đa 300s.
    private const float RETRY_DELAY_MIN = 60f;
    private const float RETRY_DELAY_MAX = 300f;
    private const int SCHEMA_VERSION = 1;

    public int LastUploadedVersion { get; private set; }
    public DateTime? LastSyncUtc { get; private set; }
    // Có thay đổi trên máy chưa đẩy lên cloud.
    public bool IsDirty { get; private set; }
    // Vừa đổi sang tài khoản khác mà chưa chốt dùng save nào → khoá tự đẩy để không ghi đè nhầm save cloud.
    public bool IsSwitchPending { get; private set; }

    private bool isUploading;
    private float dirtyTime;        // lần lưu trên máy gần nhất
    private float dirtySince;       // lúc bắt đầu có thay đổi chưa đẩy
    private float nextAllowedTime;
    private float retryDelay = RETRY_DELAY_MIN;
    private int changeSerial;       // tăng mỗi lần save trên máy đổi → biết có thay đổi mới trong lúc đang đẩy
    private int accountEpoch;       // tăng khi đổi tài khoản → bỏ kết quả của lời gọi thuộc tài khoản cũ

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        LastUploadedVersion = PlayerPrefs.GetInt(Key(PREF_LAST_UPLOADED_VERSION), 0);
        IsDirty = PlayerPrefs.GetInt(Key(PREF_UNSYNCED_LOCAL), 0) == 1;
        IsSwitchPending = PlayerPrefs.GetInt(Key(PREF_SWITCH_PENDING), 0) == 1;
        if (long.TryParse(PlayerPrefs.GetString(Key(PREF_LAST_SYNC_UTC_MS), string.Empty), out long syncMs) && syncMs > 0)
        {
            LastSyncUtc = DateTimeOffset.FromUnixTimeMilliseconds(syncMs).UtcDateTime;
        }
    }

    // czq gốc.
    public bool CanSync
    {
        get
        {
            FirebaseManager firebase = FirebaseManager.Instance;
            return firebase.IsSignedIn && firebase.IsAnonymous == false && firebase.NeedsSignIn == false;
        }
    }

    public string DeviceId
    {
        get
        {
            string id = PlayerPrefs.GetString(Key(PREF_DEVICE_ID), string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(Key(PREF_DEVICE_ID), id);
            }
            return id;
        }
    }

    // ===== Tự đẩy =====

    // UserData.Save gọi mỗi khi có module vừa ghi xuống máy.
    public static void MarkDirty()
    {
        if (instance != null)
        {
            instance.changeSerial++;
            instance.SetDirty(true);
        }
    }

    private void Update()
    {
        if (CanAutoUpload() == false)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now < nextAllowedTime)
        {
            return;
        }
        if (now - dirtyTime >= AUTO_SYNC_DELAY || now - dirtySince >= AUTO_SYNC_MAX_WAIT)
        {
            Upload(null);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && CanAutoUpload())
        {
            Upload(null);
        }
    }

    private void OnApplicationQuit()
    {
        if (CanAutoUpload())
        {
            Upload(null);
        }
    }

    private bool CanAutoUpload()
    {
        return IsDirty && isUploading == false && IsSwitchPending == false && CanSync;
    }

    // ===== Đẩy lên (czt) / tải về (czu) =====

    public void Upload(Action<bool, string> callback, bool bypassSwitchPending = false)
    {
        if (CanSync == false)
        {
            callback?.Invoke(false, "Not signed in");
            return;
        }
        if (IsSwitchPending && bypassSwitchPending == false)
        {
            callback?.Invoke(false, "Account switch unresolved");
            return;
        }
        if (isUploading)
        {
            callback?.Invoke(false, "Already uploading");
            return;
        }

        // Ghi số phiên bản thẳng xuống máy (không qua UserData.Save) để việc đẩy không tự đánh dấu "có thay đổi" lần nữa.
        UserProfileData profile = GameData.userData.profile;
        profile.saveVersion = Mathf.Max(profile.saveVersion, LastUploadedVersion) + 1;
        profile.Save(true);

        UserDto dto = new UserDto
        {
            UserJson = GameData.userData.ToJson(),
            SaveVersion = profile.saveVersion,
            SchemaVersion = SCHEMA_VERSION,
            LastWriterDeviceId = DeviceId,
        };

        isUploading = true;
        int epoch = accountEpoch;
        int serial = changeSerial;
        FirebaseManager.Instance.Call<SaveUserDocResponse>(FN_SAVE, dto, (ok, response, raw) =>
        {
            if (epoch != accountEpoch)
            {
                callback?.Invoke(false, "Stale (account changed)");
                return;
            }

            isUploading = false;
            if (ok && response != null && response.success)
            {
                SetLastUploadedVersion(response.SaveVersion);
                SetLastSync(response.LastSyncUtcMs);
                SetSwitchPending(false);
                if (serial == changeSerial)
                {
                    SetDirty(false);
                }
                else
                {
                    // Trong lúc đẩy lại có thay đổi mới → vẫn "chưa đẩy", tính lại mốc chờ từ bây giờ.
                    dirtySince = Time.unscaledTime;
                }
                retryDelay = RETRY_DELAY_MIN;
                DebugCustom.Log("[CloudSave] Uploaded to cloud OK -> version " + response.SaveVersion);
                callback?.Invoke(true, null);
                return;
            }

            string error = response != null ? response.message : null;
            // Cloud đang giữ bản mới hơn (máy khác vừa đẩy) → tải về, để AccountController đối chiếu.
            if (error == ERROR_VERSION_NOT_ADVANCING)
            {
                Download((loaded, cloud, downloadError) =>
                {
                    if (loaded && cloud != null)
                    {
                        AccountController.Instance.Reconcile(cloud, null);
                    }
                    callback?.Invoke(false, "Local was stale");
                });
                return;
            }

            retryDelay = Mathf.Min(retryDelay * 2f, RETRY_DELAY_MAX);
            nextAllowedTime = Time.unscaledTime + retryDelay;
            DebugCustom.LogWarning("[CloudSave] Upload failed: " + error);
            callback?.Invoke(false, string.IsNullOrEmpty(error) ? "Upload failed" : error);
        });
    }

    // callback(ok, cloud, error): ok mà cloud = null nghĩa là tài khoản chưa có save trên cloud.
    public void Download(Action<bool, UserDto, string> callback)
    {
        if (CanSync == false)
        {
            callback(false, null, "Not signed in");
            return;
        }

        int epoch = accountEpoch;
        FirebaseManager.Instance.Call<LoadUserDocResponse>(FN_LOAD, null, (ok, response, raw) =>
        {
            if (epoch != accountEpoch)
            {
                callback(false, null, "Stale (account changed)");
                return;
            }
            if (ok == false || response == null || response.success == false)
            {
                callback(false, null, "Download failed");
                return;
            }
            callback(true, response.exists ? response : null, null);
        });
    }

    // ===== Đối chiếu =====

    // Bảng quyết định lúc mở game của bản gốc (daj): "Boot decision: ...".
    public Decision Decide(UserDto cloud)
    {
        if (IsSwitchPending)
        {
            return cloud != null ? Decision.Ask : Decision.Nothing;
        }
        if (cloud == null || cloud.SaveVersion <= LastUploadedVersion)
        {
            return Decision.Nothing;
        }

        // Bản cloud cũ không ghi máy nào đã lưu → không tự quyết được.
        if (string.IsNullOrEmpty(cloud.LastWriterDeviceId))
        {
            return Decision.Ask;
        }
        if (cloud.LastWriterDeviceId == DeviceId)
        {
            return IsDirty ? Decision.KeepLocal : Decision.ApplyCloud;
        }
        return IsDirty ? Decision.Ask : Decision.ApplyCloud;
    }

    // Máy này chưa chơi gì (czx gốc) → nhận save cloud mà không cần hỏi.
    public bool IsLocalFresh()
    {
        UserData user = GameData.userData;
        return user.player.playerLevel <= 1 && user.player.playerExperience <= 0 && user.campaign.passedStageId <= 0;
    }

    // Vừa lấy save cloud làm save hiện hành (czv).
    public void MarkSynced(UserDto cloud)
    {
        SetSwitchPending(false);
        SetLastUploadedVersion(cloud.SaveVersion);
        SetLastSync(cloud.LastSyncUtcMs);
        SetDirty(false);
    }

    // Giữ save trên máy: đẩy đè lên cloud với số phiên bản lớn hơn bản cloud (czw).
    public void KeepLocal(UserDto cloud, Action<bool, string> callback)
    {
        SetSwitchPending(false);
        SetLastUploadedVersion(Mathf.Max(LastUploadedVersion, cloud.SaveVersion));
        SetDirty(true);
        Upload(callback, true);
    }

    // ===== Đổi tài khoản =====

    // Gắn trạng thái đồng bộ với tài khoản đang đăng nhập; khác tài khoản lần trước thì bỏ trạng thái cũ (czr).
    public void BindAccount()
    {
        if (CanSync == false)
        {
            return;
        }

        string uid = FirebaseManager.Instance.Uid;
        string boundUid = PlayerPrefs.GetString(Key(PREF_SYNC_UID), string.Empty);
        if (boundUid == uid)
        {
            return;
        }
        if (string.IsNullOrEmpty(boundUid) == false)
        {
            ClearSyncState();
            SetSwitchPending(true);
        }
        PlayerPrefs.SetString(Key(PREF_SYNC_UID), uid);
        PlayerPrefs.Save();
    }

    // Đăng xuất (czp).
    public void ResetAccount()
    {
        ClearSyncState();
        SetSwitchPending(false);
        PlayerPrefs.DeleteKey(Key(PREF_SYNC_UID));
        PlayerPrefs.Save();
    }

    public void SetSwitchPending(bool isPending)
    {
        if (IsSwitchPending == isPending)
        {
            return;
        }
        IsSwitchPending = isPending;
        PlayerPrefs.SetInt(Key(PREF_SWITCH_PENDING), isPending ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ClearSyncState()
    {
        accountEpoch++;
        isUploading = false;
        nextAllowedTime = 0f;
        retryDelay = RETRY_DELAY_MIN;
        LastUploadedVersion = 0;
        LastSyncUtc = null;
        PlayerPrefs.DeleteKey(Key(PREF_LAST_UPLOADED_VERSION));
        PlayerPrefs.DeleteKey(Key(PREF_LAST_SYNC_UTC_MS));
        SetDirty(false);
    }

    private void SetDirty(bool isDirty)
    {
        if (isDirty)
        {
            dirtyTime = Time.unscaledTime;
        }
        if (IsDirty == isDirty)
        {
            return;
        }
        IsDirty = isDirty;
        dirtySince = Time.unscaledTime;
        PlayerPrefs.SetInt(Key(PREF_UNSYNCED_LOCAL), isDirty ? 1 : 0);
    }

    private void SetLastUploadedVersion(int version)
    {
        LastUploadedVersion = version;
        PlayerPrefs.SetInt(Key(PREF_LAST_UPLOADED_VERSION), version);
        PlayerPrefs.Save();
    }

    private void SetLastSync(long utcMs)
    {
        LastSyncUtc = DateTimeOffset.FromUnixTimeMilliseconds(utcMs).UtcDateTime;
        PlayerPrefs.SetString(Key(PREF_LAST_SYNC_UTC_MS), utcMs.ToString());
        PlayerPrefs.Save();
    }

    private static string Key(string key)
    {
        return FirebaseManager.PrefKey(key);
    }
}

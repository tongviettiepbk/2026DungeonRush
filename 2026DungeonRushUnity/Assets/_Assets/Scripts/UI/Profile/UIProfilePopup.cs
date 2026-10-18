using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup hồ sơ CỦA MÌNH (ProfilePopup gốc), mở từ ô avatar ở lobby. 2 trang đổi bằng TabSelector:
//  - Profile: avatar (bấm hình / nút bút → popup chọn avatar), Power, tên (nút bút → popup đổi tên).
//  - Settings: âm lượng Sound FX / Music (slider, tay nắm ghi On/Off), rung, tài khoản (Sign In / Up ↔ Sign Out, Sync Now),
//    User ID (nút chép), phiên bản.
// Discord (kqz gốc): mở app Discord, 0.15s sau mà game chưa xuống nền thì mở link web.
// Nút Language → UILanguageSelectPopup; dòng nút hiện cờ + tên ngôn ngữ đang dùng (krj gốc).
// Nút của tính năng project chưa có (redeem code, quốc gia, báo lỗi)
// nằm trong listButtonComingSoon — bấm chỉ báo "coming soon".
public class UIProfilePopup : BaseUI
{
    public const int TAB_PROFILE = 0;
    public const int TAB_SETTINGS = 1;

    public Button btClose;
    public TabSelector tabSelector;
    public GameObject objProfilePage;
    public GameObject objSettingsPage;

    [Header("Profile")]
    public TMP_Text txtName;
    public TMP_Text txtPower;
    public Image imgAvatar;
    public Button btAvatar;
    public Button btEditAvatar;
    public Button btEditName;

    [Header("Settings")]
    public Slider sliderSound;
    public TMP_Text txtSoundStatus;
    public Image imgSoundHandle;
    public Slider sliderMusic;
    public TMP_Text txtMusicStatus;
    public Image imgMusicHandle;
    public Sprite sprHandleOn;      // reskin: núm bật = xanh, tắt = xám (đổi sprite thay cho tô màu)
    public Sprite sprHandleOff;
    public ToggleButton toggleVibrate;
    public TMP_Text txtUserId;
    public Button btCopyUserId;
    public TMP_Text txtVersion;

    [Header("Account")]
    public Button btAccount;
    public TMP_Text txtAccountButton;
    public TMP_Text txtAccountInfo;
    public Button btSyncNow;
    public Image imgSyncLoading;

    [Header("Language")]
    public Button btLanguage;
    public Image imgLanguageFlag;
    public TMP_Text txtLanguageName;

    public Button btDiscord;
    public List<Button> listButtonComingSoon;

    // ProfilePopup .ctor gốc.
    private const string DISCORD_APP_URL = "discord://discord.gg/ErRm7dkAs4";
    private const string DISCORD_WEB_URL = "https://discord.gg/ErRm7dkAs4";
    private const float DISCORD_FALLBACK_DELAY = 0.15f;

    private bool isSyncing;
    private bool isWaitingDiscordApp;
    private float discordOpenTime;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btAvatar.onClick.AddListener(OnClickAvatar);
        btEditAvatar.onClick.AddListener(OnClickAvatar);
        btEditName.onClick.AddListener(OnClickEditName);
        btAccount.onClick.AddListener(OnClickAccount);
        btSyncNow.onClick.AddListener(OnClickSyncNow);
        btCopyUserId.onClick.AddListener(OnClickCopyUserId);
        if (btDiscord != null)
            btDiscord.onClick.AddListener(OnClickDiscord);
        if (btLanguage != null)
            btLanguage.onClick.AddListener(OnClickLanguage);
        sliderSound.onValueChanged.AddListener(OnSoundChanged);
        sliderMusic.onValueChanged.AddListener(OnMusicChanged);
        toggleVibrate.onChanged = OnVibrateChanged;
        for (int i = 0; i < listButtonComingSoon.Count; i++)
        {
            listButtonComingSoon[i].onClick.AddListener(OnClickComingSoon);
        }

        // krh gốc.
        tabSelector.Setup(new[] { L("Popup.Profile.Tab.Profile"), L("Popup.Profile.Tab.Settings") });
        tabSelector.OnTabSelected += ShowPage;
        EventDispatcher.Instance.RegisterListener(EventID.AvatarChanged, OnAvatarChanged);
        EventDispatcher.Instance.RegisterListener(EventID.NicknameChanged, OnNicknameChanged);
        EventDispatcher.Instance.RegisterListener(EventID.AccountChanged, OnAccountChanged);
    }

    private void OnDestroy()
    {
        if (EventDispatcher.IsNull == false)
        {
            EventDispatcher.Instance.RemoveListener(EventID.AvatarChanged, OnAvatarChanged);
            EventDispatcher.Instance.RemoveListener(EventID.NicknameChanged, OnNicknameChanged);
            EventDispatcher.Instance.RemoveListener(EventID.AccountChanged, OnAccountChanged);
        }
    }

    // eip gốc: mỗi lần mở về trang Profile và vẽ lại toàn bộ.
    protected override void OnEnable()
    {
        base.OnEnable();
        ShowTab(TAB_PROFILE);
        DrawProfile();
        DrawSettings();
        DrawAccount();
        DrawLanguage();
    }

    // kqj gốc: mở thẳng một trang (Cloud Backup mở Settings).
    public void ShowTab(int tab)
    {
        tabSelector.Select(tab, false);
        ShowPage(tab);
    }

    // Đóng popup mới ghi save (kéo slider đổi giá trị liên tục).
    private void OnDisable()
    {
        GameData.Save();
    }

    // kqm gốc.
    private void ShowPage(int tab)
    {
        objProfilePage.SetActive(tab == TAB_PROFILE);
        objSettingsPage.SetActive(tab == TAB_SETTINGS);
    }

    // ===== Profile =====

    private void DrawProfile()
    {
        txtName.text = GameData.userData.profile.userName;
        txtPower.text = "<sprite=0>" + PlayerPower.ToLong(PlayerPower.GetCurrent()).ToLetter();
        imgAvatar.sprite = AvatarConfig.Get(GameData.userData.profile.avatarId);
    }

    private void OnClickAvatar()
    {
        UIManager.Instance.LoadUI(UIKey.AvatarSelectPopup);
    }

    private void OnAvatarChanged(object param)
    {
        imgAvatar.sprite = AvatarConfig.Get(GameData.userData.profile.avatarId);
    }

    // OnEditNicknameClicked gốc.
    private void OnClickEditName()
    {
        UIManager.Instance.LoadUI(UIKey.NicknamePopup);
    }

    private void OnNicknameChanged(object param)
    {
        txtName.text = GameData.userData.profile.userName;
    }

    // ===== Settings =====

    private void DrawSettings()
    {
        UserSettingData settings = GameData.userData.settings;
        sliderSound.SetValueWithoutNotify(settings.soundVolume);
        sliderMusic.SetValueWithoutNotify(settings.musicVolume);
        DrawVolumeStatus(txtSoundStatus, imgSoundHandle, settings.soundVolume);
        DrawVolumeStatus(txtMusicStatus, imgMusicHandle, settings.musicVolume);
        toggleVibrate.Set(settings.isHapticOn);

        txtVersion.text = "Version " + Application.version;
    }

    // kqr gốc.
    private void OnSoundChanged(float value)
    {
        UserSettingData settings = GameData.userData.settings;
        settings.soundVolume = value;
        settings.isDataChanged = true;
        DrawVolumeStatus(txtSoundStatus, imgSoundHandle, value);
    }

    // kqq gốc. Project chưa có nhạc nền — mới lưu giá trị.
    private void OnMusicChanged(float value)
    {
        UserSettingData settings = GameData.userData.settings;
        settings.musicVolume = value;
        settings.isDataChanged = true;
        DrawVolumeStatus(txtMusicStatus, imgMusicHandle, value);
    }

    // kqs gốc: tay nắm ghi On/Off, tắt hẳn (0) thì ngả xám.
    private void DrawVolumeStatus(TMP_Text text, Image handle, float value)
    {
        bool isOn = value > 0f;
        text.text = L(isOn ? "Common.On" : "Common.Off");
        handle.sprite = isOn ? sprHandleOn : sprHandleOff;
    }

    // Project chưa có chỗ nào rung — mới lưu lựa chọn.
    private void OnVibrateChanged(bool isOn)
    {
        UserSettingData settings = GameData.userData.settings;
        settings.isHapticOn = isOn;
        settings.isDataChanged = true;
    }

    // ===== Account =====

    // krb gốc: khách → nút "Sign In / Up"; đã gắn email → nút "Sign Out", dòng "email · Last sync", nút Sync Now.
    private void DrawAccount()
    {
        AccountController account = AccountController.Instance;
        bool isLinked = account.IsLinked;
        txtAccountButton.text = L(isLinked ? "Popup.Profile.SignOut" : "Popup.Profile.SignInUp");
        txtAccountInfo.gameObject.SetActive(isLinked);
        if (isLinked)
        {
            txtAccountInfo.text = LocalizeManager.Instance.GetLocalizeTextFormat("Popup.Profile.AccountInfo", account.Email, account.GetLastSyncText());
        }
        btSyncNow.gameObject.SetActive(isLinked && isSyncing == false);
        UISpinner.Set(imgSyncLoading, isLinked && isSyncing);
        txtUserId.text = "User ID: " + FirebaseManager.Instance.Uid;
    }

    // krc gốc.
    private void OnAccountChanged(object param)
    {
        if (gameObject.activeInHierarchy)
        {
            DrawAccount();
        }
    }

    // krd gốc.
    private void OnClickAccount()
    {
        if (AccountController.Instance.IsLinked == false)
        {
            UISignUpPopup.Open();
            return;
        }

        AccountController.Instance.SignOut((ok, error) =>
        {
            UIManager.Instance.ShowToastMessage(L(ok ? "Popup.Profile.SignedOut" : "Popup.Profile.SignOutFailed"), isLocalize: false);
        });
    }

    // kre / krf gốc.
    private void OnClickSyncNow()
    {
        if (isSyncing || AccountController.Instance.IsLinked == false)
        {
            return;
        }

        isSyncing = true;
        DrawAccount();
        AccountController.Instance.SyncNow((ok, error) =>
        {
            isSyncing = false;
            DrawAccount();
            string message = L("Popup.Profile.SyncedToCloud");
            if (ok == false)
            {
                bool isRateLimited = string.IsNullOrEmpty(error) == false && error.Contains(CloudSaveController.ERROR_RATE_LIMITED);
                message = L(isRateLimited ? "Popup.Profile.SyncedRecently" : "Popup.Profile.SyncFailed");
            }
            UIManager.Instance.ShowToastMessage(message, isLocalize: false);
        });
    }

    // kqk gốc.
    private void OnClickCopyUserId()
    {
        GUIUtility.systemCopyBuffer = FirebaseManager.Instance.Uid;
        UIManager.Instance.ShowToastMessage(L("Popup.Profile.UserIdCopied"), isLocalize: false);
    }

    // ===== Language =====

    // krj gốc: tìm ngôn ngữ đang dùng trong danh sách của LanguageSelectPopup → cờ (không có thì tắt Image) + tên.
    private void DrawLanguage()
    {
        string code = LocalizeManager.Instance.curLanguage.ToCode();
        UILanguageSelectPopup.LanguageOption option = UILanguageSelectPopup.GetOption(code);
        if (imgLanguageFlag != null)
        {
            bool hasFlag = option != null && option.flag != null;
            if (hasFlag)
                imgLanguageFlag.sprite = option.flag;
            imgLanguageFlag.enabled = hasFlag;
        }
        if (txtLanguageName != null)
            txtLanguageName.text = option != null ? option.displayName : code;
    }

    private void OnClickLanguage()
    {
        UIManager.Instance.LoadUI(UIKey.LanguageSelectPopup);
    }

    private static string L(string key)
    {
        return LocalizeManager.Instance.GetLocalizeText(key);
    }

    private void OnClickComingSoon()
    {
        UIManager.Instance.ShowToastMessage(L("Errors.ComingSoon"), isLocalize: false);
    }

    // kqz gốc.
    private void OnClickDiscord()
    {
        discordOpenTime = Time.time;
        isWaitingDiscordApp = true;
        Application.OpenURL(DISCORD_APP_URL);
    }

    // Update gốc: app Discord không mở (game không xuống nền) → mở bản web.
    private void Update()
    {
        if (isWaitingDiscordApp == false || Time.time - discordOpenTime < DISCORD_FALLBACK_DELAY)
            return;
        isWaitingDiscordApp = false;
        Application.OpenURL(DISCORD_WEB_URL);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            isWaitingDiscordApp = false;
    }
}

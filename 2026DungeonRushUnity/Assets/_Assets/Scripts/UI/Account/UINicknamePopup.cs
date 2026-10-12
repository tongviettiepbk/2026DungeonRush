using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.UI;

// "Enter Nickname" (NicknamePopup gốc): đặt tên lần đầu hoặc đổi tên. Tên 3-15 ký tự, chỉ chữ cái không dấu, số, dấu cách.
// Bản gốc gửi tên lên server đăng ký (kèm lọc từ tục) rồi mới nhận; project lưu tên trên máy và gửi theo hồ sơ online.
public class UINicknamePopup : BaseUI
{
    private const int NAME_MIN = 3;
    private const int NAME_MAX = 15;
    private const string NAME_PATTERN = "^[a-zA-Z0-9 ]+$";

    public TMP_InputField inputName;
    public Button btEnter;
    public Button btClose;

    private Action onClosed;

    protected override void Awake()
    {
        base.Awake();
        btEnter.onClick.AddListener(OnClickEnter);
        btClose.onClick.AddListener(Close);
    }

    // eip gốc: đã có tên thì điền sẵn để sửa.
    protected override void OnEnable()
    {
        base.OnEnable();
        onClosed = null;
        UserProfileData profile = GameData.userData.profile;
        inputName.text = profile.isNameSet ? profile.userName : string.Empty;
    }

    // onClose gọi khi popup đóng, dù đã đặt tên hay chưa.
    public void Show(Action onClose)
    {
        onClosed = onClose;
    }

    // OnDoneClicked gốc.
    private void OnClickEnter()
    {
        string name = inputName.text.Trim();
        if (name.Length < NAME_MIN || name.Length > NAME_MAX)
        {
            UIManager.Instance.ShowToastMessage("Name must be 3-15 characters", isLocalize: false);
            return;
        }
        if (Regex.IsMatch(name, NAME_PATTERN) == false)
        {
            UIManager.Instance.ShowToastMessage("Invalid Character!", isLocalize: false);
            return;
        }

        UserProfileData profile = GameData.userData.profile;
        profile.userName = name;
        profile.isNameSet = true;
        profile.isDataChanged = true;
        GameData.Save();
        this.PostEvent(EventID.NicknameChanged);
        BossRushController.Instance.UpdatePlayer();
        Close();
    }

    public override void Close()
    {
        base.Close();

        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }
}

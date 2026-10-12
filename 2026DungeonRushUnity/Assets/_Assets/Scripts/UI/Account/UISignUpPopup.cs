using TMPro;
using UnityEngine.UI;

// Tạo tài khoản (SignUpPopup gốc) = gắn email + mật khẩu vào tài khoản khách đang chơi, tiến trình giữ nguyên.
public class UISignUpPopup : BaseUI
{
    private const int MIN_PASSWORD_LENGTH = 6;

    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;
    public Button btSignUp;
    public Button btSwitchToSignIn;
    public Button btClose;
    public Image imgLoading;

    private bool isBusy;

    // Lối vào chung của nút "Sign In / Up" (ProfilePopup.krd) và Cloud Backup (CloudSavePopup.jyb):
    // phải tự đặt tên trước rồi mới được tạo tài khoản.
    public static void Open()
    {
        if (GameData.userData.profile.isNameSet)
        {
            UIManager.Instance.LoadUI(UIKey.SignUpPopup);
            return;
        }

        UIManager.Instance.ShowToastMessage("Set a nickname first to link your account", isLocalize: false);
        UINicknamePopup popup = UIManager.Instance.LoadUI(UIKey.NicknamePopup) as UINicknamePopup;
        popup.Show(() =>
        {
            if (GameData.userData.profile.isNameSet)
            {
                UIManager.Instance.LoadUI(UIKey.SignUpPopup);
            }
        });
    }

    protected override void Awake()
    {
        base.Awake();
        btSignUp.onClick.AddListener(OnClickSignUp);
        btSwitchToSignIn.onClick.AddListener(OnClickSwitchToSignIn);
        btClose.onClick.AddListener(Close);
    }

    // eip gốc.
    protected override void OnEnable()
    {
        base.OnEnable();
        inputPassword.text = string.Empty;
        SetBusy(false);
    }

    // kti gốc.
    private void OnClickSignUp()
    {
        if (isBusy)
        {
            return;
        }

        string email = inputEmail.text.Trim();
        string password = inputPassword.text;
        if (UISignInPopup.IsValidEmail(email) == false)
        {
            UIManager.Instance.ShowToastMessage("Please enter a valid email", isLocalize: false);
            return;
        }
        if (password.Length < MIN_PASSWORD_LENGTH)
        {
            UIManager.Instance.ShowToastMessage("Password must be at least 6 characters", isLocalize: false);
            return;
        }

        SetBusy(true);
        AccountController.Instance.SignUp(email, password, (ok, error) =>
        {
            SetBusy(false);
            if (ok == false)
            {
                UIManager.Instance.ShowToastMessage(string.IsNullOrEmpty(error) ? "Sign up failed" : error, isLocalize: false);
                return;
            }
            UIManager.Instance.ShowToastMessage("Account created", isLocalize: false);
            Close();
        });
    }

    // ktj gốc.
    private void OnClickSwitchToSignIn()
    {
        if (isBusy)
        {
            return;
        }
        Close();
        UIManager.Instance.LoadUI(UIKey.SignInPopup);
    }

    // Đang chờ máy chủ thì không cho đóng.
    public override bool IsBackable()
    {
        return isBusy == false && base.IsBackable();
    }

    public override void Close()
    {
        if (isBusy == false)
        {
            base.Close();
        }
    }

    // ktl gốc.
    private void SetBusy(bool busy)
    {
        isBusy = busy;
        btSignUp.gameObject.SetActive(busy == false);
        btSwitchToSignIn.gameObject.SetActive(busy == false);
        btClose.gameObject.SetActive(busy == false);
        UISpinner.Set(imgLoading, busy);
    }
}

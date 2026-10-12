using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.UI;

// Đăng nhập tài khoản email đã có (SignInPopup gốc) + "Forgot password?" gửi thư đặt lại mật khẩu về email đang nhập.
public class UISignInPopup : BaseUI
{
    public const string EMAIL_PATTERN = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";

    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;
    public Button btSignIn;
    public Button btForgotPassword;
    public Button btSwitchToSignUp;
    public Button btClose;
    public Image imgLoading;

    private bool isBusy;
    private Action onClosed;

    protected override void Awake()
    {
        base.Awake();
        btSignIn.onClick.AddListener(OnClickSignIn);
        btForgotPassword.onClick.AddListener(OnClickForgotPassword);
        btSwitchToSignUp.onClick.AddListener(OnClickSwitchToSignUp);
        btClose.onClick.AddListener(Close);
    }

    // eip gốc.
    protected override void OnEnable()
    {
        base.OnEnable();
        onClosed = null;
        inputPassword.text = string.Empty;
        SetBusy(false);
    }

    // kta gốc: điền sẵn email (mở lại sau khi phiên đăng nhập bị mất). onClose gọi khi popup đóng, dù đã đăng nhập hay chưa.
    public void Show(string email, Action onClose)
    {
        onClosed = onClose;
        if (string.IsNullOrEmpty(email) == false)
        {
            inputEmail.text = email;
        }
    }

    public static bool IsValidEmail(string email)
    {
        return string.IsNullOrEmpty(email) == false && Regex.IsMatch(email, EMAIL_PATTERN);
    }

    // ktb gốc.
    private void OnClickSignIn()
    {
        if (isBusy)
        {
            return;
        }

        string email = inputEmail.text.Trim();
        string password = inputPassword.text;
        if (IsValidEmail(email) == false)
        {
            UIManager.Instance.ShowToastMessage("Please enter a valid email", isLocalize: false);
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            UIManager.Instance.ShowToastMessage("Please enter your password", isLocalize: false);
            return;
        }

        SetBusy(true);
        AccountController.Instance.SignIn(email, password, (ok, error) =>
        {
            SetBusy(false);
            if (ok == false)
            {
                UIManager.Instance.ShowToastMessage(string.IsNullOrEmpty(error) ? "Sign in failed" : error, isLocalize: false);
                return;
            }
            UIManager.Instance.ShowToastMessage("Signed in", isLocalize: false);
            Close();
        });
    }

    // ktc gốc.
    private void OnClickForgotPassword()
    {
        if (isBusy)
        {
            return;
        }

        string email = inputEmail.text.Trim();
        if (IsValidEmail(email) == false)
        {
            UIManager.Instance.ShowToastMessage("Enter your email first", isLocalize: false);
            return;
        }

        SetBusy(true);
        AccountController.Instance.SendPasswordReset(email, (ok, error) =>
        {
            SetBusy(false);
            string message = ok ? "Reset email sent" : (string.IsNullOrEmpty(error) ? "Failed to send reset email" : error);
            UIManager.Instance.ShowToastMessage(message, isLocalize: false);
        });
    }

    // ktd gốc.
    private void OnClickSwitchToSignUp()
    {
        if (isBusy)
        {
            return;
        }
        Close();
        UIManager.Instance.LoadUI(UIKey.SignUpPopup);
    }

    // Đang chờ máy chủ thì không cho đóng.
    public override bool IsBackable()
    {
        return isBusy == false && base.IsBackable();
    }

    public override void Close()
    {
        if (isBusy)
        {
            return;
        }
        base.Close();

        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    // ktf gốc.
    private void SetBusy(bool busy)
    {
        isBusy = busy;
        btSignIn.gameObject.SetActive(busy == false);
        btForgotPassword.gameObject.SetActive(busy == false);
        btSwitchToSignUp.gameObject.SetActive(busy == false);
        btClose.gameObject.SetActive(busy == false);
        UISpinner.Set(imgLoading, busy);
    }
}

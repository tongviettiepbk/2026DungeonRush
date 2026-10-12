using UnityEngine.UI;

// "Cloud Backup" (CloudSavePopup gốc): đã có tài khoản → đẩy save lên cloud ngay; còn là khách → dẫn sang tạo tài khoản.
public class UICloudSavePopup : BaseUI
{
    public Button btSave;
    public Button btClose;
    public Image imgLoading;

    private bool isBusy;

    protected override void Awake()
    {
        base.Awake();
        btSave.onClick.AddListener(OnClickSave);
        btClose.onClick.AddListener(Close);
    }

    // eip gốc.
    protected override void OnEnable()
    {
        base.OnEnable();
        SetBusy(false);
    }

    // jxx gốc.
    private void OnClickSave()
    {
        if (isBusy)
        {
            return;
        }

        if (AccountController.Instance.IsLinked == false)
        {
            // jxy / jyb gốc: đóng popup, mở Profile ở trang Settings rồi mở Sign Up đè lên.
            Close();
            UIProfilePopup profile = UIManager.Instance.LoadUI(UIKey.ProfilePopup) as UIProfilePopup;
            profile.ShowTab(UIProfilePopup.TAB_SETTINGS);
            UISignUpPopup.Open();
            return;
        }

        SetBusy(true);
        AccountController.Instance.SyncNow((ok, error) =>
        {
            SetBusy(false);
            UIManager.Instance.ShowToastMessage(ok ? "Synced to cloud" : "Sync failed, try again later", isLocalize: false);
            Close();
        });
    }

    // Đang đẩy save thì không cho đóng.
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

    // jxz gốc.
    private void SetBusy(bool busy)
    {
        isBusy = busy;
        btSave.gameObject.SetActive(busy == false);
        btClose.gameObject.SetActive(busy == false);
        UISpinner.Set(imgLoading, busy);
    }
}

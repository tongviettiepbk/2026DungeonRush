using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Báo cáo người chơi trong chat (ReportPopup gốc): 7 ô lý do tick nhiều được, Gửi → reportChatUser.
// Chưa tick → Errors.Report.NoReasonSelected; xong → Errors.Report.Success / SubmitFailed.
public class UIReportPopup : BaseUI
{
    private const float LOADING_SPIN_SPEED = 360f;

    // veh gốc — đúng thứ tự 7 ô trong prefab, chữ gửi lên server.
    private static readonly string[] REASONS =
    {
        "Harassment", "Spam", "Inappropriate Language", "Offensive Player Name", "Cheating", "Hate Speech", "Other",
    };

    public List<CheckButton> ReasonCheckButtons;
    public Button SubmitButton;
    public Button CloseButton;
    public Image LoadingImage;

    private ChatReportContext context;
    private bool isSubmitting;

    protected override void Awake()
    {
        base.Awake();
        isPopup = true;
        SubmitButton.onClick.AddListener(OnSubmitClicked);
        CloseButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (LoadingImage != null && LoadingImage.gameObject.activeSelf)
        {
            LoadingImage.transform.Rotate(0f, 0f, -LOADING_SPIN_SPEED * Time.unscaledDeltaTime);
        }
    }

    // flf gốc.
    public void Setup(ChatReportContext context)
    {
        this.context = context;
        for (int i = 0; i < ReasonCheckButtons.Count; i++) ReasonCheckButtons[i].SetChecked(false);
        SetSubmitting(false);
    }

    // flg gốc.
    private void OnSubmitClicked()
    {
        if (isSubmitting || context == null)
        {
            return;
        }

        List<string> reasons = new List<string>();
        for (int i = 0; i < ReasonCheckButtons.Count && i < REASONS.Length; i++)
        {
            if (ReasonCheckButtons[i].IsChecked) reasons.Add(REASONS[i]);
        }
        if (reasons.Count == 0)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Report.NoReasonSelected"), isLocalize: false);
            return;
        }

        SetSubmitting(true);
        ChatController.Instance.Report(context, reasons, ok =>
        {
            if (this == null) return;
            SetSubmitting(false);
            UIManager.Instance.ShowToastMessage(Loc.L(ok ? "Errors.Report.Success" : "Errors.Report.SubmitFailed"), isLocalize: false);
            if (ok) Close();
        });
    }

    // flh gốc.
    private void SetSubmitting(bool value)
    {
        isSubmitting = value;
        SubmitButton.interactable = !value;
        if (LoadingImage != null) LoadingImage.gameObject.SetActive(value);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Edit Announcement" (EditAnnouncementPopup gốc): ô nhiều dòng ≤ 300 ký tự, bộ đếm "{n}/300", Save.
public class UIEditAnnouncementPopup : BaseUI
{
    public TMP_InputField AnnouncementInput;
    public TMP_Text CharCounter;
    public Button SaveButton;
    public Button CloseButton;
    public ClanLoadingView LoadingView;
    public TMP_Text TitleText;
    public TMP_Text SaveButtonText;

    private bool isBusy;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "Edit Announcement");
        ClanUIUtil.SetText(SaveButtonText, "Save");
        if (AnnouncementInput != null)
        {
            AnnouncementInput.characterLimit = StaticClanData.ANNOUNCEMENT_MAX;
            AnnouncementInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            AnnouncementInput.onValueChanged.AddListener(UpdateCounter);
            if (AnnouncementInput.placeholder is TMP_Text ph) ph.text = "Announcement...";
        }
        if (SaveButton != null) SaveButton.onClick.AddListener(OnSaveClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(OnCloseClicked);
    }

    // fye + eip gốc.
    public void Setup(string current)
    {
        isBusy = false;
        if (LoadingView != null) LoadingView.Hide();
        if (AnnouncementInput != null) AnnouncementInput.text = current ?? string.Empty;
        UpdateCounter(current ?? string.Empty);
    }

    // fyg gốc.
    private void UpdateCounter(string text)
    {
        ClanUIUtil.SetText(CharCounter, string.Format("{0}/{1}", (text ?? string.Empty).Length, StaticClanData.ANNOUNCEMENT_MAX));
    }

    // fyh gốc.
    private void OnSaveClicked()
    {
        if (isBusy) return;
        string text = AnnouncementInput != null ? AnnouncementInput.text.Trim() : string.Empty;
        SetBusy(true);
        ClanController.Instance.UpdateAnnouncement(text, (ok, error) =>
        {
            SetBusy(false);
            if (!ok)
            {
                ClanUIUtil.Toast(error ?? "Failed to save changes");
                return;
            }
            Close();
        });
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        if (LoadingView == null) return;
        if (busy) LoadingView.ShowLoading();
        else LoadingView.Hide();
    }

    private void OnCloseClicked()
    {
        if (!isBusy) Close();
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum PopupNoticeType
{
    Yes,
    YesNo,
    YesNoClose,
}

public class PopupNotice : BaseUI
{
    public Button btClose;
    public TMP_Text txTitle;
    public TMP_Text txContent;
    public ScrollRect scrollContent;
    public RectTransform scrollTransform;
    public Button btnYes;
    public Button btnNo;
    public TMP_Text txYes;
    public TMP_Text txNo;

    private UnityAction yesCallback;
    private UnityAction noCallback;
    private UnityAction closeCallback;

    protected override void Awake()
    {
        base.Awake();
        btnYes.onClick.AddListener(OnClickBtnYes);
        btnNo.onClick.AddListener(OnClickBtnNo);
        btClose.onClick.AddListener(OnClickBtnClose);
    }

    public void Show(string content, PopupNoticeType popupType, bool isBackable, TextAlignmentOptions textAnchor, string title, string labelYes, string labelNo,
        UnityAction yesCallback, UnityAction noCallback, UnityAction closeCallback)
    {
        this.isBackable = isBackable;

        txTitle.text = title;
        txNo.text = labelNo;
        txYes.text = labelYes;

        txContent.alignment = textAnchor;
        txContent.text = content;
        Vector2 v = txContent.rectTransform.anchoredPosition;
        v.y = 0f;
        txContent.rectTransform.anchoredPosition = v;

        UIManager.Instance.StartActionEndOfFrame(() =>
        {
            scrollContent.enabled = scrollTransform.sizeDelta.y < txContent.rectTransform.sizeDelta.y;
        });

        txYes.transform.parent.gameObject.SetActive(true);
        txNo.transform.parent.gameObject.SetActive(popupType != PopupNoticeType.Yes);
        btClose.gameObject.SetActive(popupType == PopupNoticeType.YesNoClose);

        this.yesCallback = yesCallback;
        this.noCallback = noCallback;
        this.closeCallback = closeCallback;

        gameObject.SetActive(true);
    }

    private void OnClickBtnYes()
    {
        if (yesCallback != null)
            yesCallback();

        Close();
    }

    private void OnClickBtnNo()
    {
        if (noCallback != null)
            noCallback();

        Close();
    }

    private void OnClickBtnClose()
    {
        if (closeCallback != null)
            closeCallback();

        Close();
    }

    public override void Close()
    {
        base.Close();

        yesCallback = null;
        noCallback = null;
        closeCallback = null;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup "Rate Us" — port RateUsPopup gốc (reverse il2cpp eip/eja/ejb/ejc/ejd/eje/ejf). Mở bằng RateUsService.
//  - Mở: chưa chọn sao → nút Submit tắt + chữ xám.
//  - Bấm sao thứ N: N sao đầu sáng, còn lại tối; Submit bật + chữ sáng.
//  - Submit: đánh dấu đã đánh giá (không hiện lại nữa); >= 4 sao thì mở trang store; đóng popup.
//  - Later (nút X theo mockup reskin): chỉ đóng.
// Prefab Resources/Prefabs/UI/UIRateUsPopup dựng theo mockup reskin (_Concept/RateUs).
public class UIRateUsPopup : BaseUI
{
    public Image[] StarImages;
    public Sprite StarFilledSprite;
    public Sprite StarEmptySprite;
    public Button SubmitButton;
    public Button LaterButton;
    public TMP_Text SubmitButtonText;
    public Color SubmitTextGrayColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color SubmitTextBlueColor = Color.white;
    public string iosAppId;

    private int selectedStars;   // uig

    protected override void Awake()
    {
        base.Awake();
        for (int i = 0; i < StarImages.Length; i++)
        {
            int stars = i + 1;
            Button button = StarImages[i].GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => OnClickStar(stars));
        }
        SubmitButton.onClick.AddListener(OnClickSubmit);
        LaterButton.onClick.AddListener(Close);
    }

    // eip gốc.
    public void Show()
    {
        selectedStars = 0;
        RefreshStars();
        RefreshSubmit();
    }

    // eja gốc.
    private void OnClickStar(int stars)
    {
        selectedStars = stars;
        RefreshStars();
        RefreshSubmit();
    }

    // ejb gốc.
    private void RefreshStars()
    {
        for (int i = 0; i < StarImages.Length; i++)
            StarImages[i].sprite = i < selectedStars ? StarFilledSprite : StarEmptySprite;
    }

    // ejc gốc.
    private void RefreshSubmit()
    {
        SubmitButtonText.color = selectedStars >= 1 ? SubmitTextBlueColor : SubmitTextGrayColor;
        SubmitButton.interactable = selectedStars > 0;
    }

    // ejd gốc.
    private void OnClickSubmit()
    {
        if (selectedStars < 1)
            return;

        RateUsService.MarkSubmitted();
        if (selectedStars >= RateUsService.STORE_MIN_STARS)
            OpenStore();
        Close();
    }

    // eje gốc: market:// lỗi thì mở trang web Play.
    private void OpenStore()
    {
#if UNITY_IOS
        if (string.IsNullOrEmpty(iosAppId) == false)
            Application.OpenURL("itms-apps://itunes.apple.com/app/id" + iosAppId);
#else
        try
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }
        catch
        {
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
        }
#endif
    }
}

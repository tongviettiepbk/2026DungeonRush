using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup chế tạo wing (WingCraftPopup gốc) — mở từ nút Craft trang Wing khi chưa sở hữu wing đang chọn.
// Hiện icon + "<quặng> có/cần"; Craft: thiếu quặng → toast, đủ → WingService.Craft rồi đóng và gọi onCrafted
// (trang Wing hiện ClaimPage).
public class UIWingCraftPopup : BaseUI
{
    public Image imgBackground;
    public Image imgWingIcon;
    public TMP_Text txtResource;
    public Button btCancel;
    public Image imgCancel;
    public Button btCraft;
    public Image imgCraft;
    public TMP_Text txtCraft;

    private WingData wing;
    private Action onCrafted;

    protected override void Awake()
    {
        base.Awake();
        btCancel.onClick.AddListener(Close);
        btCraft.onClick.AddListener(OnClickCraft);
    }

    public void Show(WingData wing, Sprite bgRarity, Action onCrafted)
    {
        this.wing = wing;
        imgBackground.sprite = bgRarity;
        this.onCrafted = onCrafted;
        Refresh();
        gameObject.SetActive(true);
    }

    // kyd
    private void Refresh()
    {
        imgWingIcon.sprite = wing.icon;
        imgCancel.color = UITabWing.DANGER_BUTTON_COLOR;

        int ore = WingService.GetOre(wing.craftOreType);
        bool isEnough = ore >= wing.craftOreCost;
        txtResource.text = UITabWing.GetOreText(wing.craftOreType, ore, wing.craftOreCost);
        txtResource.color = isEnough ? Color.white : Color.red;
        txtCraft.text = WingService.GetOwned(wing) != null ? "Reroll" : "Craft";
        imgCraft.color = isEnough ? UITabWing.DEFAULT_BUTTON_COLOR : UITabWing.DISABLED_BUTTON_COLOR;
    }

    // kye
    private void OnClickCraft()
    {
        if (WingService.Craft(wing) == false)
        {
            UIManager.Instance.ShowToastMessage("Không đủ tài nguyên", isLocalize: false);
            return;
        }

        Action callback = onCrafted;
        onCrafted = null;
        Close();
        this.PostEvent(EventID.WingChanged);
        callback?.Invoke();
    }
}

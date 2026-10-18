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
    public Button btCraft;
    public Image imgCraft;
    public TMP_Text txtCraft;
    public Sprite sprButtonOn;
    public Sprite sprButtonOff;

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

        int ore = WingService.GetOre(wing.craftOreType);
        bool isEnough = ore >= wing.craftOreCost;
        txtResource.text = UITabWing.GetOreText(wing.craftOreType, ore, wing.craftOreCost);
        txtResource.color = isEnough ? Color.white : Color.red;
        txtCraft.text = WingService.GetOwned(wing) != null ? Loc.L("UI.Wings.Reroll") : Loc.L("UI.Wings.Craft");
        UITabWing.SetButtonState(imgCraft, isEnough, sprButtonOn, sprButtonOff);
    }

    // kye
    private void OnClickCraft()
    {
        if (WingService.Craft(wing) == false)
        {
            UIManager.Instance.ShowToastMessage("Không đủ tài nguyên", isLocalize: false);
            return;
        }

        TutorialService.Complete(FtueStep.CraftWing);   // kye gốc: chế wing lần đầu (chỉ đánh dấu)
        Action callback = onCrafted;
        onCrafted = null;
        Close();
        this.PostEvent(EventID.WingChanged);
        callback?.Invoke();
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup thông tin 1 relic (EnchantmentInfoPopup gốc): tier, icon, "Relic Power +X%" + 3 nút.
//   • Relic trong KHO   : Equip (→ trang chọn slot) / Merge (→ popup Merge) / Dismantle (1 → 3 tier dưới).
//   • Relic ĐANG ĐEO    : Unequip / Merge (→ popup Merge, relic ở slot làm ô đầu) / Dismantle (slot tụt 1 tier, kho +2).
// Merge bật khi tier < 11, Dismantle bật khi tier > 1 (EnchantmentInfoPopup.eip gốc).
public class UIEnchantmentElementInfo : BaseUI
{
    public Button btClose;
    public TMP_Text txtTier;
    public Image imgBg;
    public Image imgIcon;
    public TMP_Text txtInfo;

    [Space(5)]
    public Button btEquip;
    public TMP_Text txtEquip;
    public Button btMerge;
    public Button btDismantle;

    [Space(5)]
    // Trạng thái nút bằng sprite (Batch4Reskin gán): Equip xanh lá / Unequip tím, Merge cam, Dismantle đỏ, khoá xám.
    public Sprite sprEquip;
    public Sprite sprUnequip;
    public Sprite sprMerge;
    public Sprite sprDismantle;
    public Sprite sprDisabled;

    private int tier;
    private bool isEquipped;
    private GearSlotType slot;
    private Action<int> onEquip;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        if (btEquip != null)
        {
            btEquip.onClick.AddListener(OnClickEquip);
        }
        if (btMerge != null)
        {
            btMerge.onClick.AddListener(OnClickMerge);
        }
        if (btDismantle != null)
        {
            btDismantle.onClick.AddListener(OnClickDismantle);
        }
    }

    // Relic trong kho. onEquip(tier) = trang Enchantment chuyển sang chế độ chọn slot.
    public void ShowOwned(int tier, Action<int> onEquip)
    {
        this.tier = tier;
        this.onEquip = onEquip;
        isEquipped = false;
        SetLayout();
    }

    // Relic đang đeo ở slot.
    public void ShowEquipped(GearSlotType slot)
    {
        this.slot = slot;
        tier = GameData.userData.enchantments.GetEquipped(slot);
        onEquip = null;
        isEquipped = true;
        SetLayout();
    }

    private void SetLayout()
    {
        StaticEnchantmentData data = GameData.staticData.enchantments;
        txtTier.text = Loc.L("Popup.Enchantment.Tier", tier);   // tiêu đề header "Relic +N"
        imgBg.sprite = data.GetTierBackground(tier);
        imgIcon.sprite = data.GetTierIcon(tier);
        // Chỉ số: nhãn "Relic Power" nằm riêng (key Popup.Enchantment.PowerLabel), ô này chỉ ghi "+X%".
        txtInfo.text = "+" + EnchantmentConfig.GetBuffPercent(tier).ToString("0.#") + "%";

        if (txtEquip != null)
        {
            txtEquip.text = isEquipped ? Loc.L("Popup.Enchantment.Unequip") : Loc.L("Popup.Enchantment.Equip");
        }
        if (btEquip != null)
        {
            SetSprite(btEquip, isEquipped ? sprUnequip : sprEquip);
        }
        if (btMerge != null)
        {
            btMerge.interactable = tier < EnchantmentConfig.MAX_TIER;
            SetSprite(btMerge, btMerge.interactable ? sprMerge : sprDisabled);
        }
        if (btDismantle != null)
        {
            btDismantle.interactable = tier >= 2;
            SetSprite(btDismantle, btDismantle.interactable ? sprDismantle : sprDisabled);
        }

        gameObject.SetActive(true);
    }

    private static void SetSprite(Button button, Sprite sprite)
    {
        if (sprite != null)
        {
            button.image.sprite = sprite;
        }
    }

    private void OnClickEquip()
    {
        Close();
        if (isEquipped)
        {
            double powerBefore = PlayerPower.GetCurrent();
            if (EnchantmentService.Unequip(slot))
            {
                this.PostEvent(EventID.EnchantmentChanged);
                PlayerPower.NotifyChange(powerBefore);
            }
            return;
        }

        onEquip?.Invoke(tier);
    }

    // EnchantmentInfoPopup.hxp gốc: đóng popup → mở popup Merge với (Tier, SlotIndex).
    private void OnClickMerge()
    {
        Close();
        UIEnchantmentMerge ui = UIManager.Instance.LoadUI(UIKey.EnchantmentMerge) as UIEnchantmentMerge;
        if (ui != null)
        {
            ui.Show(tier, isEquipped ? slot : (GearSlotType?)null);
        }
    }

    private void OnClickDismantle()
    {
        double powerBefore = PlayerPower.GetCurrent();
        bool done = isEquipped ? EnchantmentService.DismantleEquipped(slot) : EnchantmentService.Dismantle(tier);
        Close();
        if (done)
        {
            this.PostEvent(EventID.EnchantmentChanged);
            if (isEquipped)
            {
                PlayerPower.NotifyChange(powerBefore);   // gốc chỉ báo Power khi relic đang đeo đổi
            }
        }
    }
}

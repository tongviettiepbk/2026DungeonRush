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
        txtTier.text = "Relic +" + tier;
        imgBg.sprite = data.GetTierBackground(tier);
        imgIcon.sprite = data.GetTierIcon(tier);
        // "Popup.Enchantment.Buff" = "Relic Power +{0}%" với {0} = " <color=#E5F36B>{tier²:0.#}</color>".
        txtInfo.text = "Relic Power + <color=#E5F36B>" + EnchantmentConfig.GetBuffPercent(tier).ToString("0.#") + "</color>%";

        if (txtEquip != null)
        {
            txtEquip.text = isEquipped ? "Unequip" : "Equip";
        }
        if (btMerge != null)
        {
            btMerge.interactable = tier < EnchantmentConfig.MAX_TIER;
        }
        if (btDismantle != null)
        {
            btDismantle.interactable = tier >= 2;
        }

        gameObject.SetActive(true);
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

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 slot đeo relic trong trang Enchantment (EnchantmentSlotUI gốc). Hiện MÓN ĐỒ đang mặc ở slot
// (nền rarity + icon, trống thì placeholder) và badge tier relic đang đeo.
public class ElementEnchantmentEquipUI : MonoBehaviour
{
    public GearSlotType slotType;
    public Button btSlot;
    public Image imgBgTierItem;          // nền rarity món đồ đang mặc
    public Image imgItem;                // icon món đồ đang mặc
    public GameObject objPlaceholder;    // hình slot trống (chưa mặc đồ)
    public GameObject objIndicator;      // badge tier (Image nền theo tier)
    public TMP_Text tmpLvIndicator;      // "+tier" trên badge
    public TMP_Text tmpLvIndicatorBot;   // "Lvl tier" dưới ô
    public GameObject objTutEquipHere;   // sáng lên khi đang chọn slot để đeo relic

    private Action<ElementEnchantmentEquipUI> onClick;

    public int Tier { get; private set; }

    private void Awake()
    {
        if (btSlot != null)
        {
            btSlot.onClick.AddListener(OnClick);
        }
    }

    public void Init(Action<ElementEnchantmentEquipUI> onClick)
    {
        this.onClick = onClick;
    }

    // EnchantmentSlotUI.hyb gốc.
    public void Refresh()
    {
        Tier = GameData.userData.enchantments.GetEquipped(slotType);
        StaticEnchantmentData data = GameData.staticData.enchantments;

        // Món đồ đang mặc ở slot.
        string equipId = GameData.userData.equipment.GetEquipped(slotType);
        LootResult gear = LootService.BuildFromEquipId(slotType, equipId);
        bool hasGear = gear != null && gear.icon != null;
        if (imgItem != null)
        {
            imgItem.gameObject.SetActive(hasGear);
            if (hasGear)
            {
                imgItem.sprite = gear.icon;
            }
        }
        if (objPlaceholder != null)
        {
            objPlaceholder.SetActive(hasGear == false);
        }
        if (imgBgTierItem != null)
        {
            // GameResources.jgr gốc: chưa mặc đồ thì nền rarity 0 (Common).
            imgBgTierItem.sprite = data.GetRarityBackground(hasGear ? gear.rarity : Rarity.Common);
        }

        // Badge relic.
        bool hasRelic = Tier >= 1;
        if (objIndicator != null)
        {
            objIndicator.SetActive(hasRelic);
            Image badge = objIndicator.GetComponent<Image>();
            if (hasRelic && badge != null)
            {
                badge.sprite = data.GetTierBackground(Tier);
            }
        }
        if (tmpLvIndicator != null)
        {
            tmpLvIndicator.text = "+" + Tier;
        }
        if (tmpLvIndicatorBot != null)
        {
            tmpLvIndicatorBot.gameObject.SetActive(hasRelic);
            tmpLvIndicatorBot.text = "Lvl " + Tier;
        }
    }

    // EnchantmentSlotUI.hye gốc: bật chỉ dẫn "đeo vào đây" khi đang chọn slot.
    public void SetSelectMode(bool isOn)
    {
        if (objTutEquipHere != null)
        {
            objTutEquipHere.SetActive(isOn);
        }
    }

    private void OnClick()
    {
        onClick?.Invoke(this);
    }
}

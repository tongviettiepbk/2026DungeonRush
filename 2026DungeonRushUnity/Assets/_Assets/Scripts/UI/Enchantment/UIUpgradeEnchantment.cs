using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup tỉ lệ summon relic (EnchantmentTierRatesPopup gốc) — mở từ nút (i) cạnh Summon Level.
// Cột trái = Summon Level hiện tại, cột phải = level kế; số liệu từ EnchantmentConfig.
// listRarity xếp theo tier 1..; summon chỉ ra tier 1..6 → hàng thừa của prefab bị ẩn.
// Gốc (hzw): level ghi "Lv {0}", level kế = min(level+1, 50) (không có chữ MAX), tỉ lệ "{0:F2}%".
public class UIUpgradeEnchantment : BaseUI
{
    public Button btClose;
    public TMP_Text txtLevelCurrent;
    public TMP_Text txtLevelNext;
    public List<ElementRarityPet> listRarity;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
    }

    public void Show()
    {
        int level = EnchantmentService.GetSummonLevel();
        int nextLevel = Mathf.Min(level + 1, EnchantmentConfig.MAX_SUMMON_LEVEL);

        txtLevelCurrent.text = "Lv " + level;
        txtLevelNext.text = "Lv " + nextLevel;

        for (int i = 0; i < listRarity.Count; i++)
        {
            int tier = i + 1;
            bool isValid = tier <= EnchantmentConfig.SUMMON_TIER_COUNT;
            listRarity[i].gameObject.SetActive(isValid);
            if (isValid)
            {
                float current = EnchantmentConfig.GetRate(level, tier);
                float next = EnchantmentConfig.GetRate(nextLevel, tier);
                listRarity[i].txtName.text = Loc.L("Popup.Enchantment.Tier", tier);
                listRarity[i].txtRateCurret.text = current.ToString("F2") + "%";
                listRarity[i].txtRateNext.text = next.ToString("F2") + "%";
                // Cột Lv sau: tăng xanh / giảm hồng nhạt (khuôn Summon Rates Đợt 3).
                listRarity[i].txtRateNext.color = next > current ? ElementRarityPet.RATE_UP_COLOR
                    : next < current ? ElementRarityPet.RATE_DOWN_COLOR : Color.white;
            }
        }

        gameObject.SetActive(true);
    }
}

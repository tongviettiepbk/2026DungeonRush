using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup tỉ lệ summon pet (CompanionUpgradeInfoPopup gốc) — mở từ CompanionUI.btInfo.
// Cột trái = Summon Level hiện tại, cột phải = level kế; số liệu từ CompanionSummonLevelConfig.
// listRarity xếp THEO THỨ TỰ Rarity (Common..). Pet chỉ có 6 rarity (Common..Mythic) → hàng thừa
// của prefab (Exotic / SSS Tier / Z Tier) bị ẩn.
public class UIUpgradePet : BaseUI
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
        int level = CompanionSummonLevelConfig.GetLevel(GameData.userData.companions.totalSummons);
        bool isMax = level >= CompanionSummonLevelConfig.MAX_LEVEL;
        int nextLevel = isMax ? level : level + 1;

        txtLevelCurrent.text = "Lv. " + level;
        txtLevelNext.text = isMax ? "MAX" : "Lv. " + nextLevel;

        for (int i = 0; i < listRarity.Count; i++)
        {
            bool isValid = i < CompanionSummonLevelConfig.RARITY_COUNT;
            listRarity[i].gameObject.SetActive(isValid);
            if (isValid)
            {
                Rarity rarity = (Rarity)i;
                listRarity[i].SetData(CompanionSummonLevelConfig.GetRate(level, rarity),
                                      CompanionSummonLevelConfig.GetRate(nextLevel, rarity));
            }
        }

        gameObject.SetActive(true);
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

// Bảng tỉ lệ quặng level mỏ hiện tại / level kế (MiningRarityPopup gốc).
// Dòng i = OreWeights[i+1] (bỏ Dirt ở đầu) → Stone, Coal, Iron, Ruby, Emerald, Gold, Diamond;
// % = weight / tổng weight (không tính Dirt) × 100, "{0:F1}%". Level max → cột kế là "-".
public class UIMiningRarityPopup : BaseUI
{
    public List<TMP_Text> listCurrentRate;
    public List<TMP_Text> listNextRate;
    public TMP_Text txtCurrentLevel;
    public TMP_Text txtNextLevel;
    public Button btClose;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Refresh();
    }

    // jfh
    private void Refresh()
    {
        MiningConfig config = MiningService.Config;
        int level = GameData.userData.mining.mineLevel;
        int maxLevel = config.GetMaxMineLevel();
        txtCurrentLevel.text = "Lvl " + level;
        txtNextLevel.text = level < maxLevel ? "Lvl " + (level + 1) : "-";

        FillRates(listCurrentRate, config.GetRarityLevel(level));
        if (level < maxLevel)
        {
            FillRates(listNextRate, config.GetRarityLevel(level + 1));
        }
        else
        {
            for (int i = 0; i < listNextRate.Count; i++)
            {
                listNextRate[i].text = "-";
            }
        }
    }

    // jfj
    private static void FillRates(List<TMP_Text> texts, MineRarityLevel rarity)
    {
        float total = 0f;
        for (int i = 1; i < rarity.OreWeights.Count; i++)
        {
            total += rarity.OreWeights[i].Weight;
        }

        for (int i = 0; i < texts.Count && i + 1 < rarity.OreWeights.Count; i++)
        {
            float percent = total > 0f ? rarity.OreWeights[i + 1].Weight / total * 100f : 0f;
            texts[i].text = string.Format("{0:F1}%", percent);
        }
    }
}

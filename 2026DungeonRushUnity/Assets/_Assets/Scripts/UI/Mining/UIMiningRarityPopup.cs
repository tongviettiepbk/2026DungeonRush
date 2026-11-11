using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        txtCurrentLevel.text = Loc.L("Common.Level.Abbrev", level);
        txtNextLevel.text = level < maxLevel ? Loc.L("Common.Level.Abbrev", level + 1) : "-";

        float[] current = FillRates(listCurrentRate, config.GetRarityLevel(level));
        if (level < maxLevel)
        {
            float[] next = FillRates(listNextRate, config.GetRarityLevel(level + 1));
            // Reskin Đợt 5: cột level kế tăng xanh / giảm đỏ (so số đã làm tròn như chữ hiện).
            for (int i = 0; i < listNextRate.Count && i < next.Length && i < current.Length; i++)
            {
                float a = Mathf.Round(current[i] * 10f), b = Mathf.Round(next[i] * 10f);
                listNextRate[i].color = b > a ? RATE_UP_COLOR : b < a ? RATE_DOWN_COLOR : Color.white;
            }
        }
        else
        {
            for (int i = 0; i < listNextRate.Count; i++)
            {
                listNextRate[i].text = "-";
                listNextRate[i].color = Color.white;
            }
        }
    }

    private static readonly Color RATE_UP_COLOR = new Color32(98, 211, 74, 255);     // #62D34A
    private static readonly Color RATE_DOWN_COLOR = new Color32(255, 90, 90, 255);   // #FF5A5A

    // jfj — trả về % từng dòng để tô màu cột kế.
    private static float[] FillRates(List<TMP_Text> texts, MineRarityLevel rarity)
    {
        float total = 0f;
        for (int i = 1; i < rarity.OreWeights.Count; i++)
        {
            total += rarity.OreWeights[i].Weight;
        }

        float[] percents = new float[texts.Count];
        for (int i = 0; i < texts.Count && i + 1 < rarity.OreWeights.Count; i++)
        {
            percents[i] = total > 0f ? rarity.OreWeights[i + 1].Weight / total * 100f : 0f;
            texts[i].text = string.Format("{0:F1}%", percents[i]);
        }
        return percents;
    }
}

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Bảng thưởng trong sảnh (BossRushRewardsPanel gốc): "Guaranteed Rewards" theo số boss nhóm đã giết +
// thưởng theo hạng ("Your placement: {0}"). Số liệu lấy từ rewardTable server trả (config/bossRush trên Firestore).
public class UIBossRushRewardsPanel : MonoBehaviour
{
    public GameObject panel;
    public Button btClose;
    public TMP_Text txtGuaranteed;       // PrizePoolInfoText
    public TMP_Text txtPlacement;        // PlacementText
    public TMP_Text txtPlacementRewards; // PlacementRewardsText
    // Reskin: thưởng nhóm nằm trong ô riêng (txtGuaranteed giữ làm nhãn) + mỗi mốc hạng 1 dòng
    // (con TMP thứ 0 = hạng, thứ 1 = thưởng); bỏ trống thì gộp chữ như cũ.
    public TMP_Text txtGuaranteedRewards;
    public GameObject placementRowTemplate;

    private readonly List<GameObject> placementRows = new List<GameObject>();

    private void Awake()
    {
        if (btClose != null) btClose.onClick.AddListener(Hide);
    }

    public void Show(int currentPosition, int bossesKilled, BossRushRewardTableDTO table)
    {
        (panel != null ? panel : gameObject).SetActive(true);
        if (txtPlacement != null) txtPlacement.text = "Your placement: " + currentPosition;

        string guaranteed = "-";
        if (table != null && table.guaranteedRewardsByBossesKilled != null && table.guaranteedRewardsByBossesKilled.Count > 0)
        {
            int index = Mathf.Clamp(bossesKilled, 0, table.guaranteedRewardsByBossesKilled.Count - 1);
            guaranteed = FormatRewards(table.guaranteedRewardsByBossesKilled[index]);
        }
        if (txtGuaranteedRewards != null) txtGuaranteedRewards.text = "Bosses killed: " + bossesKilled + "\n" + guaranteed;
        else if (txtGuaranteed != null) txtGuaranteed.text = "Guaranteed Rewards\nBosses killed: " + bossesKilled + "\n" + guaranteed;

        if (placementRowTemplate != null)
        {
            ShowPlacementRows(table);
            return;
        }

        List<string> lines = new List<string>();
        if (table != null && table.placementBrackets != null)
        {
            for (int i = 0; i < table.placementBrackets.Count; i++)
            {
                BossRushPlacementBracketDTO b = table.placementBrackets[i];
                string rank = b.minRank == b.maxRank ? "#" + b.minRank : "#" + b.minRank + "-" + b.maxRank;
                lines.Add(rank + "  " + FormatRewards(b.rewards));
            }
        }
        if (txtPlacementRewards != null) txtPlacementRewards.text = lines.Count > 0 ? string.Join("\n", lines) : "-";
    }

    private void ShowPlacementRows(BossRushRewardTableDTO table)
    {
        placementRowTemplate.SetActive(false);
        for (int i = 0; i < placementRows.Count; i++) Destroy(placementRows[i]);
        placementRows.Clear();
        if (table == null || table.placementBrackets == null) return;
        for (int i = 0; i < table.placementBrackets.Count; i++)
        {
            BossRushPlacementBracketDTO b = table.placementBrackets[i];
            GameObject row = Instantiate(placementRowTemplate, placementRowTemplate.transform.parent);
            row.SetActive(true);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            texts[0].text = b.minRank == b.maxRank ? b.minRank.ToString() : b.minRank + "-" + b.maxRank;
            if (texts.Length > 1) texts[1].text = FormatRewards(b.rewards);
            placementRows.Add(row);
        }
    }

    public void Hide()
    {
        (panel != null ? panel : gameObject).SetActive(false);
    }

    public static string FormatRewards(List<RewardEntry> rewards)
    {
        if (rewards == null || rewards.Count == 0) return "-";
        List<string> parts = new List<string>();
        for (int i = 0; i < rewards.Count; i++)
        {
            parts.Add(rewards[i].Type + " x" + rewards[i].Amount);
        }
        return string.Join(", ", parts);
    }
}

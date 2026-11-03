using TMPro;
using UnityEngine;

// 1 khung hạng của bảng thưởng mùa: "1" hoặc "4-10" + 4 món theo thứ tự server trả (Gem, Lootbox, Bone, Cloak).
public class ElementPvPRewardBandUI : MonoBehaviour
{
    public TMP_Text txtRank;
    public TMP_Text[] txtRewards = new TMP_Text[4];

    public void Setup(PvPRewardBandDTO band)
    {
        if (txtRank != null) txtRank.text = band.MinRank == band.MaxRank ? band.MinRank.ToString() : band.MinRank + "-" + band.MaxRank;
        for (int i = 0; i < txtRewards.Length; i++)
        {
            if (txtRewards[i] == null) continue;
            bool hasReward = band.Rewards != null && i < band.Rewards.Count;
            txtRewards[i].gameObject.SetActive(hasReward);
            if (hasReward) txtRewards[i].text = "<sprite=0>" + band.Rewards[i].Amount;
        }
    }
}

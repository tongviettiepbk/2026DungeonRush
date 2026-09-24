using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Config tĩnh hệ Mastery (G1). Load toàn bộ MasteryUpgradeData asset trong Resources rồi
// index theo UpgradeType. Theo pattern StaticCompanionData/StaticGearItemData.
//
// Nguồn: AssetRipper MasteryConfig.asset + 10 file *Mastery*Data (DecodedData/tables/*).
public class StaticMasteryData
{
    public List<MasteryUpgradeData> upgrades;

    // Bật/tắt cả hệ (MasteryConfig.Enabled). Gốc = 1.
    public bool enabled = true;

    // GHI CHÚ (reverse v41): phí mở khoá = MasteryUpgradeData.unlockGemCost RIÊNG từng nhánh
    // (xem MasteryService.GetUnlockCost). MasteryConfig.UnlockGemCosts + hàm iuf là DEAD CODE,
    // cố tình KHÔNG dùng ở đây.

    // Thứ tự GỐC = MasteryConfig.Upgrades (đọc từ xapk v41). Là thứ tự hiển thị VÀ thứ tự mở khoá
    // tuần tự; phí mở (unlockGemCost) tăng dần theo đúng thứ tự này: 0, 0, 30, 60 ... 240.
    private static readonly MasteryUpgradeType[] ORDER =
    {
        MasteryUpgradeType.AutoLootHammerCount,
        MasteryUpgradeType.AdBoostWorth,
        MasteryUpgradeType.GemOfferChance,
        MasteryUpgradeType.CompanionSummonCount,
        MasteryUpgradeType.MaxOfflineTime,
        MasteryUpgradeType.MiningMaxPickaxe,
        MasteryUpgradeType.ForgeMaxItemLevel,
        MasteryUpgradeType.AdBoostDuration,
        MasteryUpgradeType.OfflineEarningWorth,
        MasteryUpgradeType.PlayerMovementSpeed,
    };

    private Dictionary<MasteryUpgradeType, MasteryUpgradeData> byType;

    public StaticMasteryData()
    {
        upgrades = Resources.LoadAll<MasteryUpgradeData>("Scriptable Objects/Mastery")
            .OrderBy(x => System.Array.IndexOf(ORDER, x.upgradeType)).ToList();

        byType = new Dictionary<MasteryUpgradeType, MasteryUpgradeData>();
        for (int i = 0; i < upgrades.Count; i++)
        {
            byType[upgrades[i].upgradeType] = upgrades[i];
        }
    }

    public MasteryUpgradeData GetUpgrade(MasteryUpgradeType type)
    {
        if (byType.TryGetValue(type, out MasteryUpgradeData data))
        {
            return data;
        }

        DebugCustom.Log("[StaticMasteryData] Not found type=" + type);
        return null;
    }

    // Giá trị đang có của 1 nhánh theo cấp (level 0 = chưa nâng → default).
    public float GetValue(MasteryUpgradeType type, int level)
    {
        MasteryUpgradeData data = GetUpgrade(type);
        return data != null ? data.GetValueAtLevel(level) : 0f;
    }
}

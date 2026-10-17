using System.Collections.Generic;
using UnityEngine;

// Luật Forge / Auto Forge port từ game gốc (ForgeController + AutoForgePopup, libil2cpp v41).
// Stateless, không dính UI. Hằng số = giá trị GameResources/prefab gốc đọc từ xapk.
// Xem DecodedData/FORGE_MODEL.md.
public static class ForgeService
{
    // GameResources.ItemSellExperiencePerRarity — EXP khi bán 1 món, index = rarity (kẹp cuối bảng).
    public static readonly int[] SELL_EXP_PER_RARITY = { 1, 2, 4, 6, 8, 10, 12, 14 };

    // AutoForgePopup.HammerUnlockLevels — mốc playerLevel mở thêm 1 búa/lượt (khi chưa tới Mastery).
    public static readonly int[] HAMMER_UNLOCK_LEVELS = { 0, 10, 20, 30, 40, 50, 60, 70 };

    // GameResources.StartingLootBoxCount — số búa tặng người mới (remote config starting_loot_box_count).
    public const int STARTING_LOOT_BOX_COUNT = 240;

    // Số ô chọn búa trong popup (Option1..Option1_7).
    public const int HAMMER_OPTION_COUNT = 8;

    // GameResources.MasteryUnlockPlayerLevel — từ level này số búa tối đa lấy theo Mastery AutoLootHammerCount.
    public const int MASTERY_UNLOCK_PLAYER_LEVEL = 6;

    // GameResources.SubStatFilterUnlockForgeLevel (so với PlayerLevel).
    public const int SUBSTAT_FILTER_UNLOCK_LEVEL = 7;

    // AutoForgeButton.VisibleAtPlayerLevel / UsableAtPlayerLevel.
    public const int AUTO_VISIBLE_LEVEL = 2;
    public const int AUTO_USABLE_LEVEL = 3;

    private static UserForgeData User => GameData.userData.forge;
    private static int PlayerLevel => GameData.userData.player.playerLevel;

    public static int HammerCount => (int)GameData.userData.items.GetQuantityHave(ItemType.LOOT_TICKET);

    // igg gốc: số búa tối đa mỗi lượt auto.
    public static int GetMaxHammerCount()
    {
        int count;
        if (PlayerLevel >= MASTERY_UNLOCK_PLAYER_LEVEL && MasteryService.IsEnabled)
        {
            count = Mathf.RoundToInt(MasteryService.GetCurrentValue(MasteryUpgradeType.AutoLootHammerCount));
        }
        else
        {
            count = 0;
            while (count < HAMMER_UNLOCK_LEVELS.Length && PlayerLevel >= HAMMER_UNLOCK_LEVELS[count])
                count++;
        }
        return Mathf.Max(1, count);
    }

    // Số búa / lượt auto đang chọn, kẹp theo max hiện tại.
    public static int GetSelectedHammerCount()
    {
        return Mathf.Clamp(User.selectedAutoHammerCount, 1, GetMaxHammerCount());
    }

    public static bool IsSubStatFilterUnlocked()
    {
        return PlayerLevel >= SUBSTAT_FILTER_UNLOCK_LEVEL;
    }

    // heo gốc: auto có BỎ (bán) món này không.
    public static bool ShouldDiscard(LootResult item)
    {
        if (User.IsRarityKept(item.rarity) == false)
            return true;

        if (User.subStatFilterEnabled == false)
            return false;

        List<GearSubStat> subStats = item.subStats;
        if (subStats != null)
        {
            for (int i = 0; i < subStats.Count; i++)
            {
                if (User.IsSubStatEnabled(subStats[i].type))
                    return false;
            }
        }
        return true;
    }

    public static int GetSellExp(Rarity rarity)
    {
        int index = Mathf.Clamp((int)rarity, 0, SELL_EXP_PER_RARITY.Length - 1);
        return SELL_EXP_PER_RARITY[index];
    }

    // hez gốc: bán món → cộng EXP (có boost, đầy thanh thì phần dư bị bỏ).
    public static void Sell(LootResult item)
    {
        PlayerExpService.Grant(GetSellExp(item.rarity), accumulateDisplay: true);
    }

    // Tỉ lệ (%) ra từng rarity ở level hiện tại (dòng bảng forge = playerLevel - 1, như LootService).
    public static List<float> GetRarityProbabilities()
    {
        return GameData.staticData.forge.GetProbabilities(PlayerLevel - 1);
    }
}

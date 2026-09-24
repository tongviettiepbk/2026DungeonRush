using System.Collections.Generic;

// Logic hệ Companion: summon (Bone/Ads), nâng cấp, trang bị nhanh. Stateless — thao tác trên
// GameData.staticData.companions + GameData.userData.companions (pattern MasteryService).
public static class CompanionService
{
    private static StaticCompanionData Static => GameData.staticData.companions;
    private static UserCompanionData User => GameData.userData.companions;
    private static CompanionSummonConfig SummonConfig => Static.summonConfig;

    // ----- Số lượt mỗi nút (đã cộng Summon Capacity từ Mastery) -----

    public static int GetAdSummonCount()
    {
        // TODO: AdBonus (save CompanionAdSummonBonusCount gốc) chưa có → 0.
        return SummonConfig.GetAdSummonCount(SummonConfig.GetCurrentSummonCapacity(), 0);
    }

    public static int GetBoneSmallSummonCount()
    {
        return SummonConfig.GetBoneSmallSummonCount(SummonConfig.GetCurrentSummonCapacity());
    }

    public static int GetBoneBigSummonCount()
    {
        return SummonConfig.GetBoneBigSummonCount(SummonConfig.GetCurrentSummonCapacity(), 1);
    }

    public static int GetBoneBigCost()
    {
        return SummonConfig.GetBoneBigCost(1);
    }

    // ----- Summon -----

    // Executor summon (mirror UserController.edi): mỗi lượt roll rarity theo summon level HIỆN TẠI →
    // random 1 con trong rarity → chưa có thì sở hữu (0 thẻ, isNew), có rồi thì +1 thẻ → totalSummons++.
    // Trả về danh sách con ra được theo thứ tự.
    public static List<CompanionData> Summon(int count)
    {
        List<CompanionData> results = new List<CompanionData>();
        for (int i = 0; i < count; i++)
        {
            Rarity rarity = CompanionSummonLevelConfig.RollRarity(User.totalSummons);
            CompanionData data = Static.GetRandom(rarity);
            if (data == null)
            {
                DebugCustom.Log("[CompanionService] Không có companion rarity=" + rarity);
                continue;
            }

            if (User.IsOwned(data.assetName))
            {
                User.AddCards(data.assetName, 1);
            }
            else
            {
                User.Own(data.assetName);
            }

            User.totalSummons++;
            results.Add(data);
        }

        User.isDataChanged = true;
        GameData.Save();
        return results;
    }

    // Summon bằng Bone: trừ cost rồi summon `count` lượt. null nếu không đủ Bone.
    public static List<CompanionData> TrySummonByBone(int cost, int count)
    {
        if (GameData.userData.items.Consume(ItemType.BONE, cost) == false)
        {
            return null;
        }

        return Summon(count);
    }

    // ----- Nâng cấp -----

    public static bool TryUpgrade(string id)
    {
        if (User.Upgrade(id) == false)
        {
            return false;
        }

        GameData.Save();
        return true;
    }

    public static bool CanUpgradeAny()
    {
        for (int i = 0; i < User.owned.Count; i++)
        {
            if (User.CanUpgrade(User.owned[i].companionId))
            {
                return true;
            }
        }
        return false;
    }

    // Nâng mọi con đang có tới khi hết thẻ. Trả về tổng số cấp đã lên.
    public static int UpgradeAll()
    {
        int levelsGained = 0;
        for (int i = 0; i < User.owned.Count; i++)
        {
            string id = User.owned[i].companionId;
            while (User.Upgrade(id))
            {
                levelsGained++;
            }
        }

        if (levelsGained > 0)
        {
            GameData.Save();
        }
        return levelsGained;
    }

    // ----- Trang bị -----

    // Trang bị nhanh 3 con tốt nhất: rarity giảm dần, cùng rarity thì level giảm dần (comparer gốc UserController.ep.dnc).
    public static void QuickEquip()
    {
        List<CompanionModel> sorted = new List<CompanionModel>(User.owned);
        sorted.Sort((a, b) =>
        {
            int ra = GetRarityValue(a.companionId);
            int rb = GetRarityValue(b.companionId);
            if (ra != rb)
            {
                return rb.CompareTo(ra);
            }
            return b.level.CompareTo(a.level);
        });

        List<string> equipped = User.GetEquipped();
        for (int i = equipped.Count - 1; i >= 0; i--)
        {
            User.Unequip(equipped[i]);
        }

        for (int i = 0; i < sorted.Count && i < UserCompanionData.MAX_EQUIPPED; i++)
        {
            User.Equip(sorted[i].companionId);
        }

        GameData.Save();
    }

    private static int GetRarityValue(string id)
    {
        CompanionData data = Static.GetData(id);
        return data != null ? (int)data.rarity : -1;
    }
}

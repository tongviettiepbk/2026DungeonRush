using System.Collections.Generic;

// Logic hệ Companion: summon (Bone/Ads), nâng cấp, trang bị nhanh. Stateless — thao tác trên
// GameData.staticData.companions + GameData.userData.companions (pattern MasteryService).
public static class CompanionService
{
    private static StaticCompanionData Static => GameData.staticData.companions;
    private static UserCompanionData User => GameData.userData.companions;
    private static CompanionSummonConfig SummonConfig => Static.summonConfig;

    // ----- Số lượt mỗi nút (đã cộng Summon Capacity từ Mastery) -----

    // eby gốc.
    public static int GetAdSummonCount()
    {
        return SummonConfig.GetAdSummonCount(SummonConfig.GetCurrentSummonCapacity(), User.adSummonBonusCount);
    }

    // ebz gốc: trần số con mỗi lượt ads = round(cap) + 35.
    public static int GetAdSummonMaxCount()
    {
        return SummonConfig.GetCurrentSummonCapacity() + CompanionSummonConfig.AD_CAP_OVER_CAPACITY;
    }

    // ebu gốc: số lượt ads đã dùng hôm nay (sang ngày mới UTC thì về 0).
    public static int GetAdSummonUsedToday()
    {
        string today = GetUtcToday();
        if (User.adSummonLastResetDate != today)
        {
            User.adSummonDailyCount = 0;
            User.adSummonLastResetDate = today;
            User.isDataChanged = true;
            GameData.Save();
        }
        return User.adSummonDailyCount;
    }

    public static int GetAdSummonRemainingToday()
    {
        return CompanionSummonConfig.AD_DAILY_LIMIT - GetAdSummonUsedToday();
    }

    // gqe gốc: thời gian tới 0h UTC hôm sau.
    public static System.TimeSpan GetTimeToAdSummonReset()
    {
        System.DateTime now = GameUtils.GetTimeNow().ToUniversalTime();
        return now.Date.AddDays(1) - now;
    }

    // gqh gốc: số con theo bonus hiện tại → +1 lượt trong ngày (ebv) → +1 bonus (ebx) → summon.
    public static List<CompanionSummonResult> SummonByAd()
    {
        int count = GetAdSummonCount();
        GetAdSummonUsedToday();
        User.adSummonDailyCount++;
        User.adSummonBonusCount++;
        User.isDataChanged = true;
        return Summon(count);
    }

    private static string GetUtcToday()
    {
        return GameUtils.GetTimeNow().ToUniversalTime().ToString("yyyy-MM-dd");
    }

    public static int GetBoneSmallSummonCount()
    {
        return SummonConfig.GetBoneSmallSummonCount(SummonConfig.GetCurrentSummonCapacity());
    }

    // gsf / gse gốc: theo hệ số đang chọn ở nút đổi multiplier.
    public static int GetBoneBigSummonCount(int multiplier)
    {
        return SummonConfig.GetBoneBigSummonCount(SummonConfig.GetCurrentSummonCapacity(), multiplier);
    }

    public static int GetBoneBigCost(int multiplier)
    {
        return SummonConfig.GetBoneBigCost(multiplier);
    }

    // ----- Summon -----

    // Executor summon (mirror UserController.edi): mỗi lượt roll rarity theo summon level HIỆN TẠI →
    // random 1 con trong rarity → chưa có thì sở hữu (0 thẻ, isNew), có rồi thì +1 thẻ → totalSummons++.
    // Riêng lượt thứ 53 (totalSummons == 52 trước khi roll) ép ra Uncommon — hằng số trong native edi.
    // Trả về danh sách con ra được theo thứ tự, kèm cờ lần đầu sở hữu (con mới ra 2 lần trong 1 lượt thì chỉ lần đầu là mới).
    public static List<CompanionSummonResult> Summon(int count)
    {
        List<CompanionSummonResult> results = new List<CompanionSummonResult>();
        for (int i = 0; i < count; i++)
        {
            Rarity rarity = User.totalSummons == CompanionSummonLevelConfig.GUARANTEED_UNCOMMON_AT
                ? Rarity.Uncommon
                : CompanionSummonLevelConfig.RollRarity(User.totalSummons);
            CompanionData data = Static.GetRandom(rarity);
            if (data == null)
            {
                DebugCustom.Log("[CompanionService] Không có companion rarity=" + rarity);
                continue;
            }

            bool isNew = User.IsOwned(data.assetName) == false;
            if (isNew)
            {
                User.Own(data.assetName);
            }
            else
            {
                User.AddCards(data.assetName, 1);
            }

            User.totalSummons++;
            results.Add(new CompanionSummonResult(data, isNew));
            // Clan War: điểm "Summon {rarity} Companion" (ClanWarController.fzl gốc).
            ClanWarController.Instance.RecordSummonCompanion(data.rarity);
        }

        User.isDataChanged = true;
        GameData.Save();
        return results;
    }

    // Summon bằng Bone: trừ cost rồi summon `count` lượt. null nếu không đủ Bone.
    public static List<CompanionSummonResult> TrySummonByBone(int cost, int count)
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

    // ----- Own Effect (buff hero khi SỞ HỮU) -----

    // 1 con: gốc lu.got / lu.gou = OwnBase + OwnScaler × level.
    public static float GetOwnAttack(CompanionData data, int level)
    {
        return data.ownAttackBase + data.ownAttackScaler * level;
    }

    public static float GetOwnHealth(CompanionData data, int level)
    {
        return data.ownHealthBase + data.ownHealthScaler * level;
    }

    // Gốc lu.gov (gọi từ Soldier.ewc): cộng Own Effect của MỌI pet ĐÃ SỞ HỮU (equip hay không) thành
    // 2 modifier FLAT Attack/MaxHp → HeroUnit cộng ở pass flat, TRƯỚC khi áp % substat (Soldier.eyv).
    public static List<StatModifier> BuildOwnEffectModifiers()
    {
        if (GameData.userData == null || User == null)
        {
            return new List<StatModifier>();
        }
        return BuildOwnEffectModifiers(User.owned);
    }

    // Bản nhận danh sách pet sở hữu bất kỳ (ghost Boss Rush dựng từ snapshot người chơi khác).
    public static List<StatModifier> BuildOwnEffectModifiers(List<CompanionModel> owned)
    {
        List<StatModifier> result = new List<StatModifier>();
        GetOwnEffectTotals(owned, out double attack, out double health);

        if (attack > 0)
        {
            result.Add(new StatModifier(StatModifierSource.Companion, StatModifierType.Attack, attack, true));
        }
        if (health > 0)
        {
            result.Add(new StatModifier(StatModifierSource.Companion, StatModifierType.MaxHp, health, true));
        }
        return result;
    }

    // Tổng Own Effect (Attack, Health) của mọi pet sở hữu — lu.gov gốc (dùng chung cho chỉ số Hero và Power).
    public static void GetOwnEffectTotals(List<CompanionModel> owned, out double attack, out double health)
    {
        attack = 0;
        health = 0;
        if (GameData.staticData == null || Static == null || owned == null)
        {
            return;
        }

        for (int i = 0; i < owned.Count; i++)
        {
            CompanionModel model = owned[i];
            CompanionData data = model != null ? Static.GetData(model.companionId) : null;
            if (data == null)
            {
                continue;
            }

            attack += GetOwnAttack(data, model.level);
            health += GetOwnHealth(data, model.level);
        }
    }

    // ----- Trang bị -----

    // Trang bị nhanh 3 con tốt nhất: rarity giảm dần, cùng rarity thì level giảm dần (comparer gốc UserController.ep.dnc).
    public static void QuickEquip()
    {
        List<CompanionModel> sorted = GetQuickEquipOrder();

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

    // CompanionTabPage.gss gốc: bấm Quick Equip có đổi đội hình không (bộ 3 con tốt nhất ≠ bộ đang mặc).
    public static bool IsQuickEquipChanged()
    {
        List<CompanionModel> sorted = GetQuickEquipOrder();
        HashSet<string> best = new HashSet<string>();
        for (int i = 0; i < sorted.Count && i < UserCompanionData.MAX_EQUIPPED; i++)
        {
            best.Add(sorted[i].companionId);
        }
        return best.SetEquals(User.GetEquipped()) == false;
    }

    private static List<CompanionModel> GetQuickEquipOrder()
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
        return sorted;
    }

    private static int GetRarityValue(string id)
    {
        CompanionData data = Static.GetData(id);
        return data != null ? (int)data.rarity : -1;
    }
}

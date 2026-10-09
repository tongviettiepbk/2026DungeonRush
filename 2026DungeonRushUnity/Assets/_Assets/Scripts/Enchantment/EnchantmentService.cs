using System.Collections.Generic;

// Logic hệ Enchantment — mirror UserController.edj..edy gốc. Stateless, thao tác trên
// GameData.userData.enchantments (+ Vial ở userData.items). Mỗi hàm đổi dữ liệu tự Save; UI gọi xong
// thì post EventID.EnchantmentChanged để Hero tính lại chỉ số + lobby cập nhật nhãn "+tier".
public static class EnchantmentService
{
    private static UserEnchantmentData User => GameData.userData.enchantments;

    // ----- Mở khoá -----

    // ItemElementUI.icu gốc: nút enchantment ở ô trang bị chỉ hiện khi PlayerLevel >=
    // GameResources.CultistDungeonUnlockPlayerLevel (cùng mốc mở dungeon Cultist — nguồn Vial).
    public static bool IsUnlocked()
    {
        DungeonConfig cultist = GameData.staticData.dungeons.GetData(DungeonType.Cultist);
        int unlockLevel = cultist != null ? cultist.unlockPlayerLevel : 0;
        return GameData.userData.player.playerLevel >= unlockLevel;
    }

    // ----- Summon -----

    public static int GetVial()
    {
        return (int)GameData.userData.items.GetQuantityHave(ItemType.VIAL);
    }

    public static int GetSummonLevel()
    {
        return EnchantmentConfig.GetLevel(User.totalSummons);
    }

    // Giá Vial / số lượt ứng với multiplier (EnchantmentTabPage.hyl / hyk).
    public static int GetSummonCost(int multiplier)
    {
        return multiplier * EnchantmentConfig.VIAL_PER_MULTIPLIER;
    }

    public static int GetSummonCount(int multiplier)
    {
        return multiplier * EnchantmentConfig.SUMMON_PER_MULTIPLIER;
    }

    // UserController.edq: mỗi lượt roll tier theo summon level HIỆN TẠI → kho +1 → totalSummons++.
    // Không trừ Vial (nơi gọi trừ). Trả về danh sách tier ra được theo thứ tự.
    public static List<int> Summon(int count)
    {
        List<int> results = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int tier = EnchantmentConfig.RollTier(EnchantmentConfig.GetLevel(User.totalSummons));
            User.AddOwned(tier, 1);
            User.totalSummons++;
            results.Add(tier);
        }

        GameData.Save();
        return results;
    }

    // EnchantmentTabPage.hyw: trừ multiplier×100 Vial rồi summon multiplier×5 lượt. null nếu thiếu Vial.
    public static List<int> TrySummon(int multiplier)
    {
        if (GameData.userData.items.Consume(ItemType.VIAL, GetSummonCost(multiplier)) == false)
        {
            return null;
        }

        return Summon(GetSummonCount(multiplier));
    }

    // ----- Đeo / tháo -----

    // UserController.edo: đeo relic tier `tier` (lấy từ kho) vào slot; relic cũ ở slot trả về kho.
    // tier = 0 → chỉ tháo. Thiếu relic trong kho → không làm gì.
    public static bool Equip(GearSlotType slot, int tier)
    {
        if (tier >= 1 && User.GetOwned(tier) < 1)
        {
            return false;
        }

        int old = User.GetEquipped(slot);
        if (old >= 1)
        {
            User.AddOwned(old, 1);
        }
        if (tier >= 1)
        {
            User.AddOwned(tier, -1);
        }
        User.SetEquipped(slot, tier);

        GameData.Save();
        return true;
    }

    public static bool Unequip(GearSlotType slot)
    {
        return User.GetEquipped(slot) >= 1 && Equip(slot, 0);
    }

    // UserController.edt: tháo hết về kho → xếp slot theo priority (rarity món đang mặc + bonus Cape/Wing)
    // giảm dần, hoà thì theo SLOT_ORDER; slot chưa mặc đồ xếp sau cùng → lần lượt gán relic tier cao nhất.
    public static void QuickEquip()
    {
        GearSlotType[] order = EnchantmentConfig.SLOT_ORDER;
        for (int i = 0; i < order.Length; i++)
        {
            int old = User.GetEquipped(order[i]);
            if (old >= 1)
            {
                User.AddOwned(old, 1);
                User.SetEquipped(order[i], 0);
            }
        }

        List<int> withItem = new List<int>();   // index trong SLOT_ORDER
        List<int> empty = new List<int>();
        int[] priority = new int[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            EquippedItemData rec = GameData.userData.equipment.GetRecord(order[i]);
            if (rec != null && string.IsNullOrEmpty(rec.equipId) == false)
            {
                priority[i] = EnchantmentConfig.GetQuickEquipPriority(order[i], rec.rarity);
                withItem.Add(i);
            }
            else
            {
                empty.Add(i);
            }
        }
        // Comparer gốc (UserController.eq.dnd): priority giảm dần, hoà thì index tăng dần.
        withItem.Sort((a, b) => priority[a] != priority[b] ? priority[b].CompareTo(priority[a]) : a.CompareTo(b));
        withItem.AddRange(empty);

        int tier = EnchantmentConfig.MAX_TIER;
        for (int i = 0; i < withItem.Count; i++)
        {
            while (tier >= 1 && User.GetOwned(tier) < 1)
            {
                tier--;
            }
            if (tier < 1)
            {
                break;
            }

            User.AddOwned(tier, -1);
            User.SetEquipped(order[withItem[i]], tier);
        }

        GameData.Save();
    }

    // ----- Merge / Dismantle -----

    // UserController.edr: 3 relic tier t (1..10) trong kho → 1 relic tier t+1.
    public static bool CanMerge(int tier)
    {
        return tier >= 1 && tier < EnchantmentConfig.MAX_TIER && User.GetOwned(tier) >= EnchantmentConfig.MERGE_COUNT;
    }

    public static bool Merge(int tier)
    {
        if (CanMerge(tier) == false)
        {
            return false;
        }

        User.AddOwned(tier, -EnchantmentConfig.MERGE_COUNT);
        User.AddOwned(tier + 1, 1);
        GameData.Save();
        return true;
    }

    public static bool CanMergeAny()
    {
        for (int tier = 1; tier < EnchantmentConfig.MAX_TIER; tier++)
        {
            if (CanMerge(tier))
            {
                return true;
            }
        }
        return false;
    }

    // UserController.eds: gộp toàn kho từ tier thấp lên (dây chuyền: tier mới sinh ra gộp tiếp ở vòng sau).
    public static bool MergeAll()
    {
        bool merged = false;
        for (int tier = 1; tier < EnchantmentConfig.MAX_TIER; tier++)
        {
            int n = User.GetOwned(tier) / EnchantmentConfig.MERGE_COUNT;
            if (n < 1)
            {
                continue;
            }

            User.AddOwned(tier, -n * EnchantmentConfig.MERGE_COUNT);
            User.AddOwned(tier + 1, n);
            merged = true;
        }

        if (merged)
        {
            GameData.Save();
        }
        return merged;
    }

    // UserController.edx: relic đang đeo (tier 1..10) + 2 relic cùng tier trong kho → slot lên tier+1.
    public static bool CanMergeEquipped(GearSlotType slot)
    {
        int tier = User.GetEquipped(slot);
        return tier >= 1 && tier < EnchantmentConfig.MAX_TIER && User.GetOwned(tier) >= EnchantmentConfig.MERGE_COUNT - 1;
    }

    public static bool MergeEquipped(GearSlotType slot)
    {
        if (CanMergeEquipped(slot) == false)
        {
            return false;
        }

        int tier = User.GetEquipped(slot);
        User.AddOwned(tier, -(EnchantmentConfig.MERGE_COUNT - 1));
        User.SetEquipped(slot, tier + 1);
        GameData.Save();
        return true;
    }

    // UserController.edw: 1 relic tier t (2..11) trong kho → 3 relic tier t-1.
    public static bool CanDismantle(int tier)
    {
        return tier >= 2 && tier <= EnchantmentConfig.MAX_TIER && User.GetOwned(tier) >= 1;
    }

    public static bool Dismantle(int tier)
    {
        if (CanDismantle(tier) == false)
        {
            return false;
        }

        User.AddOwned(tier, -1);
        User.AddOwned(tier - 1, EnchantmentConfig.MERGE_COUNT);
        GameData.Save();
        return true;
    }

    // UserController.edy: relic đang đeo tier t (>=2) → slot giữ tier t-1, kho +2 relic tier t-1.
    public static bool DismantleEquipped(GearSlotType slot)
    {
        int tier = User.GetEquipped(slot);
        if (tier < 2)
        {
            return false;
        }

        User.SetEquipped(slot, tier - 1);
        User.AddOwned(tier - 1, EnchantmentConfig.MERGE_COUNT - 1);
        GameData.Save();
        return true;
    }

    // ----- Chỉ số -----

    // Hệ số nhân main stat của món ở slot (Soldier.eyo). enchantments = null → 1 (ghost chưa có snapshot relic).
    public static float GetStatMultiplier(UserEnchantmentData enchantments, GearSlotType slot)
    {
        return enchantments != null ? EnchantmentConfig.GetStatMultiplier(enchantments.GetEquipped(slot)) : 1f;
    }
}

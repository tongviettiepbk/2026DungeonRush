using System.Collections.Generic;

// Save Enchantment người chơi. Schema gốc (il2cpp v41, class User):
//   OwnedEnchantmentCounts : List<int>  — index = tier (0 bỏ trống), giá trị = số relic tier đó trong KHO
//   EquippedEnchantmentTiers: List<int> — index = ItemType slot, giá trị = tier đang đeo (0 = trống)
//   TotalEnchantmentSummons: int        — quyết định Summon Level
// Relic đang đeo KHÔNG nằm trong kho (edo: đeo thì kho -1, tháo thì kho +1).
// Project: slot khoá = (int)GearSlotType dạng string (giống UserEquipmentData).
// Vial (VialCurrency gốc) nằm ở UserItemData — ItemType.VIAL.
public class UserEnchantmentData : BaseUserData
{
    public List<int> owned { get; set; } = new List<int>();
    public Dictionary<string, int> equipped { get; set; } = new Dictionary<string, int>();
    public int totalSummons { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_ENCHANTMENT;
    }

    public override void InitData()
    {
        base.InitData();
        owned = new List<int>();
        equipped = new Dictionary<string, int>();
        totalSummons = 0;
        ValidateData();
        isDataChanged = true;
    }

    // Đảm bảo kho đủ MAX_TIER+1 ô, số lượng không âm, tier đeo nằm trong 0..MAX_TIER.
    public override void ValidateData()
    {
        if (owned == null)
        {
            owned = new List<int>();
            isDataChanged = true;
        }
        if (equipped == null)
        {
            equipped = new Dictionary<string, int>();
            isDataChanged = true;
        }

        while (owned.Count < EnchantmentConfig.MAX_TIER + 1)
        {
            owned.Add(0);
            isDataChanged = true;
        }
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i] < 0)
            {
                owned[i] = 0;
                isDataChanged = true;
            }
        }

        List<string> keys = new List<string>(equipped.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int tier = equipped[keys[i]];
            if (tier <= 0 || tier > EnchantmentConfig.MAX_TIER)
            {
                equipped.Remove(keys[i]);
                isDataChanged = true;
            }
        }

        if (totalSummons < 0)
        {
            totalSummons = 0;
            isDataChanged = true;
        }
    }

    // ===== Kho =====

    public int GetOwned(int tier)
    {
        return tier >= 1 && tier < owned.Count ? owned[tier] : 0;
    }

    public void AddOwned(int tier, int count)
    {
        if (tier < 1 || tier >= owned.Count)
        {
            return;
        }
        owned[tier] += count;
        isDataChanged = true;
    }

    // ===== Đeo =====

    // Tier đang đeo ở slot (0 = trống).
    public int GetEquipped(GearSlotType slot)
    {
        return equipped.TryGetValue(Key(slot), out int tier) ? tier : 0;
    }

    public void SetEquipped(GearSlotType slot, int tier)
    {
        if (tier <= 0)
        {
            equipped.Remove(Key(slot));
        }
        else
        {
            equipped[Key(slot)] = tier;
        }
        isDataChanged = true;
    }

    private static string Key(GearSlotType slot)
    {
        return ((int)slot).ToString();
    }
}

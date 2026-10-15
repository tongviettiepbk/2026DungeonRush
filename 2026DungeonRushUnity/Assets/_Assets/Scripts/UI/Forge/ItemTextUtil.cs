using System.Collections.Generic;
using System.Text;

// Chữ hiển thị 1 món đồ cho NewItemPopup / ItemInfoPopup (NewItemPopup.ihx/ihz/iia gốc).
// Tiếng Anh như bản gốc: font của prefab gốc không có dấu tiếng Việt.
public static class ItemTextUtil
{
    // Tên tô màu rarity; vũ khí thêm " [Melee|Range]".
    public static string Name(LootResult item)
    {
        string name = item.displayName;
        if (item.kind == LootItemKind.Weapon)
            name += " [" + (item.weaponType == WeaponType.Melee ? "Melee" : "Range") + "]";
        return ShopUIUtil.Colored(name, UITabWing.GetRarityColor(item.rarity));
    }

    // Tên rarity tô màu rarity (NewItemPopup.ihz gốc).
    public static string RarityLine(Rarity rarity)
    {
        return ShopUIUtil.ColoredRarity(rarity);
    }

    // "Damage 163" / "Health 3.4B"; multiplier = hệ số relic (1 = không có).
    public static string MainStat(LootResult item, float multiplier = 1f)
    {
        string label = item.mainStatKind == GearMainStatKind.Health ? "Health" : "Damage";
        return label + " " + (item.mainStat * multiplier).ToLetter();
    }

    // Mỗi substat 1 dòng "+Tên x%"; không có thì chuỗi rỗng.
    public static string SubStats(List<GearSubStat> subStats)
    {
        if (subStats == null || subStats.Count == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < subStats.Count; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append('+').Append(SubStatName(subStats[i].type)).Append(' ').Append(subStats[i].value.ToString("0.##")).Append('%');
        }
        return sb.ToString();
    }

    // Tên substat theo nhãn gốc (FilterList trong AutoForgePopup).
    public static string SubStatName(SubStatType type)
    {
        switch (type)
        {
            case SubStatType.AttackSpeed: return "Attack Speed";
            case SubStatType.BlockChance: return "Block Chance";
            case SubStatType.CriticalChance: return "Critical Chance";
            case SubStatType.CriticalDamage: return "Critical Damage";
            case SubStatType.Damage: return "Damage";
            case SubStatType.DoubleHitChance: return "Double Hit Chance";
            case SubStatType.Health: return "Health";
            case SubStatType.HealthRegen: return "Health Regeneration";
            case SubStatType.Lifesteal: return "Life Steal";
            case SubStatType.MeleeDamage: return "Melee Damage";
            case SubStatType.RangedDamage: return "Ranged Damage";
            case SubStatType.CompanionCooldown: return "Companion Cooldown";
            case SubStatType.CompanionDamage: return "Companion Damage";
            default: return type.ToString();
        }
    }
}

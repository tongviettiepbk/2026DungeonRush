using System.Collections.Generic;
using System.Text;

// Chữ hiển thị 1 món đồ cho NewItemPopup / ItemInfoPopup (NewItemPopup.ihx/ihz/iia gốc).
// Tiếng Anh như bản gốc: font của prefab gốc không có dấu tiếng Việt.
public static class ItemTextUtil
{
    // Tên tô màu rarity; vũ khí thêm " [Melee|Range]".
    public static string Name(LootResult item)
    {
        string name = LocalizedName(item);
        if (item.kind == LootItemKind.Weapon)
            name += " [" + (item.weaponType == WeaponType.Melee ? Loc.L("Items.WeaponType.Melee") : Loc.L("Items.WeaponType.Range")) + "]";
        return ShopUIUtil.Colored(name, UITabWing.GetRarityColor(item.rarity));
    }

    // Tên món theo bảng gốc: gear → GearItemData.localizationKey, vũ khí → Items.Weapon.Name.{itemId}.
    public static string LocalizedName(LootResult item)
    {
        string key = null;
        if (GameData.staticData != null && string.IsNullOrEmpty(item.equipId) == false)
        {
            if (item.kind == LootItemKind.Weapon)
            {
                WeaponData weapon = GameData.staticData.weapons != null ? GameData.staticData.weapons.GetData(item.equipId) : null;
                if (weapon != null) key = "Items.Weapon.Name." + weapon.itemId;
            }
            else
            {
                GearItemData gear = GameData.staticData.gears != null ? GameData.staticData.gears.GetData(item.equipId) : null;
                if (gear != null) key = gear.localizationKey;
            }
        }
        return string.IsNullOrEmpty(key) ? item.displayName : Loc.Or(key, item.displayName);
    }

    // Tên rarity tô màu rarity (NewItemPopup.ihz gốc).
    public static string RarityLine(Rarity rarity)
    {
        return ShopUIUtil.ColoredRarity(rarity);
    }

    // "Damage 163" / "Health 3.4B"; multiplier = hệ số relic (1 = không có).
    public static string MainStat(LootResult item, float multiplier = 1f)
    {
        string label = item.mainStatKind == GearMainStatKind.Health ? Loc.L("Items.SubStat.Health") : Loc.L("Items.SubStat.Damage");
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

    // Tên substat theo bảng gốc (LocalizationController.ddm: Items.SubStat.<tên>).
    public static string SubStatName(SubStatType type)
    {
        switch (type)
        {
            case SubStatType.AttackSpeed: return Loc.L("Items.SubStat.AttackSpeed");
            case SubStatType.BlockChance: return Loc.L("Items.SubStat.BlockChance");
            case SubStatType.CriticalChance: return Loc.L("Items.SubStat.CriticalChance");
            case SubStatType.CriticalDamage: return Loc.L("Items.SubStat.CriticalDamage");
            case SubStatType.Damage: return Loc.L("Items.SubStat.Damage");
            case SubStatType.DoubleHitChance: return Loc.L("Items.SubStat.DoubleChance");
            case SubStatType.Health: return Loc.L("Items.SubStat.Health");
            case SubStatType.HealthRegen: return Loc.L("Items.SubStat.HealthRegen");
            case SubStatType.Lifesteal: return Loc.L("Items.SubStat.Lifesteal");
            case SubStatType.MeleeDamage: return Loc.L("Items.SubStat.MeleeDamage");
            case SubStatType.RangedDamage: return Loc.L("Items.SubStat.RangedDamage");
            case SubStatType.CompanionCooldown: return Loc.L("Items.SubStat.CompanionCooldown");
            case SubStatType.CompanionDamage: return Loc.L("Items.SubStat.CompanionDamage");
            default: return type.ToString();
        }
    }
}

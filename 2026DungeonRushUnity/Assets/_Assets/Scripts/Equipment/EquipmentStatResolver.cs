using System.Collections.Generic;
using UnityEngine;

// CẦU NỐI đồ đang mặc (save) → danh sách StatModifier để Hero nạp vào stat (luồng StickIdle:
// LoadPermanentModifiers → CalculateCurrentStats). Dựng 2 loại modifier:
//   1. CHỈ SỐ CHÍNH (main): flat (isFlatValue=true). Deterministic từ (slot/weaponType, rarity, level)
//      qua GearStatCalculator. weapon→Attack; gear Damage-kind→Attack / Health-kind→MaxHp.
//      Relic Enchantment đeo ở slot nhân main stat của món đó × (1 + tier²/100) (Soldier.ewc → eyo gốc).
//   2. SUBSTAT (+X% đã roll, lưu trong save): percent (isFlatValue=false), value = phần trăm/100.
//      Mỗi SubStatType map sang StatModifierType tương ứng (xem MapSubStat). MeleeDamage/RangedDamage
//      chỉ tính khi vũ khí ĐANG CẦM đúng kiểu (điều kiện toàn cục). Đủ 13 loại substat gốc đều đã map.
//
// LƯU Ý mô hình áp: main của món THAY nền PlayerBase của slot (Soldier.ewc gốc) → modifier flat = main − nền slot;
// substat = % áp lên tổng (do HeroUnit gom
// theo đích rồi nhân/cộng). Công thức TỔNG HỢP substat gốc chưa reverse → dùng mô hình % chuẩn genre.
public static class EquipmentStatResolver
{
    // Bảng công thức chỉ số, cache 1 lần từ Resources (asset Gears/GearStatConfig).
    private static GearStatConfigData gearStatConfig;

    // 5 slot gear có base main stat trong config (WEAPON, WING, CAPE xử lý riêng).
    private static readonly GearSlotType[] GearSlots =
    {
        GearSlotType.HELMET,
        GearSlotType.GLOVES,
        GearSlotType.RING,
        GearSlotType.NECKLACE,
        GearSlotType.BACKPACK,
    };

    // Dựng list modifier (main flat + substat %) từ đồ đang mặc. Slot trống góp 0 (bỏ qua).
    // enchantments = relic đang đeo (null → không nhân, VD ghost Boss Rush chưa có snapshot relic).
    public static List<StatModifier> BuildModifiers(UserEquipmentData equipment, UserEnchantmentData enchantments = null)
    {
        List<StatModifier> result = new List<StatModifier>();
        if (equipment == null)
        {
            return result;
        }

        GearStatConfigData config = LoadConfig();
        if (config == null)
        {
            return result;
        }

        // Kiểu vũ khí đang cầm — quyết định substat MeleeDamage/RangedDamage của MỌI món có tính hay không.
        WeaponType? weaponType = GetEquippedWeaponType(equipment);

        AddWeapon(result, equipment, enchantments, config, weaponType);
        for (int i = 0; i < GearSlots.Length; i++)
        {
            AddGear(result, equipment, enchantments, config, GearSlots[i], weaponType);
        }
        AddWing(result, equipment, enchantments, weaponType);
        AddCape(result, equipment, enchantments, weaponType);

        return result;
    }

    // Vũ khí (C1): main luôn là Sát thương → Attack (flat) + substat của vũ khí.
    private static void AddWeapon(List<StatModifier> result, UserEquipmentData equipment, UserEnchantmentData enchantments, GearStatConfigData config, WeaponType? weaponType)
    {
        EquippedItemData rec = equipment.GetRecord(GearSlotType.WEAPON);
        if (rec == null || string.IsNullOrEmpty(rec.equipId) || GameData.staticData == null || GameData.staticData.weapons == null)
        {
            return;
        }

        WeaponData weapon = GameData.staticData.weapons.GetData(rec.equipId);
        if (weapon == null)
        {
            return;
        }

        double mainStat = GearStatCalculator.GetWeaponMainStat(config, weapon.weaponType, rec.rarity, rec.level)
                          * EnchantmentService.GetStatMultiplier(enchantments, GearSlotType.WEAPON)
                          - config.GetPlayerBaseMain(GearSlotType.WEAPON);   // đồ thay nền slot
        result.AddOne(new StatModifier(StatModifierSource.Weapon, StatModifierType.Attack, mainStat, isFlatValue: true));

        AddSubStats(result, rec.subStats, StatModifierSource.Weapon, weaponType);
    }

    // Gear (C2..C6): main là Máu hay Sát thương tùy slot → MaxHp/Attack (flat) + substat của gear.
    private static void AddGear(List<StatModifier> result, UserEquipmentData equipment, UserEnchantmentData enchantments, GearStatConfigData config, GearSlotType slot, WeaponType? weaponType)
    {
        EquippedItemData rec = equipment.GetRecord(slot);
        if (rec == null || string.IsNullOrEmpty(rec.equipId) || GameData.staticData == null || GameData.staticData.gears == null)
        {
            return;
        }

        GearItemData gear = GameData.staticData.gears.GetData(rec.equipId);
        if (gear == null)
        {
            return;
        }

        GearMainStatKind kind;
        double mainStat = GearStatCalculator.GetGearMainStat(config, gear.slot, rec.rarity, rec.level, out kind)
                          * EnchantmentService.GetStatMultiplier(enchantments, slot)
                          - config.GetPlayerBaseMain(slot);   // đồ thay nền slot
        StatModifierType mainType = kind == GearMainStatKind.Health ? StatModifierType.MaxHp : StatModifierType.Attack;
        result.AddOne(new StatModifier(SourceOf(slot), mainType, mainStat, isFlatValue: true));

        AddSubStats(result, rec.subStats, SourceOf(slot), weaponType);
    }

    // Wing (C8) — Soldier.ewc gốc: Sát thương/Máu wing × hệ số relic slot Wing cộng thẳng vào tổng phẳng
    // (wing không thay nền slot nào) + substat wing vào nhóm %.
    private static void AddWing(List<StatModifier> result, UserEquipmentData equipment, UserEnchantmentData enchantments, WeaponType? weaponType)
    {
        EquippedItemData rec = equipment.GetRecord(GearSlotType.WING);
        if (rec == null || int.TryParse(rec.equipId, out int wingId) == false || GameData.staticData == null || GameData.staticData.wings == null)
        {
            return;
        }

        WingData wing = GameData.staticData.wings.GetData(wingId);
        if (wing == null)
        {
            return;
        }

        float multiplier = EnchantmentService.GetStatMultiplier(enchantments, GearSlotType.WING);
        result.AddOne(new StatModifier(StatModifierSource.Wing, StatModifierType.Attack, multiplier * GearStatCalculator.GetWingDamage(wing, rec.level), isFlatValue: true));
        result.AddOne(new StatModifier(StatModifierSource.Wing, StatModifierType.MaxHp, multiplier * GearStatCalculator.GetWingHealth(wing, rec.level), isFlatValue: true));

        AddSubStats(result, rec.subStats, StatModifierSource.Wing, weaponType);
    }

    // Cape (C7) — Soldier.eyv gốc: Cape cho % Sát thương / % Máu × hệ số relic slot Cape, cộng chung nhóm %
    // với substat (HeroUnit gom rồi nhân 1 + tổng) + substat cape.
    private static void AddCape(List<StatModifier> result, UserEquipmentData equipment, UserEnchantmentData enchantments, WeaponType? weaponType)
    {
        EquippedItemData rec = equipment.GetRecord(GearSlotType.CAPE);
        if (rec == null || int.TryParse(rec.equipId, out int capeId) == false || GameData.staticData == null || GameData.staticData.capes == null)
        {
            return;
        }

        CapeData cape = GameData.staticData.capes.GetData(capeId);
        if (cape == null)
        {
            return;
        }

        float multiplier = EnchantmentService.GetStatMultiplier(enchantments, GearSlotType.CAPE);
        result.AddOne(new StatModifier(StatModifierSource.Cape, StatModifierType.Attack, multiplier * GearStatCalculator.GetCapeDamagePercent(cape, rec.level) / 100.0, isFlatValue: false));
        result.AddOne(new StatModifier(StatModifierSource.Cape, StatModifierType.MaxHp, multiplier * GearStatCalculator.GetCapeHealthPercent(cape, rec.level) / 100.0, isFlatValue: false));

        AddSubStats(result, rec.subStats, StatModifierSource.Cape, weaponType);
    }

    // Chuyển các dòng substat (đã roll, lưu save) thành StatModifier percent.
    private static void AddSubStats(List<StatModifier> result, List<GearSubStat> subStats, StatModifierSource source, WeaponType? weaponType)
    {
        if (subStats == null)
        {
            return;
        }

        for (int i = 0; i < subStats.Count; i++)
        {
            GearSubStat sub = subStats[i];
            if (sub == null)
            {
                continue;
            }

            if (MapSubStat(sub, weaponType, out StatModifierType type, out double value))
            {
                result.AddOne(new StatModifier(source, type, value, isFlatValue: false));
            }
        }
    }

    // Map 1 dòng substat → (StatModifierType, value đã chuẩn hoá về phân số). Trả false nếu loại này
    // chưa áp được (không đúng kiểu vũ khí, hoặc chưa có field Stats tương ứng).
    private static bool MapSubStat(GearSubStat sub, WeaponType? weaponType, out StatModifierType type, out double value)
    {
        value = sub.value / 100.0;   // substat lưu dạng phần trăm (VD 12 = 12%) → phân số 0.12.

        switch (sub.type)
        {
            case SubStatType.AttackSpeed: type = StatModifierType.AttackSpeed; return true;
            case SubStatType.Damage: type = StatModifierType.Attack; return true;
            case SubStatType.Health: type = StatModifierType.MaxHp; return true;
            case SubStatType.CriticalChance: type = StatModifierType.CritRate; return true;
            case SubStatType.CriticalDamage: type = StatModifierType.CritDamage; return true;
            case SubStatType.DoubleHitChance: type = StatModifierType.DoubleShot; return true;
            case SubStatType.HealthRegen: type = StatModifierType.HpRecovery; return true;
            case SubStatType.CompanionDamage: type = StatModifierType.CompanionDamage; return true;
            case SubStatType.CompanionCooldown: type = StatModifierType.CompanionCooldownReduction; return true;
            case SubStatType.BlockChance: type = StatModifierType.BlockChance; return true;
            case SubStatType.Lifesteal: type = StatModifierType.Lifesteal; return true;

            // MeleeDamage/RangedDamage: chỉ cộng vào Sát thương khi vũ khí đang cầm đúng kiểu.
            case SubStatType.MeleeDamage:
                type = StatModifierType.Attack;
                return weaponType == WeaponType.Melee;
            case SubStatType.RangedDamage:
                type = StatModifierType.Attack;
                return weaponType == WeaponType.Range;

            default:
                type = StatModifierType.None;
                return false;
        }
    }

    // Kiểu vũ khí đang cầm (null nếu chưa mặc / không tra được) — cho điều kiện Melee/Ranged substat.
    private static WeaponType? GetEquippedWeaponType(UserEquipmentData equipment)
    {
        string id = equipment.GetEquipped(GearSlotType.WEAPON);
        if (string.IsNullOrEmpty(id) || GameData.staticData == null || GameData.staticData.weapons == null)
        {
            return null;
        }

        WeaponData weapon = GameData.staticData.weapons.GetData(id);
        return weapon != null ? weapon.weaponType : (WeaponType?)null;
    }

    // Nguồn modifier theo slot (hiện chỉ để đọc log/debug — công thức không phụ thuộc source).
    private static StatModifierSource SourceOf(GearSlotType slot)
    {
        switch (slot)
        {
            case GearSlotType.WEAPON: return StatModifierSource.Weapon;
            case GearSlotType.RING: return StatModifierSource.Ring;
            default: return StatModifierSource.Armor;
        }
    }

    public static GearStatConfigData LoadConfig()
    {
        if (gearStatConfig == null)
        {
            gearStatConfig = Resources.Load<GearStatConfigData>("Scriptable Objects/Gears/GearStatConfig");
        }

        if (gearStatConfig == null)
        {
            DebugCustom.LogError("[EquipmentStatResolver] Thiếu GearStatConfig — không dựng được chỉ số đồ.");
        }

        return gearStatConfig;
    }
}

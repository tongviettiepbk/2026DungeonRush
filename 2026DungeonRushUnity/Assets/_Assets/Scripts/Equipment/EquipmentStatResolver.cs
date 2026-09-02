using System.Collections.Generic;
using UnityEngine;

// CẦU NỐI đồ đang mặc (save) → danh sách StatModifier để Hero nạp vào stat (luồng StickIdle:
// LoadPermanentModifiers → CalculateCurrentStats). Dựng 2 loại modifier:
//   1. CHỈ SỐ CHÍNH (main): flat (isFlatValue=true). Deterministic từ (slot/weaponType, rarity, level)
//      qua GearStatCalculator. weapon→Attack; gear Damage-kind→Attack / Health-kind→MaxHp.
//   2. SUBSTAT (+X% đã roll, lưu trong save): percent (isFlatValue=false), value = phần trăm/100.
//      Mỗi SubStatType map sang StatModifierType tương ứng (xem MapSubStat). MeleeDamage/RangedDamage
//      chỉ tính khi vũ khí ĐANG CẦM đúng kiểu (điều kiện toàn cục). Loại chưa có field Stats
//      (Lifesteal/BlockChance/CompanionCooldown) tạm bỏ qua — data vẫn lưu, áp khi có hệ combat tương ứng.
//
// LƯU Ý mô hình áp: main = cộng dồn (PlayerBase + Σ flat); substat = % áp lên tổng (do HeroUnit gom
// theo đích rồi nhân/cộng). Công thức TỔNG HỢP substat gốc chưa reverse → dùng mô hình % chuẩn genre.
public static class EquipmentStatResolver
{
    // Bảng công thức chỉ số, cache 1 lần từ Resources (asset Gears/GearStatConfig).
    private static GearStatConfigData gearStatConfig;

    // 5 slot gear có base main stat trong config (WEAPON xử lý riêng; WING/CAPE chưa có base).
    private static readonly GearSlotType[] GearSlots =
    {
        GearSlotType.HELMET,
        GearSlotType.GLOVES,
        GearSlotType.RING,
        GearSlotType.NECKLACE,
        GearSlotType.BACKPACK,
    };

    // Dựng list modifier (main flat + substat %) từ đồ đang mặc. Slot trống góp 0 (bỏ qua).
    public static List<StatModifier> BuildModifiers(UserEquipmentData equipment)
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

        AddWeapon(result, equipment, config, weaponType);
        for (int i = 0; i < GearSlots.Length; i++)
        {
            AddGear(result, equipment, config, GearSlots[i], weaponType);
        }

        return result;
    }

    // Vũ khí (C1): main luôn là Sát thương → Attack (flat) + substat của vũ khí.
    private static void AddWeapon(List<StatModifier> result, UserEquipmentData equipment, GearStatConfigData config, WeaponType? weaponType)
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

        double mainStat = GearStatCalculator.GetWeaponMainStat(config, weapon.weaponType, rec.rarity, rec.level);
        result.AddOne(new StatModifier(StatModifierSource.Weapon, StatModifierType.Attack, mainStat, isFlatValue: true));

        AddSubStats(result, rec.subStats, StatModifierSource.Weapon, weaponType);
    }

    // Gear (C2..C6): main là Máu hay Sát thương tùy slot → MaxHp/Attack (flat) + substat của gear.
    private static void AddGear(List<StatModifier> result, UserEquipmentData equipment, GearStatConfigData config, GearSlotType slot, WeaponType? weaponType)
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
        double mainStat = GearStatCalculator.GetGearMainStat(config, gear.slot, rec.rarity, rec.level, out kind);
        StatModifierType mainType = kind == GearMainStatKind.Health ? StatModifierType.MaxHp : StatModifierType.Attack;
        result.AddOne(new StatModifier(SourceOf(slot), mainType, mainStat, isFlatValue: true));

        AddSubStats(result, rec.subStats, SourceOf(slot), weaponType);
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

            // MeleeDamage/RangedDamage: chỉ cộng vào Sát thương khi vũ khí đang cầm đúng kiểu.
            case SubStatType.MeleeDamage:
                type = StatModifierType.Attack;
                return weaponType == WeaponType.Melee;
            case SubStatType.RangedDamage:
                type = StatModifierType.Attack;
                return weaponType == WeaponType.Range;

            // Chưa có field Stats/combat: Lifesteal, BlockChance, CompanionCooldown → data vẫn lưu, chưa áp.
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

    private static GearStatConfigData LoadConfig()
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

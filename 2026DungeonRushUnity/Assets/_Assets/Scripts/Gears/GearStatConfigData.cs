using System.Collections.Generic;
using UnityEngine;

// BẢNG CÔNG THỨC CHỈ SỐ TRANG BỊ — 1 asset duy nhất cho toàn game.
// Toàn bộ số ở đây trích trực tiếp từ MonoBehaviour `GameResources` của game gốc (libil2cpp v41),
// đã verify công thức khớp ảnh in-game (Sorcerer Cape Mythic Lv1 = 14.444; Viking Horns Rare Lv99 = 1.118).
//
// Công thức chỉ số CHÍNH:  Main = Base(slot) × tierScaler^Rarity × (1 + levelScaler × Level)
//   tierScaler = √10 = 3.1622777 (ItemStatTierScaler),  levelScaler = 0.015 (ItemStatLevelScaler)
//
// Tạo asset: Create > DungOnRush > Gear Stat Config. Đặt trong Resources/Scriptable Objects/Gears
// để StaticGearStatData load được. Field đã có giá trị mặc định = số gốc, tạo asset mới là dùng ngay.
[CreateAssetMenu(fileName = "GearStatConfig", menuName = "DungOnRush/Gear Stat Config")]
public class GearStatConfigData : ScriptableObject
{
    [Header("Hằng số công thức chính")]
    public float tierScaler = 3.1622777f;   // (√10)^Rarity
    public float levelScaler = 0.015f;       // 1 + levelScaler * Level

    [Header("Base sát thương vũ khí (C1) theo kiểu tấn công")]
    public float weaponBaseMelee = 9f;       // ItemStatBaseMeleeWeapon
    public float weaponBaseRange = 7f;       // ItemStatBaseRangedWeapon

    [Header("Base 5 slot trang bị (C2..C6)")]
    public List<GearBaseStatEntry> gearBaseStats = new List<GearBaseStatEntry>
    {
        new GearBaseStatEntry(GearSlotType.HELMET,   45f, GearMainStatKind.Health), // ItemStatBaseHeadItem
        new GearBaseStatEntry(GearSlotType.BACKPACK, 30f, GearMainStatKind.Health), // ItemStatBaseBackItem
        new GearBaseStatEntry(GearSlotType.NECKLACE, 20f, GearMainStatKind.Health), // ItemStatBaseNecklaceHealth
        new GearBaseStatEntry(GearSlotType.GLOVES,   6f,  GearMainStatKind.Damage), // ItemStatBaseGlovesDamage
        new GearBaseStatEntry(GearSlotType.RING,     6f,  GearMainStatKind.Damage), // ItemStatBaseRingDamage
    };

    [Header("Pool substat: giá trị roll trong (0 .. maxValue], weight bằng nhau")]
    public float subStatDistributionSpread = 0.25f; // tham số phân bố gốc (SubStatDistributionSpread)
    public List<SubStatPoolEntry> subStatPool = new List<SubStatPoolEntry>
    {
        new SubStatPoolEntry(SubStatType.AttackSpeed,       40f, 100),
        new SubStatPoolEntry(SubStatType.BlockChance,       5f,  100),
        new SubStatPoolEntry(SubStatType.CriticalChance,    12f, 100),
        new SubStatPoolEntry(SubStatType.CriticalDamage,    100f,100),
        new SubStatPoolEntry(SubStatType.Damage,            15f, 100),
        new SubStatPoolEntry(SubStatType.DoubleHitChance,   40f, 100),
        new SubStatPoolEntry(SubStatType.Health,            15f, 100),
        new SubStatPoolEntry(SubStatType.HealthRegen,       6f,  100),
        new SubStatPoolEntry(SubStatType.Lifesteal,         20f, 100),
        new SubStatPoolEntry(SubStatType.MeleeDamage,       50f, 100),
        new SubStatPoolEntry(SubStatType.RangedDamage,      15f, 100),
        new SubStatPoolEntry(SubStatType.CompanionCooldown, 7f,  100),
        new SubStatPoolEntry(SubStatType.CompanionDamage,   30f, 100),
    };

    // Số dòng substat theo rarity — ngưỡng lũy tiến (SubStatTierSlots gốc):
    // < Rare = 0 dòng; Rare/Epic = 1; Legendary..Ancient(index 4..7) = 2; Immortal/Divine(8,9) = 3.
    [Header("Số substat theo rarity (ngưỡng minRarity lũy tiến)")]
    public List<SubStatCountThreshold> subStatCountThresholds = new List<SubStatCountThreshold>
    {
        new SubStatCountThreshold(Rarity.Rare,      1),
        new SubStatCountThreshold(Rarity.Legendary, 2),
        new SubStatCountThreshold(Rarity.Immortal,  3),
    };

    // ---- Lookup ----

    public GearBaseStatEntry GetGearBase(GearSlotType slot)
    {
        for (int i = 0; i < gearBaseStats.Count; i++)
        {
            if (gearBaseStats[i].slot == slot)
            {
                return gearBaseStats[i];
            }
        }

        DebugCustom.Log("[GearStatConfigData] Không có base cho slot=" + slot);
        return null;
    }

    public float GetSubStatMaxValue(SubStatType type)
    {
        for (int i = 0; i < subStatPool.Count; i++)
        {
            if (subStatPool[i].type == type)
            {
                return subStatPool[i].maxValue;
            }
        }

        return 0f;
    }

    // Trả về số dòng substat của rarity = count của ngưỡng cao nhất có minRarity <= rarity (0 nếu chưa đạt).
    public int GetSubStatCount(Rarity rarity)
    {
        int count = 0;
        for (int i = 0; i < subStatCountThresholds.Count; i++)
        {
            SubStatCountThreshold t = subStatCountThresholds[i];
            if ((int)rarity >= (int)t.minRarity && t.count > count)
            {
                count = t.count;
            }
        }

        return count;
    }
}

[System.Serializable]
public class GearBaseStatEntry
{
    public GearSlotType slot;
    public float baseValue;
    public GearMainStatKind mainStatKind;

    public GearBaseStatEntry() { }

    public GearBaseStatEntry(GearSlotType slot, float baseValue, GearMainStatKind mainStatKind)
    {
        this.slot = slot;
        this.baseValue = baseValue;
        this.mainStatKind = mainStatKind;
    }
}

[System.Serializable]
public class SubStatPoolEntry
{
    public SubStatType type;
    public float maxValue;   // giá trị roll tối đa (%).
    public int weight;       // trọng số random loại substat (gốc: đều = 100).

    public SubStatPoolEntry() { }

    public SubStatPoolEntry(SubStatType type, float maxValue, int weight)
    {
        this.type = type;
        this.maxValue = maxValue;
        this.weight = weight;
    }
}

[System.Serializable]
public class SubStatCountThreshold
{
    public Rarity minRarity;
    public int count;

    public SubStatCountThreshold() { }

    public SubStatCountThreshold(Rarity minRarity, int count)
    {
        this.minRarity = minRarity;
        this.count = count;
    }
}

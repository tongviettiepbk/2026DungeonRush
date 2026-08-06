using System.Collections.Generic;

// Phân loại item loot được: gear (C2..C6) hay vũ khí (C1).
public enum LootItemKind
{
    Gear = 0,
    Weapon = 1,
}

// DTO thuần chứa dữ liệu 1 item vừa loot ra. LootService tính sẵn mọi thứ vào đây,
// UI chỉ đọc để hiển thị (không truy vấn lại data/config). Không phụ thuộc UI.
public class LootResult
{
    public LootItemKind kind;
    public string displayName;
    public Rarity rarity;
    public int level;

    // Chỉ dùng khi kind == Gear.
    public GearSlotType gearSlot;

    // Chỉ dùng khi kind == Weapon.
    public WeaponType weaponType;

    // Chỉ số chính (vũ khí luôn là Damage).
    public GearMainStatKind mainStatKind;
    public double mainStat;

    // Chỉ số phụ đã roll sẵn.
    public List<GearSubStat> subStats;
}

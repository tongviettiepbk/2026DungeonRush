// Enum cho hệ CHỈ SỐ trang bị (giải mã từ libil2cpp gốc — class SubStatType, GameResources).
// Tách riêng khỏi GearEnums.cs (phân loại slot) để gom mọi thứ liên quan tới stat/công thức.

// 13 loại chỉ số phụ (dòng "+X%") một trang bị có thể roll ra.
// Giá trị = đúng thứ tự enum SubStatType trong game gốc (global-metadata v31), ĐÃ verify khớp
// ảnh in-game: 2=CriticalChance, 4=Damage, 3=CriticalDamage(73%). ĐỪNG đổi số.
public enum SubStatType
{
    AttackSpeed = 0,
    BlockChance = 1,
    CriticalChance = 2,
    CriticalDamage = 3,
    Damage = 4,
    DoubleHitChance = 5,
    Health = 6,
    HealthRegen = 7,
    Lifesteal = 8,
    MeleeDamage = 9,
    RangedDamage = 10,
    CompanionCooldown = 11,
    CompanionDamage = 12,
}

// Chỉ số CHÍNH (số to) của trang bị là Máu hay Sát thương — phụ thuộc slot.
// Helmet/Backpack/Necklace → Health; Weapon/Gloves/Ring → Damage.
public enum GearMainStatKind
{
    Health = 0,
    Damage = 1,
}

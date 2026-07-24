// Enum dùng chung toàn game. Thêm ItemType mới ở đây khi có tính năng mới.
public enum ItemType
{
    NONE = 0,
    GOLD = 1,
    GEM = 2,
    ENERGY = 3,
}

// 10 bậc rarity của DungeonRush (client). Dùng chung cho mọi loại gear (Vũ khí, Mũ, Găng...).
// Giá trị = bậc trong config gốc (0..9).
public enum Rarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Mythic = 5,
    Artifact = 6,
    Ancient = 7,
    Immortal = 8,
    Divine = 9,
}

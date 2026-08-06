// Enum dùng chung toàn game. Thêm ItemType mới ở đây khi có tính năng mới.
public enum ItemType
{
    NONE = 0,
    GOLD = 1,
    GEM = 2,
    ENERGY = 3,
}

// Loại môi trường map. Giá trị = EnvironmentType trong MapConfig gốc của DungeonRush.
public enum ModeType
{
    DefaultLevel = 0,        // MainMap — màn campaign thường
    BossRush = 1,
    DragonBossDungeon = 2,
    ZombieHordeDungeon = 3,
    PvP = 4,
    ChatBattle = 5,
    CultistDungeon = 6,
}

// Loại dungeon (theo DungeonData gốc). Giá trị = DungeonType.
public enum DungeonType
{
    DragonBoss = 0,
    ZombieHorde = 1,
    Cultist = 2,
}

// Trạng thái hiển thị trên CombatText (Effects). Port từ StickIdle.
public enum TextDamageStatus
{
    Miss,
    Block,
    Immune,
    Evade,
}

// 10 bậc rarity của DungeonRush (client). Dùng chung cho mọi loại gear (Vũ khí, Mũ, Găng...).
// Giá trị = bậc trong config gốc (0..10, enum CharacterRarity trong libil2cpp).
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
    Ultimate = 10,
}

//public enum TypeEquipment
//{
//    None =0,
//    Weapon=1,
//    Helmet=2,
//    Gloves=3,
//    Ring=4,
//    Necklace=5,
//    Backpack=6,
//    Cape=7,
//    Wings=8,
//}

// Hệ MẶC ĐỒ (equip) — trục thống nhất cho các slot NGƯỜI CHƠI mặc lên nhân vật.
// Khác GearSlotType (phân loại catalog C2..C8): ở đây gom cả Vũ khí (module riêng) và
// bỏ Ring/Necklace vì 2 slot đó KHÔNG hiển thị trên người. 6 slot dưới đây đúng bằng
// 6 bộ phận có hình trên rig Hero (Weapon/Helmet/Gloves/Backpack/Cape/Wing).
//
// Giá trị int cố định — dùng làm khoá lưu save (xem UserEquipmentData). ĐỪNG đổi số.

public enum EquipSlot
{
    Weapon = 0,   // Vũ khí (C1)
    Helmet = 1,   // Mũ (C2)
    Gloves = 2,   // Găng tay (C3)
    Backpack = 3, // Ba lô (C6)
    Cape = 4,     // Áo choàng (C7) — visual làm ở bước sau (Spine skin)
    Wing = 5,     // Cánh (C8)

    Ring = 6,
    Necklace = 7,
}
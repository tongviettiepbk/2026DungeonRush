// Enum dùng chung toàn game. Thêm ItemType mới ở đây khi có tính năng mới.
public enum ItemType
{
    NONE = 0,
    GOLD = 1,
    GEM = 2,
    ENERGY = 3,
    LOOT_TICKET = 4,
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

// GHI CHÚ: enum EquipSlot cũ đã GỘP vào GearSlotType (Scripts/Gears/GearEnums.cs) để chỉ
// còn 1 kiểu enum slot cho dễ nhớ. Hệ mặc đồ nay dùng GearSlotType.WEAPON/HELMET/...
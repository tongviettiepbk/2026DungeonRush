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

public enum TypeEquipment
{
    None =0,
    Weapon=1,
    Helmet=2,
    Gloves=3,
    Ring=4,
    Necklace=5,
    Backpack=6,
    Cape=7,
    Wings=8,
}
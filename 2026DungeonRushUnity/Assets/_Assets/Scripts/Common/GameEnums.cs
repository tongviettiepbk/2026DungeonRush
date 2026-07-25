// Enum dùng chung toàn game. Thêm ItemType mới ở đây khi có tính năng mới.
public enum ItemType
{
    NONE = 0,
    GOLD = 1,
    GEM = 2,
    ENERGY = 3,
}

// Loại môi trường map. Giá trị = EnvironmentType trong MapConfig gốc của DungeonRush.
public enum MapEnvironmentType
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

// Nội dung 1 ô lưới sau khi sinh map. Tương ứng "brush" của grid painter gốc.
public enum MapCellType
{
    Empty = 0,
    Obstacle = 1,       // box/chướng ngại
    PlayerSpawn = 2,    // ô spawn quân người chơi (hàng dưới)
    EnemySpawn = 3,     // ô spawn enemy (hàng trên)
    Door = 4,           // cửa thoát màn
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

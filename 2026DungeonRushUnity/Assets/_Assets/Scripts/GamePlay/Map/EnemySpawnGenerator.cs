using System.Collections.Generic;
using UnityEngine;

// Sinh enemy cho 1 màn campaign.
//
// LƯU Ý QUAN TRỌNG: DungeonRush KHÔNG có bảng "enemy level theo màn". Enemy dùng stat tuyệt đối
// (Health/AttackPower...) và scale theo tiến trình người chơi. Công thức scale gốc bị strip il2cpp,
// nên phần scaling dưới đây là PLACEHOLDER hợp lý — neo trên các hằng số thật đọc từ remote config
// (army_power_level_scaler, army_power_exponential_scaler). Thay bằng công thức thật khi có.
// Chi tiết: DecodedData/MAP_AND_SPAWN_MODEL.md — mục 5.
public static class EnemySpawnGenerator
{
    // Hằng số thật từ remote_config_live.json.
    private const float ARMY_POWER_LEVEL_SCALER = 20f;
    private const float ARMY_POWER_EXP_SCALER = 3.16227766f;   // ≈ √10
    private const int POWER_SEGMENT = 20;                       // army_power_segments: bậc nhỏ nhất

    // Số enemy cơ bản + tăng theo tiến trình (placeholder).
    private const int BASE_ENEMY_COUNT = 3;
    private const int ENEMY_COUNT_PER_CHAPTER = 1;
    private const int MAX_ENEMY_COUNT = 8;

    // Thông tin 1 enemy được sinh ra: ô đứng, level (để scale), và stat đã tính sẵn.
    public struct EnemySpawnInfo
    {
        public Vector2Int cell;
        public int level;
        public float health;
        public float attackPower;
        public float attackSpeed;
        public float moveSpeed;
        public bool isBoss;
    }

    // stageId theo convention campaign (101, 102... 201...).
    // enemySpawnCells = ô hàng trên đã trừ wall; bossCell = ô cửa (giữa hàng trên).
    // dungeon: CHƯA DÙNG thực sự — hiện chỉ chi phối tốc độ di chuyển theo theme (ThemeMoveSpeed);
    // caller đang để mặc định. Chừa sẵn cho lúc mỗi dungeon có bảng enemy riêng.
    public static List<EnemySpawnInfo> Generate(int stageId, List<Vector2Int> enemySpawnCells, Vector2Int bossCell, DungeonType dungeon = DungeonType.ZombieHorde)
    {
        var result = new List<EnemySpawnInfo>();

        var campaign = GameData.staticData.campaign;
        int chapter = campaign.GetChapter(stageId);
        int stageIndex = campaign.GetStageIndex(stageId);
        int globalStage = (chapter - 1) * StaticCampaignData.STAGES_PER_CHAPTER + stageIndex; // 1,2,3...

        // "Level" enemy suy từ số màn đã đi — dùng làm tham số scale.
        int level = Mathf.Max(1, globalStage);

        // Màn cuối mỗi chương = boss.
        bool isBossStage = stageIndex >= StaticCampaignData.STAGES_PER_CHAPTER;

        // Ô spawn enemy khả dụng (hàng trên cùng, không phải box/cửa).
        if (enemySpawnCells == null || enemySpawnCells.Count == 0) return result;

        int count = isBossStage
            ? 1
            : Mathf.Clamp(BASE_ENEMY_COUNT + (chapter - 1) * ENEMY_COUNT_PER_CHAPTER, 1, MAX_ENEMY_COUNT);
        count = Mathf.Min(count, enemySpawnCells.Count);

        float power = EnemyPower(level);

        for (int i = 0; i < count; i++)
        {
            // Boss dùng ô cửa (giữa), thường phân bố đều trên các ô spawn.
            Vector2Int cell = isBossStage ? bossCell : enemySpawnCells[i * enemySpawnCells.Count / count];

            float unitPower = isBossStage ? power * 6f : power;   // boss mạnh gấp ~6 lần lính thường

            result.Add(new EnemySpawnInfo
            {
                cell = cell,
                level = level,
                health = unitPower * 10f,                          // máu = 10× sát thương (đủ trâu để đấu vài nhịp)
                attackPower = unitPower,
                attackSpeed = 1f,
                moveSpeed = ThemeMoveSpeed(dungeon),
                isBoss = isBossStage,
            });
        }

        return result;
    }

    // power(level) = LEVEL_SCALER * level * EXP_SCALER^(level / SEGMENT).
    // Neo trên army_power_level_scaler & army_power_exponential_scaler thật (placeholder tổng thể).
    private static float EnemyPower(int level)
    {
        float segmentBoost = Mathf.Pow(ARMY_POWER_EXP_SCALER, (level - 1) / (float)POWER_SEGMENT);
        return ARMY_POWER_LEVEL_SCALER * level * segmentBoost;
    }

    // DragonBoss theme: enemy đứng yên (EnemyCanMove=0 trong DungeonThemeData gốc).
    private static float ThemeMoveSpeed(DungeonType dungeon)
    {
        return dungeon == DungeonType.DragonBoss ? 0f : 2f;
    }
}

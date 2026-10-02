using System.Collections.Generic;
using UnityEngine;

// Dungeon Cultist Ritual: đội cultist melee + ranged (enemyPrefab = CultistMeleeEnemy, rangedEnemyPrefab = CultistRangedEnemy).
// Gốc (rq.irg + SpawnController.huy, theme CultistDungeonThemeData): preset = CultistPresets[(level-1)%5],
// melee cầm dao (Karambit), ranged cầm cung bắn đạn (BasicBow + Projectile_Weapon_Cultist); đứng ngẫu nhiên
// ở nửa trên map (GridManager.ijx), map có obstacle (CultistDungeonObstacle). Hình/animator/vũ khí gán sẵn trên prefab.
public class CultistDungeonMode : DungeonMode
{
    [Tooltip("Prefab cultist bắn xa (role Range).")]
    public GameObject rangedEnemyPrefab;

    protected override void CreateTeamB()
    {
        if (enemyPrefab == null || rangedEnemyPrefab == null)
        {
            DebugCustom.LogWarning("[CultistDungeonMode] Chưa gán enemyPrefab/rangedEnemyPrefab — bỏ qua spawn.");
            return;
        }

        List<Vector2Int> cells = PickEnemyCells(EnemySpawnGenerator.CultistCount(dungeonLevel));
        List<EnemySpawnGenerator.EnemySpawnInfo> cultists = EnemySpawnGenerator.GenerateCultists(dungeonLevel, cells);

        Transform parent = NewGroup("Enemies");
        for (int i = 0; i < cultists.Count; i++)
        {
            Vector3 pos = MapController.Instance.CellToWorld(cultists[i].cell);
            GameObject prefab = cultists[i].isRanged ? rangedEnemyPrefab : enemyPrefab;
            EnemyUnit cultist = SpawnUnit<EnemyUnit>(prefab, pos, parent);
            cultist.SpawnEnemy(cultists[i], pos);
        }
    }
}

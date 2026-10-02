using System.Collections.Generic;
using UnityEngine;

// Dungeon Zombie Outbreak: 1 bầy zombie cận chiến (enemyPrefab = ZombieEnemy).
// Gốc (ru.irk + SpawnController.huy, theme ZombieInvasionData): số con = ZombiePresets[(level-1)%4]
// (6/8/10/12), đứng ngẫu nhiên ở nửa trên map (GridManager.ijx), map có obstacle (ZombieObstacle).
// Zombie tay không, đi được, collider ×1.0 — hình/animator gán sẵn trên prefab ZombieEnemy.
public class ZombieDungeonMode : DungeonMode
{
    protected override void CreateTeamB()
    {
        if (enemyPrefab == null)
        {
            DebugCustom.LogWarning("[ZombieDungeonMode] Chưa gán enemyPrefab (prefab zombie) — bỏ qua spawn.");
            return;
        }

        List<Vector2Int> cells = PickEnemyCells(EnemySpawnGenerator.ZombieCount(dungeonLevel));
        List<EnemySpawnGenerator.EnemySpawnInfo> zombies = EnemySpawnGenerator.GenerateZombies(dungeonLevel, cells);

        Transform parent = NewGroup("Enemies");
        for (int i = 0; i < zombies.Count; i++)
        {
            Vector3 pos = MapController.Instance.CellToWorld(zombies[i].cell);
            EnemyUnit zombie = SpawnUnit<EnemyUnit>(enemyPrefab, pos, parent);
            zombie.SpawnEnemy(zombies[i], pos);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

// Dungeon Dragon's Hoard: 1 con rồng (enemyPrefab = prefab rồng) đứng yên bắn đạn cả map.
// Gốc (SpawnController.huu/huv + GridManager.ijx): rồng đứng 1 ô trống NGẪU NHIÊN ở nửa trên map
// (row ≥ 5); chỉ số theo EnemySpawnGenerator.GenerateDragon. Không tự di chuyển (moveSpeed 0);
// hình quay 180°, collider ×1.1, vũ khí DragonWeapon — gán sẵn trên prefab rồng.
public class DragonDungeonMode : DungeonMode
{
    protected override void CreateTeamB()
    {
        if (enemyPrefab == null)
        {
            DebugCustom.LogWarning("[DragonDungeonMode] Chưa gán enemyPrefab (prefab rồng) — bỏ qua spawn.");
            return;
        }

        List<Vector2Int> cells = PickEnemyCells(1);
        MapController map = MapController.Instance;
        Vector2Int cell = cells.Count > 0 ? cells[0] : new Vector2Int(map.Rows - 1, map.Cols / 2);
        EnemySpawnGenerator.EnemySpawnInfo info = EnemySpawnGenerator.GenerateDragon(dungeonLevel, cell);
        Vector3 pos = map.CellToWorld(cell);

        EnemyUnit dragon = SpawnUnit<EnemyUnit>(enemyPrefab, pos, NewGroup("Enemies"));
        dragon.SpawnEnemy(info, pos);
    }
}

using System.Collections.Generic;
using UnityEngine;

// Dungeon Dragon's Hoard: 1 con rồng (enemyPrefab = prefab rồng) đứng yên bắn đạn cả map.
// Gốc (SpawnController.huu/huv + GridManager.ijx): rồng đứng 1 ô trống NGẪU NHIÊN ở nửa trên map
// (row ≥ 5); chỉ số theo EnemySpawnGenerator.GenerateDragon. Không tự di chuyển (moveSpeed 0);
// hình quay 180°, collider ×1.1, vũ khí DragonWeapon — gán sẵn trên prefab rồng.
public class DragonDungeonMode : DungeonMode
{
    // GridManager.ijx: chỉ xét ô có row ≥ 5 (nửa trên lưới).
    private const int ENEMY_MIN_ROW = 5;

    protected override void CreateTeamB()
    {
        if (enemyPrefab == null)
        {
            DebugCustom.LogWarning("[DragonDungeonMode] Chưa gán enemyPrefab (prefab rồng) — bỏ qua spawn.");
            return;
        }

        Vector2Int cell = PickEnemyCell();
        EnemySpawnGenerator.EnemySpawnInfo info = EnemySpawnGenerator.GenerateDragon(dungeonLevel, cell);
        Vector3 pos = MapController.Instance.CellToWorld(cell);

        EnemyUnit dragon = SpawnUnit<EnemyUnit>(enemyPrefab, pos, NewGroup("Enemies"));
        dragon.SpawnEnemy(info, pos);
    }

    private Vector2Int PickEnemyCell()
    {
        MapController map = MapController.Instance;
        var cells = new List<Vector2Int>();
        for (int row = ENEMY_MIN_ROW; row < map.Rows; row++)
        {
            for (int col = 0; col < map.Cols; col++)
            {
                if (map.Grid[row, col] == 0)
                {
                    cells.Add(new Vector2Int(row, col));
                }
            }
        }

        return cells.Count > 0 ? cells[Random.Range(0, cells.Count)] : new Vector2Int(map.Rows - 1, map.Cols / 2);
    }
}

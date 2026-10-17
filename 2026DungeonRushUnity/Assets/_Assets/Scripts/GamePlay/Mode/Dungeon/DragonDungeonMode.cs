using System.Collections.Generic;
using UnityEngine;

// Dungeon Dragon's Hoard: 1 con rồng (enemyPrefab = prefab rồng) đứng yên bắn đạn cả map.
// Gốc SpawnController.huu: dungeon DragonBoss (type 0) KHÔNG dùng ô ngẫu nhiên — rồng đứng tại iiz((0, H−3))
// với x thế giới = 0, tức ô (hàng rows−3, cột giữa) lệch trái nửa ô. Chỉ số theo EnemySpawnGenerator.GenerateDragon.
// Không tự di chuyển (theme EnemyCanMove = false → moveSpeed 0); hình quay 180°, collider ×1.1, vũ khí
// DragonWeapon — gán sẵn trên prefab rồng.
public class DragonDungeonMode : DungeonMode
{
    private const int ROWS_FROM_TOP = 3;
    private static readonly Vector3 CENTER_X_OFFSET = new Vector3(-0.5f, 0f, 0f);   // x thế giới gốc = 0

    protected override void CreateTeamB()
    {
        if (enemyPrefab == null)
        {
            DebugCustom.LogWarning("[DragonDungeonMode] Chưa gán enemyPrefab (prefab rồng) — bỏ qua spawn.");
            return;
        }

        MapController map = MapController.Instance;
        var cell = new Vector2Int(map.Rows - ROWS_FROM_TOP, map.Cols / 2);
        EnemySpawnGenerator.EnemySpawnInfo info = EnemySpawnGenerator.GenerateDragon(dungeonLevel, cell);
        Vector3 pos = map.CellToWorld(cell) + CENTER_X_OFFSET;

        EnemyUnit dragon = SpawnUnit<EnemyUnit>(enemyPrefab, pos, NewGroup("Enemies"));
        dragon.SpawnEnemy(info, pos);
    }
}

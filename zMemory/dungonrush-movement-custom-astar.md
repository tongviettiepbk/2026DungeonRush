---
name: dungonrush-movement-custom-astar
description: "Game gốc TỰ CODE di chuyển bằng A* trên grid, KHÔNG dùng Unity NavMesh; DLL Astar/NavMesh có bundle nhưng không được gameplay gọi"
metadata: 
  node_type: memory
  type: project
  originSessionId: c6b4444d-5c70-4697-8e7b-e4c99563b95f
  modified: 2026-07-30T05:07:51.631Z
---

Di chuyển/điều hướng trong map của DungeonRush gốc là **tự code**, không dùng navigation obstacle-avoidance của Unity.

Bằng chứng (decode ở AssetRipper/ExportedProject/Assets/Scripts/Assembly-CSharp):
- 0 gameplay script tham chiếu `UnityEngine.AI` / `NavMeshAgent` / `NavMeshObstacle`.
- Có `Assets/Plugins/NavMeshComponents.dll` và `AstarPathfindingProject.dll` nhưng KHÔNG có `using Pathfinding` / `Seeker` / `AstarPath` nào trong code → DLL bundle thừa, không dùng.
- `PathfindingManager.cs`: A* tự viết trên grid — hằng số `1.414f` (chéo √2), `1f` (thẳng); `ikb(start,goal)→List<Vector2Int>` = FindPath; `ike`=IsWalkable; `ikf`=min f-score; `ikg`=reconstruct.
- Node A* tự định nghĩa: class `re` (xrf=pos, xrg=g, xrh=h, xri=parent, bgsj=f).
- `GridManager.cs`: lưới GridWidth/Height/CellSize, `ija` world→cell, `iiz` cell→world, `ijb`=IsWalkable. Tường đánh dấu qua `CreativeWallSettings.BlockPathfinding`.
- 2 movement controller bám waypoint: `GridMovementController` (tween ô-sang-ô) và `PhysicsMovementController` (steering lực: MoveForceMultiplier, StoppingDistance, WaypointReachedDistance, StuckDetectionThreshold).

Tránh chướng ngại = A* chỉ đi qua ô walkable (loại tường ngay khi tìm path), không phải obstacle-avoidance runtime.

**Áp dụng khi port sang bản mới:** tự implement A* grid theo mô hình này, đừng gắn NavMeshAgent. Liên quan [[dungonrush-map-spawn-procedural]] (map procedural, grid 9x12+wall) và [[dungonrush-mainmap-gameplay-spec]].

Ghi chú: IL2CPP nên method body bị strip (return null/rỗng); chỉ có chữ ký + tên field + hằng số để suy ra thuật toán.

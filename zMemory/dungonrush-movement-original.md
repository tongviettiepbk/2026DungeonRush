---
name: dungonrush-movement-original
description: Di chuyển/AI hero-quái-pet dựng lại theo thân hàm gốc 2026-10-17 — A* 8 hướng, lái lực vật lý Rigidbody động, Idle/Move/Patrol, quái tầm phát hiện 4, pet chỉ đi theo; CHƯA test Play
metadata:
  type: project
  modified: 2026-10-17
---

User yêu cầu 2026-10-17: logic di chuyển hero/pet/enemy làm lại theo gốc. Doc gốc: `DecodedData/MOVEMENT_MODEL.md`.

**Đã làm (compile sạch, A* test bằng harness, CHƯA test Play):**
- `MapController.FindPath` = PathfindingManager.ikb (8 hướng, 1/1.414, chéo sát tường ×3, octile, Dictionary, 1000 lượt) dùng `MapSpawnCells.GetNeighbors8`; thêm `HasLineOfSight` (raycast layer 12), `ClampCell`, `IsOccupiedByOther`. Đã bỏ ResolveMove/SteerAngles.
- `Unit/UnitMover.cs` = PhysicsMovementController (AddForce 10×(dir×speed−v)+dir×Random(1,10), waypoint 0.5, kẹt tăng lực ≤6); pet bộ riêng (0.6/≤3/0.5).
- `BaseUnit`: Rigidbody root DYNAMIC (mass1, drag2, gravity0, khoá xoay), Destroy RB con "Soldier" để collider va chạm thật; Idle/Move theo state gốc (SelectTarget, IsInAttackPosition, RepathToTarget=fcf, FindFiringCell=fch, timer 0.15/1/0.2); thêm `BattleState.Patrol` + `canPatrol` (tốc ×0.5, 8 ô quanh ô nhà, chờ 0.5–1.5s); Pause hãm vận tốc.
- `EnemyUnit`: bỏ aggroRange/leash → `detectionRange` (0 = 4), bám mục tiêu cũ; `isMoveable = moveSpeed>0`, `canPatrol = isMoveable`. BossRush boss dùng detectionRange 1000.
- `PetUnit`: không lao vào đánh; theo hero khi > FollowDistance (data 3), tìm đường 0.3s, đứng 0.3s khi ra chiêu, đẩy nhau trong 1.2.

**Rủi ro cần test:** unit giờ va chạm vật lý (đẩy nhau, kẹt chướng ngại); KnockBack/Fear vẫn MovePosition; chuyển màn đặt hero Kinematic.
**Chưa làm:** AttackCooldown chặn Idle (project đánh theo nhịp riêng), xoay thân 600°/s (project lật trái/phải).
Liên quan [[dungonrush-campaign-map-original]], thay [[dungonrush-movement-custom-astar]].

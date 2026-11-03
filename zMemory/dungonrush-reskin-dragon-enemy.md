---
name: dungonrush-reskin-dragon-enemy
description: "DragonEnemy (rồng dungeon Boss Raid) reskin thành cáo + cầu lửa dùng chung art Red Dragon (2026-11-03): chỉ Idle nhún + đánh thường, không skill; test Play OK, đã commit vào main"
metadata:
  node_type: memory
  type: project
---

2026-11-03: `Prefabs/Units/Enemies/DragonEnemy.prefab` lưu đè giữ GUID, chỉ `DragonDungeonMode` dùng. Sprite trỏ thẳng vào `Art/4_Characters/BossNew/BossRushRedDragon/` (không có bản riêng — đổi art đó thì đổi cả 2). Anim `Animations/BossNew/DragonEnemy/`: chỉ Idle + Attack chép BlackDragon, đã bỏ state Move và tham số IsMoving, thêm tham số IsDying.

- Khác boss rush: script vẫn `EnemyUnit` + `OriginalAnimatorController`, nên `Body` đặt sẵn scale 1.5 trong prefab (không có code nhân), không có fireOrb/FaceTarget.
- Quay mặt: thêm `OriginalAnimatorController.isFacingRight` lật `Soldier.localScale.x` (cùng cách UnitAnimatorController). BaseUnit tự gọi Face.
- FlipPoints y .76 (thanh máu trên đầu). Collider giữ nguyên bán kính .473 và damage/data giữ nguyên, nên vùng va chạm nhỏ hơn hình nhiều.
- Chết: `BeginDie` không ai gọi nên không cần state Die; cáo biến mất rồi hiện popup Completed.
- Test: `UserData.isDisableSaveDataLocal = true` trước khi tap, `ChangeMode(ModeType.DragonBossDungeon)`, hp giữ qua reflection (`hp` là protected set). Sau khi dừng Play phải trả cờ về false.
- Đã xoá `Animations/Dragon/` (DragonAnimatorController + 2 clip RedDragon) vì không còn ai dùng, đã commit vào main. Lưu ý `dragon_without_wings.png` vẫn được các popup BossRush tham chiếu nên chưa xoá.

**Why:** user muốn rồng dungeon cùng hình boss Red Dragon nhưng chỉ đánh thường, idle nhún.
**How to apply:** đừng thêm skill/anim Move cho con này trừ khi user yêu cầu. Liên quan [[dungonrush-reskin-boss-dragon]], [[dungonrush-dungeon-system]].

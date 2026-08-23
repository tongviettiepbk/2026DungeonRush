---
name: dungonrush-mainmap-gameplay-spec
description: "Spec cách chơi MainMap DungeonRush do user mô tả — map 9x12+wall, 3 unit Hero/Enemy/Pet trên BaseUnit, AI từng loại"
metadata: 
  node_type: memory
  type: project
  originSessionId: 7067cfa0-2cf4-4e82-b0f4-f77fe1d10bdb
  modified: 2026-07-29T07:51:23.882Z
---

Spec MainMap do user mô tả (2026-07-29), là hướng gameplay CHÍNH THỨC cần bám. Xem [[dungonrush-genre-rpg-action]] (RPG action, không phải snake).

**Map:**
- Kích thước **9×12**, có ô là **wall** (mỗi wall chiếm 1 ô) → KHÔNG đi qua được.
- ✅ CHỐT (2026-07-29): vùng chơi thực là **9×12 ĐẦY ĐỦ** (toàn bộ đều đi được, wall nằm trong đó). → cần sửa `StaticMapData.GRID_WIDTH=9` + `CampaignLevelBuilder` (bỏ trừ 1 cột viền). Code CŨ đang 8×12 → SAI, phải đổi.
- ✅ CHỐT kiểu di chuyển: **continuous-space né wall** (không phải bước ô lưới). Unit đi mượt trong không gian liên tục, chỉ tránh đè lên ô wall; combat theo attackRange/khoảng cách hiện tại phù hợp.

**3 unit đều kế thừa BaseUnit:** Hero, Enemy, Pet(companion đi theo player).
- **Hero**: mặc trang bị hiển thị hình ảnh — vũ khí, găng tay, cánh, áo choàng. AI: tự tìm enemy GẦN NHẤT trong map → đi tới vùng đánh → attack.
- **Enemy**: KHÔNG mặc đồ. KHÔNG tự đi tìm hero. Có vùng tấn công: mục tiêu vào vùng thì đi tới đánh / hoặc đã trong vùng thì đánh luôn (thụ động).
- **Pet**: KHÔNG mặc đồ. Đi theo hero, tấn công enemy trong vùng tấn công.
- Cả 3 chỉ di chuyển trong map, không vào ô có wall.

**Trạng thái code khi nhận spec (đối chiếu):**
- BaseUnit ✅ có sẵn (state machine Idle/Move/Attack, FindNearestTarget, IsTargetInAttackRange).
- HeroUnit ❌ chưa có class. PetUnit/CompanionUnit ❌ chưa có. EnemyUnit có nhưng là MonoBehaviour data-holder, CHƯA kế thừa BaseUnit.
- Wall chặn di chuyển ❌: obstacle có sinh (MapCellType.Obstacle) nhưng KHÔNG có logic di chuyển nào tôn trọng wall. `BaseUnit.Moving()` rỗng (no-op); `CombatMap.ClampPointInMap` = identity; chưa có pathfinding/grid-collision.
- Vòng lặp AI KHÔNG chạy: `BaseUnit.UpdateBehavior()` KHÔNG có ai gọi (GameController không tick unit).
- Enemy thụ động (không đuổi) ❌: base FindNearestTarget khiến MỌI unit đuổi mục tiêu gần nhất — chưa có override enemy đứng yên tới khi mục tiêu vào tầm.
- Hero mặc gear hiển thị ❌ code: data+prefab slot có, chưa có code gắn gear lên slot.

**✅ ĐÃ IMPLEMENT (2026-07-29, compile dotnet 0 lỗi, CHƯA verify LIVE Unity):**
- Map 9×12 đầy đủ: `StaticMapData.GRID_WIDTH=9`, `CampaignLevelBuilder` bỏ trừ cột viền.
- Di chuyển liên tục né wall: `CombatMap` (trong GameController.cs) giữ biên+wall, `IsWalkable`/`ClampPointInMap`/`ResolveMove` (đi thẳng, kẹt thì lách ±25/50/75/90°). `BaseUnit.Moving()` thực thi (trước rỗng) + `moveDestination`; `UpdateMove` bám target.
- Vòng lặp AI: `GameController.Update()` tick `UpdateBehavior` mọi unit (driver trước đây thiếu). +`NextBattleId`/`ResetBattle`/AddUnit gắn team.
- 3 unit kế thừa THẲNG `BaseUnit` (user chọn GỘP, KHÔNG dùng lớp trung gian MapUnit — MapUnit đã xoá): phần chung (`baseStats`+`CalculateCurrentStats` đổ stat, `SpawnInBattle(stats,tag,pos)`, `OnAttackEnd` đánh tạm, helper `FindNearestEnemyFrom/Among`) nằm trong BaseUnit (region "Map unit (DungeonRush)"). `HeroUnit` (tìm enemy gần nhất khắp map), `EnemyUnit` (thụ động, target trong `aggroRange`, override `IsTargetAvailable` leash; `SpawnEnemy(info,pos)`), `PetUnit` (owner=hero; đánh enemy trong `engageRange` đo từ hero, else đi theo hero giữ `followDistance`).
- Entry chạy trận: `CombatDirector` (spawn hero/pet/enemy từ prefab rig, nạp CombatMap, hero giữa hàng dưới, pet quanh hero, enemy tại cell). `LevelRuntimeBuilder` chỉ preview map.
- **Đường dẫn (sau khi user reorg trong Unity 2026-07-29)**: unit ở `Scripts/Unit/` (BaseUnit/HeroUnit/PetUnit/EnemyUnit/GameController); map+combat ở `Scripts/GamePlay/{Map,Combat}/` (MapGenerator/CampaignLevelBuilder/GridSpace/LevelRuntimeBuilder/CombatDirector). Code KHÔNG dùng namespace nên dời thư mục không ảnh hưởng compile.
- **Verify Unity LIVE**: compile 0 lỗi (Unity tự biên dịch, không chỉ dotnet). Đã thêm tag `TeamA`/`TeamB` qua MCP. CHƯA chạy play-mode.
- `IsInBattleScreen` chống null camera (map top-down không lọc biên).
- **Sát thương**: AnimationController là stub → Spine anim-event KHÔNG bắn → sát thương gốc (qua ReleaseNormalAttack) không chạy. Tạm áp sát thương ở `MapUnit.OnAttackEnd` (EndAttack gọi chắc chắn mỗi nhịp) qua `target.TakeAttack(GetBasicAttackData(), impactAttack)`. Thay bằng anim-event thật khi port Spine.
- **Enemy leash**: override `IsTargetAvailable` = base && trong aggroRange → dừng đuổi khi mục tiêu rời vùng (base UpdateMove không tự lọc tầm).
- **FxController.GetText** thêm guard null (pool `?.New()` + prefab null) — combat mới là caller đầu tiên thực sự gây sát thương, tránh `Instantiate(null)` crash khi chưa gán prefabTextDamage.

**CÒN LẠI (cần Unity):** ~~tag TeamA/TeamB~~ (✅ đã thêm); prefab unit có rig BaseUnit (con Body/FlipPoints/CenterBody/FirePoint/health-bar + Rigidbody2D+AudioSource+AnimationController) gán vào CombatDirector; Hero mặc gear hiển thị (Spine slot); AnimationController Spine thật; verify LIVE. Hero stat hiện là placeholder trên Inspector (sau lấy từ gear/companion).

Liên quan: [[dungonrush-rebuild-progress]], [[dungonrush-map-spawn-procedural]], [[dungonrush-genre-rpg-action]].

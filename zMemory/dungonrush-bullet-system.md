---
name: dungonrush-bullet-system
description: Trạng thái hệ đạn/projectile port từ StickIdle vào DungeonRush; prefab đã import nhưng còn missing-script
metadata: 
  node_type: memory
  type: project
  originSessionId: 318adf60-26c2-488e-9c8f-ba949bafba07
  modified: 2026-08-27T18:44:07.460Z
---

Port hệ bullet theo [[dungonrush-follow-stickidle]] (2026-08-28).

**CODE đã xong & compile sạch** (BaseBullet đủ hàm, khác placeholder cũ):
- `Unit/BaseBullet.cs` — port đầy đủ từ StickIdle `Bullets/BaseBullet.cs`: enum `BulletMovingType{Straight,Parabol}`, `Active/ActiveFake/ActiveStraight/ActiveParabol/OnTargetTakeDamage/Deactive/OnResetMode`. Dùng DOTween, `MathUtils.Parabola`, `FaceUpAxisToPoint`, `FxController.SpawnFx(attacker.impactAttack)`, `target.TakeAttack`, pooling.
- `Utilities/MathUtils.cs` (MỚI) — chỉ `Parabola` (Vector3/Vector2).
- `Utilities/Extensions.cs` — thêm extension `FaceUpAxisToPoint`.
- `Utilities/DebugCustom.cs` — thêm `ShowLog(object)` / `ShowLog(object,object)`.
- `GameDesignPatterns/ObjectPooling/PoolingController.cs` — thêm lại `#region Bullets` (`groupBullet`, `poolBullets`, `GetBullet`, `StoreBullet`).
- Pattern bắn: subclass override `BaseUnit.ReleaseBullet()` (base trả null) → `PoolingController.GetBullet(prefab)` → `bullet.Active(firePoint, this, target)`. Ví dụ gốc: StickIdle `BaseCompanion.ReleaseBullet`. CHƯA nối vào Hero/Companion DungeonRush (bước sau).

**PREFAB + FX đã import sẵn** (từ AssetRipper `ExportedProject/Assets/GameObject/*Projectile*`):
- `_Assets/Prefabs/Projectiles/` (25) + `_Assets/Prefabs/Fx/Projectile/` (12). Sprite guid đã remap, có .meta.

**BLOCKER còn lại: 30 prefab vẫn trỏ MonoBehaviour game gốc** guid `c9651ccd0dae66ddf65b96a3252f4a55` (script projectile gốc, fields `RendererTransform`+`SortingOrder`) → **missing script** trong DungeonRush. Remap sang BaseBullet (guid `e062c72f0b18af64e8aba28f640f7bae`) KHÔNG đủ vì field khác nhau: cần set `Transform` + chọn `movingType`, và prefab gốc còn Rigidbody2D+CircleCollider2D (StickIdle bullet dùng DOTween, không physics). Cần user chốt cách wiring. (script phụ `3f87693b6644aa4ef4b3891978e476ec` = SkullProjectile.)

Compile-check: KHÔNG có dotnet SDK; build bằng MSBuild VS2022 `Assembly-CSharp.csproj` (0 lỗi CS, chỉ warning MSB3277 reference conflict có sẵn).

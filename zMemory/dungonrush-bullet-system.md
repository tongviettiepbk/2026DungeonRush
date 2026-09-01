---
name: dungonrush-bullet-system
description: Trạng thái hệ đạn/projectile port từ StickIdle vào DungeonRush; prefab đã import nhưng còn missing-script
metadata: 
  node_type: memory
  type: project
  originSessionId: 318adf60-26c2-488e-9c8f-ba949bafba07
  modified: 2026-08-29T09:04:26.305Z
---

Port hệ bullet theo [[dungonrush-follow-stickidle]] (2026-08-28).

**CODE đã xong & compile sạch** (BaseBullet đủ hàm, khác placeholder cũ):
- `Unit/BaseBullet.cs` — port đầy đủ từ StickIdle `Bullets/BaseBullet.cs`: enum `BulletMovingType{Straight,Parabol}`, `Active/ActiveFake/ActiveStraight/ActiveParabol/OnTargetTakeDamage/Deactive/OnResetMode`. Dùng DOTween, `MathUtils.Parabola`, `FaceUpAxisToPoint`, `FxController.SpawnFx(attacker.impactAttack)`, `target.TakeAttack`, pooling.
- `Utilities/MathUtils.cs` (MỚI) — chỉ `Parabola` (Vector3/Vector2).
- `Utilities/Extensions.cs` — thêm extension `FaceUpAxisToPoint`.
- `Utilities/DebugCustom.cs` — thêm `ShowLog(object)` / `ShowLog(object,object)`.
- `GameDesignPatterns/ObjectPooling/PoolingController.cs` — thêm lại `#region Bullets` (`groupBullet`, `poolBullets`, `GetBullet`, `StoreBullet`).
- Pattern bắn: subclass override `BaseUnit.ReleaseBullet()` (base trả null) → `PoolingController.GetBullet(prefab)` → `bullet.Active(firePoint, this, target)`. Ví dụ gốc: StickIdle `BaseCompanion.ReleaseBullet`.

**ATTACK INTEGRATION XONG (2026-08-30, compile sạch qua UnityMCP):** Vũ khí đang mặc/gán quyết định melee vs ranged.
- `WeaponData` thêm `public BaseBullet bulletPrefab` (đạn khi `hasProjectile:1`).
- `BaseUnit`: fields `hasProjectile`+`bulletPrefab`; `OnBeginBattle` gọi `ApplyWeapon(GetCombatWeapon())` (set hasProjectile/bulletPrefab + `stats.attackRange=weapon.attackDistance`, weapon GHI ĐÈ tầm đánh baseStats). Điểm ra đòn gộp về `ReleaseAttack()`: `hasProjectile&&bulletPrefab!=null` → `PoolingController.GetBullet→bullet.Active(firePoint,this,target,GetBasicAttackData())`; ngược lại `target.TakeAttack` (đánh thẳng). `OnAttackEnd()` gọi `ReleaseAttack()` (điểm ra đòn TẠM vì Spine anim-event còn stub; sau này chuyển sang `RaiseEventNormalAttack` & bỏ ở OnAttackEnd tránh double-hit). Damage vẫn tính chung từ `GetBasicAttackData` (server-driven), nhánh chỉ khác CÁCH truyền.
- Nguồn vũ khí qua virtual `GetCombatWeapon()`: `HeroUnit`→`ResolveEquippedWeapon()` (save slot WEAPON→StaticWeaponData.GetData); `EnemyUnit`→2 field serialize `weaponData`+`bossWeaponData` (isBoss ưu tiên bossWeaponData, vì boss & quái thường DÙNG CHUNG enemyPrefab); `PetUnit`→1 field serialize `weaponData` (companion-skill đầy đủ là hệ RIÊNG, chưa làm — pet chỉ basic attack theo weapon).
- CÒN LẠI (data trong Unity): gán `weaponData`/`bossWeaponData` cho prefab quái/boss & `weaponData` cho prefab pet. Hero tự đọc từ save không cần gán.

**PREFAB + FX đã import sẵn** (từ AssetRipper `ExportedProject/Assets/GameObject/*Projectile*`):
- `_Assets/Prefabs/Projectiles/` (25) + `_Assets/Prefabs/Fx/Projectile/` (12). Sprite guid đã remap, có .meta.

**WIRING XONG (2026-08-29):** 25 prefab cũ + 5 Orb (Red/Blue/Green/Purple/Divine mới copy) đều đã remap `c9651ccd...`→BaseBullet `e062c72f0b18af64e8aba28f640f7bae`, giữ nguyên anchor MonoBehaviour nhỏ + Rigidbody2D+CircleCollider2D, thay `RendererTransform`+`SortingOrder` bằng `isPooling:1, movingType:1, Transform:{root transform}, height:0.5, speed:5`.

**WeaponData.bulletPrefab đã nối cho CẢ 39 weapon có `hasProjectile:1`** (23 ranged r_* + Cultist + 3 Dragon + 6 staff + 5 boss Lych/Witch). Mapping khôi phục từ `DecodedData/tables/WeaponData.csv` cột `ProjectilePrefab` (guid namespace bundle GỐC, metas export đã bị lột → tra bằng quy ước tier/loại, cluster guid dùng chung tự xác nhận: r_3_2=r_3_3→Arrow3, r_4_2=r_4_3→Arrow4, `6_1_Throw`=r_6_1). Ref YAML dạng `bulletPrefab: {fileID:<BaseBullet component fileID>, guid:<prefab guid>, type:3}`.

**Staff→Orb (SUY LUẬN theo nguyên tố, guid-cluster chắc chắn nhưng tên màu chưa verify tint):** RedOrb=Flame(r_5_3)/Tyrant(r_9_3)/Lych2; PurpleOrb=Wizard(r_6_3)/Lych; GreenOrb=Wild(r_7_3)/Lych3/Witch/Witch2; BlueOrb=Ice(r_8_3); DivineOrb=Radiant(r_10_3). User chốt dùng map suy luận.

**Lưu ý:** Spear6 KHÔNG tồn tại (tier6 dùng 6_1_Throw). Orb particle tham chiếu mesh `585c0f75644bc9b48b84499756acf46d` (type2, no meta) → MISSING trong Unity, render mesh có thể lỗi nhưng bullet vẫn chạy. CHƯA mở Unity import lại 5 prefab mới → cần reimport & verify reference. (script phụ `3f87693b6644aa4ef4b3891978e476ec` = SkullProjectile.)

Compile-check: KHÔNG có dotnet SDK; build bằng MSBuild VS2022 `Assembly-CSharp.csproj` (0 lỗi CS, chỉ warning MSB3277 reference conflict có sẵn).

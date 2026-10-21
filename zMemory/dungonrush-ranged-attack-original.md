---
name: dungonrush-ranged-attack-original
description: Đánh xa GỐC (reverse v41 RangeAttackState + ProjectileArrow/Bullet); 2026-10-21 đã sửa theo gốc (bay thẳng, 50%/25%+50%, nhả từ weapon_slot), test Play pass, đã commit
metadata:
  node_type: memory
  type: project
  originSessionId: 3e270eb0-4dca-4e90-b6cf-58da6fd487fa
  modified: 2026-10-21T14:56:48.978Z
---

Kiểm 2026-10-21 (reverse libil2cpp v41 arm64):

**Data khớp gốc:** 29 vũ khí r_* + 10 vũ khí quái (Dragon/Lych/Witch/Cultist): attackDistance (3.5 / boss 100), projectileSpeed, prefab đạn đều đúng WeaponData.csv. AreaDamageRadius gốc = 0 hết.

**Logic gốc:**
- `RangeAttackState`: chu kỳ = 1/AttackSpeed; nhả đạn ở **50% chu kỳ** (hoặc sớm hơn nếu anim event AttackAnimationHit bắn); đánh đôi (DoubleChance, phe IsHome) → anim ×2, nhả ở 25% và 50%. Lúc nhả chỉ cần target còn sống (không check tầm).
- `fcm`: LeanPool.Spawn(Soldier.ProjectilePrefab) tại **vị trí sprite vũ khí** (Character.WeaponImage, null thì transform unit), rotation identity; nhắm `target.transform.position` (gốc unit, không phải giữa thân).
- `ProjectileArrow`/`ProjectileBullet` (giống hệt nhau): **bay THẲNG**, Rigidbody2D velocity = hướng(lúc bắn)×ProjectileSpeed, gravity 0, không đuổi; xoay z = atan2(v)−90; trúng bằng OnTriggerEnter2D, chỉ trúng đúng TargetCharacter → **có thể trượt**; sống 4s; target chết giữa đường → bay tới điểm cuối rồi biến, không damage; người bắn chết → đạn biến ngay. Damage tính LÚC TRÚNG qua Shooter.ewb(target).
- Âm bắn/trúng chỉ cho IsMainCharacter.

**ĐÃ SỬA 2026-10-21 (test Play pass, đã commit):** `BaseBullet.ActiveLinear/UpdateLinear` (bay thẳng, chạm `BaseUnit.BodyCollider` bằng ClosestPoint chia bước theo bán kính CircleCollider đạn, sống 4s, người bắn chết → tắt); `BaseUnit.ScheduleRangedShots` nhả 50% nhịp / đánh đôi 25%+50% (cờ first/secondShot + attackCycle; `RollDoubleAttack` virtual, Hero override); `GetProjectileSpawnPoint` = node `weapon_slot` (fallback FirePoint); BossRushBossUnit anim-hit → `ReleaseRangedShotFromAnimation`. CHƯA làm: anim ×2 khi đánh đôi, damage tính lúc trúng, bỏ check tầm lúc nhả, nhắm vẫn là transform.position target (đúng gốc).

**Trước khi sửa project lệch:** BaseBullet prefab đang movingType=Parabol (height 0.5) + bù `endPoint.x -= t*moveSpeed` (kiểu side-scroll StickIdle) và luôn trúng khi tween xong; nhả ở CUỐI nhịp (OnAttackEnd); đánh đôi 50%+100%; firePoint = FlipPoints/FirePoint (0,0.5); nhắm centerBodyPoint; damage tính lúc bắn; bỏ nhả nếu target ra khỏi tầm.

**How to apply:** khi sửa đánh xa đọc file này; liên quan [[dungonrush-bullet-system]], [[dungonrush-hero-anim-unit-all]], [[dungonrush-substat-combat]].

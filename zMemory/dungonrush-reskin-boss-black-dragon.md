---
name: dungonrush-reskin-boss-black-dragon
description: "Boss Rush Black Dragon reskin thành pháp sư cáo + quả cầu (2026-11-02) — rig Body/Main/Sprite+Weapon, anim BossNew/BlackDragon, skill FireBreath cầu bay vòng; test Play OK, đã commit vào main"
metadata:
  node_type: memory
  type: project
  originSessionId: 581cae7f-59b6-438c-abb4-4d0f70903b8e
  modified: 2026-11-02T09:51:24.018Z
---

2026-11-02: art ở `Art/4_Characters/BossNew/BlackDragon/` (Boss 8.png thân, Boss 8 - weapon.png quả cầu). Prefab `Prefabs/Units/Enemies/BossRush/BossRushBlackDragon.prefab` lưu đè (giữ GUID, BossRushMode vẫn trỏ đúng).

- Rig: `Soldier`(Animator) → `Body` (code nhân ×bodyScale 1.5 + crit punch — KHÔNG animate node này) → `Main` (gốc ở CHÂN, y = −nửa cao sprite, clip nhún scale) → `Sprite` (SpriteRenderer) + `Weapon` (quả cầu) → `weapon_slot` (BaseUnit tự lấy làm điểm bắn đạn).
- Anim ở `Animations/BossNew/<TênBoss>/` (user chốt chia thư mục theo tên boss): BlackDragon.controller, bool IsIdle/IsAttacking (như CharacterAnimatorController gốc) + IsMoving (BossRushBossUnit tự set nếu controller có param, = mover.HasPath). Idle = UnitIdle hero (Y 0.9→1.1), Move = UnitRun, Attack 0.5s = cầu 1→0.8→1.45@0.3 (event OnAttackAnimationHit)→1 (OnAttackAnimationCompleted).
- Đã xoá art cũ chỉ rồng đen dùng: PurpleDragonGlove/HelmetData.png + 2 SO GearsEnemy cùng tên, và node ẩn găng/mũ/balo/cloak trong prefab. GIỮ DragonAnimatorController + clip RedDragon + Hero/body.png vì Green/Red Dragon, DragonEnemy còn dùng. Object cáo trong scene Root đã xoá.
- Đánh thường vẫn là đạn PurpleDragonWeaponData (gốc), bay ra từ weapon_slot.
- Skill: BossRushBossUnit.fireOrb (gán = Weapon) → FireBreath không xoay thân mà cho cầu bay ra vòng bán kính muzzleOffset quanh tâm, quét 360° phun lửa, nghỉ thì bay về chỗ cũ; damage/tầm giữ nguyên gốc. fireOrb trống = rồng cũ xoay thân.

**Why:** user reskin boss Boss Rush sang nhân vật mới, anim kiểu hero.
**How to apply:** reskin boss khác làm y khuôn này (rig Main ở chân, controller riêng trong BossNew/<boss>, fireOrb nếu có cầu). Quay mặt: BossRushBossUnit.FaceTarget đầu mỗi nhịp đánh thường lật Sprite.flipX + đổi dấu x quả cầu (chỉ rig có Main/Sprite). Liên quan [[dungonrush-boss-rush-model]], [[dungonrush-hero-anim-unit-all]].

---
name: dungonrush-hero-anim-unit-all
description: "00Hero đã đồng bộ rig theo _ExportTemp/unit_base_all (Animator unit_all.controller); gốc main = chân, scale 1.68 dời xuống body/weapon; chưa có code gọi Play state"
metadata:
  node_type: memory
  type: project
  originSessionId: ad4ec583-669b-4420-b156-47759a387850
  modified: 2026-10-15T06:56:56.885Z
---

2026-10-15: user muốn dùng anim của `_Assets/_ExportTemp/unit_base_all.prefab` cho `Prefabs/Units/Hero/00Hero.prefab`. Đã sửa YAML trực tiếp (UnityMCP rớt), chưa xem trong Unity, chưa commit.

- 2026-10-21 (commit 6fe0172): controller chỉ còn 10 state (UnitShow/Idle/Run/Die/Celebrate, strike/gun/push/smash/throw_atk); đã XOÁ multi_hit/multi_knife/multi_strike/dot_chainsaw/dot_fire/gun_high_speed/sniper (state+overrideController+clip; prefab _ExportTemp tương ứng mất controller). 10 clip đã chuyển sang `_Assets/Animations/HeroesNew/` (giữ GUID). 00Enemy cũng dùng unit_all.controller.
- Animator trên `Soldier` → `_QuackBag/Animators/Battle/Runtime/unit_all.controller` (bật lên). Controller KHÔNG có parameter/transition, default = `UnitIdle` → chuyển state bằng `Animator.Play` theo tên.
- Code (2026-10-15, compile sạch, CHƯA test Play): `Scripts/Unit/UnitAnimatorController.cs` (kế thừa stub AnimationController, gắn trên root 00Hero) — Renew→UnitShow rồi tự về UnitIdle; Idle/Run theo PlayAnimIdle/Move của BaseUnit; đánh: `isRangedAttack` (BaseUnit.PlayAnimAttack gán = hasProjectile && bulletPrefab) → gun_atk, còn lại strike_atk (user chốt tạm: gần=strike, xa=gun). Theo dõi state đã yêu cầu trong biến riêng vì ngay frame gọi Play thì GetCurrentAnimatorStateInfo vẫn trả state cũ.
- Lật hướng (2026-10-15): `isFacingRight` thành virtual; UnitAnimatorController lật localScale.x của Soldier (art mặc định quay phải). BaseUnit.BeginAttack gọi SetFacingDirection(GetDirectionToTarget()) đầu mỗi nhịp đánh → quay về phía đối thủ (isEnableFaceDirection vẫn false nên chỉ quay lúc bắt đầu đánh + lúc di chuyển).
- Lỗi có sẵn: 00Hero để FlipPoints(+health-bar bake) DƯỚI Soldier, nhưng BaseUnit tìm `FlipPoints` ở con trực tiếp của root → runtime tạo FlipPoints + health-bar thứ 2; bar bake dưới Soldier không bao giờ Init.
- Path của clip: `main`, `main/body`, `main/weapon`, `main/weapon/weapon_slot`, `main/weapon/weapon_slot_2`, `shadow` (phân biệt hoa thường) → đã đổi tên Body/Weapon/Weapon_Slot sang chữ thường, thêm `shadow/shadow_slot`, `weapon_slot_2` (tắt), `launch_point`.
- Clip ghi đè **scale của `main`** và **vị trí của `main/weapon`** = (-0.2,0.4) → scale 1.68 cũ của main chuyển xuống `body` + `weapon`. Gốc `main` phải ở CHÂN (rung theo kiểu squash quanh chân): Soldier y −0.7476, art con của body +0.445, bù +0.7476 cho collider/FlipPoints/Particles. 0.445 = nửa chiều cao Hero.png (ước lượng).
- Clip đánh bắn event `AE_Attack`/`AE_AttackStep`/`AE_AttackWindowBegin/End` vào GO có Animator (Soldier) → `UnitAnimationEventRelay` đã thêm AE_* no-op (UnitAnimatorController tự AddComponent lên Soldier); sát thương vẫn ra ở cuối nhịp (OnAttackEnd), CHƯA theo AE_Attack.

**Why:** sprite gốc có pivot ở chân/chuôi vũ khí, còn art hero reskin có pivot giữa → vị trí vũ khí và tâm xoay có thể cần chỉnh tay.
**How to apply:** khi làm anim cho hero (hoặc enemy theo cùng rig) thì đọc file này; xem thêm [[dungonrush-baseunit-rig-mismatch]], [[dungonrush-hero-equipment-reskin]].

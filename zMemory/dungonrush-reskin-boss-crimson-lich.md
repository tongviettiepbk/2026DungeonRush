---
name: dungonrush-reskin-boss-crimson-lich
description: "Boss Rush Crimson Lich reskin thành heo rừng cầm chùy (2026-11-03) — khuôn cho boss vũ khí cầm tay: WeaponRoot lật gương, clip gõ chùy; test Play OK, đã commit trên nhánh reskin-boss-rush"
metadata:
  node_type: memory
  type: project
  originSessionId: 8c4a14c1-01f2-4dbc-95fd-7e4b589b1743
  modified: 2026-11-02T17:45:52.757Z
---

2026-11-03: art `Art/4_Characters/BossNew/BossRushCrimsonLich/` (Boss 2.png heo rừng, Boss 2 - weapon.png chùy gai, pivot chùy đổi về cán 0.18/0.15). Prefab `BossRushCrimsonLich.prefab` lưu đè giữ GUID. Anim `Animations/BossNew/BossRushCrimsonLich/` (controller + Idle/Move chép của BlackDragon).

- Rig: `Soldier` (đã bỏ xoay 180° của rig Lich cũ) → `Body` → `Main` (y −1.17) → `Sprite` + `WeaponRoot` → `Weapon` (1.0, 0.6) → `weapon_slot` (0.46, 0.84 = tâm đầu chùy). FlipPoints y 0.4 để thanh máu nằm trên đầu.
- `BossRushBossUnit.weaponMirror` (mới) = WeaponRoot: FaceTarget đổi dấu scale x cả nhánh. Không lật bằng scale chính node Weapon vì khi đó chiều xoay của clip không đối xứng.
- Attack 0.5s: Weapon xoay z 0 → +20@0.18 → −16@0.3 (event OnAttackAnimationHit, đạn RedOrb gốc bay ra) → 0@0.5. Skill Slam giữ anim Idle + FX có sẵn; damage/tầm/data giữ nguyên.
- Bẫy khi chép SpriteRenderer từ rig BlackDragon: Weapon của rồng có flipX = true, chép sang phải tắt. Rig rip cũ có Soldier xoay 180°, phải đưa về 0 không thì hình lộn ngược.
- Art/SO cũ Lych2 CHƯA xoá (user chưa yêu cầu).

**Why:** user reskin từng boss Boss Rush; boss có vũ khí cầm tay khác boss quả cầu [[dungonrush-reskin-boss-black-dragon]].
**How to apply:** boss cầm vũ khí thì làm theo khuôn này (WeaponRoot + weaponMirror, pivot ở cán, không dùng fireOrb trừ skill FireBreath). Test: Play, bấm btTapToPlay, `BossRushFightSession.Begin` + `ChangeMode(BossRush)`; popup Combat Summary che view thì SetActive(false), không bấm nhận thưởng; ghi nhịp đánh bằng EditorApplication.update.

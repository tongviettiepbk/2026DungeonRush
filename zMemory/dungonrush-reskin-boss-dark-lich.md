---
name: dungonrush-reskin-boss-dark-lich
description: "Boss Rush Dark Lich reskin thành gấu cầm búa (2026-11-03) — chép khuôn Crimson Lich, skill Slam giữ Idle + FX ô 0; test Play ô thật OK, đã commit trên nhánh reskin-boss-rush"
metadata:
  node_type: memory
  type: project
---

2026-11-03: art `Art/4_Characters/BossNew/BossRushDarkLich/` (Boss 1.png gấu, Boss 1 - weapon.png búa, pivot búa đổi về cán 0.18/0.15). Prefab `BossRushDarkLich.prefab` lưu đè giữ GUID, ô index 0 (Boss Number 1). Anim `Animations/BossNew/BossRushDarkLich/` chép bộ Crimson Lich (Attack gõ ±z 20/−16, event hit @0.3).

- Rig giống hệt [[dungonrush-reskin-boss-crimson-lich]]: Main y −1.17, Sprite y 1.17, WeaponRoot → Weapon (1.0, 0.6), FlipPoints y 0.4, weaponMirror = WeaponRoot. Khác duy nhất: weapon_slot (0.6, 1.0) = tâm đầu búa (ước lượng từ ảnh).
- Skill Slam: không đổi code, giữ anim Idle + FX có sẵn của ô (vòng xanh báo trước rồi nổ xanh). User chốt "skill dùng fx là được". Đạn thường giữ LychWeaponData.
- Test: đủ Idle/Move/Attack, 5 lần Slam trong 40s, quay trái lật cả nhánh búa (mirror −1, slot x −1.92), console sạch. Đầu búa chạm mõm gấu ở khung gồng (z +20).
- Art/SO cũ của Dark Lich đã xoá 2026-11-03 (xem [[dungonrush-reskin-boss-rest]]).

**Why:** user reskin từng boss Boss Rush.
**How to apply:** Elder Lich và boss cầm vũ khí khác chép từ Crimson như vậy; đặt Body scale về 1 ngay trước khi lưu, đọc YAML kiểm lại ([[dungonrush-reskin-boss-dragon]]).

---
name: dungonrush-reskin-boss-rest
description: "Boss Rush đã reskin ĐỦ 10 boss (2026-11-03): 6 boss cuối gồm Elder Lich, Green Dragon, 2 Ogre, 2 Hag theo 2 khuôn cầu/vũ khí, kèm bảng số rig; test Play ô thật OK, đã commit trên nhánh reskin-boss-rush"
metadata:
  node_type: memory
  type: project
---

2026-11-03: xong 6 boss cuối, mỗi boss art `Art/4_Characters/BossNew/<Prefab>/`, anim `Animations/BossNew/<Prefab>/`, prefab lưu đè giữ GUID. Damage/tầm/đạn/data giữ nguyên.

| Prefab | Nhân vật + vũ khí | Khuôn | Main y | Pivot vũ khí | Weapon | weapon_slot | FlipPoints y |
|---|---|---|---|---|---|---|---|
| ElderLich | thỏ + gậy trăng | vũ khí | −1.43 | .205/.15 | 1.04, .6 | .42, 1.31 | .72 |
| GreenDragon | sói + cầu lửa xanh | cầu (fireOrb) | −1.22 | giữa | 1.07, 2.51 | 0, 0 | .26 |
| GreenHag | dê + trượng đá cam | vũ khí | −1.23 | .274/.15 | .96, .6 | .13, 1.54 | .35 |
| OgreChieftain | linh cẩu + dao phay | vũ khí | −1.17 | .148/.18 | .93, .6 | .65, .5 | .76 |
| OgreKing | hổ + mã tấu | vũ khí | −1.17 | .147/.103 | 1.03, .6 | .69, 1.01 | .76 |
| PurpleHag | rùa + trượng cầu xanh | vũ khí | −1.1 | .205/.15 | 1.32, .6 | .25, 1.23 | .21 |

- Công thức (đo pixel bằng script zlib, máy không có PIL): pivot = tâm cán ở ~15% từ dưới; Weapon x = mép phải thân ở cao 60px + 0.36; FlipPoints y = bodyScale × nửa cao + 0.1 − 1.1 để thanh máu trên đầu.
- Skill không sửa code, thân Idle: Slam FX ô (Elder tím), Earthquake vòng đất (Ogre), Teleport thu nhỏ Body + nổ sáng (Hag), FireBreath cầu bay vòng (sói).
- Test: 1 Play chạy lần lượt Boss Number 3/6/7/8/9/10, mỗi con 26s (Begin + ChangeMode, ExitMidFight rồi đợi isChangingMode). Cả 6: đúng controller, đủ Idle/Move/Attack, 3 lần skill, quay trái OK, console sạch.
- Còn lưu ý: khung gồng +20° trượng dài (dê, rùa, thỏ) che mặt thoáng qua; thanh máu nhóm Black/Red/Green Dragon ở .26 nằm trên trán, chưa chỉnh để 3 con giống nhau.

**Why:** user yêu cầu reskin hết boss còn lại theo [[dungonrush-reskin-boss-black-dragon]] và [[dungonrush-reskin-boss-crimson-lich]].
**How to apply:** đủ 10 ô bossPrefabs đã có hình mới ([[dungonrush-reskin-boss-dragon]], [[dungonrush-reskin-boss-dark-lich]]); chỉnh vị trí vũ khí thì sửa prefab từng boss, không chạy lại khuôn.

**Dọn art 2026-11-03:** đã xoá prefab BossRushDragon + art/anim của nó (không có trong bossPrefabs), cả thư mục `Art/4_Characters/BossRush/` (33 PNG rig cũ), 18 SO Glove/Helmet của boss ở GearsEnemy (Lych/Ogre/Witch/Green+RedDragon — không prefab nào trỏ, code không nạp thư mục này). GIỮ: 13 WeaponData trong GearsEnemy (boss vẫn dùng làm đạn), Glove/Helmet + art của Dragon/Zombie/Cultist (quái thường), override controller cũ Lich/Ogre/Witch (0 tham chiếu, không phải art, chưa xoá). Đã commit trên nhánh reskin-boss-rush.

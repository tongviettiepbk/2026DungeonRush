---
name: dungonrush-reskin-boss-dragon
description: "Boss Rush Red Dragon reskin thành cáo + cầu lửa (2026-11-03), test Play ô thật OK; đã commit vào main; BossRushDragon.prefab thừa đã XOÁ"
metadata:
  node_type: memory
  type: project
---

2026-11-03: 2 prefab cùng art Boss 9 (cáo áo choàng đỏ + cầu lửa, 188x244 / 104x104 bằng BlackDragon nên số rig giống hệt), mỗi prefab art + anim riêng:
- `BossRushRedDragon.prefab` — boss thật, ô index 3 (Boss Number 4). Art `Art/4_Characters/BossNew/BossRushRedDragon/`, anim `Animations/BossNew/BossRushRedDragon/`. Đạn giữ RedDragonWeaponData.
- `BossRushDragon.prefab` — ĐÃ XOÁ 2026-11-03 cùng art/anim; trước đó KHÔNG nằm trong `BossRushMode.bossPrefabs` / `StaticBossRushData.bosses`, code không gọi tên → không xuất hiện trong game. Art/anim ở thư mục cùng tên, đạn DragonWeaponData.

Cả 2: lưu đè giữ GUID, chép khuôn [[dungonrush-reskin-boss-black-dragon]] (Main y −1.22, Weapon 1.07/2.51, FlipPoints y 0.26), fireOrb = Weapon, Weapon flipX tắt, Soldier hết xoay 180°.

- Test Red Dragon ở ô thật: Body 1.5, đủ Idle/Move/Attack, cầu phình 1.45, ảnh chụp thấy cầu bay vòng phun lửa FX của ô, quay trái flipX + cầu x −1.07, console sạch.
- Bẫy 1: 8 prefab boss có `m_Mesh` trỏ GUID của BossRushDragon nhưng mesh Cloak là "Skeleton Prefab Mesh" sinh lúc import, nằm riêng từng prefab (DontSaveInEditor, không có trong YAML). Xoá Cloak không làm hỏng boss khác: đã ForceUpdate import các prefab còn lại, mesh vẫn đủ.
- Bẫy 2: rig rip có Body scale 0.5. Với Dragon, đặt 1 giữa chừng rồi lưu thì vẫn 0.5 (chưa rõ nguyên nhân); với Red, đặt ngay trước SaveAsPrefabAsset thì đúng. Sau khi dựng rig luôn đọc YAML kiểm scale.
- Đừng gọi `GameController.Instance` lúc còn ở màn Login: Singleton tự AddComponent → NullReference ở InitGame (lỗi do lệnh dò, không phải game).

**Why:** user reskin từng boss Boss Rush.
**How to apply:** boss quả cầu còn lại (Green Dragon...) làm y khuôn này, đặt Body scale ngay trước khi lưu. Liên quan [[dungonrush-boss-rush-model]], [[dungonrush-boss-rush-tester]].

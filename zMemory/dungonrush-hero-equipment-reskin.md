---
name: dungonrush-hero-equipment-reskin
description: Reskin đồ hero (concept vịt) — ảnh gốc 8 slot đã gom về Art/3_Equipment/; brief art ở GDD/ART_HERO_EQUIPMENT.md; mapping slot cũ→đồ mới
metadata:
  node_type: memory
  type: project
  originSessionId: 80f48afc-66cc-4da6-b804-ba63ea63437d
  modified: 2026-10-15T04:20:08.320Z
---

2026-10-15: gom ảnh icon của 241 món (8 slot) + 8 icon ô trống về `_Assets/Art/3_Equipment/` (00_EmptySlot, 01_Weapon/Melee|Ranged, 02_Helmet … 08_Wing) bằng `git mv` kèm .meta → GUID giữ nguyên, SO vẫn trỏ đúng. Thư mục cũ `BGGearEmpty/`, `Items/Ring/` đã xoá; `GearIcons/` còn 22 ảnh không SO nào dùng, `WeaponIcons/` còn 1.

Brief cho đội art: `GDD/ART_HERO_EQUIPMENT.md` + `.html` (ảnh nhúng base64, gửi 1 file), ảnh bảng xem trước ở `GDD/img/hero_equipment/`.

Mapping slot cũ → đồ mới (suy từ tên node prefab 00Hero, user chưa xác nhận bằng lời): Weapon→dao, Helmet→mũ, Gloves→áo (node Hand_Clothes), Backpack→balo, Wing→kính (Wing_Glass), Cape→tẩu (Cap_Pipes); Ring/Necklace chưa có concept. Art mới ở `ArtIncoming/_Concept/Items/<loại>/<loại>-LXX-A.png (L01-A đang dùng ở Art/3_Equipment/OnBody/)` (6 loại × 10 tier, mới mẫu A).

**Why:** user chuẩn bị gửi đội art vẽ lại toàn bộ đồ hero.

**How to apply:** tool cũ `tools/gen_gear_assets.py`, `gen_cape_wing_assets.py`, `attach_weapon_icons.py` vẫn trỏ ICON_DIR cũ (GearIcons/WeaponIcons) — chạy lại phải sửa đường dẫn. Khi art giao: gán vào field `bodySprite` (ảnh gắn người) / `icon` của SO, xem [[dungonrush-equipment-system]]. Chưa chốt: vũ khí tầm xa, nhẫn/dây chuyền, Cape 6 tier vs tẩu 10 tier.

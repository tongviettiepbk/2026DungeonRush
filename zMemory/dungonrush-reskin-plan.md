---
name: dungonrush-reskin-plan
description: Reskin toàn game (concept vịt) — CHỈ thay hình giữ bố cục; đợt 0 XONG trên nhánh reskin: _ResourceGame→Art/<1_UIKit..7_FX>, ArtIncoming/ (gương) + ArtArchive/, lệnh Tools/DungeonRush/Reskin/Apply Reskin
metadata:
  type: project
---
Kế hoạch: `GDD/RESKIN_PLAN.md`. User chốt 2026-10-18: reskin CHỈ thay hình, GIỮ bố cục UI hiện tại; duyệt cấu trúc thư mục.

Đợt 0 xong 2026-10-18 trên nhánh `reskin` (tag `pre-reskin` = trạng thái trước khi chuyển, commit c9eab7a trên main):
- `_ResourceGame` + `Graphics/Ingame` → `Assets/_Assets/Art/` chia tầng 1 theo mảng art: 1_UIKit (Common/PetUI/Rarity/HUD), 2_Icons (… Flags/Language = 10 cờ ngôn ngữ), 3_Equipment (00_EmptySlot..08_Wing + OnBody/InGame/Projectiles/GearIcons/…), 4_Characters (Hero/Enemies/Pets), 5_Maps (Campaign[+Materials,Shaders]/Dungeon*/Mining), 6_Features (Clan/BossRush/PvP/Forge/Misc), 7_FX. Bảng cũ→mới ở mục 3.2 của GDD.
- Ngoài Assets (gốc repo): `ArtIncoming/` (cây gương + `_Concept` = _ArtReskin cũ, `_Reference` = sheets/CSV/báo cáo, README cho art), `ArtArchive/` (_CommingUse 656 ảnh không dùng + HeroEquipment.zip).
- Kiểm: 706 ảnh đang dùng trước/sau, 0 GUID gãy mới, Play Root→Login→MainGame 0 lỗi. 12 builder + 8 tool đã sửa đường dẫn. Đã xoá object `unit_base_all` (tắt) khỏi Root.unity.
- Tool: `Scripts/Art/Editor/ArtReskinTool.cs` (menu Tools/DungeonRush/Reskin: Kiểm tra / Apply Reskin / Xuất CSV + thư mục gương), `tools/art_contact_sheets.py` (chạy bằng tools/.venv). Apply = ghi đè PNG cùng đường dẫn, giữ .meta, nhân PPU + viền 9-slice theo tỉ lệ ảnh mới/cũ (giữ kích thước hiển thị), bỏ qua thư mục `_`*, idempotent.

**Why:** user chuẩn bị reskin cả game theo mockup `ArtIncoming/_Concept/Lobby/lobby demo.png`.

**How to apply:** ảnh art mới → thả vào `ArtIncoming/<đường dẫn y hệt Art/>` → Apply Reskin → đọc reskin_report.txt. Ảnh mới gán tay vào `Art/`. KHÔNG chạy lại builder prefab cũ sau khi reskin (ghi đè chỉnh tay). Chuyển asset trong Unity: KHÔNG tạo thư mục trong StartAssetEditing (Unity sinh "X 1", "X 2"... thừa). Chưa commit đợt 0 (chờ user). Xem [[dungonrush-hero-equipment-reskin]], [[dungonrush-resource-dedupe-rule]].

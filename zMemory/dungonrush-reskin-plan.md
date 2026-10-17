---
name: dungonrush-reskin-plan
description: Kế hoạch reskin toàn game (concept vịt) ở GDD/RESKIN_PLAN.md — "thay ruột giữ .meta", đề xuất Art/ + ArtIncoming/ + ArtArchive/, 8 đợt; CHỜ user duyệt
metadata:
  type: project
---
2026-10-18 viết `GDD/RESKIN_PLAN.md`. Số đo: 1477 ảnh trong _Assets, 706 đang dùng lúc chạy (lần GUID từ 3 scene build + Resources/), 656 ảnh không dùng nằm ở `_ResourceGame/_CommingUse` nhưng 19 ảnh trong đó LẠI đang dùng (10 cờ, 8 Mining, 1 diamond). Code không nạp ảnh theo đường dẫn → chuyển thư mục/ghi đè PNG không phải sửa code; chỉ 12 `*PrefabBuilder.cs` + 8 tool trong `tools/` có hằng `_ResourceGame/...`. Root.unity đang trỏ `_ExportTemp/unit_base_all.prefab`.

Đề xuất (CHƯA duyệt): đổi `_ResourceGame` → `Art/` chia tầng 1 theo mảng art (1_UIKit, 2_Icons, 3_Equipment, 4_Characters, 5_Maps, 6_Features, 7_FX); `ArtIncoming/` (gốc repo, cây y hệt Art/) + lệnh Editor `Apply Reskin` ghi đè PNG giữ .meta; `ArtArchive/` ngoài Assets. Thứ tự: dọn → style guide → UI kit+font → icon → đồ hero → nhân vật → map → FX/Clan/app icon.

**Why:** user chuẩn bị reskin cả game theo mockup `_ArtReskin/Lobby/lobby demo.png` (bố cục khác hiện tại: 5 tab, 8 ô đồ ở lobby).

**How to apply:** chưa làm đợt 0 nào; chờ user trả lời mục 7.1 (chỉ thay hình hay đổi bố cục, duyệt cấu trúc thư mục). Chuyển ảnh bằng AssetDatabase.MoveAsset qua UnityMCP, đếm lại ảnh đang dùng trước/sau. Xem [[dungonrush-hero-equipment-reskin]], [[dungonrush-resource-dedupe-rule]], [[dungonrush-uinew-prefab-folder]].

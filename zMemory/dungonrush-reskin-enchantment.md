---
name: dungonrush-reskin-enchantment
description: "Reskin Enchantment 2026-10-27 (CHƯA commit, chưa test Play) — trang PageEnchantment trong UIMainGame2 + 4 popup theo mockup _Concept/Enchantment; kit 1_UIKit/Enchantment; ChatGPT vẽ 3 hình ô trống kính/tẩu/áo"
metadata:
  node_type: memory
  type: project
  modified: 2026-10-27T10:38:43.236Z
  originSessionId: dd7b7f89-1bb8-4299-8785-6239b5faa85b
---

Reskin 2026-10-27 theo mockup `ArtIncoming/_Concept/Enchantment/` (3 demo + summon; `_art/ribbon_red.png`). Ảnh so: `compare_page.png`, `compare_popups.png`, `popups_game.png`; chụp ở `cap/`. CHƯA commit, chỉ chụp preview (UICaptureTool), CHƯA test Play.

- Trang = `SafeArea/Pages/PageEnchantment` NẰM TRONG `Prefabs/UI/UIMainGame2.prefab` (không phải prefab riêng). Mockup top bar cao hơn game 63px, nav thấp hơn 35px → phần trên neo ĐỈNH (y mockup −63), khu dưới `Bottom` neo đáy cao 386 (y +35), kho `Inventory` giãn giữa. Nền #403F51 / khu dưới #302C3D / vạch #18161F; panel = Mastery/mastery_panel; nút = Lobby/btn_green; thanh = Companion/bar_track + Lobby/xp_fill; X = Mastery/btn_close.
- Ô vuông VÀNG cạnh Summon trong mockup = nút multiplier (`EnchantmentTabPageMultiplierButton`, chỉ hiện khi Vial ≥ 2000) → dời sang TRÁI Summon, sprite `1_UIKit/Enchantment/btn_yellow` (fit màu từ btn_green, `tools/reskin/gen_enchantment.py`). Ô Vial: thêm node `VialIcon` (vial_icon) — mockup để trống.
- Sửa lỗi có sẵn: `PatternMask` trong 8 slot thiếu component Mask → ảnh trắng đè nền rarity + hình ô trống; đã thêm Mask showMaskGraphic=false (như slot lobby).
- 4 popup Resources dùng kit Mastery (frame/header/X/btn_blue/box_tan): UiEnchantmentInfoItem (822x696 theo mockup demo 3), UiSumonEnchantment (nền #403F51, ô 148 cách 44, 5 cột, top 266), UiEnchantmentMerge + uiUpgradeEnchantment (không mockup → đồng bộ kit; mũi tên = `1_UIKit/Enchantment/arrow_right` = Gameplay/arrow_up xoay).
- Hình ô trống Wing/Cape/Gloves (`3_Equipment/00_EmptySlot/empty_icon_{wing,cloak,gloves}_transparent.png`) GHI ĐÈ thành kính/tẩu/áo (ChatGPT qua Codex, prompt `_Concept/Enchantment/gen/`, nền xanh lá → bảng xám 69/94/150 alpha 140); dùng chung lobby + UIPlayerPopup (đều preserveAspect). Áo đổi khung 136x136.
- BẪY: `ArtIncoming/` đang chứa ~207 ảnh cũ lệch `Art/` (đồ trang bị…) → KHÔNG chạy ArtReskinTool.Run(true); chỉ chép từng file.
- Mockup có mà bỏ: popup "Enchantment Offer" (game chưa có tính năng; gốc có EnchantmentOfferPopup).

Xem [[dungonrush-reskin-plan]], [[dungonrush-reskin-ui-howto]], [[dungonrush-enchantment-system]].

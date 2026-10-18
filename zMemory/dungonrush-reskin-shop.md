---
name: dungonrush-reskin-shop
description: "Reskin Shop (StoreTabPage) theo mockup _Concept/Shop 2026-10-18 — khung art giao gán prefab, ChatGPT vẽ lại 19 icon (rương đóng/mở, cuốc/khoan, chìa, exp, vương miện, thùng, xương) + 6 gói gem; ghi đè giữ đúng pixel size ảnh cũ"
metadata:
  node_type: memory
  type: project
  originSessionId: 6ba134aa-2733-407f-aee4-25706a8fa4c8
  modified: 2026-10-18T16:45:17.927Z
---

Reskin Shop 2026-10-18 (đã commit, CHƯA test bấm mua), mockup `ArtIncoming/_Concept/Shop/` (shop1/2/3 demo + `_art/` 13 ảnh art giao). Ảnh so: `_Concept/Shop/compare_mockup_vs_game.png`.

- Ảnh khung: `tools/reskin/gen_shop.py` → `Art/1_UIKit/Shop/` (card_boost, deal_orange/purple, chest_card, card_tan đầu vàng 308x422 viền trên 118, gem_card 268x378 viền trên 96, ribbon = Rounded Rect 12 đệm 25px trong suốt mỗi bên, reward_slot = đen 20% đã bù alpha Linear, gem_pack_1..6). Nút = `Lobby/btn_green`.
- Prefab: `Resources/Prefabs/UI/StoreTabPage` (padding trang 37 → thẻ 1006; nền #403F51; ô deal 1 cam, 2-3 tím theo shop1; lưới thưởng 244x58 cách 7x10, cao deal 302/370 khi 5 thưởng; gói gem thêm node PackIcon, tiêu đề bỏ <sprite>), `UINew/Shop/Chest_Single_Product_Vertical` (466x608), `..._Horizontal` (rương Legendary+ = thẻ cam 1006x302, nút i góc trên phải theo shop2), `DailyDealRewardEntryUI`.
- Bỏ theo mockup (game không có): VIP CLUB / ELITE CLUB, Skip'its (dấu tích + Rounded Rect 13, Layer 73, Layer 64, exec-…). Gói tắt (NoAds/Starter/Epic/DungeonDeal/ResourceDeal) không đụng.
- Icon ChatGPT (`_Concept/Shop/gen/` prompts/prompts2/prompts3; rương mở nắp + legendary_chest vẽ bằng ref = rương mới "edit…"; mythic_chest = bản chest_offer_01 mới): ghi đè TẠI CHỖ qua ArtIncoming + Apply Reskin, ảnh làm bằng `tools/reskin/fit_icon_to_old.py <gen> <ảnh_cũ_gốc> <out>` (giữ đúng W×H + hộp bao hình cũ → không đổi PPU, không phải sửa glyph TMP sprite asset IsDrill/IsRedKey/...). Ảnh cũ gốc lấy bằng `git show 3e179aa:<path>` (commit ngay trước reskin Shop) (Art/ đã bị ghi đè thì đừng dùng làm mốc). Đổi cả game (Mining, lobby Loot dùng box_icon, crown = Cloak...).
- BẪY tách nền: công thức magenta cũ (`key_magenta_icon.py`) làm MÀU TÍM (gem, rương mythic) thành bán trong suốt ~60% → `fit_icon_to_old.key_magenta` chỉ coi nền là điểm r>200,b>200,g<70, khử viền trong dải 3px sát nền.
- KHÔNG chạy lại `Tools/DungeonRush/Build Shop UI Prefabs` (ghi đè reskin).
- Chưa reskin: các popup Shop (ChestInfo/ChestReveal/ChestBundleOffer/RewardedChest/Offer) — chỉ đổi icon rương.

Xem [[dungonrush-reskin-ui-howto]], [[dungonrush-reskin-plan]], [[dungonrush-shop-system]].

---
name: dungonrush-forge-auto-loot
description: "Loot + Auto Loot dựng lại theo gốc 2026-10-15 — ForgeUIController ở lobby, popup gốc NewItemPopup/ItemInfoPopup/AutoForgePopup, bán đồ = EXP; doc DecodedData/FORGE_MODEL.md"
metadata:
  node_type: memory
  type: project
  originSessionId: a7a13290-8879-4f3d-a10b-9bf6dc65113c
  modified: 2026-10-15T11:03:10.476Z
---

Dựng 2026-10-15 theo reverse libil2cpp (doc: `DecodedData/FORGE_MODEL.md`). User chốt: làm ĐỦ như gốc, bán đồ cộng EXP, popup 1 thẻ.
- Lobby: `ForgeUIController` gắn ở `UIMainGame2/.../BottomUI/ForgeUI` (bục ItemList, nút Loot/Auto); UIMainLobby chỉ còn `forgeUI`, `RefreshEquipment()`, `RefreshExp()`.
- Luồng: tiêu N hộp (LOOT_TICKET) → N món lên bục → từng món: auto + `ForgeService.ShouldDiscard` → bán (EXP [1,2,4,6,8,10,12,14] qua `PlayerExpService.Grant`, có boost); slot TRỐNG → UINewItemPopup (chỉ Equip vào hero); slot CÓ ĐỒ → UINewForgePopup (2 thẻ; Equip = mặc + đổi chỗ 2 thẻ trong popup; Sell = bán, rarity cao hơn đồ đang mặc thì hỏi xác nhận). User từng báo lỗi khi gộp chung 1 popup — 2 popup 2 nhiệm vụ.
- Save mới `UserForgeData` (key_user_forge): selectedAutoHammerCount, disabledRarities, subStatFilterEnabled, enabledSubStats. AutoSellLowerLevel gốc là field chết → không port.
- Số hộp/lượt: lv≥6 → Mastery AutoLootHammerCount (chưa mở = 1); lv<6 → mốc [0,10..70]. Lọc substat mở lv7; nút Auto hiện lv2, dùng lv3.
- Prefab dựng bằng `Tools/DungeonRush/Build Forge UI/All` (ForgePrefabBuilder): Resources UINewItemPopup / UINewForgePopup / UIItemInfoPopup (UIKey.InfoGear, class UIGearInfo) / UIAutoForgePopup + `Prefabs/UINew/Forge/ItemElementUI`. Chạy lại = ghi đè.
- Chữ trong popup đồ để TIẾNG ANH: font prefab gốc thiếu dấu tiếng Việt ("Mũ" → "M0").
- Đã xoá UiInfoGer / UiLootGearInfo / UILootGearInfo.cs tự dựng. Test Play pass (loot tay, equip-đổi đồ, bán, auto dừng ở món giữ); đã commit 03dadd7.

- Ô item (ItemElementView): nền theo `RarityBackgroundConfig` + "Lv.N"; 3 rarity cuối có FX pattern chạy (2026-10-16): `RarityPatternEffect` (port gốc, DOTween: Ancient trượt xuống 128.5, Immortal xoay 360, Divine trượt chéo 128, 5s Linear lặp) + `Mask` thay SoftMask gốc. Vá prefab đã dựng bằng menu `Build Forge UI/Patch Rarity FX` (không ghi đè layout). 8 ô đồ đang mặc ở lobby (ElementEquipmentUILobby, node `PatternMask`) cũng đã có nền rarity + FX; ô trống = nền Common. Chưa test Play.

- NewForgePopup có mũi tên so chỉ số chính (2026-10-16): thẻ phải > đồ đang mặc = `stat_arrow_up` xanh, < = `stat_arrow_down` đỏ, bằng = ẩn; sprite cắt từ `_CommingUse/UI/stat_arrow.png` ra `_ResourceGame/UI/`. Node `CompareArrow` con của BaseStatText (builder thêm).
- BẪY: chạy builder khi Unity đang PLAY → AddComponent gọi Awake với field rỗng → NRE → component popup bị lưu `m_Enabled: 0`. Dựng prefab ở Edit mode.

- Anim/FX nút Loot (2026-10-16, reverse ForgeUI.idn/ied/iee/ids/iei/iem + ForgeButton.ibt/ibu): ForgeUIController thêm state PlayingAnimation — hộp `Box` 0.8→1 OutBack 0.15s → chờ 0.4 → tiếng BoxPunch + PunchRotation(0,0,15) 0.4s → chờ 0.2 → tiếng BoxOpen, ẩn hộp, particle `LootBoxUIParticle` → món nảy 0→1 (OutBack 0.05) + `ItemGlowImage` màu rarity alpha 0.24; bán → particle exp `CoinUIParticle` (chặn 0.1s); gỡ món thu về 0 InBack 0.05. Nút `ForgeButtonView` (port ForgeButton) lún xám suốt lượt loot/auto, nhả khi rảnh; rảnh 3s → vệt sáng `MovingImage` chạy +500 trong 0.4s, nghỉ 2s, lặp (Mask thay SoftMask, node Mask đã bật). Test Play pass.
- Particle không có UIParticle: root LootBox scale 50 / Coin scale 250 (= Scale3D gốc), renderer sorting = root canvas +1 (set lúc Play). BẪY: renderer bản rip bị `enabled=false` + Coin thiếu material → đã tạo `Prefabs/Fx/Materials/exp_AB.mat`, TSA sprite = box-loot. Âm thanh gốc (AudioConfig): BoxPunch = footstep_wood_wobble_03, BoxOpen = wood_tree_branch_break_03 (đã có trong _Sound).

**Why:** user muốn bỏ 2 prefab tự dựng, dùng prefab gốc và hoàn thiện auto loot.
**How to apply:** sửa loot thì sửa ForgeUIController/ForgeService; đổi layout popup bằng tay thì đừng chạy lại builder. Xem [[dungonrush-loot-forge-design]], [[dungonrush-main-ui-uimaingame2]], [[dungonrush-mastery-feature]].

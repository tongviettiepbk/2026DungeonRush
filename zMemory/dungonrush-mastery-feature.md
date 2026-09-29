---
name: dungonrush-mastery-feature
description: Hệ Mastery (G1) — nâng cấp META vĩnh viễn bằng Gem; data+service+save+2 hook đã xong
metadata: 
  node_type: memory
  type: project
  originSessionId: 05782737-faab-46cc-83c2-411a347173d2
  modified: 2026-09-29T00:00:00.000Z
---

Hệ Mastery (G1): 10 nhánh buff toàn cục vĩnh viễn, tiêu GEM. Data gốc ở DecodedData/tables/MasteryUpgradeData.json + MasteryConfig.json (chính xác 100%). Làm theo pattern StickIdle/Companion — xem [[dungonrush-follow-stickidle]].

**Đã xong (2026-09-22): Data + Service + Save + nối 2 hook (KHÔNG UI).**
- Code: `Scripts/Mastery/` — MasteryEnums (enum MasteryUpgradeType, giá trị=UpgradeType gốc 1..8,10,11), MasteryUpgradeData (SO), StaticMasteryData (Resources.LoadAll), UserMasteryData:BaseUserData (dict level, có key=unlocked), MasteryService (static: TryUnlock/TryUpgrade tiêu ItemType.GEM, GetCurrentValue).
- Wire: StaticGameData.mastery + UserData.mastery (key `key_user_mastery`, đủ 4 bước). Hook: ForgeController cap += round(GetCurrentValue(ForgeMaxItemLevel)); CompanionSummonConfig.GetCurrentSummonCapacity() đọc mastery.
- Assets: 10 .asset ở Resources/Scriptable Objects/Mastery/ + 9 icon ở _ResourceGame/MasteryIcons/ (MiningMaxPickaxe icon=fileID:0, png "pickaxe" nằm ở AssetRipper Texture2D chưa lấy). Sinh bằng `tools/gen_mastery_assets.py`.
- Compile: `dotnet build 2026DungeonRushUnity/Assembly-CSharp.csproj` sạch (đã thêm 5 file vào csproj — Unity sẽ tự regen).

**✅ ĐÃ REVERSE il2cpp v41 (2026-09-22) — công thức CHỐT:**
- Phí mở khoá nhánh = `MasteryUpgradeData.UnlockGemCost` RIÊNG từng nhánh (MasteryTabPage.ivw đọc field @0x1C; cost<1 → mở MIỄN PHÍ: AutoLoot & AdBoostWorth = 0). KHÔNG phải "theo thứ tự mở".
- `MasteryConfig.iuf` (đọc UnlockGemCosts[count-2], 2 nhánh đầu free) = **DEAD CODE, không nơi nào gọi** → UnlockGemCosts + iuf bỏ hẳn.
- Nâng cấp tier: cost = `iup` = `Levels[].GemCost`; trừ gem qua UserController.dob; MasteryUpgradeDetailPanel.iws.
- Giá trị: `iuo(a)` = a<=0 ? Default : (IsValueAdditive ? Levels[a-1].Value+Default : Levels[a-1].Value). Đã port vào GetValueAtLevel.
- Enum gốc (TypeDefIndex 897): type 0=GemOfferCount, 9=LevelUpRewardWorth (v41 KHÔNG ship 2 type này); còn lại 1..8,10,11. Đã sửa MasteryEnums (trước đó để nhầm None=0).
- Pipeline reverse mac: extract .so+metadata từ xapk → Il2CppDumper (clone GitHub, dotnet build net8, chạy DOTNET_ROLL_FORWARD=Major) → dump.cs/script.json → capstone disasm (offset=VA-0x4000). Artifact nặng (.so/dump) đã xoá khỏi repo sau khi xong.

**Thứ tự mở khoá GỐC (đọc MasteryConfig.Upgrades từ xapk, 2026-09-25):** mở TUẦN TỰ theo list: AutoLoot(0) → AdBoostWorth(0) → GemOfferChance(30) → CompanionSummon(60) → MaxOffline(90) → MaxPickaxe(120) → ForgeMaxItemLevel(150) → AdBoostDuration(180) → OfflineEarning(210) → MoveSpeed(240) — phí = unlockGemCost nhánh kế, tăng dần. Đã chốt vào StaticMasteryData.ORDER.
**UI (2026-09-25):** MasteryUI (lưới + nút mở nhánh kế, cờ isTestFree) + ElementMasteryUI XONG; chưa có click nâng cấp từng ô.

**✅ CHỐT theo ảnh game thật (2026-09-29, 6 ảnh khớp 100%):**
- Mở khoá xong = **Lvl 1** (save level=1, value=levels[0]); max = levels.Count. ValidateData tự nâng save cũ 0→1.
- Phí nâng Lvl k→k+1 = **levels[k-1].gemCost** (entry cấp HIỆN TẠI). VD MaxOffline Lvl1→10, AutoLoot Lvl3→60. (Ghi chú cũ "cost ở cấp đích" là SAI.)
- Hiển thị: prefix+số+suffix, " > " giữa; số nguyên in không lẻ, số lẻ "0.00" theo culture ("x1,10"); text "Lvl N" (loc Common.Level.Abbrev).
- Tên/mô tả lấy localization EN (Items.Mastery.Name/Desc.<enum>) — JSON gốc ghi nhầm MaxPickaxe = "Max Item Level".
- Icon: AutoLoot=mastery_10 (hộp+), MaxPickaxe=AssetRipper mastery_13, ForgeMaxItemLevel=mastery_14 (kiếm+, suy đoán). AutoLoot suffix "<sprite=0> " + TMP_SpriteAsset IsBox (sinh từ rip, _ResourceGame/MasteryIcons). Generator giữ guid khi chạy lại.
- UI: popup MasterUpgradeUI (MateryPopupUpgrade trong MainGame.unity) chỉ mở cho nhánh ĐÃ unlock; thiếu Gem → chữ phí đỏ, nút vẫn bấm (toast).

**Consumer đã nối (rà 2026-09-29):** ForgeMaxItemLevel → ForgeController cap=100+round(v); CompanionSummonCount → CompanionSummonConfig (Lvl1 → 12/16/36 khớp số thật); PlayerMovementSpeed → HeroUnit.CalculateCurrentStats `moveSpeed *= v` (suy từ data prefix x, CHƯA reverse). EventID.MasteryChanged → Hero ReloadStats.

**Chưa làm:** nối các consumer còn lại (AutoLoot/GemOffer/AdBoost/Offline/MoveSpeed/Mining — các hệ đó chưa có trong game).

KHÁC với StatModifierSource.MasteryCommon/MasteryPromotion (MechanicEnums) = bậc mastery của CARD trong battle, không liên quan.

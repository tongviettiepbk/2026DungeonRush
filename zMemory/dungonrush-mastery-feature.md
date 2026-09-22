---
name: dungonrush-mastery-feature
description: Hệ Mastery (G1) — nâng cấp META vĩnh viễn bằng Gem; data+service+save+2 hook đã xong
metadata: 
  node_type: memory
  type: project
  originSessionId: 05782737-faab-46cc-83c2-411a347173d2
  modified: 2026-09-22T09:16:25.172Z
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

**Chưa làm:** UI MasteryPage; nối các consumer còn lại (AutoLoot/GemOffer/AdBoost/Offline/MoveSpeed/Mining — các hệ đó chưa có trong game).

KHÁC với StatModifierSource.MasteryCommon/MasteryPromotion (MechanicEnums) = bậc mastery của CARD trong battle, không liên quan.

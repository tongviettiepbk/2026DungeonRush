---
name: dungonrush-item-level-formula
description: "Công thức Level của gear khi forge/summon (reverse il2cpp) — KHÔNG cố định 1, là hệ nâng cấp theo món đang mặc"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 4095ffb9-8b00-441c-9121-8000f0ded565
  modified: 2026-09-20T02:52:11.026Z
---

Reverse từ libil2cpp.so (APK 41): **Level gear KHÔNG phải hardcode = 1**. Là hệ **forge theo món ĐANG MẶC**.

Luồng (`ForgeController.hel` → `qv.iap` → `qv.iaq`):
- chọn slot ItemType random 0..5; đọc món đang mặc slot đó (`UserController.dww`); `iao()`=ItemId<0 → slot trống.
- base = trống?1:cur.Level ; inRarity = trống?Common:cur.Rarity ; rolledRarity = `iau()` (roll theo ForgeRarityProbabilities+EconomyController.hcq).
- `iaq(base,inRarity,rolledRarity,isEmpty)`:
  - isEmpty HOẶC rolledRarity>inRarity → **Level=ForgeMinLevel=1** (lên rarity thì reset 1)
  - rolledRarity==inRarity → **Level=clamp(base + weightedOffset(rarity), 1, cap)** (cùng rarity → cộng dồn offset)
  - rolledRarity<inRarity → **Random(ForgeLowerRarityMinLevel=90, cap+1)**
  - cap = `iar()` = ForgeMaxLevel(100) + round(MasteryController.iuh(10)); base player = 100.

weightedOffset = `ias(rarity)` weighted-pick từ `iat(rarity)`:
- Common(0):{3:10,6:30,9:40,12:15,15:5} Uncommon(1):{2:10,4:30,6:40,8:15,10:5} Rare(2):{1:10,2:30,3:40,4:15,5:5}
- rarity≥3 (Epic+) → fallback ForgeDefaultLevelOffsets {-2:5,-1:10,0:15,1:20,2:25,3:15,4:10}

Hằng số & bảng đã merge vào DecodedData/gameresources_values.json (ForgeMinLevel/MaxLevel/LowerRarityMinLevel + 2 bảng offset). Doc đầy đủ: DecodedData/ITEM_LEVEL_MODEL.md.

Liên quan: [[dungonrush-item-stats-source]] [[dungonrush-loot-forge-design]] [[dungonrush-reverse-native-il2cpp]]. Đồ nghề reverse chạy lại được trên Mac: dotnet build Il2CppDumper (roll-forward net8→10), venv capstone/lief/UnityPy/TypeTreeGeneratorAPI; xapk gốc còn ở repo root.


**Đã port vào code (2026-09-20):** `ForgeController.RollForgeLevel(baseLevel, inRarity, rolledRarity, isEmpty)` chứa 4 nhánh + bảng offset; `LootService.RollOne` gọi `ComputeForgeLevel(slot, rolledRarity)` đọc `UserEquipmentData.GetRecord(slot)` làm base. Main stat cũng dùng level này. Compile sạch (csc Unity 6000.3.9f1). Khác gốc: rebuild roll rarity→chọn item (theo pool) thay vì chọn slot trước; công thức LEVEL giữ nguyên gốc.

**Verify bằng ảnh in-game thật (2026-09-20):** khớp 100%.
- Level NHÂN thẳng vào MAIN stat: `Main = Base × √10^rarity × (1 + 0.015×level)`. Lv1→×1.015, Lv50→×1.75, Lv94→×2.41, Lv100→×2.5 (trần). Substat (%) KHÔNG dính level.
- Ảnh Gloves: Rare(2) Lv1 = 6×10×1.015 = 60.9→**61** ✓ ; Uncommon(1) Lv94 = 6×√10×2.41 = 45.73→**46** ✓.
- Ảnh cũng xác nhận LEVEL formula: đồ mới Uncommon (thấp hơn Rare đang mặc) ra Lv94 = đúng nhánh "tụt rarity → Random(90,100)"; Rare đang mặc Lv1 = "lên rarity → reset 1".
- **Ảnh hưởng trước fix:** GearStatCalculator + EquipmentStatResolver.AddGear/AddWeapon ĐÃ dùng rec.level sẵn; lỗi là LootService lưu level=1 → Hero tính damage/hp ở level 1 → THẤP hơn thực tế (vd gloves 19.26 thay vì 46, ~2.4 lần). Fix level tự chữa luôn damage/hp Hero.
- **Display:** game gốc hiện main stat LÀM TRÒN NGUYÊN. Đã đổi UIGearInfo.cs:43 + UILootGearInfo.cs:90 từ `ToString("0.##")` → `ToString("0")`. Substat vẫn "0.##" (+28.92%).
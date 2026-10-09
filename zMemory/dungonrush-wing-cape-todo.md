---
name: dungonrush-wing-cape-todo
description: Wing (C8) / Cape (C7) — ĐÃ có data + hình mặc + chỉ số trận + Power + relic; CHƯA có cách nhận/nâng cấp (craft/summon/level/reroll/salvage), save riêng, UI, mở khoá
metadata:
  type: project
---

Trạng thái 2026-10-09:
- ĐÃ CÓ: WingData/CapeData/CapeConfigData (asset Resources/Scriptable Objects/Gears/Wings|Capes), HeroVisual mặc wing/cape, chỉ số trận (EquipmentStatResolver.AddWing/AddCape theo Soldier.ewc/eyv), Power (PlayerPower), relic slot WING/CAPE, snapshot Boss Rush. Wing/Cape lưu chung UserEquipmentData (equipId = wingId/capeId dạng chuỗi số, level, subStats).
- CHƯA CÓ (game gốc có): cách NHẬN và NÂNG Wing/Cape nên hiện người chơi không có Wing/Cape nào.
  - Wing: chế tạo bằng quặng (WingCraftTabPage, CraftOreType/Cost), nhận (WingClaimPage), reroll substat (WingRerollPopup, RerollCosts), lên cấp bằng quặng (LevelUpCost × LevelUpCostMultiplier, MaxLevel), mở khoá GameResources.WingUnlockPlayerLevel. Phụ thuộc hệ Mining (quặng) — chưa có.
  - Cape: triệu hồi bằng CloakCurrency (CapeConfig.SummonCost = 20), salvage cape thừa lấy XP, lên cấp theo công thức XP (CapeRarityConfig), substat (SubStatCount 2), CapeDetailPopup, mở khoá CapeUnlockPlayerLevel / CapeFeatureUnlockedEarly, nút ShowCloak (ẩn/hiện áo choàng).
  - Ô Wing/Cape trên lobby + popup info (UIGearInfo chỉ hỗ trợ weapon/gear qua LootService.BuildFromEquipId — trả null cho Wing/Cape).
- Lúc làm: reverse native các class trên (pipeline [[dungonrush-reverse-native-il2cpp]]), giữ chỉ số dùng chung GearStatCalculator.GetWing*/GetCape*. Xem [[dungonrush-power-model]], [[dungonrush-enchantment-system]].

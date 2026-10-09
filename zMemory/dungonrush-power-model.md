---
name: dungonrush-power-model
description: Power người chơi GỐC (rm.iqm/iqr/iqi) đã reverse + port PlayerPower.cs (lobby, Boss Rush, toast đổi Power relic); HeroUnit đã sửa: đồ THAY nền PlayerBase slot
metadata:
  type: project
---

Power = (ΣDamage món + nền slot TRỐNG + phần thêm) × (1 + Damage% + Melee/Ranged% theo vũ khí) × (ΣHealth + nền slot trống + phần thêm) × (1 + Health%). Phần thêm = relic main×(hệ số−1) + Own Effect pet + Wing×hệ số relic; Cape cho % × hệ số relic. Chỉ substat Damage/Health/Melee/Ranged tính; mastery không tính. Trần = 10×50 = 500. Doc: `DecodedData/POWER_MODEL.md`.

Port 2026-10-09: `Scripts/Player/PlayerPower.cs` (GetCurrent/Calculate/ToLong=rm.iov/NotifyChange=rm.iqq); lobby txtPower (ToLetter, nghe Equipment/Companion/Enchantment changed); BossRushPower giờ = PlayerPower (từ save, không cần Hero); toast đổi Power sau 5 thao tác relic đang đeo.

Đã sửa theo gốc (user chốt 2026-10-09): Soldier.ewc dùng main món THAY nền PlayerBase của slot → EquipmentStatResolver cộng (main × relic − GearStatConfigData.GetPlayerBaseMain(slot)); nền tổng 10/50 vẫn đặt ở BaseMode.BuildHeroStats.

**Why:** trước đó HeroUnit cộng nền + món → chỉ số trận cao hơn gốc.
Wing/Cape đã vào chỉ số trận (EquipmentStatResolver.AddWing/AddCape, theo Soldier.ewc/eyv; StatModifierSource thêm Wing, Cape).
**How to apply:** chỉ số trận Hero và Power giờ cùng công thức gốc; sửa công thức nào thì sửa cả 2 (dùng chung GearStatCalculator). Xem [[dungonrush-enchantment-system]], [[dungonrush-hero-base-stats]].

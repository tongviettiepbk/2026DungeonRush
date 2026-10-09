---
name: dungonrush-substat-combat
description: 13 substat trong combat đã đối chiếu native (Soldier.eyw/eyv, Character.ewb/ewf, AttackState.ewu); 2026-10-09 sửa CritDamage nền 1.05, thêm tick HealthRegen ×0.5, thêm DoubleChance đánh đôi
metadata:
  type: project
---

Bảng đầy đủ: `DecodedData/SUBSTAT_COMBAT_MODEL.md`. Đã đúng từ trước: AttackSpeed, Block (chỉ chặn đòn enemy), CritChance, Damage/Health/Melee/Ranged %, Lifesteal, CompanionCooldown, CompanionDamage.

Sửa 2026-10-09: CritDamage nền = 1 + BaseCriticalDamagePercent 5/100 (trước 1.2 kế thừa StickIdle); HealthRegen trước KHÔNG tick (GetHpRegenPerSecond không ai gọi) → BaseUnit.TickHpRegen, hồi/giây = MaxHp × Regen% × HealthRegenMultiplier 0.5 (config GearStatConfigData); DoubleChance trước chỉ tính chưa dùng → HeroUnit.BeginAttack roll, đòn 2 ở giữa nhịp (gốc: anim x2, 2 hit/nhịp).

**Why:** user hỏi substat đã xử lý chưa; đối chiếu native thấy 3 lệch.
**How to apply:** chưa test Unity; gốc check DoubleChance cho phe người chơi (Hero + ghost). Xem [[dungonrush-power-model]].

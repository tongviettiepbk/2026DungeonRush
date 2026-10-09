# Substat trong combat — reverse từ game gốc v41

Nguồn: `Soldier.eyw` (gom), `Soldier.eyv` (áp %), `Character.ewb` (nhận đòn), `Character.ewf` (hồi máu),
`MeleeAttackState/RangeAttackState.ewu` (đánh đôi), `rm.ioz(x)` = `Random.Range(0,100) <= x`. Ngày: 2026-10-09.

| SubStatType | Gốc gom vào | Áp dụng gốc | Project |
|---|---|---|---|
| AttackSpeed (0) | % (v/100) | AttackSpeed = tốc vũ khí (=1) × (1 + Σ%) × tốc game | ✅ |
| BlockChance (1) | Character.BlockChance += v | chỉ đòn của ENEMY: `ioz(Block)` → chặn trọn | ✅ |
| CriticalChance (2) | CritChance += v (nền 0) | `ioz(CritChance)` → damage × CritDamage | ✅ |
| CriticalDamage (3) | CritDamage += v/100, nền = 1 + BaseCriticalDamagePercent(5)/100 | | ✅ sửa 2026-10-09 (trước nền 1.2) |
| Damage (4) | % | Damage × (1 + Σ%) | ✅ |
| DoubleChance (5) | DoubleChance += v | đầu đòn phe người chơi `ioz(Double)` → anim x2, 2 đòn/1 nhịp | ✅ thêm 2026-10-09 (đòn 2 ở giữa nhịp) |
| Health (6) | % | MaxHp × (1 + Σ%) | ✅ |
| HealthRegen (7) | HealthRegen += v | mỗi frame chưa đầy máu: hp += MaxHp × v/100 × HealthRegenMultiplier(0.5) × dt × tốc game | ✅ thêm 2026-10-09 (trước không tick) |
| Lifesteal (8) | Lifesteal += v | đánh thường trúng, chưa đầy máu: hp += dmg × v/100 | ✅ |
| MeleeDamage (9) / RangedDamage (10) | % | cộng vào Damage% nếu vũ khí đúng loại | ✅ |
| CompanionCooldown (11) | += v | cooldown pet × (1 − v/100) (`Companion.gis/giq`) | ✅ |
| CompanionDamage (12) | += v | damage pet × (1 + v/100) | ✅ |

Nguồn substat: 6 món + Wing + Cape. Cape còn cộng % Damage/Health riêng (× hệ số relic slot Cape).

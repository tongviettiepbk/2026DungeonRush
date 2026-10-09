# Power người chơi — reverse từ game gốc v41

Nguồn: `libil2cpp.so` (Il2CppDumper v6.7.46 + capstone ARM64). Ngày: 2026-10-09.
Port: `Scripts/Player/PlayerPower.cs`.

## 1. Hàm gốc

| Hàm | Vai trò |
|---|---|
| `rm.iqp()` | Power hiện tại = `iqm(UserController, GameResources, OwnedCompanions)` |
| `rm.iqm(user, gr, companions)` | Gom nguồn: phần tăng từ relic, Own Effect pet, Wing, Cape → gọi `iqr` |
| `rm.iqr(gr, items, extraDmg, extraHp, dmg%, hp%, melee%, ranged%)` | Công thức cuối |
| `rm.iqi(subStats, ref dmg%, ref hp%, ref melee%, ref ranged%)` | Gom substat |
| `rm.iov(double)` | Làm tròn banker's, kẹp [1, 1.5e17] → long |
| `rm.ioy(diff)` | Chuỗi chênh lệch: `<color=#4CF856FF><sprite=0> +X</color>` / đỏ `#F84C4CFF` `-X`, số `rn.iqt(x, 2)` |
| `rm.iqq(before)` | `diff = iqp() − before`; `|diff| ≥ 1e-5` → `WarningUI.dgq(ioy(diff))` |

Người gọi `iqm`: BossRush (`ell`), Profile, ProfileButton, CompanionTabPage/InfoPopup, Wing/Cape popup, Clan, PvP, ExperienceController.
Người gọi `iqq`: chỉ 5 thao tác relic đang đeo (`EnchantmentTabPage.hzb/hyy`, `EnchantmentInfoPopup` unequip/dismantle đang đeo,
`EnchantmentMergePopup.hxy` nhánh `edx`).

## 2. Công thức (`rm.iqr`)

```
Với mỗi món đang mặc (jgl(ItemId)):
  main = GameResources.jgu(loại, rarity, level, weaponType)     // = Base × √10^rarity × (1 + 0.015 × level)
  Weapon/Gloves/Ring → D += main ; Helmet/Backpack/Necklace → H += main
  iqi(substat món) → dmg%, hp%, melee%, ranged%
Slot TRỐNG → cộng nền: Weapon 6 (PlayerBaseWeaponDamage), Gloves 4, Ring 0 → D ; Helmet 30, Backpack 20, Necklace 0 → H
D' = (D + extraDmg) × (1 + dmg% + (vũ khí Range ? ranged% : Melee ? melee%))      // chưa cầm vũ khí = Melee (giá trị 0)
H' = (H + extraHp)  × (1 + hp%)
Power = D' × H'
```
Không mặc gì: 10 × 50 = **500** (= `army_power_base`).

## 3. Nguồn cộng thêm (`rm.iqm`)

- **Relic**: mỗi món: `m = 1 + tier²/100` (`rm.iqo` = `Soldier.eyo`); nếu `m − 1 > 0` → main × (m − 1) vào extraDmg/extraHp theo loại.
- **Own Effect pet** (`lu.gov`): Σ OwnAttack → extraDmg, Σ OwnHealth → extraHp (mọi pet SỞ HỮU).
- **Wing** (`vh.lak` / `vh.lal`): `round(DamageBase × DamageTierScaler^(Rarity+1) × (1 + DamageScaler × level))` × m(slot Wing) → extraDmg;
  Health tương tự → extraHp. Làm tròn `Math.Round` (về số chẵn ở .5). Substat wing → iqi.
- **Cape** (`fu.eqx` / `fu.eqy`): `DamageBase × (1 + DamageScaler × (level − 1)) / 100 × m(slot Cape)` → dmg%; Health → hp%. Substat cape → iqi.

## 4. Gom substat (`rm.iqi`)

Bảng nhảy `SubStatType − 4`: Damage(4) → dmg%, Health(6) → hp%, MeleeDamage(9) → melee%, RangedDamage(10) → ranged%;
giá trị `Value / 100`. Loại khác (crit, tốc đánh, lifesteal...) KHÔNG vào Power. Mastery không vào Power.

## 5. Ghi chú port

- Project dùng `ToLetter()` (Extensions) để rút gọn số; bộ rút gọn gốc `rn.iqt` dùng hậu tố `K, M, B, T, q, Q, s, S, O, N, D, aa, ab…`
  (quy tắc làm tròn chi tiết chưa reverse).
- Toast project là `UI.Text` → thay `<sprite=0>` bằng chữ "Power".
- `Soldier.ewc` gốc **thay** chỉ số nền PlayerBase của slot bằng món đồ khi có đồ (slot trống mới dùng nền).
  Đã sửa `HeroUnit` theo gốc (2026-10-09): `EquipmentStatResolver` cộng (main × hệ số relic − nền slot).
- Wing/Cape trong chỉ số trận (2026-10-09, theo `Soldier.ewc/eyv`): Wing cộng Sát thương/Máu phẳng × hệ số relic slot Wing;
  Cape cộng % Sát thương/Máu × hệ số relic slot Cape vào chung nhóm % với substat; substat Wing/Cape tính như món thường.
  Công thức dùng chung ở `GearStatCalculator.GetWingDamage/GetWingHealth/GetCapeDamagePercent/GetCapeHealthPercent`.

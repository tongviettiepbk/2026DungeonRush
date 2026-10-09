using System;
using System.Collections.Generic;

// Power người chơi — REVERSE il2cpp v41: rm.iqm (gom nguồn) → rm.iqr (công thức) → rm.iqi (gom substat),
// làm tròn rm.iov. Tính thẳng từ SAVE (không cần Hero trong scene). Xem DecodedData/POWER_MODEL.md.
//
//   Power = Damage × (1 + Damage%) × Health × (1 + Health%)
//   Damage = Σ main stat món Damage (vũ khí/găng/nhẫn) + nền PlayerBase* cho slot TRỐNG + phần cộng thêm
//   Health = Σ main stat món Health (mũ/ba lô/dây chuyền) + nền slot trống + phần cộng thêm
//   Phần cộng thêm = relic: main × (hệ số − 1) + Own Effect pet + chỉ số Wing × hệ số relic slot Wing
//   Damage% = substat Damage + (vũ khí Range ? RangedDamage : Melee ? MeleeDamage) + % Cape × hệ số relic slot Cape
//   (substat lấy từ cả 6 món + Wing + Cape; chưa cầm vũ khí thì tính như Melee — giá trị mặc định gốc).
// Mastery và các substat khác (crit, tốc đánh...) KHÔNG tính vào Power.
public static class PlayerPower
{
    // Slot có main stat (thứ tự không ảnh hưởng kết quả).
    private static readonly GearSlotType[] GearSlots =
    {
        GearSlotType.HELMET,
        GearSlotType.GLOVES,
        GearSlotType.RING,
        GearSlotType.NECKLACE,
        GearSlotType.BACKPACK,
    };

    // rm.iqp: Power hiện tại của người chơi.
    public static double GetCurrent()
    {
        if (GameData.userData == null)
        {
            return 0d;
        }
        return Calculate(GameData.userData.equipment, GameData.userData.enchantments, GameData.userData.companions.owned);
    }

    // rm.iqm + rm.iqr.
    public static double Calculate(UserEquipmentData equipment, UserEnchantmentData enchantments, List<CompanionModel> companions)
    {
        GearStatConfigData config = EquipmentStatResolver.LoadConfig();
        if (config == null || equipment == null || GameData.staticData == null)
        {
            return 0d;
        }

        double damage = 0d;
        double health = 0d;
        double extraDamage = 0d;
        double extraHealth = 0d;
        // 4 ô % (rm.iqi): Damage, Health, MeleeDamage, RangedDamage.
        float[] percent = new float[4];
        WeaponType weaponType = WeaponType.Melee;

        // Vũ khí.
        EquippedItemData weaponRec = equipment.GetRecord(GearSlotType.WEAPON);
        WeaponData weapon = weaponRec != null && string.IsNullOrEmpty(weaponRec.equipId) == false
            ? GameData.staticData.weapons.GetData(weaponRec.equipId)
            : null;
        if (weapon != null)
        {
            double main = GearStatCalculator.GetWeaponMainStat(config, weapon.weaponType, weaponRec.rarity, weaponRec.level);
            damage += main;
            extraDamage += GetRelicBonus(main, enchantments, GearSlotType.WEAPON);
            weaponType = weapon.weaponType;
            AddSubStats(weaponRec.subStats, percent);
        }
        else
        {
            damage += config.GetPlayerBaseMain(GearSlotType.WEAPON);
        }

        // 5 gear.
        for (int i = 0; i < GearSlots.Length; i++)
        {
            GearSlotType slot = GearSlots[i];
            EquippedItemData rec = equipment.GetRecord(slot);
            GearItemData gear = rec != null && string.IsNullOrEmpty(rec.equipId) == false
                ? GameData.staticData.gears.GetData(rec.equipId)
                : null;
            if (gear == null)
            {
                // rm.iqr: slot trống dùng nền PlayerBase của slot (thay cho món đồ).
                if (slot == GearSlotType.HELMET || slot == GearSlotType.BACKPACK || slot == GearSlotType.NECKLACE)
                {
                    health += config.GetPlayerBaseMain(slot);
                }
                else
                {
                    damage += config.GetPlayerBaseMain(slot);
                }
                continue;
            }

            double main = GearStatCalculator.GetGearMainStat(config, gear.slot, rec.rarity, rec.level, out GearMainStatKind kind);
            double bonus = GetRelicBonus(main, enchantments, slot);
            if (kind == GearMainStatKind.Health)
            {
                health += main;
                extraHealth += bonus;
            }
            else
            {
                damage += main;
                extraDamage += bonus;
            }
            AddSubStats(rec.subStats, percent);
        }

        // Own Effect pet sở hữu (lu.gov).
        CompanionService.GetOwnEffectTotals(companions, out double ownAttack, out double ownHealth);
        extraDamage += ownAttack;
        extraHealth += ownHealth;

        AddWing(equipment, enchantments, percent, ref extraDamage, ref extraHealth);
        AddCape(equipment, enchantments, percent);

        float damagePercent = percent[0];
        if (weaponType == WeaponType.Range)
        {
            damagePercent += percent[3];
        }
        else if (weaponType == WeaponType.Melee)
        {
            damagePercent += percent[2];
        }
        float healthPercent = percent[1];

        double finalDamage = (damage + extraDamage) * (1d + damagePercent);
        double finalHealth = (health + extraHealth) * (1d + healthPercent);
        return finalDamage * finalHealth;
    }

    // rm.iov: làm tròn banker's, kẹp [1, 1.5e17] → số nguyên (giá trị gửi server / hiển thị).
    public static long ToLong(double power)
    {
        double rounded = Math.Max(1d, Math.Min(Math.Round(power, MidpointRounding.ToEven), 1.5e17));
        return (long)rounded;
    }

    // rm.ioy: chuỗi chênh lệch Power (xanh +, đỏ −). Gốc là "<sprite=0> ±X" (icon Power); toast của project
    // là UI.Text không có sprite asset nên thay icon bằng chữ "Power".
    public static string FormatChange(double diff)
    {
        string color = diff > 0 ? "#4CF856FF" : "#F84C4CFF";
        string sign = diff > 0 ? "+" : "-";
        return "<color=" + color + ">Power " + sign + Math.Abs(diff).ToLetter() + "</color>";
    }

    // rm.iqq: so Power hiện tại với `before` (lấy bằng GetCurrent() trước thao tác), lệch >= 1e-5 thì hiện toast.
    // Gốc chỉ gọi sau thao tác relic đang ĐEO: đeo vào slot, Quick Equip, Unequip, Dismantle/Merge relic đang đeo.
    public static void NotifyChange(double before)
    {
        double diff = GetCurrent() - before;
        if (Math.Abs(diff) < 1e-5 || UIManager.Instance == null)
        {
            return;
        }
        UIManager.Instance.ShowToastMessage(FormatChange(diff), isLocalize: false);
    }

    // ----- Nội bộ -----

    // rm.iqm: phần tăng từ relic = main × (hệ số − 1), chỉ khi hệ số > 1.
    private static double GetRelicBonus(double main, UserEnchantmentData enchantments, GearSlotType slot)
    {
        float extra = EnchantmentService.GetStatMultiplier(enchantments, slot) - 1f;
        return extra > 0f ? main * extra : 0d;
    }

    // rm.iqi: Damage → [0], Health → [1], MeleeDamage → [2], RangedDamage → [3]; value/100. Loại khác bỏ qua.
    private static void AddSubStats(List<GearSubStat> subStats, float[] percent)
    {
        if (subStats == null)
        {
            return;
        }

        for (int i = 0; i < subStats.Count; i++)
        {
            GearSubStat sub = subStats[i];
            if (sub == null)
            {
                continue;
            }

            int index;
            switch (sub.type)
            {
                case SubStatType.Damage: index = 0; break;
                case SubStatType.Health: index = 1; break;
                case SubStatType.MeleeDamage: index = 2; break;
                case SubStatType.RangedDamage: index = 3; break;
                default: continue;
            }
            percent[index] += sub.value / 100f;
        }
    }

    // vh.lak / vh.lal: chỉ số Wing = round(Base × TierScaler^(rarity+1) × (1 + Scaler × level)), × hệ số relic slot Wing.
    private static void AddWing(UserEquipmentData equipment, UserEnchantmentData enchantments, float[] percent,
                                ref double extraDamage, ref double extraHealth)
    {
        EquippedItemData rec = equipment.GetRecord(GearSlotType.WING);
        if (rec == null || int.TryParse(rec.equipId, out int wingId) == false || GameData.staticData.wings == null)
        {
            return;
        }

        WingData wing = GameData.staticData.wings.GetData(wingId);
        if (wing == null)
        {
            return;
        }

        float multiplier = EnchantmentService.GetStatMultiplier(enchantments, GearSlotType.WING);
        extraDamage += multiplier * GearStatCalculator.GetWingDamage(wing, rec.level);
        extraHealth += multiplier * GearStatCalculator.GetWingHealth(wing, rec.level);
        AddSubStats(rec.subStats, percent);
    }

    // fu.eqx / fu.eqy: Cape cho % = Base × (1 + Scaler × (level − 1)) / 100, × hệ số relic slot Cape.
    private static void AddCape(UserEquipmentData equipment, UserEnchantmentData enchantments, float[] percent)
    {
        EquippedItemData rec = equipment.GetRecord(GearSlotType.CAPE);
        if (rec == null || int.TryParse(rec.equipId, out int capeId) == false || GameData.staticData.capes == null)
        {
            return;
        }

        CapeData cape = GameData.staticData.capes.GetData(capeId);
        if (cape == null)
        {
            return;
        }

        float multiplier = EnchantmentService.GetStatMultiplier(enchantments, GearSlotType.CAPE);
        percent[0] += multiplier * (GearStatCalculator.GetCapeDamagePercent(cape, rec.level) / 100f);
        percent[1] += multiplier * (GearStatCalculator.GetCapeHealthPercent(cape, rec.level) / 100f);
        AddSubStats(rec.subStats, percent);
    }
}

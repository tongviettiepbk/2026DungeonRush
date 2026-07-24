using System.Collections.Generic;
using UnityEngine;

// C8 Cánh — 1 mẫu wing/rarity (10 mẫu). Slot chế tạo bằng quặng (nhóm E Đào mỏ):
// craft, reroll substat và lên cấp đều tốn quặng. Có base stat + scaler theo level và theo tier.
// LƯU Ý: subStatType và các *OreType đang lưu dạng int GỐC (chưa map enum) — sẽ map khi
// làm nhóm E Mining / hệ combat stat. rerollCosts = mảng chi phí quặng cho mỗi lần reroll.
[CreateAssetMenu(fileName = "Wing-", menuName = "DungOnRush/Wing Data")]
public class WingData : ScriptableObject
{
    public int wingId;
    public string wingName;
    public Rarity rarity;
    public string localizationKey;
    public Sprite icon;

    [Header("Stat gốc + scaler")]
    public float healthBase;
    public float damageBase;
    public float healthScaler;
    public float damageScaler;
    public float healthTierScaler;
    public float damageTierScaler;

    [Header("SubStat cố định")]
    public List<WingSubStat> subStats = new List<WingSubStat>();

    [Header("Kinh tế chế tạo (quặng)")]
    public int craftOreType;
    public int craftOreCost;
    public int rerollOreType;
    public List<int> rerollCosts = new List<int>();   // chi phí lần reroll thứ 1,2,3...
    public int levelUpOreType;
    public int levelUpCost;
    public float levelUpCostMultiplier;
    public int maxLevel;
}

[System.Serializable]
public class WingSubStat
{
    public int type;    // int gốc của StatType (map enum sau).
    public float value;
}

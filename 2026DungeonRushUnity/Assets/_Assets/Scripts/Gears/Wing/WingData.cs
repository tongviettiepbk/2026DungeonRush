using System.Collections.Generic;
using UnityEngine;

// C8 Cánh — 1 mẫu wing/rarity (10 mẫu). Slot chế tạo bằng quặng (nhóm E Đào mỏ):
// craft, reroll substat và lên cấp đều tốn quặng. Có base stat + scaler theo level và theo tier.
// subStats = substat KHỞI ĐIỂM khi chế tạo (WingCraftPopup.kye copy sang WingModel). *OreType = MineOreType
// gốc (asset lưu số int, enum giữ đúng giá trị). rerollCosts[i] = giá reroll khi đang khoá i dòng substat.
[CreateAssetMenu(fileName = "Wing-", menuName = "DungOnRush/Wing Data")]
public class WingData : ScriptableObject
{
    public int wingId;
    public string wingName;
    public Rarity rarity;
    public string localizationKey;
    public Sprite icon;

    // Ảnh gắn LÊN NGƯỜI khi mặc cánh (khác icon inventory). Chưa gán → hệ mặc tạm dùng icon.
    public Sprite bodySprite;

    [Header("Stat gốc + scaler")]
    public float healthBase;
    public float damageBase;
    public float healthScaler;
    public float damageScaler;
    public float healthTierScaler;
    public float damageTierScaler;

    [Header("SubStat khởi điểm khi craft")]
    public List<WingSubStat> subStats = new List<WingSubStat>();

    [Header("Kinh tế chế tạo (quặng)")]
    public MineOreType craftOreType;
    public int craftOreCost;
    public MineOreType rerollOreType;
    public List<int> rerollCosts = new List<int>();   // index = số dòng substat đang khoá
    public MineOreType levelUpOreType;
    public int levelUpCost;
    public float levelUpCostMultiplier;
    public int maxLevel;
}

[System.Serializable]
public class WingSubStat
{
    public SubStatType type;   // = SubStatType gốc (asset lưu số int).
    public float value;
}

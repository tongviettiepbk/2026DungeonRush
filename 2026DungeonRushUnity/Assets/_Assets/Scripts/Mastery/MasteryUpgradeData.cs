using System;
using System.Collections.Generic;
using UnityEngine;

// Config 1 nhánh Mastery (G1). Asset đặt trong Resources/Scriptable Objects/Mastery/,
// sinh từ tool tools/gen_mastery_assets.py (data gốc DecodedData/tables/MasteryUpgradeData.json).
//
// Theo pattern CompanionData: layer data CHỈ giữ số cân bằng + icon để hiển thị. Đã bỏ
// reference của bản gốc: ValueSpriteAsset (TMP_SpriteAsset) — không cần cho logic.
[CreateAssetMenu(fileName = "Mastery-", menuName = "DungOnRush/Mastery Upgrade Data")]
public class MasteryUpgradeData : ScriptableObject
{
    [Header("Định danh")]
    public string assetName;               // = _name gốc, VD "1-AutoLootHammerCount".
    public MasteryUpgradeType upgradeType;
    public string upgradeName;             // tên hiển thị (EN), VD "Auto-Loot Speed".
    [TextArea] public string description;
    public Sprite icon;

    [Header("Phí & giá trị")]
    // Phí Ngọc MỞ KHOÁ nhánh (một lần). Reverse v41: game đọc ĐÚNG field này (MasteryTabPage.ivw),
    // và nếu = 0 thì mở khoá MIỄN PHÍ (AutoLoot, AdBoostWorth). KHÔNG dùng MasteryConfig.UnlockGemCosts
    // (hàm gốc MasteryConfig.iuf là dead code, không nơi nào gọi).
    public int unlockGemCost;
    public bool addedLater;                // 2 nhánh bổ sung sau bản gốc (Forge/Mining).
    public float defaultValue;             // giá trị khi CHƯA nâng (level 0).
    public bool isValueAdditive;           // true: value là bonus cộng vào base hệ khác (Forge/Mining).
    public bool applyDefaultBeforeFeatureUnlock;
    public bool applyDefaultBeforeCardUnlock;

    [Header("Hiển thị")]
    public string valuePrefix;             // "x" / "%" / "+" / null
    public string valueSuffix;             // "s" / null (bỏ chuỗi <sprite=...>)

    [Header("Thang cấp")]
    public List<MasteryLevelEntry> levels; // mỗi cấp: GemCost + Value.

    public int MaxLevel => levels != null ? levels.Count : 0;

    // Giá trị đạt được ở 1 cấp (port ĐÚNG MasteryUpgradeData.iuo, reverse v41).
    // level <= 0: trả defaultValue. level > 0: Levels[level-1].Value, và nếu IsValueAdditive
    // thì CỘNG thêm defaultValue (2 nhánh additive Forge/Mining đều default=0 nên không đổi).
    public float GetValueAtLevel(int level)
    {
        if (level <= 0 || levels == null || levels.Count == 0)
        {
            return defaultValue;
        }

        int idx = Mathf.Clamp(level, 1, levels.Count) - 1;
        float v = levels[idx].value;
        return isValueAdditive ? v + defaultValue : v;
    }

    // Phí Ngọc để nâng TỪ level hiện tại lên level+1 (chưa tính phí mở khoá nhánh).
    // GemCost lưu ở entry của cấp ĐÍCH: nâng lên cấp k → tốn levels[k-1].gemCost.
    public int GetUpgradeGemCost(int currentLevel)
    {
        if (levels == null || currentLevel >= levels.Count)
        {
            return -1; // đã max
        }

        int nextIdx = Mathf.Max(currentLevel, 0);
        return levels[nextIdx].gemCost;
    }
}

[Serializable]
public class MasteryLevelEntry
{
    public int gemCost;
    public float value;
}

using UnityEngine;

// Logic hệ Mastery (G1): mở khoá & nâng cấp nhánh bằng Ngọc (GEM), đọc giá trị đang có.
// Stateless — thao tác trực tiếp trên GameData.staticData.mastery + GameData.userData.mastery.
// Theo pattern service của DungeonRush (LootService/ForgeController gọi qua GameData).
public static class MasteryService
{
    private static StaticMasteryData Static => GameData.staticData.mastery;
    private static UserMasteryData User => GameData.userData.mastery;

    // ----- Truy vấn -----

    public static bool IsEnabled => Static != null && Static.enabled;

    // Phí Ngọc mở khoá 1 nhánh = unlockGemCost RIÊNG của nhánh (reverse v41: MasteryTabPage.ivw).
    // Trả 0 nghĩa là mở khoá MIỄN PHÍ (AutoLoot, AdBoostWorth).
    public static int GetUnlockCost(MasteryUpgradeType type)
    {
        MasteryUpgradeData data = Static != null ? Static.GetUpgrade(type) : null;
        return data != null ? data.unlockGemCost : 0;
    }

    // Phí Ngọc để nâng nhánh lên cấp kế tiếp (-1 nếu đã max hoặc chưa mở khoá).
    public static int GetUpgradeCost(MasteryUpgradeType type)
    {
        if (User.IsUnlocked(type) == false)
        {
            return -1;
        }

        MasteryUpgradeData data = Static.GetUpgrade(type);
        return data != null ? data.GetUpgradeGemCost(User.GetLevel(type)) : -1;
    }

    // Giá trị đang tác dụng của 1 nhánh (dùng ở các hệ tiêu thụ: Forge, Companion...).
    // Chưa mở khoá: dùng defaultValue nếu ApplyDefaultBeforeFeatureUnlock, ngược lại 0.
    public static float GetCurrentValue(MasteryUpgradeType type)
    {
        MasteryUpgradeData data = Static != null ? Static.GetUpgrade(type) : null;
        if (data == null)
        {
            return 0f;
        }

        if (User.IsUnlocked(type) == false)
        {
            return data.applyDefaultBeforeFeatureUnlock ? data.defaultValue : 0f;
        }

        return data.GetValueAtLevel(User.GetLevel(type));
    }

    // ----- Hành động -----

    public static bool CanUnlock(MasteryUpgradeType type)
    {
        if (IsEnabled == false || User.IsUnlocked(type))
        {
            return false;
        }

        int cost = GetUnlockCost(type);
        return cost <= 0 || GameData.userData.items.IsEnough(ItemType.GEM, cost);
    }

    public static bool TryUnlock(MasteryUpgradeType type)
    {
        if (CanUnlock(type) == false)
        {
            return false;
        }

        // cost 0 = miễn phí (bỏ qua trừ Ngọc, giống ivw kiểm tra cost < 1).
        int cost = GetUnlockCost(type);
        if (cost > 0 && GameData.userData.items.Consume(ItemType.GEM, cost) == false)
        {
            return false;
        }

        User.Unlock(type);
        GameData.Save();
        return true;
    }

    public static bool CanUpgrade(MasteryUpgradeType type)
    {
        if (IsEnabled == false || User.IsUnlocked(type) == false)
        {
            return false;
        }

        int cost = GetUpgradeCost(type);
        if (cost < 0)
        {
            return false; // đã max
        }

        return GameData.userData.items.IsEnough(ItemType.GEM, cost);
    }

    public static bool TryUpgrade(MasteryUpgradeType type)
    {
        if (CanUpgrade(type) == false)
        {
            return false;
        }

        int cost = GetUpgradeCost(type);
        if (GameData.userData.items.Consume(ItemType.GEM, cost) == false)
        {
            return false;
        }

        User.SetLevel(type, User.GetLevel(type) + 1);
        GameData.Save();
        return true;
    }
}

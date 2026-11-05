using System;
using UnityEngine;

// Thưởng 7 ngày đầu: mỗi ngày UTC nhận 1 ô theo thứ tự, bỏ lỡ ngày không mất ô. LỆCH GỐC có chủ ý: gốc không có
// điểm danh, user duyệt 2026-11-05 để tăng D7. Bản rút gọn của Road to Pro bên Racing: bỏ phần nhiệm vụ từng ngày.
// Xem GDD/analytics/GROWTH_PLAN.md mục 5.1-C.
public static class SevenDayService
{
    public const int DAY_COUNT = 7;

    // Chờ người chơi qua vài màn để không chen vào hướng dẫn đầu game.
    private const int MIN_STAGE_LEVEL = 3;

    // Số thưởng là đề xuất, chưa có số liệu kinh tế thật. Tổng 250 gem, ít hơn Starter Pack 500 gem.
    private static readonly ShopReward[][] REWARDS =
    {
        new[] { new ShopReward(ShopRewardType.Gem, 20) },
        new[] { new ShopReward(ShopRewardType.LootBox, 100) },
        new[] { new ShopReward(ShopRewardType.Gem, 30) },
        new[] { new ShopReward(ShopRewardType.Bone, 200) },
        new[] { new ShopReward(ShopRewardType.LootBox, 150) },
        new[] { new ShopReward(ShopRewardType.Gem, 50) },
        new[] { new ShopReward(ShopRewardType.Gem, 150), new ShopReward(ShopRewardType.Bone, 500) },
    };

    public static event Action Changed;

    private static UserProfileData Profile => GameData.userData.profile;
    private static string Today => ServerTime.UtcNow.ToString("yyyy-MM-dd");

    public static int ClaimedCount => Profile.sevenDayClaimed;

    public static ShopReward[] GetRewards(int dayIndex)
    {
        return REWARDS[dayIndex];
    }

    public static bool IsActive
    {
        get
        {
            UserCampaignData campaign = GameData.userData.campaign;
            return ClaimedCount < DAY_COUNT
                && campaign.passedStageId > 0
                && GameData.staticData.campaign.GetLevel(campaign.passedStageId) >= MIN_STAGE_LEVEL;
        }
    }

    public static bool CanClaim => IsActive && Profile.sevenDayLastDate != Today;

    public static TimeSpan GetTimeToNextClaim()
    {
        DateTime now = ServerTime.UtcNow;
        return now.Date.AddDays(1) - now;
    }

    public static bool Claim()
    {
        if (CanClaim == false)
        {
            return false;
        }

        using (Analytics.Scope("seven_day"))
        {
            ShopReward[] rewards = GetRewards(ClaimedCount);
            for (int i = 0; i < rewards.Length; i++)
            {
                ShopService.Grant(rewards[i]);
            }
        }

        Profile.sevenDayClaimed++;
        Profile.sevenDayLastDate = Today;
        Profile.isDataChanged = true;
        GameData.Save(true);
        Analytics.Log("seven_day_claim", ("day", ClaimedCount));
        Changed?.Invoke();
        return true;
    }

    public static void NotifyChanged()
    {
        Changed?.Invoke();
    }
}

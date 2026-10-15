// Cộng EXP người chơi (ExperienceController.hdq gốc): áp boost exp mua ở Store, đẩy playerLevel,
// mỗi level vừa lên thưởng gem theo bảng LevelPopup.ywc. Dùng chung cho thắng màn campaign và bán đồ forge.
// Không tự Save — nơi gọi lo việc lưu.
public static class PlayerExpService
{
    // Trả về số level vừa lên.
    public static int Grant(int baseExp)
    {
        int exp = ShopService.ApplyExpBoost(baseExp);
        UserPlayerData player = GameData.userData.player;
        int oldPlayerLevel = player.playerLevel;
        int levelsGained = player.AddExperience(exp);

        if (levelsGained > 0)
        {
            StaticExperienceData config = GameData.staticData.experience;
            int totalGem = 0;
            for (int lv = oldPlayerLevel + 1; lv <= player.playerLevel; lv++)
            {
                totalGem += config.GetLevelUpGemReward(lv);
            }
            GameData.userData.items.Receive(ItemType.GEM, totalGem);
            DebugCustom.Log($"[Exp] Lên {levelsGained} level -> playerLevel = {player.playerLevel}, +{totalGem} gem");
        }

        return levelsGained;
    }
}

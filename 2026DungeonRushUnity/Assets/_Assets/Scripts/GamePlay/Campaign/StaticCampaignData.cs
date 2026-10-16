using UnityEngine;

// Config campaign dạng formula placeholder — sau này thay bằng data thật của DungOnRush
// (load từ ScriptableObject giống StaticItemData).
public class StaticCampaignData
{
    public const int FIRST_STAGE_ID = 101;
    public const int STAGES_PER_CHAPTER = 10;
    // Mỗi 100 level = 1 bậc độ khó (GameResources.jgj gốc: index = (level-1)/100, kẹp vào Tier cuối).
    public const int LEVELS_PER_TIER = 100;

    // TierData gốc Tier1..Tier8 (AssetRipper MonoBehaviour/TierN.asset): Name (bản en của Items.Tier.Name.TierN) + NameColor.
    private static readonly string[] TIER_NAMES = { "Normal", "Hard", "Extreme", "Overkill", "Impossible", "Hell I", "Hell II", "Hell III" };
    private static readonly Color[] TIER_COLORS =
    {
        new Color(0.8113208f, 0.8113208f, 0.8113208f),
        new Color(1f, 0.5949937f, 0f),
        new Color(0.9339623f, 0.17943157f, 0.17181383f),
        new Color(0.8880435f, 0.1745283f, 1f),
        new Color(0.26059723f, 0.17647058f, 1f),
        new Color(1f, 0.17647058f, 0.27792463f),
        new Color(1f, 0.1764706f, 0.2784314f),
        new Color(1f, 0.1764706f, 0.2784314f),
    };

    // Chữ LevelText thanh trên (GameplayUI.kii gốc = rm.ipz + " " + rm.ipy):
    // "<color=#màuTier>TênTier</color> {x/10+1}-{x%10+1}" với x = (level-1)%100. VD level 155 → "Hard 6-5".
    public static string GetLevelText(int level)
    {
        int index = level - 1;
        int tier = Mathf.Clamp(index / LEVELS_PER_TIER, 0, TIER_NAMES.Length - 1);
        int inTier = index % LEVELS_PER_TIER;
        string tierText = "<color=#" + ColorUtility.ToHtmlStringRGB(TIER_COLORS[tier]) + ">" + TIER_NAMES[tier] + "</color>";
        return tierText + " " + (inTier / 10 + 1) + "-" + (inTier % 10 + 1);
    }

    public int GetChapter(int stageId)
    {
        return stageId / 100;
    }

    public int GetStageIndex(int stageId)
    {
        return stageId % 100;
    }

    public int GetNextStageId(int stageId)
    {
        int index = GetStageIndex(stageId);

        if (index >= STAGES_PER_CHAPTER)
        {
            return (GetChapter(stageId) + 1) * 100 + 1;
        }

        return stageId + 1;
    }

    // Lùi 1 màn (dùng khi thua): màn đầu chương → về màn cuối chương trước.
    // KHÔNG bao giờ lùi thấp hơn FIRST_STAGE_ID.
    public int GetPrevStageId(int stageId)
    {
        if (stageId <= FIRST_STAGE_ID)
        {
            return FIRST_STAGE_ID;
        }

        int index = GetStageIndex(stageId);

        if (index <= 1)
        {
            return (GetChapter(stageId) - 1) * 100 + STAGES_PER_CHAPTER;
        }

        return stageId - 1;
    }

    // combatLevel LIÊN TỤC của màn (1-1=1 ... 1-10=10, 2-1=11 ...). Dùng cho sinh enemy + exp reward.
    public int GetLevel(int stageId)
    {
        int level = (GetChapter(stageId) - 1) * STAGES_PER_CHAPTER + GetStageIndex(stageId);
        return level < 1 ? 1 : level;
    }
}

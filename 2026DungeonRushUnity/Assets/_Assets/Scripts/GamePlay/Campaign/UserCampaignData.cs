public class UserCampaignData : BaseUserData
{
    // Level tra bảng xác suất rarity ForgeData (0..MAX_FORGE_LEVEL, 100 dòng). Độc lập với
    // curStageId/passedStageId — cơ chế tăng forgeLevel sẽ định nghĩa sau.
    private const int MAX_FORGE_LEVEL = 99;

    // Stage id dạng 101, 102... (chương 1), 201... (chương 2) — theo convention StickIdle
    public int curStageId { get; set; }
    public int passedStageId { get; set; }
    public int forgeLevel { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_CAMPAIGN;
    }

    public override void InitData()
    {
        base.InitData();
        curStageId = StaticCampaignData.FIRST_STAGE_ID;
        passedStageId = 0;
        forgeLevel = 0;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (curStageId < StaticCampaignData.FIRST_STAGE_ID)
        {
            curStageId = StaticCampaignData.FIRST_STAGE_ID;
            isDataChanged = true;
        }

        if (forgeLevel < 0)
        {
            forgeLevel = 0;
            isDataChanged = true;
        }
        else if (forgeLevel > MAX_FORGE_LEVEL)
        {
            forgeLevel = MAX_FORGE_LEVEL;
            isDataChanged = true;
        }
    }

    public void PassStage(int stageId)
    {
        if (stageId > passedStageId)
        {
            passedStageId = stageId;
            curStageId = GameData.staticData.campaign.GetNextStageId(stageId);
            isDataChanged = true;
        }
    }
}

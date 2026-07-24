public class UserCampaignData : BaseUserData
{
    // Stage id dạng 101, 102... (chương 1), 201... (chương 2) — theo convention StickIdle
    public int curStageId { get; set; }
    public int passedStageId { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_CAMPAIGN;
    }

    public override void InitData()
    {
        base.InitData();
        curStageId = StaticCampaignData.FIRST_STAGE_ID;
        passedStageId = 0;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (curStageId < StaticCampaignData.FIRST_STAGE_ID)
        {
            curStageId = StaticCampaignData.FIRST_STAGE_ID;
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

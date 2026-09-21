public class UserCampaignData : BaseUserData
{
    // Chỉ giữ tiến trình màn campaign (~ User.Level / User.HighestLevel trong save gốc).
    // Bảng rarity KHÔNG bám vào đây nữa — nó bám playerLevel (hệ exp, xem UserPlayerData).
    // Stage id dạng 101, 102... (chương 1), 201... (chương 2) — theo convention StickIdle
    //
    // stageIdCurrent = màn campaign HIỆN TẠI sẽ đánh (UI hiển thị + tính thưởng bám vào đây).
    //                  Thắng → tiến 1 màn; Thua → lùi 1 màn.
    // passedStageId  = màn campaign CAO NHẤT đã vượt (chỉ tiến, KHÔNG lùi khi thua).
    public int stageIdCurrent { get; set; }
    public int passedStageId { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_CAMPAIGN;
    }

    public override void InitData()
    {
        base.InitData();
        stageIdCurrent = StaticCampaignData.FIRST_STAGE_ID;
        passedStageId = 0;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (stageIdCurrent < StaticCampaignData.FIRST_STAGE_ID)
        {
            stageIdCurrent = StaticCampaignData.FIRST_STAGE_ID;
            isDataChanged = true;
        }
    }

    // Thắng → ghi nhận màn cao nhất đã qua (nếu là mốc mới) rồi tiến sang màn kế.
    // stageIdCurrent LUÔN tiến (kể cả khi đang đánh lại màn cũ sau thua) để không bị kẹt.
    public void PassStage(int stageId)
    {
        if (stageId > passedStageId)
        {
            passedStageId = stageId;
        }

        stageIdCurrent = GameData.staticData.campaign.GetNextStageId(stageId);
        isDataChanged = true;
    }

    // Thua → lùi màn hiện tại 1 bậc (passedStageId GIỮ NGUYÊN). Đã ở màn đầu thì giữ nguyên.
    public void StepBackStage()
    {
        int prevStageId = GameData.staticData.campaign.GetPrevStageId(stageIdCurrent);
        if (prevStageId != stageIdCurrent)
        {
            stageIdCurrent = prevStageId;
            isDataChanged = true;
        }
    }
}

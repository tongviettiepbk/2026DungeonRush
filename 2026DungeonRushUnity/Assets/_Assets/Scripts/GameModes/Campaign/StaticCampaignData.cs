// Config campaign dạng formula placeholder — sau này thay bằng data thật của DungOnRush
// (load từ ScriptableObject giống StaticItemData).
public class StaticCampaignData
{
    public const int FIRST_STAGE_ID = 101;
    public const int STAGES_PER_CHAPTER = 10;

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
}

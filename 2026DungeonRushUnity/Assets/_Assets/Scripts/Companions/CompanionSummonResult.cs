// 1 lượt summon (CompanionSummonResult gốc): con ra được + có phải lần đầu sở hữu không (hiện chữ NEW ở popup kết quả).
public struct CompanionSummonResult
{
    public CompanionData data;
    public bool isNew;

    public CompanionSummonResult(CompanionData data, bool isNew)
    {
        this.data = data;
        this.isNew = isNew;
    }
}

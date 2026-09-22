// Save model 1 companion người chơi SỞ HỮU — đúng schema gốc `CompanionModel` (il2cpp v41):
//   CompanionId(string) + Level(int) + CardCount(int) + IsNew(bool). Companion lên cấp bằng THẺ
//   (tích CardCount đủ ngưỡng → Level++), KHÔNG có XP. Level 1-based, cap 100 (native `ly.gpx`).
// Dùng property { get; set; } để Newtonsoft serialize ổn định (đồng bộ các UserXxxData khác).
[System.Serializable]
public class CompanionModel
{
    public string companionId { get; set; }   // = CompanionData.assetName
    public int level { get; set; }             // 1-based, cap CompanionUpgradeConfig.MAX_LEVEL
    public int cardCount { get; set; }         // thẻ đang tích (chưa đủ để lên cấp kế)
    public bool isNew { get; set; }            // cờ chấm đỏ "mới nhận"

    public CompanionModel() { }

    public CompanionModel(string id)
    {
        companionId = id;
        level = 1;
        cardCount = 0;
        isNew = true;
    }
}

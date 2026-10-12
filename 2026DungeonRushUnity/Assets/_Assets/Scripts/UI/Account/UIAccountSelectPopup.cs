using System;
using TMPro;
using UnityEngine.UI;

// "Choose Save" (AccountSelectPopup gốc): save trên máy và save cloud lệch nhau → người chơi chọn giữ bản nào.
// Bắt buộc phải chọn — không có nút đóng, không đóng bằng Back.
public class UIAccountSelectPopup : BaseUI
{
    public enum Choice
    {
        KeepLocal = 0,
        UseCloud = 1,
    }

    public struct PreviewData
    {
        public string Nickname;
        public int HighestStageId;     // màn campaign cao nhất đã vượt (101, 102...), 0 = chưa vượt màn nào
        public int PlayerLevel;
        public long LastSaveUtcMs;
    }

    public TMP_Text txtLocalName;
    public TMP_Text txtLocalProgress;
    public Button btKeepLocal;
    public TMP_Text txtCloudName;
    public TMP_Text txtCloudProgress;
    public Button btUseCloud;

    private Action<Choice> onChoice;

    protected override void Awake()
    {
        base.Awake();
        btKeepLocal.onClick.AddListener(() => Choose(Choice.KeepLocal));
        btUseCloud.onClick.AddListener(() => Choose(Choice.UseCloud));
    }

    // jte gốc.
    public void Show(PreviewData local, PreviewData cloud, Action<Choice> onChoose)
    {
        onChoice = onChoose;
        DrawPreview(local, txtLocalName, txtLocalProgress);
        DrawPreview(cloud, txtCloudName, txtCloudProgress);
    }

    // jtf gốc: tên (chưa đặt → "Guest"), rồi 3 dòng level người chơi / màn cao nhất / lưu cách đây bao lâu.
    private static void DrawPreview(PreviewData data, TMP_Text txtName, TMP_Text txtProgress)
    {
        txtName.text = string.IsNullOrEmpty(data.Nickname) ? "Guest" : data.Nickname;
        txtProgress.text = string.Format("Player Lv. {0}", data.PlayerLevel) + "\n" + GetStageText(data.HighestStageId) + "\n" + GetSavedText(data.LastSaveUtcMs);
    }

    // jth gốc.
    private static string GetStageText(int stageId)
    {
        if (stageId <= 0)
        {
            return "New adventurer";
        }
        StaticCampaignData campaign = GameData.staticData.campaign;
        return "Stage " + campaign.GetChapter(stageId) + "-" + campaign.GetStageIndex(stageId);
    }

    // jtg gốc.
    private static string GetSavedText(long lastSaveUtcMs)
    {
        if (lastSaveUtcMs <= 0)
        {
            return "Last save: unknown";
        }

        long seconds = Math.Max(0, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastSaveUtcMs) / 1000);
        if (seconds < 60)
        {
            return "Saved just now";
        }
        if (seconds < 3600)
        {
            return string.Format("Saved {0} min ago", seconds / 60);
        }
        if (seconds < 86400)
        {
            return string.Format("Saved {0}h ago", seconds / 3600);
        }
        return string.Format("Saved {0}d ago", seconds / 86400);
    }

    // jti gốc.
    private void Choose(Choice choice)
    {
        Action<Choice> callback = onChoice;
        onChoice = null;
        Close();
        callback?.Invoke(choice);
    }
}

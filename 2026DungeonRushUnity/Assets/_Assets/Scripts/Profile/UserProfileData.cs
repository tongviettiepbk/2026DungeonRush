using System;
using System.Collections.Generic;

public class UserProfileData : BaseUserData
{
    public string userName { get; set; }
    // Người chơi đã tự đặt tên (UserController.dpi gốc: phải có nickname mới được tạo tài khoản). Tên "Player123456" sinh sẵn không tính.
    public bool isNameSet { get; set; }
    public int avatarId { get; set; }   // index trong AvatarConfig
    // Số phiên bản save, +1 mỗi lần đẩy lên cloud (User.SaveVersion gốc) — server chỉ nhận bản có số lớn hơn bản đang giữ.
    public int saveVersion { get; set; }

    public double firstTimeLogin { get; set; } = StaticValue.defaultDateMiliseconds;
    public double lastDayLogin { get; set; } = StaticValue.defaultDateMiliseconds;
    public int loginDayIndex { get; set; }
    public int firstVersion { get; set; }

    public List<int> completedTutorials { get; set; } = new List<int>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_PROFILE;
    }

    public override void ValidateData()
    {
        DateTime now = GameUtils.GetTimeNow();
        double nowMiliseconds = now.ToMiliseconds();

        if (firstTimeLogin == StaticValue.defaultDateMiliseconds)
        {
            firstTimeLogin = nowMiliseconds;
        }

        // lastDayLogin ở tương lai = giờ máy bị chỉnh → kéo về hiện tại
        if (lastDayLogin > nowMiliseconds)
        {
            lastDayLogin = nowMiliseconds;
        }

        isDataChanged = true;
    }

    public override void RefreshNewDay()
    {
        base.RefreshNewDay();

        lastDayLogin = GameUtils.GetTimeNow().ToMiliseconds();
        loginDayIndex++;
    }

    public bool IsTutorialCompleted(int tutorialId)
    {
        return completedTutorials.Contains(tutorialId);
    }

    public void CompleteTutorial(int tutorialId)
    {
        if (completedTutorials.Contains(tutorialId) == false)
        {
            completedTutorials.Add(tutorialId);
            isDataChanged = true;
        }
    }
}

using System;

// Trạng thái tutorial (FTUE) — UserController.efw/efx gốc. Stateless, lưu ở GameData.userData.profile.completedTutorials
// (List<int>, theo kiểu StickIdle). Tutorial gốc là tay chỉ mềm: mỗi màn tự hỏi IsCompleted để Show/Hide TutorialUI
// của nó và tự gọi Complete khi người chơi làm xong — không có controller dẫn từng bước. Xem DecodedData/TUTORIAL_MODEL.md.
public static class TutorialService
{
    // GameResources.IsTutorialEnabled && remote config "tutorials_enabled" (TutorialUI.kxg gốc) — v41 đều bật.
    public static bool IsEnabled = true;

    // UserController.FtueStepCompleted gốc: bắn sau khi lưu step mới → các màn đang mở cập nhật tay chỉ.
    public static event Action<FtueStep> StepCompleted;

    private static UserProfileData Profile => GameData.userData.profile;

    public static bool IsCompleted(FtueStep step)
    {
        return Profile.IsTutorialCompleted((int)step);
    }

    // efx gốc: chỉ ghi lần đầu, lưu ngay (Save(true)) rồi báo event. Trả true nếu step vừa được ghi.
    public static bool Complete(FtueStep step)
    {
        if (IsCompleted(step))
        {
            return false;
        }

        Profile.CompleteTutorial((int)step);
        GameData.Save(true);
        DebugCustom.Log("[FTUE] complete " + step);
        StepCompleted?.Invoke(step);
        return true;
    }
}

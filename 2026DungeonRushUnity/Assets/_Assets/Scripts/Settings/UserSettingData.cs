public class UserSettingData : BaseUserData
{
    public bool isSoundOn { get; set; } = true;
    public bool isMusicOn { get; set; } = true;
    public bool isHapticOn { get; set; } = true;
    // Âm lượng 0..1 (Settings.AudioVolume / MusicVolume gốc, kéo slider ở UIProfilePopup). 0 = tắt.
    public float soundVolume { get; set; } = 1f;
    public float musicVolume { get; set; } = 1f;

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_SETTING;
    }
}

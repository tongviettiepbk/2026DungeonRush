public class UserSettingData : BaseUserData
{
    public bool isSoundOn { get; set; } = true;
    public bool isMusicOn { get; set; } = true;
    public bool isHapticOn { get; set; } = true;

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_SETTING;
    }
}

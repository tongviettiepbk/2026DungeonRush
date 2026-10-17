// eu.L gốc: lấy chuỗi theo key bảng gốc (VD "Common.Claim") ở ngôn ngữ đang dùng.
public static class Loc
{
    public static string L(string key)
    {
        return LocalizeManager.Instance.GetLocalizeText(key);
    }

    // Data cũ chưa chắc có key trong bảng (VD 1 món thiếu tên dịch) → giữ tên mặc định.
    public static string Or(string key, string fallback)
    {
        return LocalizeManager.Instance.HasKey(key) ? LocalizeManager.Instance.GetLocalizeText(key) : fallback;
    }

    // ddk gốc: tên rarity (Common.Rarity.<tên>).
    public static string Rarity(Rarity rarity)
    {
        return L("Common.Rarity." + rarity);
    }

    // Chuỗi có {0}, {1}...
    public static string L(string key, params object[] args)
    {
        return LocalizeManager.Instance.GetLocalizeTextFormat(key, args);
    }
}

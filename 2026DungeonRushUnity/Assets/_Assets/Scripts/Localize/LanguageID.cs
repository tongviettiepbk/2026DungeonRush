// 10 ngôn ngữ của game gốc, đúng thứ tự LanguageSelectPopup.Languages (en, tr, ar, fr, de, it, ja, ko, es, pt-BR).
// File chuỗi: Resources/Json/Localize/Languages/{id}-{tên enum}.json (trích từ StringTables gốc, key dạng "Popup.X.Y").
public enum LanguageID
{
    English = 0,
    Turkish = 1,
    Arabic = 2,
    French = 3,
    German = 4,
    Italian = 5,
    Japanese = 6,
    Korean = 7,
    Spanish = 8,
    PortugueseBR = 9
}

public static class LanguageCode
{
    // Mã locale gốc (LocalizationController lưu chuỗi mã này trong Settings).
    private static readonly string[] CODES = { "en", "tr", "ar", "fr", "de", "it", "ja", "ko", "es", "pt-BR" };

    public static string ToCode(this LanguageID id)
    {
        int i = (int)id;
        return i >= 0 && i < CODES.Length ? CODES[i] : CODES[0];
    }

    public static bool TryParse(string code, out LanguageID id)
    {
        for (int i = 0; i < CODES.Length; i++)
        {
            if (CODES[i] == code)
            {
                id = (LanguageID)i;
                return true;
            }
        }
        id = LanguageID.English;
        return false;
    }
}

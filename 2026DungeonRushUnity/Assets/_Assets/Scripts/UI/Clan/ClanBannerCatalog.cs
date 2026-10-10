using UnityEngine;

// Bộ hình banner clan (ClanBannerCatalog gốc): 8 nền + 8 lớp màu nền, 10 màu nền, 20 icon, 8 màu icon.
// Asset sinh bằng ClanPrefabBuilder theo thứ tự gốc (DecodedData/tables/ClanBannerCatalog.names.json).
[CreateAssetMenu(menuName = "DungeonRush/Clan Banner Catalog")]
public class ClanBannerCatalog : ScriptableObject
{
    public Sprite[] backgroundSprites;
    public Sprite[] backgroundOverlaySprites;
    public Color[] backgroundColors;
    public Sprite[] imageSprites;
    public Color[] imageColors;

    public int BackgroundTypeCount => backgroundSprites != null ? backgroundSprites.Length : 0;
    public int BackgroundColorCount => backgroundColors != null ? backgroundColors.Length : 0;
    public int ImageTypeCount => imageSprites != null ? imageSprites.Length : 0;
    public int ImageColorCount => imageColors != null ? imageColors.Length : 0;

    // fqv gốc: banner mặc định.
    public ClanBannerData Default => new ClanBannerData(0, 0, 0, 0);

    public Sprite GetBackground(int id) => Pick(backgroundSprites, id);
    public Sprite GetBackgroundOverlay(int id) => Pick(backgroundOverlaySprites, id);
    public Color GetBackgroundColor(int id) => Pick(backgroundColors, id, Color.white);
    public Sprite GetImage(int id) => Pick(imageSprites, id);
    public Color GetImageColor(int id) => Pick(imageColors, id, Color.white);

    private static Sprite Pick(Sprite[] arr, int id)
    {
        if (arr == null || arr.Length == 0) return null;
        return arr[Mathf.Clamp(id, 0, arr.Length - 1)];
    }

    private static Color Pick(Color[] arr, int id, Color fallback)
    {
        if (arr == null || arr.Length == 0) return fallback;
        return arr[Mathf.Clamp(id, 0, arr.Length - 1)];
    }
}

using UnityEngine;
using UnityEngine.UI;

// Hiển thị banner clan (ClanBannerView gốc): nền + lớp màu nền (tô backgroundColor) + icon (tô imageColor) + vương miện Champion.
public class ClanBannerView : MonoBehaviour
{
    public ClanBannerCatalog Catalog;
    public Image BackgroundImage;
    public Image BackgroundOverlayImage;
    public Image ImageLayer;
    public Image CrownImage;

    // fsx gốc.
    public void SetBanner(ClanBannerData data)
    {
        if (data == null) data = Catalog != null ? Catalog.Default : new ClanBannerData();
        SetBanner(data.BackgroundTypeId, data.BackgroundColorId, data.ImageTypeId, data.ImageColorId);
    }

    // fsy gốc.
    public void SetBanner(int backgroundTypeId, int backgroundColorId, int imageTypeId, int imageColorId)
    {
        if (Catalog == null) return;
        if (BackgroundImage != null)
        {
            BackgroundImage.sprite = Catalog.GetBackground(backgroundTypeId);
            BackgroundImage.color = Color.white;
        }
        if (BackgroundOverlayImage != null)
        {
            Sprite overlay = Catalog.GetBackgroundOverlay(backgroundTypeId);
            BackgroundOverlayImage.sprite = overlay;
            BackgroundOverlayImage.color = Catalog.GetBackgroundColor(backgroundColorId);
            BackgroundOverlayImage.enabled = overlay != null;
        }
        if (ImageLayer != null)
        {
            ImageLayer.sprite = Catalog.GetImage(imageTypeId);
            ImageLayer.color = Catalog.GetImageColor(imageColorId);
            ImageLayer.preserveAspect = true;
        }
    }

    // fsz gốc: vương miện clan Champion.
    public void SetCrown(bool visible)
    {
        if (CrownImage != null) CrownImage.gameObject.SetActive(visible);
    }
}

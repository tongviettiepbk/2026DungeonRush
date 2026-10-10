using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 món trong gói rương (ChestBundleOfferContentIcon gốc, prefab ChestContentUI): icon + "x{số}"; bấm vào rương thì mở info.
public class ChestBundleOfferContentIcon : MonoBehaviour
{
    public Image Icon;
    public TextMeshProUGUI CountText;
    public Button Button;

    private ChestData chest;

    // flq gốc. chest = null với ô gem.
    public void Setup(Sprite icon, string countText, ChestData chestData)
    {
        chest = chestData;
        if (Icon != null) Icon.sprite = icon;
        ShopUIUtil.SetText(CountText, countText);
        if (Button != null)
        {
            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(OnClicked);
        }
    }

    // flr gốc.
    private void OnClicked()
    {
        if (chest == null) return;
        UIChestInfoPopup popup = UIManager.Instance.LoadUI(UIKey.ChestInfoPopup) as UIChestInfoPopup;
        if (popup != null) popup.Setup(chest);
    }
}

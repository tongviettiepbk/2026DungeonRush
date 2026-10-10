using UnityEngine;

// Định dạng chung UI Store.
public static class ShopUIUtil
{
    public const string NOT_ENOUGH_GEMS = "Not enough gems!";   // Errors.NotEnoughGems

    // Số thưởng rút gọn như nhãn gốc: 255 / 1.1K / 2.4K / 1.5M.
    public static string FormatAmount(int amount)
    {
        if (amount >= 1000000) return (amount / 1000000f).ToString("0.#") + "M";
        if (amount >= 1000) return (amount / 1000f).ToString("0.#") + "K";
        return amount.ToString();
    }

    // "<color=#RRGGBB>Rarity</color>" theo bảng màu rarity (GameResources.jgf gốc).
    public static string ColoredRarity(Rarity rarity)
    {
        return Colored(rarity.ToString(), UITabWing.GetRarityColor(rarity));
    }

    public static string Colored(string text, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }

    // ev.ehx gốc: 1200 → "20m", 323 → "5m 23s".
    public static string FormatDuration(long seconds)
    {
        long minutes = seconds / 60;
        long rest = seconds % 60;
        if (minutes <= 0) return rest + "s";
        return rest > 0 ? minutes + "m " + rest + "s" : minutes + "m";
    }

    public static void SetText(TMPro.TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    public static void SetActive(Component c, bool active)
    {
        if (c != null) c.gameObject.SetActive(active);
    }

    public static void Toast(string message)
    {
        UIManager.Instance.ShowToastMessage(message, isLocalize: false);
    }

    // Cập nhật số gem / hộp loot / ô pet-cape trên lobby sau khi mua.
    public static void RefreshLobby()
    {
        UIMainLobby lobby = GameController.Instance != null ? GameController.Instance.uiLobby : null;
        if (lobby == null) return;
        lobby.Refresh();
        lobby.UpdateLootTicketText();
    }
}

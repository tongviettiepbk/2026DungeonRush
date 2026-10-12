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

    // ev.ehx gốc: lấy tối đa 2 đơn vị khác 0 theo thứ tự d/h/m/s. 1200 → "20m", 323 → "5m 23s", 9480 → "2h 38m".
    public static string FormatDuration(long seconds)
    {
        long[] values = { seconds / 86400, seconds % 86400 / 3600, seconds % 3600 / 60, seconds % 60 };
        string[] units = { "d", "h", "m", "s" };
        string text = string.Empty;
        int count = 0;
        for (int i = 0; i < values.Length && count < 2; i++)
        {
            if (values[i] <= 0) continue;
            text += (count > 0 ? " " : string.Empty) + values[i] + units[i];
            count++;
        }
        return count > 0 ? text : "0s";
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

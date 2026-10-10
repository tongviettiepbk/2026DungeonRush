using UnityEngine;

// Định dạng chung UI Clan.
public static class ClanUIUtil
{
    // rm.iox gốc: Power rút gọn kèm icon.
    public static string PowerText(double power)
    {
        return "<sprite=0>" + (power > 0d ? power.ToLetter() : "0");
    }

    public static void SetActive(Component c, bool active)
    {
        if (c != null) c.gameObject.SetActive(active);
    }

    public static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    public static void SetText(TMPro.TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    public static void Toast(string message)
    {
        if (!string.IsNullOrEmpty(message)) UIManager.Instance.ShowToastMessage(message, isLocalize: false);
    }
}

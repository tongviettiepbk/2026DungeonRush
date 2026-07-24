using System;
using UnityEngine;

public static class GameUtils
{
    public static string GetColorHexCode(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    public static string GetNewUserName()
    {
        string now = ((long)DateTime.UtcNow.ToMiliseconds()).ToString();
        string tail = now.Length > 6 ? now.Substring(now.Length - 6) : now;
        return "Player" + tail;
    }

    // Thời gian hiện tại của game. Base dùng giờ máy;
    // sau này có server time (MasterInfo) thì chỉ cần sửa tại đây.
    public static DateTime GetTimeNow()
    {
        return DateTime.Now;
    }
}

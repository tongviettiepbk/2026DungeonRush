using System;
using UnityEngine;

public class StaticValue
{
    public const string SCENE_ROOT = "Root";
    public const string SCENE_LOGIN = "Login";
    public const string SCENE_LOBBY = "Lobby";

    public const string SORTING_LAYER_UI = "UI";

    // Combat team tags (port từ StickIdle). Cần khai báo tag tương ứng trong Unity Tags & Layers.
    public const string TAG_TEAM_A = "TeamA";
    public const string TAG_TEAM_B = "TeamB";

    public static readonly DateTime defaultDate = new DateTime(2026, 1, 1);
    public static double defaultDateMiliseconds = defaultDate.ToMiliseconds();
    public static WaitForEndOfFrame waitEndFrame = new WaitForEndOfFrame();
}

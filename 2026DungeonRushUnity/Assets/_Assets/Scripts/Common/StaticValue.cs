using System;
using UnityEngine;

public class StaticValue
{
    public const string SCENE_ROOT = "Root";
    public const string SCENE_LOGIN = "Login";
    public const string SCENE_MAINGAME = "MainGame";

    public const string SORTING_LAYER_UI = "UI";
    public const string SORTING_LAYER_DEFAULT = "Default";

    // Canvas lobby (UIMainGame2) ở layer Default order 100 (giá trị gốc) → mọi thứ trong world
    // (thanh máu, chữ damage, FX) phải ở Default với order < 100 để không vẽ đè lên UI.
    public const int SORTING_ORDER_WORLD_HUD = 50;

    // Combat team tags (port từ StickIdle). Cần khai báo tag tương ứng trong Unity Tags & Layers.
    public const string TAG_TEAM_A = "TeamA";
    public const string TAG_TEAM_B = "TeamB";

    public static readonly DateTime defaultDate = new DateTime(2026, 1, 1);
    public static double defaultDateMiliseconds = defaultDate.ToMiliseconds();
    public static WaitForEndOfFrame waitEndFrame = new WaitForEndOfFrame();
}

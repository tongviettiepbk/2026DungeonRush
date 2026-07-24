using System;
using UnityEngine;

public class StaticValue
{
    public const string SCENE_ROOT = "Root";
    public const string SCENE_LOGIN = "Login";
    public const string SCENE_LOBBY = "Lobby";

    public static readonly DateTime defaultDate = new DateTime(2026, 1, 1);
    public static double defaultDateMiliseconds = defaultDate.ToMiliseconds();
    public static WaitForEndOfFrame waitEndFrame = new WaitForEndOfFrame();
}

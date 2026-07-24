using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public enum LogColor
{
    None = 0,
    Yellow = 1,
    Green = 2,
    Cyan = 3
}

public class DebugCustom
{
    private const string FORMAT_COLOR_CONTENT = "<color=#{0}>{1}</color>";

    // Sau này GameConfig sẽ set giá trị này theo build mode (Developer/Production)
    public static bool enableDebugLog;

    private static bool IsEnableLog()
    {
#if UNITY_EDITOR
        return true;
#endif
#pragma warning disable CS0162
        return enableDebugLog;
#pragma warning restore CS0162
    }

    public static void Log(object content, LogColor color = LogColor.None)
    {
        if (IsEnableLog())
        {
            if (color == LogColor.None)
            {
                Debug.Log(content);
            }
            else
            {
                string hexColor = string.Empty;

                switch (color)
                {
                    case LogColor.Yellow: hexColor = GameUtils.GetColorHexCode(Color.yellow); break;
                    case LogColor.Green: hexColor = GameUtils.GetColorHexCode(Color.green); break;
                    case LogColor.Cyan: hexColor = GameUtils.GetColorHexCode(Color.cyan); break;
                }

                if (string.IsNullOrEmpty(hexColor))
                {
                    Debug.Log(content);
                }
                else
                {
                    Debug.Log(string.Format(FORMAT_COLOR_CONTENT, hexColor, content));
                }
            }
        }
    }

    public static void LogFormat(string format, params object[] args)
    {
        if (IsEnableLog())
        {
            Debug.Log(string.Format(format, args));
        }
    }

    public static void LogError(object content)
    {
        if (IsEnableLog())
        {
            Debug.LogError(content);
        }
    }

    public static void LogWarning(object content)
    {
        if (IsEnableLog())
        {
            Debug.LogWarning(content);
        }
    }

    public static void LogWarning(object message, Object context)
    {
        if (IsEnableLog())
        {
            Debug.LogWarning(message, context);
        }
    }
}

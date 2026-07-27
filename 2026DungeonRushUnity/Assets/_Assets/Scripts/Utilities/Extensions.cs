using System;
using System.Collections;
using UnityEngine;

public static class Extensions
{
    private const string FORMAT_LETTER = "{0}{1}";
    private const string FORMAT_DECIMAL = "0.##";

    public static double ToMiliseconds(this DateTime time)
    {
        DateTime beginDate = new DateTime(1970, 1, 1);
        TimeSpan ts = time - beginDate;
        return ts.TotalMilliseconds;
    }

    public static DateTime ToDateTime(this double miliseconds)
    {
        return new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(miliseconds);
    }

    #region Coroutine (port từ StickIdle)
    public static void StartDelayAction(this MonoBehaviour mono, float time, Action callback)
    {
        if (mono.gameObject.activeInHierarchy)
        {
            mono.StartCoroutine(Delay(callback, time));
        }
        else
        {
            DebugCustom.LogFormat("[StartDelayAction] {0} inactive", mono.name);
        }
    }

    private static IEnumerator Delay(Action callBack, float time)
    {
        yield return Yielder.Get(time);

        if (callBack != null)
            callBack();
    }
    #endregion

    #region Vector (port từ StickIdle)
    public static Vector3 GetRandomPointAround(this Vector3 center, float minRadius, float maxRadius)
    {
        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        float radius = UnityEngine.Random.Range(minRadius, maxRadius);
        return center + new Vector3(dir.x, dir.y, 0f) * radius;
    }
    #endregion

    #region Number format (port từ StickIdle)
    public static string ToLetter<T>(this T number, bool isRounded = true) where T : IConvertible
    {
        double value = Convert.ToDouble(number);
        if (value < 1000)
        {
            if (isRounded)
            {
                return Math.Truncate(value).ToString();
            }
            else
            {
                return value.ToString(FORMAT_DECIMAL);
            }
        }
        else
        {
            int logValue = (int)Math.Floor(Math.Log10(value));
            double dividedValue = value / Math.Pow(10, logValue - logValue % 3);
            string letter = GetLetter(logValue / 3);

            return string.Format(FORMAT_LETTER, dividedValue.ToString(FORMAT_DECIMAL), letter);
        }
    }

    private static string GetLetter(int index)
    {
        string result = "";
        while (index > 0)
        {
            index--;
            char letter = (char)('A' + index % 26);
            result = letter + result;
            index /= 26;
        }

        return result;
    }
    #endregion
}

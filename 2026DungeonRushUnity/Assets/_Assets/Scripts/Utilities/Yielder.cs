using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Yielder
{
    private static Dictionary<float, WaitForSeconds> yields = new Dictionary<float, WaitForSeconds>();
    private static Dictionary<float, WaitForSecondsRealtime> realtimeYields = new Dictionary<float, WaitForSecondsRealtime>();

    public static WaitForSeconds Get(float delay)
    {
        if (yields.ContainsKey(delay))
        {
            return yields[delay];
        }
        else
        {
            WaitForSeconds tmp = new WaitForSeconds(delay);
            yields.Add(delay, tmp);
            return tmp;
        }
    }

    public static WaitForSecondsRealtime GetRealtime(float delay)
    {
        if (realtimeYields.ContainsKey(delay))
        {
            return realtimeYields[delay];
        }

        WaitForSecondsRealtime tmp = new WaitForSecondsRealtime(delay);
        realtimeYields.Add(delay, tmp);
        return tmp;
    }
}
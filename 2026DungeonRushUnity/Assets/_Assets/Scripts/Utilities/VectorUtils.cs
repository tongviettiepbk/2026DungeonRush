using System.Collections;
using UnityEngine;


public class VectorUtils
{
    public static bool IsInRange(Vector3 startPosition, Vector3 endPosition, float range)
    {
        float sqrRange = range * range;
        return (endPosition - startPosition).sqrMagnitude <= sqrRange;
    }

    public static float SqrDistance(Vector3 startPosition, Vector3 endPoisition)
    {
        return (endPoisition - startPosition).sqrMagnitude;
    }
}

using UnityEngine;

// Tiện ích toán học dùng chung — port từ StickIdle (chỉ mang phần đang cần: Parabola cho đạn bay vòng cung).
public static class MathUtils
{
    // Nội suy điểm trên đường parabol từ start -> end với độ cao đỉnh height, tham số t trong [0..1].
    public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float t)
    {
        float Func(float x) => 4 * (-height * x * x + height * x);

        var mid = Vector3.Lerp(start, end, t);

        return new Vector3(mid.x, Func(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
    }

    public static Vector2 Parabola(Vector2 start, Vector2 end, float height, float t)
    {
        float Func(float x) => 4 * (-height * x * x + height * x);

        var mid = Vector2.Lerp(start, end, t);

        return new Vector2(mid.x, Func(t) + Mathf.Lerp(start.y, end.y, t));
    }
}

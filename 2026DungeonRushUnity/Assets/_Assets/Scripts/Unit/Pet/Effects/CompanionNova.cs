using System;
using System.Collections.Generic;
using UnityEngine;

// Vòng sáng nở dần (gốc CompanionHealNovaEffect): tâm bám theo 'anchor' (hero), bán kính đi từ
// startRadius → endRadius trong 'duration'. Mỗi 'tickInterval' quét enemy trong bán kính hiện tại;
// MỖI enemy chỉ bị trúng 1 lần / 1 vòng (HashSet) → onHit(enemy). Hình = vòng LineRenderer.
public class CompanionNova : MonoBehaviour
{
    private const int CIRCLE_SEGMENTS = 48;

    private BaseUnit anchor;
    private Func<Vector3, float, List<BaseUnit>> queryEnemies;
    private Action<BaseUnit> onHit;
    private float startRadius;
    private float endRadius;
    private float duration;
    private float tickInterval;
    private float elapsed;
    private float tickTimer;
    private LineRenderer circle;
    private readonly HashSet<BaseUnit> hitUnits = new HashSet<BaseUnit>();

    public static CompanionNova Spawn(BaseUnit anchor, float startRadius, float endRadius, float duration, float tickInterval,
        Color color, Func<Vector3, float, List<BaseUnit>> queryEnemies, Action<BaseUnit> onHit)
    {
        GameObject go = new GameObject("CompanionNova");
        go.transform.position = anchor.Transform.position;
        CompanionNova nova = go.AddComponent<CompanionNova>();
        nova.anchor = anchor;
        nova.startRadius = startRadius;
        nova.endRadius = endRadius;
        nova.duration = Mathf.Max(0.01f, duration);
        nova.tickInterval = Mathf.Max(0.01f, tickInterval);
        nova.queryEnemies = queryEnemies;
        nova.onHit = onHit;
        nova.circle = CompanionFx.CreateLine(go.transform, "Circle", color, 0.08f);
        nova.circle.loop = true;
        nova.circle.enabled = true;
        nova.Tick();
        return nova;
    }

    private float CurrentRadius => Mathf.Lerp(startRadius, endRadius, Mathf.Clamp01(elapsed / duration));

    private void Update()
    {
        if (anchor != null)
        {
            transform.position = anchor.Transform.position;
            if (anchor.isPause)
            {
                return;
            }
        }

        float dt = Time.deltaTime * GameController.Instance.gameSpeed;
        elapsed += dt;
        tickTimer += dt;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            Tick();
        }

        DrawCircle(CurrentRadius);

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void Tick()
    {
        List<BaseUnit> enemies = queryEnemies(transform.position, CurrentRadius);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (hitUnits.Add(enemies[i]))
            {
                onHit?.Invoke(enemies[i]);
            }
        }
    }

    private void DrawCircle(float radius)
    {
        circle.positionCount = CIRCLE_SEGMENTS;
        Vector3 center = transform.position;
        for (int i = 0; i < CIRCLE_SEGMENTS; i++)
        {
            float a = i * Mathf.PI * 2f / CIRCLE_SEGMENTS;
            circle.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
        }
    }
}

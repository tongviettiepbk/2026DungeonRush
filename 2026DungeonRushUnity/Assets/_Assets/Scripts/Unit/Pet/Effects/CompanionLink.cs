using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tia nối pet → chuỗi mục tiêu (gốc CompanionLightningEffect / CompanionBeamEffect / ChainHealEffect):
//   Pha 1 TRAVEL: đầu tia chạy lần lượt qua từng mục tiêu với 'travelSpeed'; chạm mục tiêu i → onReach(i).
//   Pha 2 ACTIVE: giữ tia trong 'activeDuration', mỗi 'tickInterval' → onTick().
// Mục tiêu chết/biến mất → bị bỏ khỏi chuỗi (hình). Hình = LineRenderer; 'jagged' cho tia sét.
public class CompanionLink
{
    private readonly MonoBehaviour runner;
    private readonly LineRenderer line;
    private readonly bool jagged;
    private readonly List<Vector3> points = new List<Vector3>();
    private Coroutine routine;

    private const int JAG_SEGMENTS = 6;
    private const float JAG_AMPLITUDE = 0.15f;

    public CompanionLink(MonoBehaviour runner, Transform parent, Color color, float width, bool jagged)
    {
        this.runner = runner;
        this.jagged = jagged;
        line = CompanionFx.CreateLine(parent, "CompanionLink", color, width);
    }

    public void Play(Transform source, List<BaseUnit> targets, float travelSpeed, float activeDuration, float tickInterval,
        Action<int> onReach, Action onTick, Func<bool> isPaused)
    {
        Stop();
        routine = runner.StartCoroutine(Run(source, targets, travelSpeed, activeDuration, tickInterval, onReach, onTick, isPaused));
    }

    public void Stop()
    {
        if (routine != null)
        {
            runner.StopCoroutine(routine);
            routine = null;
        }
        line.enabled = false;
    }

    private IEnumerator Run(Transform source, List<BaseUnit> targets, float travelSpeed, float activeDuration, float tickInterval,
        Action<int> onReach, Action onTick, Func<bool> isPaused)
    {
        line.enabled = true;

        // Pha 1: đầu tia chạy qua từng mục tiêu.
        Vector3 head = source.position;
        for (int i = 0; i < targets.Count; i++)
        {
            while (IsAlive(targets[i]))
            {
                if (!isPaused())
                {
                    float step = Mathf.Max(0.01f, travelSpeed) * Time.deltaTime * GameController.Instance.gameSpeed;
                    head = Vector3.MoveTowards(head, targets[i].centerBodyPoint.position, step);
                }

                Draw(source, targets, i, head);
                if (VectorUtils.IsInRange(head, targets[i].centerBodyPoint.position, 0.05f))
                {
                    break;
                }
                yield return null;
            }

            if (IsAlive(targets[i]))
            {
                onReach?.Invoke(i);
            }
        }

        // Pha 2: giữ tia + tick.
        float elapsed = 0f;
        float tickTimer = 0f;
        while (elapsed < activeDuration && HasAliveTarget(targets))
        {
            if (!isPaused())
            {
                float dt = Time.deltaTime * GameController.Instance.gameSpeed;
                elapsed += dt;
                tickTimer += dt;
                if (tickInterval > 0f && tickTimer >= tickInterval)
                {
                    tickTimer -= tickInterval;
                    onTick?.Invoke();
                }
            }

            Draw(source, targets, targets.Count, Vector3.zero);
            yield return null;
        }

        line.enabled = false;
        routine = null;
    }

    // Vẽ: source → các mục tiêu đã chạm (index < reached) → đầu tia (nếu đang travel).
    private void Draw(Transform source, List<BaseUnit> targets, int reached, Vector3 head)
    {
        points.Clear();
        points.Add(source.position);
        for (int i = 0; i < reached && i < targets.Count; i++)
        {
            if (IsAlive(targets[i]))
            {
                points.Add(targets[i].centerBodyPoint.position);
            }
        }
        if (reached < targets.Count)
        {
            points.Add(head);
        }

        if (!jagged)
        {
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                line.SetPosition(i, points[i]);
            }
            return;
        }

        // Tia sét: chia mỗi đoạn thành vài khúc lệch ngẫu nhiên.
        int count = (points.Count - 1) * JAG_SEGMENTS + 1;
        line.positionCount = Mathf.Max(0, count);
        int index = 0;
        for (int s = 0; s < points.Count - 1; s++)
        {
            Vector3 a = points[s];
            Vector3 b = points[s + 1];
            Vector3 side = new Vector3(-(b - a).y, (b - a).x, 0f).normalized;
            for (int k = 0; k < JAG_SEGMENTS; k++)
            {
                float t = (float)k / JAG_SEGMENTS;
                Vector3 p = Vector3.Lerp(a, b, t);
                if (k > 0)
                {
                    p += side * UnityEngine.Random.Range(-JAG_AMPLITUDE, JAG_AMPLITUDE);
                }
                line.SetPosition(index++, p);
            }
        }
        if (points.Count > 0 && index < line.positionCount)
        {
            line.SetPosition(index, points[points.Count - 1]);
        }
    }

    private static bool IsAlive(BaseUnit unit)
    {
        return unit != null && unit.isTargetable;
    }

    private static bool HasAliveTarget(List<BaseUnit> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (IsAlive(targets[i]))
            {
                return true;
            }
        }
        return false;
    }
}

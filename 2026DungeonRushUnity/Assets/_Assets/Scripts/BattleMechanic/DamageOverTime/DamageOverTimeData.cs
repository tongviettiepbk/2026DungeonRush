using System.Collections;
using UnityEngine;


public class DamageOverTimeData
{
    public BaseUnit attacker { get; private set; }
    public DamageOverTimeType type { get; private set; }
    public DamageOverTimeCalculateType calculateType { get; private set; }
    public float multiply { get; private set; }
    public float duration { get; private set; }
    public float rate { get; private set; }
    public int maxStacks { get; private set; }
    public bool isPureDamage { get; private set; }

    public DamageOverTimeData() { }

    public DamageOverTimeData(BaseUnit attacker, DamageOverTimeType type, DamageOverTimeCalculateType calculateType, float duration,
        float multiply, float rate = 1f, int maxStacks = 1, bool isPureDamage = false)
    {
        this.attacker = attacker;
        this.type = type;
        this.calculateType = calculateType;
        this.duration = duration;
        this.multiply = multiply;
        this.rate = rate;
        this.maxStacks = maxStacks;
        this.isPureDamage = isPureDamage;
    }

    public virtual DamageOverTimeData Clone()
    {
        DamageOverTimeData data = new DamageOverTimeData();

        data.attacker = attacker;
        data.type = type;
        data.calculateType = calculateType;
        data.duration = duration;
        data.multiply = multiply;
        data.rate = rate;
        data.maxStacks = maxStacks;
        data.isPureDamage = isPureDamage;

        return data;
    }

    public void ReduceDuration(float time)
    {
        duration -= time;
    }
}

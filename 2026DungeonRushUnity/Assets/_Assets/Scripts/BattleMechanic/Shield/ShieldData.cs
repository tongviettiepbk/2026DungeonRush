using System.Collections;
using UnityEngine;


public class ShieldData
{
    public BaseUnit caster { get; private set; }
    public ShieldId id { get; private set; }
    public ShieldType type { get; private set; }
    public int level { get; private set; }
    public double value { get; private set; }
    public float duration { get; private set; }

    public ShieldData(BaseUnit caster, ShieldId id, ShieldType type, int level, double value, float duration)
    {
        this.caster = caster;
        this.id = id;
        this.type = type;
        this.level = level;
        this.value = value;
        this.duration = duration;
    }

    public ShieldData Clone()
    {
        float shieldRate = 1f;
        return new ShieldData(caster, id, type, level, value * shieldRate, duration);
    }

    public void ReduceDuration(float time)
    {
        duration -= time;
    }

    public void ReduceValue(double value)
    {
        this.value -= value;
    }
}

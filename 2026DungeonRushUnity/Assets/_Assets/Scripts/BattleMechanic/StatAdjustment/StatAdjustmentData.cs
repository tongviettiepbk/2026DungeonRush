using System.Collections;
using UnityEngine;


public class StatAdjustmentData
{
    public BaseUnit attacker { get; private set; }
    public StatModifier modifier { get; private set; }
    public float duration { get; private set; }
    public float chance { get; private set; }
    public int maxStacks { get; private set; }

    public StatAdjustmentData() { }

    public StatAdjustmentData(BaseUnit attacker, StatModifier modifier, float duration, float chance = 1f, int maxStacks = 1)
    {
        this.attacker = attacker;
        this.modifier = modifier;
        this.duration = duration;
        this.chance = chance;
        this.maxStacks = maxStacks;
    }

    public StatAdjustmentData Clone()
    {
        StatAdjustmentData data = new StatAdjustmentData();
        data.attacker = attacker;
        data.modifier = new StatModifier(modifier.source, modifier.type, modifier.value);
        data.duration = duration;
        data.chance = chance;
        data.maxStacks = maxStacks;

        return data;
    }

    public bool IsStackable(StatAdjustmentData data)
    {
        if (data.chance == chance && data.maxStacks == maxStacks)
        {
            if (data.attacker.battleId == attacker.battleId)
            {
                return data.modifier.value == modifier.value;
            }
            // TODO(follow-stick): nhánh gộp buff theo heroData.id bỏ tạm — BaseHero chưa port.
        }

        return false;
    }

    public virtual void ReduceDuration(float time)
    {
        duration -= time;
    }
}

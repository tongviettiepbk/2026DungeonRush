using UnityEngine;
using System.Collections;


/// <summary>
/// Hiệu ứng khống chế của đòn đánh
/// </summary>
public class CrowdControlData
{
    public BaseUnit attacker { get; private set; }
    public CrowdControlType type { get; private set; }
    public float duration { get; private set; }
    public float rate { get; private set; }
    public bool isUnableToResist { get; private set; }
    public double value { get; private set; } // Biến dùng thêm nếu các cc cần thêm thông số (ví dụ KnockBack bao xa)
    public bool isKnockCC => type == CrowdControlType.KnockBack || type == CrowdControlType.KnockDown || type == CrowdControlType.KnockUp;

    public CrowdControlData() { }

    public CrowdControlData(BaseUnit attacker, CrowdControlType type, float duration, float rate = 1f, float value = 0f, bool isUnableToResist = false)
    {
        this.attacker = attacker;
        this.type = type;
        this.duration = duration;
        this.rate = rate;
        this.value = value;
        this.isUnableToResist = isUnableToResist;
    }

    public CrowdControlData Clone()
    {
        CrowdControlData data = new CrowdControlData();

        data.attacker = attacker;
        data.type = type;
        data.duration = duration;
        data.rate = rate;
        data.value = value;
        data.isUnableToResist = isUnableToResist;

        return data;
    }

    public virtual void ReduceDuration(float time)
    {
        duration -= time;
    }

    public virtual void ReduceValue(double amount)
    {
        value -= amount;
    }
}

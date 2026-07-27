using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackData
{
    public BaseUnit attacker { get; private set; }
    public AttackType attackType { get; private set; }
    public CritType critType { get; private set; }
    public double damage { get; private set; }
    public bool damageOutScreen { get; private set; }
    public List<DamageOverTimeData> dots { get; private set; }
    public List<StatAdjustmentData> debuffs { get; private set; }
    public List<CrowdControlData> controls { get; private set; }

    public AttackData() { }

    public AttackData(BaseUnit attacker, AttackType attackType, CritType critType = CritType.Critable, double damage = 0f,
        List<DamageOverTimeData> dots = null, List<StatAdjustmentData> debuffs = null, List<CrowdControlData> controls = null, bool damageOutScreen = false)
    {
        this.attacker = attacker;
        this.attackType = attackType;
        this.critType = critType;
        this.damage = damage;
        this.dots = dots;
        this.debuffs = debuffs;
        this.controls = controls;
        this.damageOutScreen = damageOutScreen;
    }

    public void MultiplyDamage(float rate)
    {
        this.damage *= rate;
    }

    public void SetDamage(double damage)
    {
        this.damage = damage;
    }
}

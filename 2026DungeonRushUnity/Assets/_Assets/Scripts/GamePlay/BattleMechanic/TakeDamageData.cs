using System.Collections;
using UnityEngine;


public class TakeDamageData
{
    public BaseUnit attacker { get; private set; }
    public AttackType attackType { get; private set; }
    public double damage { get; private set; }
    public bool isCrit { get; private set; }

    public TakeDamageData() { }

    public TakeDamageData(BaseUnit attacker, AttackType attackType, double damage, bool isCrit = false)
    {
        this.attacker = attacker;
        this.attackType = attackType;
        this.damage = damage;
        this.isCrit = isCrit;
    }

    public TakeDamageData(ProcessedAttackData processedAttackData)
    {
        attacker = processedAttackData.attacker;
        attackType = processedAttackData.attackType;
        damage = processedAttackData.damage;
        isCrit = processedAttackData.isCrit;
    }
}

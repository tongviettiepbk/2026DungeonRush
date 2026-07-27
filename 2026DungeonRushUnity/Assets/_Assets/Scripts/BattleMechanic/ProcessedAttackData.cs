using System.Collections;
using UnityEngine;


public class ProcessedAttackData
{
    public BaseUnit attacker { get; private set; }
    public BaseUnit victim { get; private set; }
    public AttackType attackType { get; private set; }
    public double damage { get; private set; }
    public bool isCrit { get; private set; }

    public ProcessedAttackData() { }

    public ProcessedAttackData(BaseUnit attacker, BaseUnit victim, AttackType attackType, double damage, bool isCrit)
    {
        this.attacker = attacker;
        this.victim = victim;
        this.attackType = attackType;
        this.damage = damage;
        this.isCrit = isCrit;
    }
}

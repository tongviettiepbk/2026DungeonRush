using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Stats
{
    public double attack;
    public float multishot;
    public float basicAttackDamage = 1f;
    public float advancedAttack;
    public float attackSpeed;
    public float attackRange;
    public float critRate;
    public float critDamage = StatUtils.DEFAULT_CRIT_DAMAGE;
    public float skillDamage = 1f;
    public float enhancementSkillSlots = 1f;
    public float bossDamage = 1f;
    public float normalEnemyDamage = 1f;
    public float doubleShot;
    public float tripleShot;
    public float companionDamage = 1f;
    public float companionAttackSpeed = 1f;
    public float companionAttackBonus = 1f;

    public double maxHp;
    public double hpRecovery;

    public float evasionRate;
    public float skillHealingRate;
    public float moveSpeed;
    public float skillCooldownRemaining = 1f;
    public float multiplyGoldObtain = 1f;
    public float multiplyWelcomeBack = 1f;
    public float multiplyMineResearchSpeed = 1f;
    public float multiplyPickAxeGainSpeed = 1f;
    public float multiplyOreGain = 1f;
    public float multiplyShardExp = 1f;
    public float addedBattleTime;

    public Stats() { }

    public Stats(BaseStats baseStats)
    {
        attack = baseStats.attack;
        attackSpeed = baseStats.attackPerSecond;
        attackRange = baseStats.attackRange;

        maxHp = baseStats.maxHp;
        moveSpeed = baseStats.moveSpeed;
    }

    public double GetCombatPower()
    {
        double power = 1;

        power *= attack;
        power *= attackSpeed;
        power *= (1f + critRate);
        power *= critDamage;
        power *= basicAttackDamage;
        power *= companionDamage;
        power *= companionAttackSpeed;
        power *= companionAttackBonus;
        power *= skillDamage;
        power *= enhancementSkillSlots;
        power *= bossDamage;
        power *= (1f + doubleShot);
        power *= (1f + tripleShot);
        power *= 4f;

        return power;
    }
}

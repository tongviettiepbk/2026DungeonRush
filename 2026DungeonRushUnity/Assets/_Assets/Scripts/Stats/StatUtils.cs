using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StatUtils
{
    public const float DEFAULT_HERO_ATTACK_SPEED = 0f;
    // Gốc DungeonRush: PlayerBaseMoveSpeed = 2.0 (GameResources native). (Trước là 2.5 kế thừa StickIdle.)
    // LƯU Ý: CalculateMovementSpeed bên dưới còn nhân ×2 kiểu StickIdle — cần rà lại khi wire path modifier.
    public const float DEFAULT_MOVEMENT_SPEED = 2.0f;
    public const float DEFAULT_ATTACK_RANGE = 2f;
    public const float DEFAULT_CRIT_DAMAGE = 1.2f;
    public const float MAX_EVASION_RATE = 0.85f;
    public const float MAX_ATTACK_SPEED = 2.5f;

    #region Heroes
    public static Stats CalculateHeroStats(List<StatModifier> modifiers)
    {
        Stats stats = new Stats();

        stats.advancedAttack = CalculateAdditiveBonus(modifiers, StatModifierType.AdvancedAttack);
        stats.attack = CalculateAttack(modifiers, stats.advancedAttack);
        stats.attackSpeed = DEFAULT_HERO_ATTACK_SPEED + CalculateAttackSpeed(modifiers);

        stats.critRate = CalculateAdditiveBonus(modifiers, StatModifierType.CritRate);
        stats.critDamage = CalculateCritDamage(modifiers);
        stats.basicAttackDamage = CalculateBasicAttackBonus(modifiers);
        stats.attackRange = CalculateAttackRange(modifiers);
        stats.companionDamage = CalculateCompanionDamage(modifiers);
        stats.companionAttackSpeed = CalculateCompanionAttackSpeed(modifiers);
        stats.companionAttackBonus = 1f + CalculateAdditiveBonus(modifiers, StatModifierType.CompanionAttackBonus);
        stats.skillDamage = CalculateSkillDamage(modifiers);
        stats.enhancementSkillSlots = 1f + CalculateAdditiveBonus(modifiers, StatModifierType.EnhancementSkillSlots);
        stats.bossDamage = 1f + CalculateAdditiveBonus(modifiers, StatModifierType.BossDamageBonus);
        stats.normalEnemyDamage = CalculateNormalEnemyDamage(modifiers);
        stats.multishot = CalculateAdditiveBonus(modifiers, StatModifierType.Multishot);
        stats.doubleShot = CalculateAdditiveBonus(modifiers, StatModifierType.DoubleShot);
        stats.tripleShot = CalculateAdditiveBonus(modifiers, StatModifierType.TripleShot);

        stats.maxHp = CalculateHp(modifiers);
        stats.hpRecovery = CalculateHpRecovery(modifiers);
        stats.evasionRate = Mathf.Clamp(CalculateAdditiveBonus(modifiers, StatModifierType.EvasionRate), 0f, MAX_EVASION_RATE);
        stats.skillHealingRate = CalculateAdditiveBonus(modifiers, StatModifierType.SkillHealingRate);

        stats.moveSpeed = CalculateMovementSpeed(modifiers);
        stats.skillCooldownRemaining = CalculateSkillCooldown(modifiers);
        stats.multiplyGoldObtain = CalculateGoldObtain(modifiers);

        stats.multiplyWelcomeBack = 1f + CalculateAdditiveBonus(modifiers, StatModifierType.WelcomeBackRewards);
        stats.multiplyMineResearchSpeed = (float)CalculateMultiplyBonus(modifiers, StatModifierType.MineResearchSpeed);
        stats.multiplyPickAxeGainSpeed = (float)CalculateMultiplyBonus(modifiers, StatModifierType.PickaxeGainSpeed);
        stats.multiplyOreGain = 1f + CalculateAdditiveBonus(modifiers, StatModifierType.OreGain);
        stats.multiplyShardExp = 1f + CalculateAdditiveBonus(modifiers, StatModifierType.ShardExp);
        stats.addedBattleTime = CalculateAdditiveBonus(modifiers, StatModifierType.BattleTime);

        return stats;
    }

    private static double GetTrainingValue(List<StatModifier> modifiers, StatModifierType type)
    {
        double value = 0f;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.source == StatModifierSource.Training && modifier.type == type)
                {
                    value += modifier.value;
                }
            }
        }

        return value;
    }

    private static float CalculateAdditiveBonus(List<StatModifier> modifiers, StatModifierType type)
    {
        float value = 0f;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.type == type)
                {
                    value += (float)modifier.value;
                }
            }
        }

        return value;
    }

    private static double CalculateMultiplyBonus(List<StatModifier> modifiers, StatModifierType type)
    {
        double value = 1f;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.type == type)
                {
                    value *= (1f + modifier.value);
                }
            }
        }

        return value;
    }

    private static double CalculateAttack(List<StatModifier> modifiers, float advancedAttack)
    {
        double valueMultiply = GetTrainingValue(modifiers, StatModifierType.Attack);
        double valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];

            if (modifier.type == StatModifierType.Attack && modifier.source != StatModifierSource.Training)
            {
                if (modifier.source == StatModifierSource.Collection && modifier.isFlatValue)
                {
                    valueAdditive += modifier.value;
                }
                else
                {
                    valueMultiply *= (1f + modifier.value);
                }
            }
        }

        valueMultiply *= (1f + advancedAttack);
        double value = valueMultiply + valueAdditive;
        return value;
    }

    private static double CalculateHp(List<StatModifier> modifiers)
    {
        double valueMultiply = GetTrainingValue(modifiers, StatModifierType.MaxHp);
        double valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];

            if (modifier.type == StatModifierType.MaxHp && modifier.source != StatModifierSource.Training)
            {
                if (modifier.source == StatModifierSource.Collection && modifier.isFlatValue)
                {
                    valueAdditive += modifier.value;
                }
                else
                {
                    valueMultiply *= (1f + modifier.value);
                }
            }
        }

        double value = valueMultiply + valueAdditive;
        return value;
    }

    private static double CalculateHpRecovery(List<StatModifier> modifiers)
    {
        double valueMultiply = GetTrainingValue(modifiers, StatModifierType.HpRecovery);
        double valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];

            if (modifier.type == StatModifierType.MaxHp && modifier.source != StatModifierSource.Training)
            {
                if (modifier.source == StatModifierSource.Collection && modifier.isFlatValue)
                {
                    //valueAdditive += modifier.value;
                }
                else
                {
                    valueMultiply *= (1f + modifier.value);
                }
            }
        }

        double value = valueMultiply + valueAdditive;
        return value;
    }

    private static float CalculateAttackSpeed(List<StatModifier> modifiers)
    {
        float valueTraining = 0f;
        float valueMultiply = 0f;
        float valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];

            if (modifier.type == StatModifierType.AttackSpeed)
            {
                float fValue = (float)modifier.value;

                if (modifier.source == StatModifierSource.Training)
                {
                    valueTraining += fValue;
                }
                else if (modifier.source == StatModifierSource.MasteryCommon)
                {
                    valueMultiply += fValue;
                }
                else
                {
                    valueAdditive += fValue;
                }
            }
        }

        float value = valueTraining * (1f + valueMultiply);
        value += valueAdditive;

        return value;
    }

    private static float CalculateCritDamage(List<StatModifier> modifiers)
    {
        float valueTraining = 0f;
        float valueMultiply = 0f;
        float valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.CritDamage)
            {
                float fValue = (float)modifier.value;

                if (modifier.source == StatModifierSource.Training)
                {
                    valueTraining += fValue;
                }
                else if (modifier.source == StatModifierSource.HeroLevelUpPassive)
                {
                    valueMultiply += fValue;
                }
                else
                {
                    valueAdditive += fValue;
                }
            }
        }

        float value = valueTraining * (1f + valueMultiply);
        value += valueAdditive;

        return value;
    }

    private static float CalculateSkillCooldown(List<StatModifier> modifiers)
    {
        float value = 1f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.SkillCooldownReduction)
            {
                float fValue = (float)modifier.value;
                value *= (1f - fValue);
            }
        }

        return value;
    }

    private static float CalculateMovementSpeed(List<StatModifier> modifiers)
    {
        float valueMultiply = 2f;
        float valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.MoveSpeed)
            {
                float fValue = (float)modifier.value;

                if (modifier.source == StatModifierSource.Toy || modifier.source == StatModifierSource.Ring)
                {
                    valueAdditive += fValue;
                }
                else
                {
                    valueMultiply *= (1f + fValue);
                }
            }
        }

        float bonus = valueMultiply + valueAdditive;
        float value = DEFAULT_MOVEMENT_SPEED * bonus;
        return value;
    }

    private static float CalculateBasicAttackBonus(List<StatModifier> modifiers)
    {
        float valueMultiply = 1f;
        float valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.BasicAttackDamage)
            {
                float fValue = (float)modifier.value;

                if (modifier.source == StatModifierSource.MasteryPromotion)
                {
                    valueAdditive += fValue;
                }
                else
                {
                    valueMultiply *= (1f + fValue);
                }
            }
        }

        float value = valueMultiply + valueAdditive;
        return value;
    }

    private static float CalculateAttackRange(List<StatModifier> modifiers)
    {
        float bonus = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.AttackRange)
            {
                float fValue = (float)modifier.value;
                bonus += fValue;
            }
        }

        float value = DEFAULT_ATTACK_RANGE * (1f + bonus);
        return value;
    }

    private static float CalculateNormalEnemyDamage(List<StatModifier> modifiers)
    {
        double valueMultiply = GetTrainingValue(modifiers, StatModifierType.NormalEnemyDamage);
        if (valueMultiply >= 1f)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.source != StatModifierSource.Training && modifier.type == StatModifierType.NormalEnemyDamage)
                {
                    float fValue = (float)modifier.value;
                    valueMultiply *= (1f + fValue);
                }
            }

            return (float)valueMultiply;
        }
        else
        {
            return 1f;
        }
    }

    private static float CalculateCompanionDamage(List<StatModifier> modifiers)
    {
        float valueMultiply = 1f;
        float valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.CompanionDamage)
            {
                float fValue = (float)modifier.value;
                if (modifier.source == StatModifierSource.Trait || modifier.source == StatModifierSource.Treasure)
                {
                    valueMultiply *= (1f + fValue);
                }
                else
                {
                    valueAdditive += fValue;
                }
            }
        }

        float value = valueMultiply + valueAdditive;
        return value;
    }

    private static float CalculateCompanionAttackSpeed(List<StatModifier> modifiers)
    {
        float value = 1f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.CompanionAttackSpeed)
            {
                float fValue = (float)modifier.value;
                value *= (1f + fValue);
            }
        }

        return value;
    }

    private static float CalculateSkillDamage(List<StatModifier> modifiers)
    {
        float valueMultiply = 1f;
        float valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.SkillDamage)
            {
                float fValue = (float)modifier.value;
                if (modifier.source == StatModifierSource.Trait || modifier.source == StatModifierSource.Talisman)
                {
                    valueMultiply *= (1f + fValue);
                }
                else
                {
                    valueAdditive += fValue;
                }
            }
        }

        float value = valueMultiply + valueAdditive;
        return value;
    }

    private static float CalculateGoldObtain(List<StatModifier> modifiers)
    {
        float valueMultiply = 1f;
        float valueAdditive = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];
            if (modifier.type == StatModifierType.GoldObtain)
            {
                float fValue = (float)modifier.value;

                if (modifier.source == StatModifierSource.Blessing
                    || modifier.source == StatModifierSource.Trait
                    || modifier.source == StatModifierSource.HeroLevelUpPassive
                    || modifier.source == StatModifierSource.MasteryCommon)
                {
                    valueMultiply *= (1f + fValue);
                }
                else
                {
                    valueAdditive += fValue;
                }
            }
        }

        float value = valueMultiply + valueAdditive;
        return value;
    }
    #endregion

    #region Enemies

    #endregion

    // TODO(follow-stick): StatFromHandNavigationType bỏ tạm — phụ thuộc enum HandNavigationType (UI) chưa port.
}
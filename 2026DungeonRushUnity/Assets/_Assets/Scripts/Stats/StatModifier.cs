using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class StatModifier
{
    public StatModifierSource source;
    public StatModifierType type;
    public double value;
    public bool isFlatValue;

    public StatModifier() { }

    public StatModifier(StatModifierSource source, StatModifierType type, double value, bool isFlatValue = false)
    {
        this.source = source;
        this.type = type;
        this.value = value;
        this.isFlatValue = isFlatValue;
    }
}

public static class ModifierExtensions
{
    public static List<StatModifier> GroupValue(this List<StatModifier> modifiers)
    {
        List<StatModifier> groupedModifiers = new List<StatModifier>();

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier mod = modifiers[i];
            if (mod != null)
            {
                bool isDuplicate = false;

                for (int j = 0; j < groupedModifiers.Count; j++)
                {
                    StatModifier gMod = groupedModifiers[j];
                    if (gMod.source == mod.source && gMod.type == mod.type && gMod.isFlatValue == mod.isFlatValue)
                    {
                        gMod.value += mod.value;
                        isDuplicate = true;
                    }
                }

                if (isDuplicate == false)
                {
                    groupedModifiers.Add(mod);
                }
            }
        }

        return groupedModifiers;
    }

    public static void Add(this List<StatModifier> modifiers, List<StatModifier> input)
    {
        if (input != null)
        {
            for (int i = 0; i < input.Count; i++)
            {
                var mod = input[i];
                modifiers.Add(mod);
            }
        }
    }
    
    public static void AddOne(this List<StatModifier> modifiers, StatModifier input)
    {
        if (input != null)
        {
            modifiers.Add(input);
        }
    }

    public static void AddOne(this List<StatModifier> modifiers, StatModifier input, StatModifierSource newSource)
    {
        if (input != null)
        {
            input.source = newSource;
            modifiers.Add(input);
        }
    }

    // TODO(follow-stick): GetStringValue/GetStringValues bỏ tạm — phụ thuộc GameConfig/ToStringRate/ToStringColor
    // (UI formatting) chưa port. Thêm lại khi port hệ UI stats từ StickIdle.

    public static StatModifier Clone(this StatModifier modifier)
    {
        return new StatModifier(modifier.source, modifier.type, modifier.value, modifier.isFlatValue);
    }

    public static StatModifier CloneByLevel(this StatModifier modifier, int level, float change)
    {
        if (level < 0) level = 0;
        return new StatModifier(modifier.source, modifier.type, modifier.value + (level * change), modifier.isFlatValue);
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DebuffController : MonoBehaviour
{
    private BaseUnit unit;
    private bool flagReduceTime;
    private Dictionary<StatModifierType, List<StatAdjustmentData>> remainingDebuffs = new Dictionary<StatModifierType, List<StatAdjustmentData>>();

    private const float INTERVAL_TIMER = 0.5f;

    private void Awake()
    {
        if (unit == null)
            unit = GetComponent<BaseUnit>();
    }

    public void Reset()
    {
        if (unit == null)
            unit = GetComponent<BaseUnit>();

        StopAllCoroutines();
        flagReduceTime = false;
        remainingDebuffs.Clear();
    }

    public void RemainModifiers()
    {
        //Công thức stack debuffs: FinalReduction = 1 - (1-A%) * (1-B%) * (1-C%)...
        var e = remainingDebuffs.GetEnumerator();
        while (e.MoveNext())
        {
            float value = 1f;
            StatModifierType modType = e.Current.Key;
            List<StatAdjustmentData> debuffs = e.Current.Value;

            for (int i = 0; i < debuffs.Count; i++)
            {
                StatAdjustmentData debuff = debuffs[i];

                float debuffValue = 1 - Mathf.Abs((float)debuff.modifier.value);
                if (debuffValue < 0f)
                    debuffValue = 0f;

                value *= debuffValue;
            }

            float finalReduction = 1f - value;
            unit.AddModifier(new StatModifier(StatModifierSource.None, modType, -finalReduction));
        }

        //// Cùng một loại debuffs đến từ nhiều đối tượng, sẽ lấy đối tượng có value lớn nhất.
        //var e = remainingDebuffs.GetEnumerator();
        //while (e.MoveNext())
        //{
        //    Dictionary<int, float> groupByAttacker = new Dictionary<int, float>();
        //    float maxValue = 0f;
        //    List<StatAdjustmentData> debuffs = e.Current.Value;

        //    for (int i = 0; i < debuffs.Count; i++)
        //    {
        //        StatAdjustmentData debuff = debuffs[i];
        //        float value = Mathf.Abs(debuff.modifier.value);

        //        if (groupByAttacker.ContainsKey(debuff.attacker.battleId))
        //        {
        //            value += Mathf.Abs(groupByAttacker[debuff.attacker.battleId]);
        //        }

        //        groupByAttacker[debuff.attacker.battleId] = value;

        //        if (value > maxValue)
        //            maxValue = value;
        //    }

        //    StatModifier modifier = new StatModifier(e.Current.Key, -maxValue);
        //    unit.AddModifier(modifier);
        //}
    }

    public void TakeDebuffs(List<StatAdjustmentData> input)
    {
        if (unit.isDead || input == null || input.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < input.Count; i++)
        {
            StatAdjustmentData inputDebuff = input[i];

            if (inputDebuff != null && Mathf.Abs((float)inputDebuff.modifier.value) > 0 && inputDebuff.chance > 0f && Random.value <= inputDebuff.chance)
            {
                StatModifierType type = inputDebuff.modifier.type;

                // Nếu debuff cùng loại đến từ cùng một đối tượng, khi max stack sẽ remove index đầu tiên
                if (remainingDebuffs.ContainsKey(type))
                {
                    int currentStacks = 0;
                    int firstIndexSameType = -1;
                    for (int j = 0; j < remainingDebuffs[type].Count; j++)
                    {
                        StatAdjustmentData debuffData = remainingDebuffs[type][j];
                        if (debuffData.attacker.battleId == inputDebuff.attacker.battleId && debuffData.modifier.value == inputDebuff.modifier.value)
                        {
                            if (firstIndexSameType == -1)
                                firstIndexSameType = j;

                            currentStacks++;
                        }
                    }

                    if (currentStacks >= inputDebuff.maxStacks && firstIndexSameType != -1)
                    {
                        remainingDebuffs[type].RemoveAt(firstIndexSameType);
                    }
                }
                else
                {
                    ShowFx(type);
                    remainingDebuffs[type] = new List<StatAdjustmentData>();
                }

                remainingDebuffs[type].Add(inputDebuff.Clone());
            }
        }

        unit.ReloadStats();

        if (!flagReduceTime && unit.gameObject.activeInHierarchy)
        {
            StartCoroutine(TimerDebuff());
        }
    }

    private IEnumerator TimerDebuff()
    {
        flagReduceTime = true;
        bool isChecking = remainingDebuffs.Count > 0;
        while (isChecking)
        {
            if (unit.isPause)
            {
                yield return Yielder.Get(INTERVAL_TIMER);
                continue;
            }

            List<StatModifierType> timeOutDebuffs = new List<StatModifierType>();
            var e = remainingDebuffs.GetEnumerator();
            while (e.MoveNext())
            {
                for (int i = e.Current.Value.Count - 1; i >= 0; i--)
                {
                    StatAdjustmentData debuff = e.Current.Value[i];
                    debuff.ReduceDuration(INTERVAL_TIMER);

                    if (debuff.duration <= 0f)
                    {
                        e.Current.Value.Remove(debuff);
                    }
                }

                if (e.Current.Value.Count <= 0 && !timeOutDebuffs.Contains(e.Current.Key))
                {
                    timeOutDebuffs.Add(e.Current.Key);
                }
            }

            yield return Yielder.Get(INTERVAL_TIMER);

            if (timeOutDebuffs.Count > 0)
            {
                for (int i = 0; i < timeOutDebuffs.Count; i++)
                {
                    StatModifierType type = timeOutDebuffs[i];
                    RemoveFx(type);
                }

                unit.ReloadStats();
            }

            isChecking = remainingDebuffs.Count > 0;
        }

        flagReduceTime = false;
    }

    private void ShowFx(StatModifierType type)
    {
    }

    private void RemoveFx(StatModifierType type)
    {
        if (remainingDebuffs.ContainsKey(type))
            remainingDebuffs.Remove(type);
    }
}

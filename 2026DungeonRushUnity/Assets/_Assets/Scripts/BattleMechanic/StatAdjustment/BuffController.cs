using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuffController : MonoBehaviour
{
    private BaseUnit unit;
    private bool flagBuff;
    private Dictionary<StatModifierType, List<StatAdjustmentData>> remainingBuffs = new Dictionary<StatModifierType, List<StatAdjustmentData>>();

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
        flagBuff = false;
        remainingBuffs.Clear();
    }

    public void RemainModifiers()
    {
        var e = remainingBuffs.GetEnumerator();
        while (e.MoveNext())
        {
            List<StatAdjustmentData> buffs = e.Current.Value;
            for (int i = 0; i < buffs.Count; i++)
            {
                StatAdjustmentData buff = buffs[i];
                if (buff.duration > 0)
                {
                    unit.AddModifier(buff.modifier);
                }
            }
        }
    }

    public void TakeBuffs(StatAdjustmentData input)
    {
        TakeBuffs(new List<StatAdjustmentData>() { input });
    }

    public void TakeBuffs(List<StatAdjustmentData> input)
    {
        if (unit.isDead || input == null || input.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < input.Count; i++)
        {
            StatAdjustmentData inputBuff = input[i];

            if (inputBuff != null && Mathf.Abs((float)inputBuff.modifier.value) > 0f && inputBuff.chance > 0f && Random.value <= inputBuff.chance)
            {
                StatModifierType type = inputBuff.modifier.type;

                if (remainingBuffs.ContainsKey(type))
                {
                    // Nếu buff cùng loại đến từ cùng một đối tượng, khi max stack sẽ remove index đầu tiên
                    int currentStacks = 0;
                    int firstIndexSameType = -1;
                    for (int j = 0; j < remainingBuffs[type].Count; j++)
                    {
                        StatAdjustmentData buffData = remainingBuffs[type][j];
                        if (buffData.IsStackable(inputBuff))
                        {
                            if (firstIndexSameType == -1)
                                firstIndexSameType = j;

                            currentStacks++;
                            //DebugCustom.LogFormat("{0} take buff type={1} from {2} ---------- Stacks={3}", unit.GetHierarchyName(), type, inputBuff.attacker.GetHierarchyName(), currentStacks);
                        }
                    }

                    if (currentStacks >= inputBuff.maxStacks && firstIndexSameType != -1)
                    {
                        remainingBuffs[type].RemoveAt(firstIndexSameType);
                        //DebugCustom.LogFormat("{0} take buff type={1} from {2} ---------- MAX STACKS", unit.GetHierarchyName(), type, inputBuff.attacker.GetHierarchyName());
                    }
                }
                else
                {
                    ShowFx(type);
                    remainingBuffs[type] = new List<StatAdjustmentData>();
                }

                remainingBuffs[type].Add(inputBuff.Clone());
            }
        }

        unit.ReloadStats();

        if (!flagBuff && unit.gameObject.activeInHierarchy)
        {
            StartCoroutine(TimerBuff());
        }
    }

    public bool IsReceivingBuffFrom(int bufferBattleId, StatModifierType type)
    {
        var e = remainingBuffs.GetEnumerator();
        while (e.MoveNext())
        {
            StatModifierType modType = e.Current.Key;
            List<StatAdjustmentData> buffs = e.Current.Value;

            if (modType == type)
            {
                for (int i = 0; i < buffs.Count; i++)
                {
                    if (buffs[i].attacker.battleId == bufferBattleId)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private IEnumerator TimerBuff()
    {
        flagBuff = true;
        bool isChecking = remainingBuffs.Count > 0;
        while (isChecking)
        {
            if (unit.isPause)
            {
                yield return Yielder.Get(INTERVAL_TIMER);
                continue;
            }

            List<StatModifierType> timeOutBuffs = new List<StatModifierType>();
            var e = remainingBuffs.GetEnumerator();
            while (e.MoveNext())
            {
                for (int i = e.Current.Value.Count - 1; i >= 0; i--)
                {
                    StatAdjustmentData buff = e.Current.Value[i];
                    buff.ReduceDuration(INTERVAL_TIMER);

                    if (buff.duration <= 0f)
                    {
                        e.Current.Value.Remove(buff);
                    }
                }

                if (e.Current.Value.Count <= 0 && !timeOutBuffs.Contains(e.Current.Key))
                {
                    timeOutBuffs.Add(e.Current.Key);
                }
            }

            yield return Yielder.Get(INTERVAL_TIMER);

            if (timeOutBuffs.Count > 0)
            {
                for (int i = 0; i < timeOutBuffs.Count; i++)
                {
                    StatModifierType type = timeOutBuffs[i];
                    RemoveFx(type);
                }

                unit.ReloadStats();
            }

            isChecking = remainingBuffs.Count > 0;
        }

        flagBuff = false;
    }

    private void ShowFx(StatModifierType type)
    {
    }

    private void RemoveFx(StatModifierType type)
    {
        if (remainingBuffs.ContainsKey(type))
            remainingBuffs.Remove(type);
    }
}

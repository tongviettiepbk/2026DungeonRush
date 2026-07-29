using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;

public class DamageOverTimeController : MonoBehaviour
{
    private BaseUnit unit;
    private Dictionary<DamageOverTimeType, List<DamageOverTimeData>> remainingDots = new Dictionary<DamageOverTimeType, List<DamageOverTimeData>>();
    private IEnumerator routineApplyDamage;

    private const float INTERVAL_APPLY_DAMAGE = 1f;

    private void Awake()
    {
        unit = GetComponent<BaseUnit>();
    }

    public void Reset()
    {
        if (routineApplyDamage != null)
        {
            StopCoroutine(routineApplyDamage);
            routineApplyDamage = null;
        }

        remainingDots.Clear();
    }

    public void TakeDots(List<DamageOverTimeData> input)
    {
        if (unit.isDead || input == null)
        {
            return;
        }

        for (int i = 0; i < input.Count; i++)
        {
            DamageOverTimeData inputDot = input[i];

            if (inputDot != null && inputDot.rate > 0f && UnityEngine.Random.value <= inputDot.rate)
            {
                DamageOverTimeType type = inputDot.type;

                if (remainingDots.ContainsKey(type))
                {
                    // Nếu DOT cùng loại đến từ cùng một đối tượng, khi max stack sẽ remove index đầu tiên
                    int currentStacks = 0;
                    int firstIndexSameType = -1;
                    for (int j = 0; j < remainingDots[type].Count; j++)
                    {
                        DamageOverTimeData dotData = remainingDots[type][j];
                        if (dotData.attacker.battleId == inputDot.attacker.battleId && dotData.multiply == inputDot.multiply && dotData.rate == inputDot.rate)
                        {
                            if (firstIndexSameType == -1)
                                firstIndexSameType = j;

                            currentStacks++;
                            //DebugCustom.Log("stacks=" + currentStacks);
                        }
                    }

                    if (currentStacks >= inputDot.maxStacks && firstIndexSameType != -1)
                    {
                        remainingDots[type].RemoveAt(firstIndexSameType);
                    }
                }
                else
                {
                    ShowFx(type);
                    remainingDots[type] = new List<DamageOverTimeData>();
                }

                remainingDots[type].Add(inputDot.Clone());
            }
        }

        if (remainingDots.Count > 0 && routineApplyDamage == null && unit.gameObject.activeInHierarchy)
        {
            routineApplyDamage = TimerApplyDamage();
            StartCoroutine(routineApplyDamage);
        }
    }

    private void ShowFx(DamageOverTimeType type)
    {
    }

    private void RemoveFx(DamageOverTimeType type)
    {
        if (remainingDots.ContainsKey(type))
        {
            remainingDots.Remove(type);
        }
    }

    private IEnumerator TimerApplyDamage()
    {
        bool isChecking = remainingDots.Count > 0;
        while (isChecking)
        {
            if (unit.isPause)
            {
                yield return Yielder.Get(INTERVAL_APPLY_DAMAGE);
                continue;
            }

            yield return Yielder.Get(INTERVAL_APPLY_DAMAGE);
            List<DamageOverTimeType> timeOutDots = new List<DamageOverTimeType>();

            // Nếu có nhiều DOT 1 lúc sẽ nhảy số dần dần
            double burn = 0f;
            double poison = 0f;
            double bleed = 0f;

            var e = remainingDots.GetEnumerator();
            while (e.MoveNext())
            {
                DamageOverTimeType dotType = e.Current.Key;
                List<DamageOverTimeData> listDotData = e.Current.Value;
                for (int i = 0; i < listDotData.Count; i++)
                {
                    DamageOverTimeData dotData = listDotData[i];
                    double damage = unit.ProcessDamageOverTime(dotData);

                    if (unit.isDead)
                    {
                        break;
                    }

                    switch (dotType)
                    {
                        case DamageOverTimeType.Burn: burn += damage; break;
                        case DamageOverTimeType.Poison: poison += damage; break;
                        case DamageOverTimeType.Bleed: bleed += damage; break;
                    }
                }

                if (unit.isDead)
                {
                    break;
                }

                // Remove những DOT hết thời gian
                for (int i = listDotData.Count - 1; i >= 0; i--)
                {
                    DamageOverTimeData dotData = listDotData[i];
                    dotData.ReduceDuration(INTERVAL_APPLY_DAMAGE);

                    if (dotData.duration < 0)
                    {
                        listDotData.Remove(dotData);
                    }
                }

                if (listDotData.Count <= 0 && !timeOutDots.Contains(dotType))
                {
                    timeOutDots.Add(dotType);
                }
            }

            // Xóa những Dots hết thời gian
            for (int i = 0; i < timeOutDots.Count; i++)
            {
                DamageOverTimeType type = timeOutDots[i];
                RemoveFx(type);
            }

            // Apply damage

            Dictionary<DamageOverTimeType, double> finalDots = new Dictionary<DamageOverTimeType, double>();
            if (burn > 0f) finalDots[DamageOverTimeType.Burn] = burn;
            if (poison > 0f) finalDots[DamageOverTimeType.Poison] = poison;
            if (bleed > 0f) finalDots[DamageOverTimeType.Bleed] = bleed;

            if (unit.gameObject.activeInHierarchy)
            {
                StartCoroutine(RoutineShowDamage(finalDots));
            }

            isChecking = remainingDots.Count > 0;
        }

        routineApplyDamage = null;
    }

    private IEnumerator RoutineShowDamage(Dictionary<DamageOverTimeType, double> finalDots)
    {
        var e = finalDots.GetEnumerator();
        while (e.MoveNext())
        {
            DamageOverTimeType type = e.Current.Key;
            double value = e.Current.Value;

            try
            {
                unit.ShowTextDamage(value);

                Color color = Color.white;
                if (type == DamageOverTimeType.Bleed) color = Color.red;
                else if (type == DamageOverTimeType.Burn) color = Color.red;
                else if (type == DamageOverTimeType.Poison) color = Color.green;

                unit.animationController.FlashColor(color);
            }
            catch (Exception er)
            {
                DebugCustom.LogError(er.Message);
            }

            yield return Yielder.Get(0.2f);
        }

        //List<float> values = finalDots.Values.ToList();
        //int count = 0;
        //while (count < values.Count)
        //{
        //    try
        //    {
        //        unit.ShowTextDamage(values[count], isCrit: false, isPureDamage: false);
        //    }
        //    catch { }

        //    count++;
        //    yield return Yielder.Get(0.2f);
        //}
    }
}

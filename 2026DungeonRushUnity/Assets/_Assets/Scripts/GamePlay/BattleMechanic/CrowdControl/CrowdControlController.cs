using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Linq;

/// <summary>
/// Xử lý hiệu ứng khống chế (CC)
/// </summary>
public class CrowdControlController : MonoBehaviour
{
    private BaseUnit unit;
    private bool flagTimer;
    private const float INTERVAL_TIMER = 0.1f;

    public Dictionary<CrowdControlType, List<CrowdControlData>> remainingCCData { get; protected set; } = new Dictionary<CrowdControlType, List<CrowdControlData>>();
    public Dictionary<CrowdControlType, BaseFx> remainingFxInstances { get; set; } = new Dictionary<CrowdControlType, BaseFx>();

    public bool isDisableMove => isHardCC || isRooted;
    public bool isDisableAttack => isHardCC || isDisarm || isFear;
    public bool isDisableSkill => isHardCC || isSilenced || isTaunted || isFear;
    public bool isDisableKnock => isKnocked || isFrozen || isParalyzed || isRooted || isHex || unit.IsImmuneKnock();
    public bool isDisablePull => isPulled || isKnockedUp || isKnockedBack;
    public bool isHardCC => isStunned || isFrozen || isParalyzed || isKnocked || isDarkPrisonByWorldBoss || isHex || isPulled;

    public bool isStunned => remainingCCData.ContainsKey(CrowdControlType.Stun);
    public bool isFrozen => remainingCCData.ContainsKey(CrowdControlType.Freeze);
    public bool isParalyzed => remainingCCData.ContainsKey(CrowdControlType.Paralyze);
    public bool isRooted => remainingCCData.ContainsKey(CrowdControlType.Rooted);
    public bool isTaunted => remainingCCData.ContainsKey(CrowdControlType.Taunted);
    public bool isSilenced => remainingCCData.ContainsKey(CrowdControlType.Silent);
    public bool isSilencedUltimate => remainingCCData.ContainsKey(CrowdControlType.SilentUltimate);
    public bool isDisarm => remainingCCData.ContainsKey(CrowdControlType.Disarm);
    public bool isCharm => remainingCCData.ContainsKey(CrowdControlType.Charm);
    public bool isHex => remainingCCData.ContainsKey(CrowdControlType.Hex);
    public bool isFear => remainingCCData.ContainsKey(CrowdControlType.Fear);

    public bool isKnockedBack => remainingCCData.ContainsKey(CrowdControlType.KnockBack);
    public bool isKnockedDown => remainingCCData.ContainsKey(CrowdControlType.KnockDown);
    public bool isKnockedUp => unit.stateCurrent == BattleState.KnockUp || remainingCCData.ContainsKey(CrowdControlType.KnockUp);
    public bool isKnocked => isKnockedBack || isKnockedDown || isKnockedUp;

    public bool isDarkPrisonByWorldBoss { get; set; }
    public bool isPulled { get; set; }

    public Vector3 fearMovePoint { get; set; }


    private void Awake()
    {
        unit = GetComponent<BaseUnit>();
    }

    public void Reset()
    {
        StopAllCoroutines();
        RemoveAllCCs();
        isDarkPrisonByWorldBoss = false;
        isPulled = false;
        flagTimer = false;
    }

    public void OnTakeDamage(double damage)
    {
        try
        {
            if (remainingCCData.ContainsKey(CrowdControlType.Freeze))
            {
                List<CrowdControlData> freezes = remainingCCData[CrowdControlType.Freeze];
                for (int i = freezes.Count - 1; i >= 0; i--)
                {
                    CrowdControlData freeze = freezes[i];
                    if (freeze.value > 0f)
                    {
                        freeze.ReduceValue(damage);
                        //DebugCustom.LogFormat("{0} reduce {1} freeze value, remaining={2}", unit.GetHierarchyName(), damage, freeze.value);
                        if (freeze.value <= 0f)
                        {
                            //DebugCustom.LogFormat("{0} remove freeze", unit.GetHierarchyName());
                            freezes.Remove(freeze);
                        }
                    }
                }

                if (freezes.Count <= 0)
                {
                    RemoveCC(CrowdControlType.Freeze);
                }
            }
        }
        catch (Exception e)
        {
            DebugCustom.LogError(e.Message);
        }
    }

    public void TakeCC(CrowdControlData input)
    {
        TakeCC(new List<CrowdControlData>() { input });
    }

    public void TakeCC(List<CrowdControlData> input)
    {
        if (input == null || unit.isDead || unit.isImmuneCC)
        {
            return;
        }

        for (int i = 0; i < input.Count; i++)
        {
            CrowdControlData inputCC = input[i];

            if (IsReceivedThisCC(inputCC))
            {
                float rateTaken = inputCC.rate;

                if (inputCC.isUnableToResist || (rateTaken > 0f && UnityEngine.Random.value <= rateTaken))
                {
                    CrowdControlData cloneCC = inputCC.Clone();

                    if (!remainingCCData.ContainsKey(inputCC.type))
                    {
                        remainingCCData[inputCC.type] = new List<CrowdControlData>();
                        unit.AnimateCC(cloneCC);
                    }

                    remainingCCData[inputCC.type].Add(cloneCC);
                }
            }
        }

        if (remainingCCData.Count > 0 && !flagTimer && unit.gameObject.activeInHierarchy)
        {
            StartCoroutine(TimerCC());
        }
    }

    private bool IsReceivedThisCC(CrowdControlData cc)
    {
        if (cc == null) return false;
        if (cc.isKnockCC && isDisableKnock) return false;
        if (cc.type != CrowdControlType.Hex && isHex) return false;
        if (cc.type == CrowdControlType.Taunted && isTaunted) return false;

        return true;
    }

    private IEnumerator TimerCC()
    {
        flagTimer = true;
        bool isChecking = remainingCCData.Count > 0;
        while (isChecking)
        {
            if (unit.isPause)
            {
                yield return Yielder.Get(INTERVAL_TIMER);
                continue;
            }

            yield return Yielder.Get(INTERVAL_TIMER);

            try
            {
                List<CrowdControlType> timeOutCCs = new List<CrowdControlType>();
                var e = remainingCCData.GetEnumerator();
                while (e.MoveNext())
                {
                    CrowdControlType ccType = e.Current.Key;
                    List<CrowdControlData> listCC = e.Current.Value;

                    for (int i = listCC.Count - 1; i >= 0; i--)
                    {
                        CrowdControlData ccData = listCC[i];
                        ccData.ReduceDuration(INTERVAL_TIMER);

                        if (ccData.duration <= 0)
                        {
                            listCC.Remove(ccData);
                        }
                    }

                    if (listCC.Count <= 0 && !timeOutCCs.Contains(ccType))
                    {
                        timeOutCCs.Add(ccType);
                    }
                }

                // Xóa những CC hết thời gian
                for (int i = timeOutCCs.Count - 1; i >= 0; i--)
                {
                    CrowdControlType type = timeOutCCs[i];
                    RemoveCC(type);
                }
            }
            catch { }

            isChecking = remainingCCData.Count > 0;
        }

        flagTimer = false;
    }

    public void RemoveCC(CrowdControlType type)
    {
        if (remainingCCData.ContainsKey(type))
            remainingCCData.Remove(type);

        if (remainingFxInstances.ContainsKey(type))
        {
            remainingFxInstances[type].Deactive();
            remainingFxInstances.Remove(type);
        }

        unit.RemoveCC(type);
    }

    private void RemoveAllCCs()
    {
        if (remainingCCData.Count > 0)
        {
            List<CrowdControlType> types = remainingCCData.Keys.ToList();
            for (int i = 0; i < types.Count; i++)
            {
                CrowdControlType type = types[i];
                RemoveCC(type);
            }

            unit.ReloadStats();
        }
    }
}

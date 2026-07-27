using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ShieldController : MonoBehaviour
{
    private BaseUnit unit;
    private bool flagBuff;
    private Dictionary<ShieldId, ShieldData> remainingShields = new Dictionary<ShieldId, ShieldData>();
    private Dictionary<ShieldId, BaseFx> remainingFx = new Dictionary<ShieldId, BaseFx>();

    private const float INTERVAL_TIMER = 0.5f;

    private void Awake()
    {
        unit = GetComponent<BaseUnit>();
    }

    public void Reset()
    {
        if (unit == null)
            unit = GetComponent<BaseUnit>();

        StopAllCoroutines();
        RemoveAllShield();
        flagBuff = false;
    }

    public void RemainModifiers()
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        if (unit != null)
            unit.AddModifier(modifiers);
    }

    public ShieldData GetRemainingValue(ShieldId shieldType)
    {
        if (remainingShields.ContainsKey(shieldType))
        {
            ShieldData data = remainingShields[shieldType];
            return data;
        }

        return null;
    }

    public double GetRemainingValue(ShieldType type)
    {
        double value = 0f;

        var e = remainingShields.GetEnumerator();
        while (e.MoveNext())
        {
            ShieldData data = e.Current.Value;
            if (data.type == type)
            {
                value += data.value;
            }
        }

        return value;
    }

    public void ReduceAbsorbValue(double totalValue)
    {
        if (totalValue <= 0)
        {
            return;
        }

        List<ShieldId> outValueShield = new List<ShieldId>();
        double remainingValue = totalValue;
        var e = remainingShields.GetEnumerator();
        while (e.MoveNext())
        {
            ShieldId type = e.Current.Key;
            ShieldData data = e.Current.Value;

            if (data.type == ShieldType.Absorb)
            {
                if (remainingValue >= data.value)
                {
                    remainingValue -= data.value;
                    outValueShield.Add(type);
                }
                else
                {
                    data.ReduceValue(remainingValue);
                }
            }
        }

        if (outValueShield.Count > 0)
        {
            for (int i = 0; i < outValueShield.Count; i++)
            {
                RemoveShield(outValueShield[i]);
            }

            unit.ReloadStats();
        }
    }

    public void BlockDamage()
    {
        List<ShieldId> outValueShield = new List<ShieldId>();
        var e = remainingShields.GetEnumerator();
        while (e.MoveNext())
        {
            ShieldId type = e.Current.Key;
            ShieldData data = e.Current.Value;

            if (data.type == ShieldType.Block)
            {
                data.ReduceValue(1f);

                if (data.value <= 0f)
                {
                    outValueShield.Add(type);
                }
            }
        }

        if (outValueShield.Count > 0)
        {
            for (int i = 0; i < outValueShield.Count; i++)
            {
                RemoveShield(outValueShield[i]);
            }

            unit.ReloadStats();
        }
    }

    public void TakeShield(ShieldData shield)
    {
        TakeShield(new List<ShieldData>() { shield });
    }

    public void TakeShield(List<ShieldData> input)
    {
        if (unit.isDead || input == null || input.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < input.Count; i++)
        {
            ShieldData inputShield = input[i];
            if (inputShield != null && inputShield.id != ShieldId.None && Mathf.Abs((float)inputShield.value) > 0f)
            {
                ShieldId id = inputShield.id;
                bool isReplaced = false;

                if (remainingShields.ContainsKey(id))
                {
                    ShieldData data = remainingShields[id];
                    if (inputShield.level >= data.level)
                    {
                        isReplaced = true;
                    }
                }
                else
                {
                    ShowFx(id);
                    isReplaced = true;
                }

                if (isReplaced)
                {
                    remainingShields[id] = inputShield.Clone();
                }
            }
        }

        if (flagBuff == false && unit.gameObject.activeInHierarchy)
        {
            StartCoroutine(Timer());
        }
    }

    private IEnumerator Timer()
    {
        flagBuff = true;
        bool isChecking = remainingShields.Count > 0;
        while (isChecking)
        {
            if (unit.isPause)
            {
                yield return Yielder.Get(INTERVAL_TIMER);
                continue;
            }

            yield return Yielder.Get(INTERVAL_TIMER);

            List<ShieldId> timeOutShields = new List<ShieldId>();
            var e = remainingShields.GetEnumerator();
            while (e.MoveNext())
            {
                ShieldId type = e.Current.Key;
                ShieldData data = e.Current.Value;
                data.ReduceDuration(INTERVAL_TIMER);
                if (data.duration <= 0f && timeOutShields.Contains(type) == false)
                {
                    timeOutShields.Add(type);
                }
            }

            if (timeOutShields.Count > 0)
            {
                for (int i = 0; i < timeOutShields.Count; i++)
                {
                    ShieldId type = timeOutShields[i];
                    RemoveShield(type);
                }

                unit.ReloadStats();
            }
        }

        flagBuff = false;
    }

    private void ShowFx(ShieldId type)
    {
        BaseFx fx = null;
        BaseFx fxPrefab = null;

        switch (type)
        {
            //case ShieldId.RelicAngelProtection: fxPrefab = FxController.Instance.fxShieldAngelProtection; break;
            //case ShieldId.Dante: fxPrefab = FxController.Instance.fxShieldDante; break;
            //case ShieldId.Selena: fxPrefab = FxController.Instance.fxShieldSelena; break;
        }

        if (fxPrefab != null)
        {
            fx = FxController.Instance.SpawnFx(fxPrefab, unit.Transform.position);
        }

        if (fx != null)
        {
            fx.transform.SetParent(unit.Transform);
            remainingFx[type] = fx;
        }

        unit.ActiveShield(type, true);
    }

    private void RemoveShield(ShieldId type)
    {
        if (remainingShields.ContainsKey(type))
            remainingShields.Remove(type);

        if (remainingFx.ContainsKey(type))
        {
            remainingFx[type].Deactive();
            remainingFx.Remove(type);
        }

        unit.ActiveShield(type, false);
    }

    private void RemoveAllShield()
    {
        if (remainingShields.Count > 0)
        {
            List<ShieldId> types = remainingShields.Keys.ToList();
            for (int i = 0; i < types.Count; i++)
            {
                ShieldId type = types[i];
                RemoveShield(type);
            }

            unit.ReloadStats();
        }
    }
}

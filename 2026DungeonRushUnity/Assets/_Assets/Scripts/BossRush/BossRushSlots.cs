using System.Collections.Generic;
using UnityEngine;
using SR = StaticBossRushData;

// Chỗ đứng quanh boss cho đội cận chiến — port class gq gốc v59 (GameController.isk tạo với số chỗ = số người trong đội).
//   • Chỗ i: góc i × 360 / N; vị trí = tâm collider boss + hướng × (bán kính collider theo hướng đó + SlotStandoff).
//   • Người đã có chỗ giữ nguyên; người mới lấy chỗ còn trống có góc gần góc (người − boss) nhất (hết chỗ trống thì xét cả chỗ có người).
//   • Đánh được khi đứng cách chỗ ≤ SlotArrivalTolerance; lệch chỗ thì đếm, đủ SlotGraceSeconds vẫn cho đánh.
public class BossRushSlots
{
    private const float FAR_PROBE_DISTANCE = 50f;   // gq.fkp: điểm dò xa để lấy ClosestPoint trên collider

    private readonly int slotCount;                                                     // yfc
    private readonly Dictionary<BaseUnit, int> slotByUnit = new Dictionary<BaseUnit, int>();       // yfd
    private readonly Dictionary<BaseUnit, float> offSlotSince = new Dictionary<BaseUnit, float>(); // yfe

    public BossRushSlots(int count)
    {
        slotCount = Mathf.Max(1, count);
    }

    // fkm: vị trí chỗ của attacker quanh boss.
    public Vector2 GetSlotPosition(BaseUnit attacker, BossRushBossUnit boss)
    {
        int slot = GetSlot(attacker, boss);
        float angle = slot * Mathf.PI * 2f / slotCount;
        var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Collider2D body = boss.BodyCollider;
        Vector2 center = body != null ? (Vector2)body.bounds.center : (Vector2)boss.Transform.position;
        return center + direction * (BodyRadius(body, center, direction) + SR.SLOT_STANDOFF);
    }

    // fkn: đứng ở chỗ (≤ tolerance) → được đánh; lệch chỗ → đủ SlotGraceSeconds mới được.
    public bool IsAtSlot(BaseUnit attacker, BossRushBossUnit boss)
    {
        Vector2 slot = GetSlotPosition(attacker, boss);
        float toleranceSqr = SR.SLOT_ARRIVAL_TOLERANCE * SR.SLOT_ARRIVAL_TOLERANCE;
        if (((Vector2)attacker.Transform.position - slot).sqrMagnitude <= toleranceSqr)
        {
            offSlotSince.Remove(attacker);
            return true;
        }
        if (offSlotSince.TryGetValue(attacker, out float since))
        {
            return Time.time - since >= SR.SLOT_GRACE_SECONDS;
        }
        offSlotSince[attacker] = Time.time;
        return false;
    }

    // fko
    private int GetSlot(BaseUnit attacker, BossRushBossUnit boss)
    {
        if (slotByUnit.TryGetValue(attacker, out int held))
        {
            return held;
        }

        var taken = new HashSet<int>();
        foreach (KeyValuePair<BaseUnit, int> pair in slotByUnit)
        {
            if (pair.Key != null && pair.Key.isTargetable)
            {
                taken.Add(pair.Value);
            }
        }

        Vector2 offset = (Vector2)attacker.Transform.position - (Vector2)boss.Transform.position;
        float attackerAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        int best = 0;
        float bestDelta = float.MaxValue;
        for (int i = 0; i < slotCount; i++)
        {
            if (taken.Count < slotCount && taken.Contains(i))
            {
                continue;
            }
            float delta = Mathf.Abs(Mathf.DeltaAngle(attackerAngle, i * 360f / slotCount));
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = i;
            }
        }
        slotByUnit[attacker] = best;
        return best;
    }

    // fkp: bán kính collider theo hướng (khoảng cách từ tâm tới điểm gần nhất trên collider so với điểm dò xa). Không collider → 1.
    private static float BodyRadius(Collider2D body, Vector2 center, Vector2 direction)
    {
        if (body == null)
        {
            return 1f;
        }
        Vector2 edge = body.ClosestPoint(center + direction * FAR_PROBE_DISTANCE);
        return Vector2.Distance(edge, center);
    }
}

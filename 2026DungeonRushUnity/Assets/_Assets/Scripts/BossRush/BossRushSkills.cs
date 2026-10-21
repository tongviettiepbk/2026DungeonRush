using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using SR = StaticBossRushData;

// Skill boss Boss Rush — port class gy gốc v59 (gz Earthquake / ha FireBreath / hb Slam / hc Teleport).
// BossRushBossUnit chạy: Begin → mỗi frame Tick(dt × tốc game) tới khi trả true → Cancel khi rời state skill.
// Damage = 1 đòn thường của boss lên từng người (Character.fyw). Mục tiêu = đội nhà còn sống (SpawnController.HomeCharacters).
// Thông số: StaticBossRushData; reverse: DecodedData/BOSS_RUSH_MODEL.md mục 10.4.
public abstract class BossRushSkill
{
    protected readonly BossRushBossUnit boss;

    protected BossRushSkill(BossRushBossUnit boss)
    {
        this.boss = boss;
    }

    public abstract void Begin();

    // Trả true khi skill xong.
    public abstract bool Tick(float dt);

    public abstract void Cancel();

    // gy.Create
    public static BossRushSkill Create(BossRushBossSkillType type, BossRushBossUnit boss)
    {
        switch (type)
        {
            case BossRushBossSkillType.Slam: return new BossRushSlamSkill(boss);
            case BossRushBossSkillType.Earthquake: return new BossRushEarthquakeSkill(boss);
            case BossRushBossSkillType.Teleport: return new BossRushTeleportSkill(boss);
            case BossRushBossSkillType.FireBreath: return new BossRushFireBreathSkill(boss);
            default: return null;
        }
    }

    // gy.fnd: vị trí boss (Rigidbody nếu có).
    protected Vector2 BossPosition => boss.Position;

    // gy.fne: đội nhà (duyệt ngược, bỏ người đã chết).
    protected List<BaseUnit> GetHomeUnits()
    {
        return boss.Mode != null ? boss.Mode.teamA : new List<BaseUnit>();
    }

    protected static bool IsAlive(BaseUnit unit)
    {
        return unit != null && unit.isTargetable;
    }

    // gy.fnh: rung camera.
    protected void ShakeCamera(float duration, float strength)
    {
        if (boss.Mode != null)
        {
            boss.Mode.ShakeCamera(duration, strength);
        }
    }
}

// hb — Slam (Lich): gồng tại chỗ, hết gồng đập trúng mọi người trong bán kính SlamRadius + đẩy lùi.
public class BossRushSlamSkill : BossRushSkill
{
    private static readonly Color FLASH_COLOR = new Color(1f, 0.9f, 0.7f, 0.9f);    // yhd
    private static readonly Color RING_COLOR = new Color(1f, 0.35f, 0.2f, 0.85f);   // yhe

    private float timer;            // yhf
    private bool hasImpacted;       // yhg
    private BossRushGroundFX telegraph;   // yhh

    public BossRushSlamSkill(BossRushBossUnit boss) : base(boss) { }

    // fnb: không có prefab telegraph → vòng đỏ thu từ 2R về cỡ thân boss trong thời gian gồng.
    public override void Begin()
    {
        timer = 0f;
        hasImpacted = false;
        telegraph = BossRushGroundFX.Telegraph(BossPosition, SR.SLAM_RADIUS, boss.BodyDiameter * 0.5f, SR.SLAM_WINDUP_SECONDS);
    }

    public override bool Tick(float dt)
    {
        timer += dt;
        if (!hasImpacted && timer >= SR.SLAM_WINDUP_SECONDS)
        {
            hasImpacted = true;
            Impact();
        }
        return timer >= SR.SLAM_WINDUP_SECONDS + SR.SLAM_RECOVERY_SECONDS;
    }

    // fnz
    private void Impact()
    {
        Vector2 center = BossPosition;
        float radiusSqr = SR.SLAM_RADIUS * SR.SLAM_RADIUS;
        List<BaseUnit> units = GetHomeUnits();
        for (int i = units.Count - 1; i >= 0; i--)
        {
            BaseUnit unit = units[i];
            if (!IsAlive(unit) || ((Vector2)unit.Transform.position - center).sqrMagnitude > radiusSqr)
            {
                continue;
            }
            boss.SkillAttack(unit);
            if (IsAlive(unit))
            {
                unit.AddKnockbackImpulse(center, SR.SLAM_KNOCKBACK_FORCE);
            }
        }

        if (telegraph != null)
        {
            telegraph.Dismiss();
            telegraph = null;
        }
        BossRushGroundFX.Flash(center, 1.3f, 0.3f, FLASH_COLOR);
        BossRushGroundFX.Ring(center, 0.4f, SR.SLAM_RADIUS, 0.3f, RING_COLOR);
        ShakeCamera(0.3f, 0.3f);
    }

    public override void Cancel()
    {
        if (telegraph != null)
        {
            telegraph.Dismiss();
            telegraph = null;
        }
    }
}

// gz — Earthquake (Ogre): tâm cố định lúc bắt đầu; sóng i nổ lúc Windup + Interval × i, trúng người trong vành
// [bán kính sóng trước (sóng đầu 0.3), Radii[i]] → 1 đòn + đẩy lùi + nảy hình.
public class BossRushEarthquakeSkill : BossRushSkill
{
    private static readonly Color WAVE_COLOR = new Color(0.6f, 0.38f, 0.18f, 0.95f);     // ygj
    private static readonly Color INNER_COLOR = new Color(0.85f, 0.72f, 0.5f, 0.55f);    // ygk
    private static readonly Color DEBRIS_COLOR = new Color(0.8f, 0.68f, 0.48f, 0.6f);    // ygl
    private const int DEBRIS_COUNT = 7;          // ygm
    private const float FIRST_INNER_RADIUS = 0.3f;
    private const float END_DELAY = 0.4f;

    private float timer;        // ygn
    private int waveIndex;      // ygo
    private Vector2 center;     // ygp

    public BossRushEarthquakeSkill(BossRushBossUnit boss) : base(boss) { }

    private static int WaveCount => SR.EARTHQUAKE_WAVE_RADII.Length;   // fni

    public override void Begin()
    {
        timer = 0f;
        waveIndex = 0;
        center = BossPosition;
    }

    public override bool Tick(float dt)
    {
        timer += dt;
        while (waveIndex < WaveCount && timer >= SR.EARTHQUAKE_WINDUP_SECONDS + SR.EARTHQUAKE_WAVE_INTERVAL * waveIndex)
        {
            FireWave(waveIndex);
            waveIndex++;
        }
        float lastWave = SR.EARTHQUAKE_WINDUP_SECONDS + SR.EARTHQUAKE_WAVE_INTERVAL * Mathf.Max(WaveCount - 1, 0);
        return timer >= lastWave + END_DELAY;
    }

    // fnj
    private void FireWave(int index)
    {
        float outer = SR.EARTHQUAKE_WAVE_RADII[index];
        float inner = index >= 1 ? SR.EARTHQUAKE_WAVE_RADII[index - 1] : FIRST_INNER_RADIUS;

        BossRushGroundFX.Ring(center, inner, outer, 0.35f, WAVE_COLOR);
        BossRushGroundFX.Ring(center, inner * 0.75f, outer * 0.85f, 0.5f, INNER_COLOR);
        for (int i = 0; i < DEBRIS_COUNT; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (outer * Random.Range(0.8f, 1f));
            BossRushGroundFX.Flash(position, Random.Range(0.35f, 0.6f), Random.Range(0.35f, 0.55f), DEBRIS_COLOR);
        }
        ShakeCamera(0.26f, 0.2f + 0.1f * index);

        float outerSqr = outer * outer;
        float innerSqr = inner * inner;
        List<BaseUnit> units = GetHomeUnits();
        for (int i = units.Count - 1; i >= 0; i--)
        {
            BaseUnit unit = units[i];
            if (!IsAlive(unit))
            {
                continue;
            }
            float distanceSqr = ((Vector2)unit.Transform.position - center).sqrMagnitude;
            if (distanceSqr > outerSqr || distanceSqr < innerSqr)
            {
                continue;
            }
            boss.SkillAttack(unit);
            if (IsAlive(unit))
            {
                unit.AddKnockbackImpulse(center, SR.EARTHQUAKE_KNOCKBACK_FORCE);
                unit.PlayHop(SR.EARTHQUAKE_HOP_HEIGHT);
            }
        }
    }

    public override void Cancel() { }
}

// hc — Teleport (Hag): thu hình rồi chớp sang ô xa đội, phóng hình lại. Không gây damage.
public class BossRushTeleportSkill : BossRushSkill
{
    private static readonly Color FLASH_COLOR = new Color(0.75f, 0.35f, 1f, 0.85f);   // yhj

    private float timer;            // yhk
    private bool hasTeleported;     // yhl
    private Vector2 destination;    // yhm
    private Vector3 visualScale;    // yhn
    private bool hasVisualScale;    // yho

    public BossRushTeleportSkill(BossRushBossUnit boss) : base(boss) { }

    // fnb
    public override void Begin()
    {
        timer = 0f;
        hasTeleported = false;
        destination = PickDestination();
        Flash(BossPosition, SR.TELEPORT_WINDUP_SECONDS);

        Transform visual = boss.VisualTransform;
        if (visual == null)
        {
            return;
        }
        visualScale = visual.localScale;
        hasVisualScale = true;
        visual.DOKill();
        visual.DOScale(visualScale * 0.1f, SR.TELEPORT_WINDUP_SECONDS * 0.7f)
              .SetDelay(SR.TELEPORT_WINDUP_SECONDS * 0.3f)
              .SetEase(Ease.InBack);
    }

    public override bool Tick(float dt)
    {
        timer += dt;
        if (!hasTeleported && timer >= SR.TELEPORT_WINDUP_SECONDS)
        {
            hasTeleported = true;
            Teleport();
        }
        bool isDone = timer >= SR.TELEPORT_WINDUP_SECONDS + SR.TELEPORT_RECOVERY_SECONDS;
        if (isDone)
        {
            RestoreVisual();
        }
        return isDone;
    }

    // foa
    private void Teleport()
    {
        Flash(BossPosition, 0.3f);
        boss.TeleportTo(destination);
        Flash(destination, 0.3f);
        FaceNearest();

        Transform visual = boss.VisualTransform;
        if (hasVisualScale && visual != null)
        {
            visual.DOKill();
            visual.localScale = visualScale * 0.1f;
            visual.DOScale(visualScale, 0.2f).SetEase(Ease.OutBack);
        }
    }

    // fob: gốc xoay thân về người gần nhất; project lật trái/phải.
    private void FaceNearest()
    {
        BaseUnit nearest = FindNearest(destination);
        if (nearest != null)
        {
            boss.Face(nearest.Transform.position.x > destination.x);
        }
    }

    // fod: không có prefab chớp → chớp tròn 1.1.
    private static void Flash(Vector2 position, float duration)
    {
        BossRushGroundFX.Flash(position, 1.1f, duration, FLASH_COLOR);
    }

    // foe: ô cách ô boss MinDistance..MaxDistance (Euclid), đi được, trống, xa người gần nhất ≥ MinPlayerDistance;
    // chọn ngẫu nhiên. Không có ô đạt → ô xa người nhất trong vùng.
    private Vector2 PickDestination()
    {
        MapController map = MapController.Instance;
        Vector2Int bossCell = map.WorldToCell(BossPosition);
        int radius = Mathf.CeilToInt(SR.TELEPORT_MAX_DISTANCE_CELLS);
        float minSqr = SR.TELEPORT_MIN_DISTANCE_CELLS * SR.TELEPORT_MIN_DISTANCE_CELLS;
        float maxSqr = SR.TELEPORT_MAX_DISTANCE_CELLS * SR.TELEPORT_MAX_DISTANCE_CELLS;

        var candidates = new List<Vector2Int>();
        Vector2Int best = bossCell;
        float bestDistance = -1f;
        for (int row = bossCell.x - radius; row <= bossCell.x + radius; row++)
        {
            int dRow = row - bossCell.x;
            for (int dCol = -radius; dCol <= radius; dCol++)
            {
                var cell = new Vector2Int(row, bossCell.y + dCol);
                if ((dRow == 0 && dCol == 0) || !map.IsWalkable(cell) || map.IsOccupiedByOther(cell, boss))
                {
                    continue;
                }
                float distanceSqr = dRow * dRow + dCol * dCol;
                if (distanceSqr < minSqr || distanceSqr > maxSqr)
                {
                    continue;
                }
                float playerDistance = DistanceToNearest(map.CellToWorld(cell));
                if (playerDistance >= SR.TELEPORT_MIN_PLAYER_DISTANCE_CELLS)
                {
                    candidates.Add(cell);
                }
                if (playerDistance > bestDistance)
                {
                    bestDistance = playerDistance;
                    best = cell;
                }
            }
        }

        Vector2Int chosen = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : best;
        return map.CellToWorld(chosen);
    }

    // fof
    private BaseUnit FindNearest(Vector2 point)
    {
        BaseUnit nearest = null;
        float nearestSqr = float.MaxValue;
        List<BaseUnit> units = GetHomeUnits();
        for (int i = 0; i < units.Count; i++)
        {
            BaseUnit unit = units[i];
            if (!IsAlive(unit))
            {
                continue;
            }
            float distanceSqr = ((Vector2)unit.Transform.position - point).sqrMagnitude;
            if (distanceSqr < nearestSqr)
            {
                nearestSqr = distanceSqr;
                nearest = unit;
            }
        }
        return nearest;
    }

    // fog
    private float DistanceToNearest(Vector2 point)
    {
        BaseUnit nearest = FindNearest(point);
        return nearest != null ? Vector2.Distance(nearest.Transform.position, point) : float.MaxValue;
    }

    // foc
    private void RestoreVisual()
    {
        Transform visual = boss.VisualTransform;
        if (hasVisualScale && visual != null)
        {
            visual.DOKill();
            visual.localScale = visualScale;
            hasVisualScale = false;
        }
    }

    public override void Cancel()
    {
        RestoreVisual();
    }
}

// ha — FireBreath (rồng): gồng rồi quét lửa 360° quanh mình; trúng người trong nón (hoặc sát thân), mỗi người 1 lần / HitInterval.
public class BossRushFireBreathSkill : BossRushSkill
{
    private static readonly Color CONE_COLOR = new Color(1f, 0.45f, 0.1f, 0.75f);   // ygq

    private float timer;            // ygr
    private float startAngle;       // ygs
    private Vector2 origin;         // ygt
    private bool hasStarted;        // ygu
    private BaseUnit savedTarget;   // ygv
    private readonly Dictionary<BaseUnit, float> nextHitTime = new Dictionary<BaseUnit, float>();   // ygw
    private BossRushGroundFX cone;  // ygy
    private float muzzleOffset;     // yhc
    private Quaternion visualRotation;
    private bool hasVisualRotation;

    public BossRushFireBreathSkill(BossRushBossUnit boss) : base(boss) { }

    private float Range => SR.FIRE_BREATH_RADIUS + muzzleOffset;                                  // fnk
    private float PointBlankRadius => boss.BodyDiameter * 0.5f + SR.FIRE_BREATH_POINT_BLANK_MARGIN;  // fnl

    // fnb: miệng = MuzzleOffset × BodyScale; góc bắt đầu = hướng "lên" của hình boss; bỏ mục tiêu đang đánh.
    public override void Begin()
    {
        hasStarted = false;
        timer = 0f;
        nextHitTime.Clear();
        origin = BossPosition;
        float bodyScale = boss.Definition != null && boss.Definition.bodyScaleMultiplier > 0f ? boss.Definition.bodyScaleMultiplier : 1f;
        muzzleOffset = bodyScale * SR.FIRE_BREATH_MUZZLE_OFFSET;

        Transform visual = boss.VisualTransform;
        Vector2 up = visual != null ? (Vector2)visual.up : Vector2.up;
        startAngle = Mathf.Atan2(up.y, up.x) * Mathf.Rad2Deg;
        if (visual != null)
        {
            visualRotation = visual.localRotation;
            hasVisualRotation = true;
        }
        savedTarget = boss.ClearTargetForSkill();
    }

    public override bool Tick(float dt)
    {
        timer += dt;
        if (!hasStarted && timer >= SR.FIRE_BREATH_WINDUP_SECONDS)
        {
            hasStarted = true;
            StartFlame();
        }

        float sweepEnd = SR.FIRE_BREATH_WINDUP_SECONDS + SR.FIRE_BREATH_SWEEP_SECONDS;
        if (hasStarted && timer < sweepEnd)
        {
            float progress = Mathf.Clamp01((timer - SR.FIRE_BREATH_WINDUP_SECONDS) / SR.FIRE_BREATH_SWEEP_SECONDS);
            float angle = startAngle + progress * 360f;
            RotateVisual(angle);
            if (cone != null)
            {
                cone.transform.position = MuzzlePosition(angle);
                cone.SetAngle(angle);
            }
            DamageCone(angle);
        }
        else if (timer >= sweepEnd)
        {
            StopFlame();
        }

        bool isDone = timer >= sweepEnd + SR.FIRE_BREATH_RECOVERY_SECONDS;
        if (isDone)
        {
            Finish();
        }
        return isDone;
    }

    // fnq: không có prefab lửa → nón vẽ bằng code bám miệng.
    private void StartFlame()
    {
        cone = BossRushGroundFX.Cone(MuzzlePosition(startAngle), SR.FIRE_BREATH_RADIUS, SR.FIRE_BREATH_CONE_ANGLE, CONE_COLOR);
        cone.SetAngle(startAngle);
    }

    // fnp
    private Vector2 MuzzlePosition(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        return origin + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * muzzleOffset;
    }

    // fnm: thân boss xoay theo hướng phun.
    private void RotateVisual(float angle)
    {
        Transform visual = boss.VisualTransform;
        if (visual != null)
        {
            visual.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    // fnt
    private void DamageCone(float angle)
    {
        float rangeSqr = Range * Range;
        float pointBlankSqr = PointBlankRadius * PointBlankRadius;
        float halfCone = SR.FIRE_BREATH_CONE_ANGLE * 0.5f;
        List<BaseUnit> units = GetHomeUnits();
        for (int i = units.Count - 1; i >= 0; i--)
        {
            BaseUnit unit = units[i];
            if (!IsAlive(unit))
            {
                continue;
            }
            if (nextHitTime.TryGetValue(unit, out float next) && timer < next)
            {
                continue;
            }
            Vector2 offset = (Vector2)unit.Transform.position - origin;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > rangeSqr)
            {
                continue;
            }
            if (distanceSqr > pointBlankSqr)
            {
                float toUnit = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(toUnit, angle)) > halfCone)
                {
                    continue;
                }
            }

            nextHitTime[unit] = timer + SR.FIRE_BREATH_HIT_INTERVAL;
            boss.SkillAttack(unit);
            if (IsAlive(unit))
            {
                unit.AddKnockbackImpulse(origin, SR.FIRE_BREATH_KNOCKBACK_FORCE);
            }
        }
    }

    // fns
    private void StopFlame()
    {
        if (cone != null)
        {
            cone.Dismiss();
            cone = null;
        }
    }

    // fnn: trả mục tiêu + hình về như trước.
    private void Finish()
    {
        if (savedTarget != null && savedTarget.isTargetable)
        {
            boss.RestoreTargetAfterSkill(savedTarget);
        }
        savedTarget = null;

        Transform visual = boss.VisualTransform;
        if (hasVisualRotation && visual != null)
        {
            visual.localRotation = visualRotation;
            hasVisualRotation = false;
        }
    }

    public override void Cancel()
    {
        StopFlame();
        Finish();
    }
}

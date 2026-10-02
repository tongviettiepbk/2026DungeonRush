using DG.Tweening;
using UnityEngine;

// Boss Boss Rush (Character.IsBossRushBoss gốc). Chỉ số lấy từ BossRushBossModel của trận:
//   HP hiện tại/tối đa = máu boss CHUNG của nhóm (server), sát thương = BossDamageByTier[tier] (EnemyAttackPower).
// Hành vi BossRushBossDefinition: CanMove = false, RepositionAfterAttack — đánh xong chờ RepositionDelay (5s)
// rồi đổi sang ô trống ngẫu nhiên trong bán kính RepositionSearchRadius (3 ô).
// Mọi đòn trúng boss được báo về BossRushMode để cộng damage đội / damage của mình (GameController.hif gốc).
public class BossRushBossUnit : EnemyUnit
{
    private const float REPOSITION_MOVE_TIME = 0.35f;

    public BossRushBossDefinition Definition { get; private set; }
    public BossRushBossModel Model { get; private set; }
    public string DisplayName => Definition != null ? Definition.bossName : "Boss";

    private BossRushMode mode;
    private bool pendingReposition;
    private float repositionAtTime;

    public override bool isMoveable
    {
        get { return Definition != null && Definition.canMove; }
        set { }
    }

    public void SpawnBoss(BossRushMode owner, BossRushBossModel model, BossRushBossDefinition definition, Vector3 position)
    {
        mode = owner;
        Model = model;
        Definition = definition;
        isBoss = true;
        aggroRange = 1000f;     // đánh mọi mục tiêu trong tầm vũ khí, không cần aggro

        var bs = new BaseStats
        {
            attack = model.AttackPower,
            maxHp = (float)model.MaxHP,
            attackPerSecond = 1f,
            moveSpeed = 0f,
        };

        SpawnInBattle(bs, StaticValue.TAG_TEAM_B, position);
        if (bodyCollider != null)
        {
            bodyCollider.transform.localScale *= definition.colliderRadiusMultiplier;
        }

        // Máu thật = máu CHUNG còn lại của nhóm (có thể < max).
        hp = System.Math.Min(model.CurrentHP, GetMaxHp());
        UpdateHealthBar();
        pendingReposition = false;
    }

    public override double GetMaxHp()
    {
        return Model != null ? Model.MaxHP : base.GetMaxHp();
    }

    public override double TakeDamage(TakeDamageData data, bool showTextDamage = true, bool flashColor = true)
    {
        double taken = base.TakeDamage(data, showTextDamage, flashColor);
        if (Model != null)
        {
            Model.CurrentHP = System.Math.Max(0d, hp);
        }
        if (mode != null && taken > 0d && data != null)
        {
            mode.OnBossDamaged(data.attacker, taken);
        }
        return taken;
    }

    // ===== RepositionAfterAttack (BossRushRepositionState gốc) =====

    protected override void OnAttackEnd()
    {
        base.OnAttackEnd();
        if (Definition != null && Definition.behaviorType == BossRushEnemyBehaviorType.RepositionAfterAttack && !pendingReposition)
        {
            pendingReposition = true;
            repositionAtTime = Time.time + Definition.repositionDelay;
        }
    }

    public override void UpdateBehavior()
    {
        base.UpdateBehavior();
        if (pendingReposition && !isDead && !isPause && Time.time >= repositionAtTime)
        {
            pendingReposition = false;
            Reposition();
        }
    }

    private void Reposition()
    {
        MapController map = MapController.Instance;
        if (map == null || Definition == null)
        {
            return;
        }

        Vector2Int current = map.WorldToCell(Transform.position);
        int r = Definition.repositionSearchRadius;
        Vector2Int best = current;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector2Int cell = current + new Vector2Int(Random.Range(-r, r + 1), Random.Range(-r, r + 1));
            if (cell != current && map.IsWalkable(cell))
            {
                best = cell;
                break;
            }
        }

        if (best != current)
        {
            Transform.DOKill();
            Transform.DOMove(map.CellToWorld(best), REPOSITION_MOVE_TIME);
        }
    }
}

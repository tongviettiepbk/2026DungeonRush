using UnityEngine;

// Enemy (TeamB) — theo Character phe "Away" gốc (GridCombatController.hos):
//   chỉ nhắm hero/đồng minh trong TẦM PHÁT HIỆN = max(DetectionRange, tầm đánh), DetectionRange = 0 thì lấy
//   GameResources.EnemyDetectionRange = 4. Đã có mục tiêu còn sống thì BÁM TIẾP dù mục tiêu chạy ra xa.
//   Không có mục tiêu → đi tuần quanh ô xuất phát (SoldierPrefab gốc CanMove = CanPatrol = true).
//   Dungeon có theme EnemyCanMove = false (rồng) → moveSpeed 0 → không đi, không tuần.
public class EnemyUnit : BaseUnit
{
    private const float DEFAULT_DETECTION_RANGE = 4f;   // GameResources.EnemyDetectionRange

    [Tooltip("Tầm phát hiện riêng (Character.DetectionRange). 0 = dùng mặc định gốc 4.")]
    public float detectionRange;
    public bool isBoss;

    [Tooltip("Vũ khí quái thường (cận chiến/bắn xa + tầm đánh). Trống = cận chiến tay không.")]
    [SerializeField] private WeaponData weaponData;
    [Tooltip("Vũ khí khi là boss (VD boss dragon bắn đạn). Trống → dùng weaponData.")]
    [SerializeField] private WeaponData bossWeaponData;
    [Tooltip("SpriteRenderer vũ khí trên rig (weapon_slot) — hiện hình vũ khí campaign chọn theo tier.")]
    [SerializeField] private SpriteRenderer weaponRenderer;
    [Tooltip("Hình vũ khí khi quái tay không (chỉ là hình, đánh vẫn tay không).")]
    [SerializeField] private Sprite defaultWeaponSprite;

    // Node đồ trên rig (copy từ rig hero). Quái chỉ hiện đồ có trong data — xem HideUnwornGear.
    [Header("Node đồ trên rig")]
    [SerializeField] private SpriteRenderer helmetRenderer;
    [SerializeField] private SpriteRenderer handRenderer;
    [SerializeField] private SpriteRenderer backpackRenderer;
    [SerializeField] private SpriteRenderer glassRenderer;
    [SerializeField] private SpriteRenderer pipeRenderer;

    // Boss ưu tiên vũ khí boss (đạn Dragon...); quái thường dùng weaponData.
    protected override WeaponData GetCombatWeapon()
    {
        return (isBoss && bossWeaponData != null) ? bossWeaponData : weaponData;
    }

    // Spawn từ dữ liệu EnemySpawnGenerator (health/attack tuyệt đối + role + isBoss).
    public void SpawnEnemy(EnemySpawnGenerator.EnemySpawnInfo info, Vector3 position)
    {
        isBoss = info.isBoss;
        level = info.level;
        if (info.overrideWeapon)
        {
            weaponData = info.weapon;   // trước SpawnInBattle để ReloadStats áp tầm đánh/đạn của vũ khí
            ShowWeapon(info.weapon);
        }
        else
        {
            ShowDefaultWeaponIfEmpty();
        }
        HideUnwornGear(info.overrideWeapon);

        var bs = new BaseStats
        {
            attack = info.attackPower,
            attackPerSecond = info.attackSpeed,
            attackRange = info.attackRange,   // melee 1.5 / ranged 3.0 (EnemyBase*AttackDistance gốc)
            maxHp = info.health,
            moveSpeed = info.moveSpeed,
        };

        isMoveable = info.moveSpeed > 0f;
        canPatrol = isMoveable;

        SpawnInBattle(bs, StaticValue.TAG_TEAM_B, position);
    }

    // Hình vũ khí như hero (EquipVisualResolver); tay không thì cầm defaultWeaponSprite.
    private void ShowWeapon(WeaponData weapon)
    {
        if (weaponRenderer == null)
        {
            return;
        }

        Sprite sprite = weapon != null ? EquipVisualResolver.GetBodySprite(GearSlotType.WEAPON, weapon.assetName) : null;
        if (sprite == null) sprite = defaultWeaponSprite;
        weaponRenderer.sprite = sprite;
        weaponRenderer.gameObject.SetActive(sprite != null);
    }

    // Quái dungeon: vũ khí theme gán sẵn trên prefab (Cultist). Ô vũ khí trống (Zombie tay không) thì cầm dao mặc định như hero.
    private void ShowDefaultWeaponIfEmpty()
    {
        if (weaponRenderer == null || weaponRenderer.sprite != null)
        {
            return;
        }

        weaponRenderer.sprite = defaultWeaponSprite;
        weaponRenderer.gameObject.SetActive(defaultWeaponSprite != null);
    }

    // Gốc Soldier.fbb(weapon, role, helmet, gloves, animator, backpack): quái campaign chỉ có vũ khí (mũ/găng/balo = null);
    // dungeon lấy mũ/găng theo theme (gán sẵn trên prefab variant), balo null. Kính/tẩu (Wing/Cape) quái không có.
    private void HideUnwornGear(bool isCampaign)
    {
        SetGearShown(backpackRenderer, false);
        SetGearShown(glassRenderer, false);
        SetGearShown(pipeRenderer, false);
        if (isCampaign)
        {
            SetGearShown(helmetRenderer, false);
            SetGearShown(handRenderer, false);
        }
    }

    private static void SetGearShown(SpriteRenderer sr, bool shown)
    {
        if (sr != null) sr.gameObject.SetActive(shown);
    }

    // GridCombatController.hos: đối thủ gần nhất trong tầm phát hiện; không có thì giữ mục tiêu cũ còn sống.
    protected override BaseUnit SelectTarget()
    {
        if (crowdController.isTaunted && IsTargetAvailable())
        {
            return target;
        }

        float range = Mathf.Max(detectionRange > 0f ? detectionRange : DEFAULT_DETECTION_RANGE, stats.attackRange);
        BaseUnit nearest = FindNearestEnemyAmong(GetAliveEnemies(), range);
        if (nearest == null && IsTargetAvailable())
        {
            return target;
        }
        return nearest;
    }

    protected override void FindNearestTarget()
    {
        target = SelectTarget();
    }
}

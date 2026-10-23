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
    [Tooltip("SpriteRenderer vũ khí trên rig (weapon_slot) — hiện hình vũ khí CẬN CHIẾN campaign chọn theo tier.")]
    [SerializeField] private SpriteRenderer weaponRenderer;
    [Tooltip("weapon_slot_2 — hình vũ khí BẮN XA (như hero).")]
    [SerializeField] private SpriteRenderer weaponRangedRenderer;
    [Tooltip("Đầu nòng (con của weapon_slot_2) — đạn bay ra từ đây.")]
    [SerializeField] private Transform rangedMuzzle;
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

    // Hình vũ khí như hero (EquipVisualResolver): cận chiến → weapon_slot, bắn xa → weapon_slot_2;
    // tay không thì cầm defaultWeaponSprite ở weapon_slot.
    private void ShowWeapon(WeaponData weapon)
    {
        Sprite sprite = weapon != null ? EquipVisualResolver.GetBodySprite(GearSlotType.WEAPON, weapon.assetName) : null;
        if (sprite == null) sprite = defaultWeaponSprite;
        ApplyWeaponSprite(sprite, IsRanged(weapon));
    }

    // Quái dungeon: vũ khí theme gán sẵn trên prefab (Cultist: cận chiến ở weapon_slot, bắn xa ở weapon_slot_2).
    // Ô vũ khí trống (Zombie tay không) thì cầm dao mặc định như hero.
    private void ShowDefaultWeaponIfEmpty()
    {
        bool isRanged = IsRanged(weaponData);
        SpriteRenderer sr = isRanged ? weaponRangedRenderer : weaponRenderer;
        Sprite sprite = sr != null ? sr.sprite : null;
        if (sprite == null && isRanged == false) sprite = defaultWeaponSprite;
        ApplyWeaponSprite(sprite, isRanged);
    }

    private void ApplyWeaponSprite(Sprite sprite, bool isRanged)
    {
        SetWeaponSlot(weaponRenderer, isRanged ? null : sprite);
        SetWeaponSlot(weaponRangedRenderer, isRanged ? sprite : null);
    }

    private static void SetWeaponSlot(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null)
        {
            return;
        }

        if (sprite != null) sr.sprite = sprite;
        sr.gameObject.SetActive(sprite != null);
    }

    private static bool IsRanged(WeaponData weapon)
    {
        return weapon != null && weapon.weaponType == WeaponType.Range;
    }

    // Vũ khí bắn xa nằm ở weapon_slot_2 → đạn nhả từ muzzle (thiếu muzzle thì từ tâm súng).
    protected override Transform GetProjectileSpawnPoint()
    {
        if (weaponRangedRenderer != null && weaponRangedRenderer.gameObject.activeSelf)
        {
            return rangedMuzzle != null ? rangedMuzzle : weaponRangedRenderer.transform;
        }
        return base.GetProjectileSpawnPoint();
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

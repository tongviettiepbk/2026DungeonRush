using UnityEngine;

// Hero (quân người chơi, TeamA). Hành vi CHỦ ĐỘNG đúng spec:
//   tự tìm enemy GẦN NHẤT trong map → đi tới vùng đánh → attack.
// Toàn bộ state machine (Idle→Move→Attack) đã có ở BaseUnit; Hero chỉ chỉnh cách chọn mục
// tiêu để không giới hạn tầm (luôn tìm khắp map) và không lọc theo biên camera.
//
// Hình ảnh trang bị (mặc đồ) do HeroVisual xử lý — ref các node đã gán sẵn trong prefab.
public class HeroUnit : BaseUnit
{
    [SerializeField] private HeroVisual heroVisual;

    // Bản sao hero của companion MirrorClone (gốc lt.gon): > 0 = đây là clone, máu tối đa = maxHp ×
    // cloneHealthPercent/100. Clone chụp chỉ số lúc sinh, không nhận sự kiện đổi đồ.
    private float cloneHealthPercent;
    public bool IsMirrorClone => cloneHealthPercent > 0f;

    // Gọi TRƯỚC SpawnInBattle để lần tính chỉ số đầu tiên đã áp tỉ lệ máu.
    public void SetupAsMirrorClone(float healthPercent)
    {
        cloneHealthPercent = Mathf.Max(0.01f, healthPercent);
    }

    protected override void Awake()
    {
        base.Awake();

        if (heroVisual == null)
        {
            heroVisual = GetComponentInChildren<HeroVisual>();
        }
        heroVisual?.RefreshAll();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventDispatcher.Instance.RegisterListener(EventID.EquipmentChanged, OnEquipmentChanged);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventDispatcher.Instance.RemoveListener(EventID.EquipmentChanged, OnEquipmentChanged);
    }

    // Dựng lại hình trang bị từ save — gọi sau khi người chơi đổi đồ ở menu.
    public void RefreshEquipment()
    {
        heroVisual?.RefreshAll();
    }

    // ===== CHỈ SỐ (luồng StickIdle: LoadPermanentModifiers → CalculateCurrentStats) =====

    // Modifier "vĩnh viễn" của Hero = chỉ số đồ đang mặc (save). Nạp vào list để CalculateCurrentStats dùng.
    protected override void LoadPermanentModifiers()
    {
        UserEquipmentData equipment = GameData.userData != null ? GameData.userData.equipment : null;
        AddModifier(EquipmentStatResolver.BuildModifiers(equipment));
    }

    // Chỉ số cuối = NỀN PlayerBase + Σ CHỈ SỐ CHÍNH đồ (flat) rồi ÁP SUBSTAT (%).
    //   Pass 1 (flat): attack = PlayerBaseDamage + Σ main(Damage); maxHp = PlayerBaseHealth + Σ main(Health).
    //   Pass 2 (%):    gom substat theo đích rồi nhân/cộng lên kết quả pass 1 (xem EquipmentStatResolver).
    // Đúng mô hình game gốc (main cộng dồn) + mô hình % chuẩn genre cho substat (công thức tổng hợp
    // substat gốc chưa reverse — nếu sau này có thì chỉ sửa phần gom % dưới đây).
    protected override void CalculateCurrentStats()
    {
        base.CalculateCurrentStats();

        // Pass 1: CHỈ SỐ CHÍNH (flat) — cộng dồn lên nền PlayerBase.
        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier m = modifiers[i];
            if (m == null || m.isFlatValue == false)
            {
                continue;
            }

            if (m.type == StatModifierType.Attack)
            {
                stats.attack += m.value;
            }
            else if (m.type == StatModifierType.MaxHp)
            {
                stats.maxHp += m.value;
            }
        }

        // Pass 2: SUBSTAT (%) — gom theo đích (value đã là phân số, VD 0.12 = +12%).
        float attackPct = 0f, hpPct = 0f, atkSpeedPct = 0f, compDmgPct = 0f;
        float critRateAdd = 0f, critDmgAdd = 0f, doubleShotAdd = 0f, hpRegenPct = 0f, compCooldownPct = 0f;
        float blockPct = 0f, lifestealPct = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier m = modifiers[i];
            if (m == null || m.isFlatValue)
            {
                continue;
            }

            float v = (float)m.value;
            switch (m.type)
            {
                case StatModifierType.Attack: attackPct += v; break;
                case StatModifierType.MaxHp: hpPct += v; break;
                case StatModifierType.AttackSpeed: atkSpeedPct += v; break;
                case StatModifierType.CompanionDamage: compDmgPct += v; break;
                case StatModifierType.CritRate: critRateAdd += v; break;
                case StatModifierType.CritDamage: critDmgAdd += v; break;
                case StatModifierType.DoubleShot: doubleShotAdd += v; break;
                case StatModifierType.HpRecovery: hpRegenPct += v; break;
                case StatModifierType.CompanionCooldownReduction: compCooldownPct += v; break;
                case StatModifierType.BlockChance: blockPct += v; break;
                case StatModifierType.Lifesteal: lifestealPct += v; break;
            }
        }

        stats.attack *= (1f + attackPct);
        stats.maxHp *= (1f + hpPct);
        stats.attackSpeed *= (1f + atkSpeedPct);
        stats.companionDamage *= (1f + compDmgPct);
        stats.critRate += critRateAdd;
        stats.critDamage += critDmgAdd;
        stats.doubleShot += doubleShotAdd;
        stats.hpRecovery += stats.maxHp * hpRegenPct;   // hồi máu = % máu tối đa (sau khi đã áp Health%).

        // GỐC (Soldier.eyw @0x2A1D320): substat CompanionCooldown / BlockChance / Lifesteal cộng dồn THÔ %
        // của mọi món vào Character.CompanionCooldownReduction / BlockChance / Lifesteal, không trần.
        // Lưu đơn vị % như gốc (value ở đây là phân số → ×100).
        stats.companionCooldownReduction = compCooldownPct * 100f;
        stats.blockChance = blockPct * 100f;
        stats.lifesteal = lifestealPct * 100f;

        // Bản sao MirrorClone: máu tối đa = maxHp hero × cloneHealthPercent/100 (gốc lt.gon).
        if (IsMirrorClone)
        {
            stats.maxHp *= cloneHealthPercent / 100f;
        }
    }

    // Hút máu — GỐC (Character.ewb @0x2A15640): phe người chơi đánh thường trúng → nếu Lifesteal > 0 và
    // chưa đầy máu: hp = min(maxHp, hp + damage × Lifesteal/100). damage = damage đòn đã tính chí mạng
    // (trước khi mục tiêu chặn/giảm). Không hiện số / FX.
    public override void OnAttackDone(ProcessedAttackData processedAttackData, double damageDealt)
    {
        base.OnAttackDone(processedAttackData, damageDealt);

        if (stats.lifesteal <= 0f || processedAttackData == null || processedAttackData.attackType != AttackType.BasicAttack)
        {
            return;
        }

        if (hp > 0f && hp < GetMaxHp())
        {
            GetHeal(this, processedAttackData.damage * stats.lifesteal / 100f, showFx: false, showText: false);
        }
    }

    // Đổi đồ khi Hero đang sống → tính lại chỉ số ngay (ReloadStats tự áp lại vũ khí: tầm đánh/đạn).
    // Bản sao MirrorClone bỏ qua (chỉ số chụp lúc sinh).
    private void OnEquipmentChanged(object param)
    {
        if (IsMirrorClone)
        {
            return;
        }

        ReloadStats();

        if (hp > stats.maxHp)
        {
            hp = stats.maxHp;
        }
        UpdateHealthBar();
    }

    protected override void FindNearestTarget()
    {
        target = FindNearestEnemyAmong(GetAliveEnemies());
    }

    // Combat theo vũ khí ĐANG MẶC (từ save). Vũ khí quyết định tầm đánh & có đạn hay không —
    // Hero không tự quyết tầm đánh. Damage vẫn tính chung từ stats (server-driven).
    protected override WeaponData GetCombatWeapon()
    {
        return ResolveEquippedWeapon();
    }

    // Vũ khí ở slot WEAPON (từ save) → WeaponData tĩnh. Null nếu chưa mặc / không tra được.
    private WeaponData ResolveEquippedWeapon()
    {
        UserEquipmentData equip = GameData.userData != null ? GameData.userData.equipment : null;
        if (equip == null || GameData.staticData == null || GameData.staticData.weapons == null)
        {
            return null;
        }

        string id = equip.GetEquipped(GearSlotType.WEAPON);
        return string.IsNullOrEmpty(id) ? null : GameData.staticData.weapons.GetData(id);
    }
}

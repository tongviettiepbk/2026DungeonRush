using System.Collections.Generic;
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

    // Ghost Boss Rush (Soldier.IsGhost gốc): đồng đội ảo dựng từ snapshot đồ + pet sở hữu của người chơi khác.
    // Gọi TRƯỚC SpawnInBattle. Ghost không nhận sự kiện đổi đồ của save mình.
    private UserEquipmentData ghostEquipment;
    private List<CompanionModel> ghostCompanions;
    private UserEnchantmentData ghostEnchantments;
    public bool IsGhost => ghostEquipment != null;
    public string GhostName { get; private set; }

    public void SetupAsGhost(string playerName, UserEquipmentData equipment, List<CompanionModel> ownedCompanions,
                             UserEnchantmentData enchantments)
    {
        GhostName = playerName;
        ghostEquipment = equipment ?? new UserEquipmentData();
        ghostCompanions = ownedCompanions ?? new List<CompanionModel>();
        ghostEnchantments = enchantments ?? new UserEnchantmentData();
        if (heroVisual != null)
        {
            heroVisual.SetOverrideEquipment(ghostEquipment);
        }
    }

    private UserEquipmentData EquipmentSource => ghostEquipment ?? (GameData.userData != null ? GameData.userData.equipment : null);

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
        EventDispatcher.Instance.RegisterListener(EventID.CompanionOwnedChanged, OnEquipmentChanged);
        EventDispatcher.Instance.RegisterListener(EventID.MasteryChanged, OnEquipmentChanged);
        EventDispatcher.Instance.RegisterListener(EventID.EnchantmentChanged, OnEquipmentChanged);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventDispatcher.Instance.RemoveListener(EventID.EquipmentChanged, OnEquipmentChanged);
        EventDispatcher.Instance.RemoveListener(EventID.CompanionOwnedChanged, OnEquipmentChanged);
        EventDispatcher.Instance.RemoveListener(EventID.MasteryChanged, OnEquipmentChanged);
        EventDispatcher.Instance.RemoveListener(EventID.EnchantmentChanged, OnEquipmentChanged);
    }

    // Dựng lại hình trang bị từ save — gọi sau khi người chơi đổi đồ ở menu.
    public void RefreshEquipment()
    {
        heroVisual?.RefreshAll();
    }

    // ===== CHỈ SỐ (luồng StickIdle: LoadPermanentModifiers → CalculateCurrentStats) =====

    // Modifier "vĩnh viễn" của Hero = chỉ số đồ đang mặc (save) + Own Effect của mọi pet đã sở hữu (flat,
    // gốc lu.gov trong Soldier.ewc). Nạp vào list để CalculateCurrentStats dùng.
    protected override void LoadPermanentModifiers()
    {
        // Ghost: relic lấy từ snapshot Boss Rush (EnchantmentTiers) như Soldier.eyn gốc.
        UserEnchantmentData enchantments = IsGhost ? ghostEnchantments
            : (GameData.userData != null ? GameData.userData.enchantments : null);
        AddModifier(EquipmentStatResolver.BuildModifiers(EquipmentSource, enchantments));
        AddModifier(IsGhost ? CompanionService.BuildOwnEffectModifiers(ghostCompanions) : CompanionService.BuildOwnEffectModifiers());
    }

    // Chỉ số cuối = mỗi slot (món đang mặc, slot trống dùng nền PlayerBase) + Own Effect pet (flat) rồi ÁP SUBSTAT (%).
    //   Pass 1 (flat): attack = PlayerBaseDamage + Σ (main(Damage) − nền slot đó) + Σ ownAttack;
    //                  maxHp  = PlayerBaseHealth + Σ (main(Health) − nền slot đó) + Σ ownHealth.
    //   Pass 2 (%):    gom substat theo đích rồi nhân (1 + tổng) / cộng thô — GỐC Soldier.eyw/eyv:
    //                  AttackSpeed/Damage/Health/Melee/Ranged → nhân (1 + Σ%); CritChance/Block/Double/Regen/
    //                  Lifesteal/CompanionCooldown/CompanionDamage → cộng thô; CritDamage = 1 + 5% nền + Σ%.
    protected override void CalculateCurrentStats()
    {
        base.CalculateCurrentStats();

        // Pass 1: CHỈ SỐ CHÍNH (flat) — nền PlayerBase + (main món − nền slot) → đồ thay nền slot.
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
        stats.doubleShot += doubleShotAdd;

        // GỐC Soldier.eyv: CritDamage = 1 + BaseCriticalDamagePercent/100 (5% → x1.05) + Σ substat CritDamage/100
        // (không dùng nền 1.2 kế thừa StickIdle). Hồi máu (Character.ewf): MaxHp × Regen%/100 × HealthRegenMultiplier mỗi giây.
        GearStatConfigData config = EquipmentStatResolver.LoadConfig();
        float baseCritDamagePercent = config != null ? config.baseCriticalDamagePercent : 5f;
        float regenMultiplier = config != null ? config.healthRegenMultiplier : 0.5f;
        stats.critDamage = 1f + baseCritDamagePercent / 100f + critDmgAdd;
        stats.hpRecovery = stats.maxHp * hpRegenPct * regenMultiplier;

        // GỐC (Soldier.eyw @0x2A1D320): substat CompanionCooldown / BlockChance / Lifesteal cộng dồn THÔ %
        // của mọi món vào Character.CompanionCooldownReduction / BlockChance / Lifesteal, không trần.
        // Lưu đơn vị % như gốc (value ở đây là phân số → ×100).
        stats.companionCooldownReduction = compCooldownPct * 100f;
        stats.blockChance = blockPct * 100f;
        stats.lifesteal = lifestealPct * 100f;

        // Mastery "Movement Speed" (PlayerMovementSpeed): hệ số NHÂN tốc chạy (data gốc prefix "x",
        // default 1 = chưa mở khoá; Lvl1 x1.05 ... Lvl20 x2).
        stats.moveSpeed *= MasteryService.GetCurrentValue(MasteryUpgradeType.PlayerMovementSpeed);

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

    // Đổi đồ / pet sở hữu đổi (summon, nâng cấp) / mastery đổi khi Hero đang sống → tính lại chỉ số ngay
    // (ReloadStats tự áp lại vũ khí: tầm đánh/đạn).
    // Bản sao MirrorClone bỏ qua (chỉ số chụp lúc sinh).
    private void OnEquipmentChanged(object param)
    {
        if (IsMirrorClone || IsGhost)
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

    // ===== Đánh đôi (DoubleChance) =====
    // GỐC MeleeAttackState/RangeAttackState.ewu: đầu mỗi đòn của phe người chơi roll rm.ioz(DoubleChance)
    // (Random.Range(0,100) <= %); trúng thì animation đánh chạy x2 và ra 2 đòn trong cùng 1 nhịp. Project ra
    // đòn ở cuối nhịp (OnAttackEnd) nên đòn thứ 2 tung ở GIỮA nhịp, đòn còn lại vẫn ở cuối nhịp.
    protected override void BeginAttack()
    {
        base.BeginAttack();

        if (!isAttacking || stats.doubleShot <= 0f || Random.Range(0f, 100f) > stats.doubleShot * 100f)
        {
            return;
        }

        float halfDelay = 0.5f / (stats.attackSpeed * GameController.Instance.gameSpeed);
        this.StartDelayAction(halfDelay, () =>
        {
            if (!isDead && isAttacking && stateCurrent == BattleState.Attack)
            {
                ReleaseAttack();
            }
        });
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
        UserEquipmentData equip = EquipmentSource;
        if (equip == null || GameData.staticData == null || GameData.staticData.weapons == null)
        {
            return null;
        }

        string id = equip.GetEquipped(GearSlotType.WEAPON);
        return string.IsNullOrEmpty(id) ? null : GameData.staticData.weapons.GetData(id);
    }
}

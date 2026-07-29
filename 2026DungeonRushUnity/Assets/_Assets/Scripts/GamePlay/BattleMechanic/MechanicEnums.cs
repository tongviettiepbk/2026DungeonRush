public enum StatModifierType
{
    None = 0,

    // Offense
    Attack = 1,
    BasicAttackDamage = 2,
    Multishot = 3,
    AdvancedAttack = 5,
    AttackSpeed = 10,
    AttackRange = 15,
    CritRate = 20,
    CritDamage = 25,
    CompanionDamage = 30,
    CompanionAttackBonus = 31,
    CompanionAttackSpeed = 32,
    SkillDamage = 35,
    EnhancementSkillSlots = 36,
    BossDamageBonus = 40,
    NormalEnemyDamage = 41,
    DoubleShot = 45,
    TripleShot = 46,

    MaxHp = 100,
    HpRecovery = 110,
    EvasionRate = 120,
    SkillHealingRate = 130,

    MoveSpeed = 200,
    SkillCooldownReduction = 205,
    GoldObtain = 210,
    WelcomeBackRewards = 215,
    MineResearchSpeed = 220,
    PickaxeGainSpeed = 225,
    PickaxeLimit = 226,
    ResearchSpeed = 227,
    OreGain = 230,
    BattleTime = 240,
    ShardExp = 250,
}

public enum StatModifierSource
{
    None,

    Training,
    Weapon,
    Armor,
    Earring,
    Talisman,
    Skill,
    Companion,
    RelicOwned,
    Treasure,
    HeroLevelUpPassive,
    HeroPromotion,
    MasteryCommon,
    MasteryPromotion,
    Trait,
    Synergy,
    Research,
    Ring,
    Toy,
    Collection,
    Blessing,
    AdVip,
    Astrology,
    RelicEquipped,
    FlyBox,
    TreasureEquipped,
    AstrologyEquiped,
    AstrologyLevel,
    SkillUnique,
}

public enum DamageOverTimeType
{
    Burn,
    Poison,
    Bleed,
}

public enum DamageOverTimeCalculateType
{
    ByAttack,
    ByVictimMaxHp,
}

public enum CrowdControlType
{
    Stun,
    Freeze,
    Paralyze,
    Rooted,
    Taunted,
    KnockBack,
    KnockDown,
    KnockUp,
    Silent,
    SilentUltimate,
    Disarm,
    Charm,
    Hex,
    Fear,
}

public enum ShieldId
{
    None
}

public enum ShieldType
{
    Absorb,
    Block
}

public enum AttackType
{
    BasicAttack,
    SkillEquipped,
    SkillUnique,
    Splash,
    Fatigue,
}

public enum CritType
{
    Critable,
    NonCrit,
    AlwaysCrit
}

// TextDamageStatus đã định nghĩa ở Common/GameEnums.cs của DungeonRush — không khai lại ở đây.

public enum UnitBodyPoint
{
    None,
    Pivot,
    CenterBody,
}
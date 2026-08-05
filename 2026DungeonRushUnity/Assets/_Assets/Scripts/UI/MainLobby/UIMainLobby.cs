using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum TypeMenuLobby
{
    None = 0,
    Main,
    Shop,
    Pet,
    Mode,
    Clan
}

public class UIMainLobby : BaseUI
{
    public Toggle togleAutoPet;
    public List<ElementPetUILobby> listElementPet;

    [Space(20)]
    public List<ElementEquipmentUILobby> listElementEquipment;

    [Space(20)]
    public List<ElementTabMenuUILobby> listElementMenu;

    [Space(20)]
    public Button btAutoLoot;
    public Button btLoot;
    public Button btBoost;

    // Level của gear rơi ra (bản cơ bản: cố định Lv1, chỉ số chính đã đủ khác nhau theo rarity).
    private const int LOOT_LEVEL = 1;

    // Bảng công thức chỉ số, load 1 lần từ Resources (asset Gears/GearStatConfig).
    private GearStatConfigData gearStatConfig;

    private void Start()
    {
        if (btLoot != null)
            btLoot.onClick.AddListener(OnClickLoot);
    }

    private void ClickTooglePet()
    {

    }

    // ---- LOOT: bấm Btn_Loot -> random 1 gear (C2..C6) HOẶC 1 vũ khí (C1), show đầy đủ qua PopupNotice ----
    // KHÔNG bao giờ ra Cape (C7) / Wing (C8): chúng là class riêng, mở bằng cách khác nên vốn không nằm trong pool này.

    private void OnClickLoot()
    {
        DebugCustom.Log("[Loot] Bấm loot: random gear/vũ khí level " + LOOT_LEVEL + " theo rarity (C1..C6).");

        // Đảm bảo data tĩnh đã nạp (gears + weapons pool + config công thức).
        if (GameData.staticData.gears == null || GameData.staticData.weapons == null)
            GameData.staticData.Load();

        if (gearStatConfig == null)
            gearStatConfig = Resources.Load<GearStatConfigData>("Scriptable Objects/Gears/GearStatConfig");

        if (gearStatConfig == null)
        {
            DebugCustom.LogError("[Loot] Thiếu GearStatConfig.");
            return;
        }

        List<GearItemData> gearPool = GameData.staticData.gears.all;

        // Vũ khí rơi cho người chơi: loại bỏ vũ khí quái/boss.
        List<WeaponData> weaponPool = new List<WeaponData>();
        List<WeaponData> allWeapons = GameData.staticData.weapons.weapons;
        if (allWeapons != null)
        {
            for (int i = 0; i < allWeapons.Count; i++)
            {
                if (allWeapons[i].isMonsterWeapon == false)
                    weaponPool.Add(allWeapons[i]);
            }
        }

        int gearCount = gearPool != null ? gearPool.Count : 0;
        if (gearCount == 0 && weaponPool.Count == 0)
        {
            DebugCustom.LogError("[Loot] Cả gear pool lẫn weapon pool đều rỗng.");
            return;
        }

        // Random đều tay trên toàn bộ (gear + vũ khí) để tỉ lệ ra vũ khí đúng theo số lượng asset.
        bool lootWeapon = weaponPool.Count > 0 && (gearCount == 0 || Random.Range(0, gearCount + weaponPool.Count) >= gearCount);

        string info = lootWeapon
            ? BuildWeaponInfo(weaponPool[Random.Range(0, weaponPool.Count)])
            : BuildGearInfo(gearPool[Random.Range(0, gearCount)]);

        UIManager.Instance.ShowNotice(
            content: info,
            isLocalizeContent: false,
            popupType: PopupNoticeType.Yes,
            textAnchor: TMPro.TextAlignmentOptions.TopLeft,
            title: "LOOT",
            labelYes: "OK");
    }

    // Ghép chuỗi mô tả đầy đủ 1 gear (C2..C6).
    private string BuildGearInfo(GearItemData gear)
    {
        GearMainStatKind mainKind;
        double mainStat = GearStatCalculator.GetGearMainStat(gearStatConfig, gear.slot, gear.rarity, LOOT_LEVEL, out mainKind);
        List<GearSubStat> subStats = GearStatCalculator.RollSubStats(gearStatConfig, gear.rarity);

        string mainLabel = mainKind == GearMainStatKind.Health ? "Máu" : "Sát thương";
        return BuildInfo(gear.displayName, gear.rarity, SlotName(gear.slot), mainLabel, mainStat, subStats);
    }

    // Ghép chuỗi mô tả đầy đủ 1 vũ khí (C1) — chỉ số chính luôn là Sát thương.
    private string BuildWeaponInfo(WeaponData weapon)
    {
        double mainStat = GearStatCalculator.GetWeaponMainStat(gearStatConfig, weapon.weaponType, weapon.rarity, LOOT_LEVEL);
        List<GearSubStat> subStats = GearStatCalculator.RollSubStats(gearStatConfig, weapon.rarity);

        string typeLabel = "Vũ khí (" + (weapon.weaponType == WeaponType.Melee ? "Cận chiến" : "Bắn xa") + ")";
        return BuildInfo(weapon.displayName, weapon.rarity, typeLabel, "Sát thương", mainStat, subStats);
    }

    // Builder chung cho gear & vũ khí.
    private string BuildInfo(string name, Rarity rarity, string typeLabel, string mainStatLabel, double mainStat, List<GearSubStat> subStats)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Tên: " + name);
        sb.AppendLine("Độ hiếm: " + rarity);
        sb.AppendLine("Loại: " + typeLabel);
        sb.AppendLine("Level: " + LOOT_LEVEL);
        sb.AppendLine();
        sb.AppendLine("Chỉ số chính:");
        sb.AppendLine("  " + mainStatLabel + ": " + mainStat.ToString("0.##"));

        sb.AppendLine();
        if (subStats.Count == 0)
        {
            sb.AppendLine("Chỉ số phụ: (không có)");
        }
        else
        {
            sb.AppendLine("Chỉ số phụ:");
            for (int i = 0; i < subStats.Count; i++)
            {
                sb.AppendLine("  " + SubStatName(subStats[i].type) + ": +" + subStats[i].value.ToString("0.##") + "%");
            }
        }

        return sb.ToString();
    }

    private string SlotName(GearSlotType slot)
    {
        switch (slot)
        {
            case GearSlotType.HELMET: return "Mũ";
            case GearSlotType.GLOVES: return "Găng tay";
            case GearSlotType.RING: return "Nhẫn";
            case GearSlotType.NECKLACE: return "Dây chuyền";
            case GearSlotType.BACKPACK: return "Ba lô";
            case GearSlotType.CAPE: return "Áo choàng";
            case GearSlotType.WING: return "Cánh";
            default: return slot.ToString();
        }
    }

    private string SubStatName(SubStatType type)
    {
        switch (type)
        {
            case SubStatType.AttackSpeed: return "Tốc đánh";
            case SubStatType.BlockChance: return "Tỉ lệ đỡ";
            case SubStatType.CriticalChance: return "Tỉ lệ chí mạng";
            case SubStatType.CriticalDamage: return "Sát thương chí mạng";
            case SubStatType.Damage: return "Sát thương";
            case SubStatType.DoubleHitChance: return "Tỉ lệ đánh đôi";
            case SubStatType.Health: return "Máu";
            case SubStatType.HealthRegen: return "Hồi máu";
            case SubStatType.Lifesteal: return "Hút máu";
            case SubStatType.MeleeDamage: return "Sát thương cận chiến";
            case SubStatType.RangedDamage: return "Sát thương bắn xa";
            case SubStatType.CompanionCooldown: return "Hồi chiêu pet";
            case SubStatType.CompanionDamage: return "Sát thương pet";
            default: return type.ToString();
        }
    }
}

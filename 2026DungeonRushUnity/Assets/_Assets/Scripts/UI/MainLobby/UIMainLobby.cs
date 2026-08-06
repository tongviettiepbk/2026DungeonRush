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

    private void Start()
    {
        if (btLoot != null)
            btLoot.onClick.AddListener(OnClickLoot);
    }

    private void ClickTooglePet()
    {

    }

    // ---- LOOT: bấm Btn_Loot -> gọi LootService random 1 item (C1..C6), UI chỉ format & hiển thị ----

    private void OnClickLoot()
    {
        LootResult result = LootService.RollOne();
        if (result == null)
            return; // LootService đã log lỗi cụ thể.


        for(int i = 0; i < listElementEquipment.Count; i++)
        {
            if(listElementEquipment[i].typeEquipment == result.EquipSlot)
            {
                listElementEquipment[i].SetLayout(result);
            }
        }

        UIManager.Instance.ShowNotice(
            content: BuildInfo(result),
            isLocalizeContent: false,
            popupType: PopupNoticeType.Yes,
            textAnchor: TMPro.TextAlignmentOptions.TopLeft,
            title: "LOOT",
            labelYes: "OK");
    }

    #region Info gear

    // Format chuỗi mô tả đầy đủ 1 item loot được (thuần hiển thị, không đụng logic).
    private string BuildInfo(LootResult r)
    {
        string typeLabel = r.kind == LootItemKind.Weapon
            ? "Vũ khí (" + (r.weaponType == WeaponType.Melee ? "Cận chiến" : "Bắn xa") + ")"
            : SlotName(r.gearSlot);
        string mainLabel = r.mainStatKind == GearMainStatKind.Health ? "Máu" : "Sát thương";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Tên: " + r.displayName);
        sb.AppendLine("Độ hiếm: " + r.rarity);
        sb.AppendLine("Loại: " + typeLabel);
        sb.AppendLine("Level: " + r.level);
        sb.AppendLine();
        sb.AppendLine("Chỉ số chính:");
        sb.AppendLine("  " + mainLabel + ": " + r.mainStat.ToString("0.##"));

        sb.AppendLine();
        List<GearSubStat> subStats = r.subStats;
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

    #endregion


}

using System.Collections.Generic;
using TMPro;
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

    [Space(20)]
    public TMP_Text txtLootTicket;

    private void Start()
    {
        if (btLoot != null)
            btLoot.onClick.AddListener(OnClickLoot);

        UpdateLootTicketText();
    }

    private void Update()
    {

#if UNITY_EDITOR
        // add more loot ticket for test
        if (Input.GetKeyDown(KeyCode.L))
        {
            GameData.userData.items.Receive(ItemType.LOOT_TICKET, 100);
            UpdateLootTicketText();
        }

#endif

    }

    private void UpdateLootTicketText()
    {
        if (txtLootTicket != null)
            txtLootTicket.text = GameData.userData.items.GetQuantityHave(ItemType.LOOT_TICKET).ToString("0");
    }

    private void ClickTooglePet()
    {

    }

    // ---- LOOT: bấm Btn_Loot -> tiêu 1 LOOT_TICKET -> LootService random 1 item theo forgeLevel
    // (rarity roll từ ForgeData), UI chỉ format & hiển thị ----

    private void OnClickLoot()
    {
        if (GameData.userData.items.IsEnough(ItemType.LOOT_TICKET, 1) == false)
        {
            UIManager.Instance.ShowNotice(
                content: "Không đủ vé loot.",
                isLocalizeContent: false,
                popupType: PopupNoticeType.Yes,
                title: "LOOT",
                labelYes: "OK");
            return;
        }

        LootResult result = LootService.RollOne(GameData.userData.campaign.forgeLevel);
        if (result == null)
            return; // LootService đã log lỗi cụ thể.

        GameData.userData.items.Consume(ItemType.LOOT_TICKET, 1);
        UpdateLootTicketText();

        ElementEquipmentUILobby targetElement = null;
        for (int i = 0; i < listElementEquipment.Count; i++)
        {
            if (listElementEquipment[i].typeEquipment == result.EquipSlot)
            {
                targetElement = listElementEquipment[i];
                break;
            }
        }

        LootResult oldResult = targetElement != null ? targetElement.GetDataGear() : null;

        UILootGearInfo uiLootGearInfo = UIManager.Instance.LoadUI(UIKey.LootGearInfo) as UILootGearInfo;
        if (uiLootGearInfo != null)
        {
            uiLootGearInfo.Show(result, oldResult,
                onEquip: () =>
                {
                    if (targetElement != null)
                        targetElement.SetLayout(result);

                    // Ghép loot -> mặc đồ: lưu món vào save rồi báo Hero mặc lại đúng slot (live nếu
                    // hero đang trong scene; nếu không, hero sẽ đọc save khi spawn ở trận).
                    GameData.userData.equipment.Equip(result.EquipSlot, result.equipId);
                    this.PostEvent(EventID.EquipmentChanged, result.EquipSlot);
                },
                onSell: null);
        }
    }

    #region Info gear

    public static string SlotName(GearSlotType slot)
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

    public static string SubStatName(SubStatType type)
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

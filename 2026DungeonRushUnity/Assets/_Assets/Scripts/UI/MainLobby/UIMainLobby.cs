using Newtonsoft.Json;
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

    [Space(20)]
    public TMP_Text txtQuantityName;
    public TMP_Text txtQuantityPower;
    public TMP_Text txtQuantityGem;


#if UNITY_EDITOR
    [Space(20)]
    [Header("DEBUG - chỉ dùng test")]
    // Nhập forgeLevel để test bảng rarity. -1 = dùng giá trị thật trong save (campaign.forgeLevel).
    [SerializeField] private int debugForgeLevel = -1;
#endif

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

        // chỉnh nhanh debugForgeLevel khi đang chơi: mũi tên lên/xuống
        if (Input.GetKeyDown(KeyCode.UpArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {++debugForgeLevel}");
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {--debugForgeLevel}");

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

        int forgeLevel = GameData.userData.campaign.forgeLevel;
#if UNITY_EDITOR
        // Test: nếu có nhập debugForgeLevel (>=0) thì override level dùng để roll rarity.
        if (debugForgeLevel >= 0)
            forgeLevel = debugForgeLevel;
#endif

        LootResult result = LootService.RollOne(forgeLevel);
        if (result == null)
            return; // LootService đã log lỗi cụ thể.


        DebugCustom.ShowLog("subStats:", JsonConvert.SerializeObject(result.subStats));

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

                    // Ghép loot -> mặc đồ: lưu món (kèm rarity/level/substat đã roll) vào save rồi báo
                    // Hero mặc lại đúng slot (live nếu hero đang trong scene; nếu không, hero đọc save khi spawn).
                    GameData.userData.equipment.Equip(result.EquipSlot, result.equipId, result.rarity, result.level, result.subStats);
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

    // Đổ danh sách dòng substat vào các ô text: ô thứ i hiện "Tên: +giá trị%" nếu có, thừa thì ẩn.
    // Dùng chung cho UIGearInfo/UILootGearInfo. Null-guard cả subStats lẫn danh sách text (prefab có
    // thể chưa wire hết), gear rarity thấp có thể 0 dòng.
    public static void FillSubStats(List<GearSubStat> subStats, List<TMP_Text> texts)
    {
        if (texts == null)
            return;

        int count = subStats != null ? subStats.Count : 0;
        for (int i = 0; i < texts.Count; i++)
        {
            if (texts[i] == null)
                continue;

            bool hasSubStat = i < count;
            texts[i].gameObject.SetActive(hasSubStat);
            if (hasSubStat)
                texts[i].text = SubStatName(subStats[i].type) + ": +" + subStats[i].value.ToString("0.##") + "%";
        }
    }

    #endregion

    #region Load all Info Gear current
    public void Refresh()
    {
        ReloadInfoGear();
    }

    private void ReloadInfoGear()
    {
        // Mở lobby: đọc save "đang mặc gì ở mỗi slot" rồi dựng lại LootResult cho từng ô trang bị.
        // Slot trống -> SetLayout(null) để ô về trạng thái chưa có đồ.
        if (listElementEquipment == null)
            return;

        for (int i = 0; i < listElementEquipment.Count; i++)
        {
            ElementEquipmentUILobby element = listElementEquipment[i];
            if (element == null)
                continue;

            string equipId = GameData.userData.equipment.GetEquipped(element.typeEquipment);
            LootResult result = LootService.BuildFromEquipId(element.typeEquipment, equipId);
            element.SetLayout(result);
        }
    }
    #endregion
}

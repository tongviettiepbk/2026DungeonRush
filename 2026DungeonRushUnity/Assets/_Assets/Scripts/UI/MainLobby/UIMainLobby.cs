using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TypeMenuLobby
{
    Shop = 0,
    Pet = 1,
    Dungeon = 2,
    Event = 3,
    Clan = 4,
}

public class UIMainLobby : BaseUI
{
    [Space(20)]
    [Header(" Top")]
    public TMP_Text txtNamePlayer;
    public TMP_Text txtPower;
    public TMP_Text txtLevelMap;
    public TMP_Text txtGem;

    [Space(20)]
    [Header("MID")]
    public Button btAutoPet;
    public TMP_Text txtAutoPet;
    public List<ElementPetUILobby> listElementPet;

    public TMP_Text txtLevelPlayer;
    public Image imgProcessPlayer;
    public TMP_Text txtProcess;

    [Space(20)]
    [Header("Loop")]
    public List<ElementEquipmentUILobby> listElementEquipment;
    public Button btAutoLoot;
    public Button btLoot;
    public Button btBoost;
    public TMP_Text txtLootTicket;

    [Space(20)]
    public List<ElementTabMenuUILobby> listElementMenu;

    [Space(20)]
    // Trang Enchantment (ContentPage/PageEnchantment) — mở từ nút enchantment ở ô trang bị.
    public UITabEnchantment pageEnchantment;
    // Trang Wing (ContentPage/PageWing) — mở từ ô Wing.
    public UITabWing pageWing;

    [Space(20)]
    public List<GameObject> listObjTab = new List<GameObject>();

    [Space(20)]
    public GameObject objDungeonUI;
    public Button btExitDungeon;

    [Space(20)]
    public GameObject objBossRushUI;
    // Tấm phủ đáy "Battle in progress" + nút Exit (EventBlockerUI gốc) — che thanh pet + tab trong Boss Rush.
    public GameObject objEventBlockerUI;
    public Button btExitEventBlocker;
    public TMP_Text txtTimeBoss;
    public TMP_Text txtHpRemainBoss;
    public Image imgProcessRed;
    public Image imgProcessWhite;

    [Space(20)]
    public GameObject objPvpUI;
    public TMP_Text txtTimePvp;

    private string petAuto = "Auto On";
    private string petOff = "Auto Off";

#if UNITY_EDITOR
    [Space(20)]
    [Header("DEBUG - chỉ dùng test")]
    // Nhập level để test bảng rarity. -1 = dùng giá trị thật trong save (playerLevel).
    [SerializeField] private int debugForgeLevel = -1;
#endif

    private void Start()
    {
        if (btLoot != null)
            btLoot.onClick.AddListener(OnClickLoot);

        if (btExitDungeon != null)
            btExitDungeon.onClick.AddListener(OnClickExitDungeon);

        if (btExitEventBlocker != null)
            btExitEventBlocker.onClick.AddListener(OnClickExitEventBlocker);

        btAutoPet.onClick.AddListener(ClickTooglePet);
        for (int i = 0; i < listElementPet.Count; i++)
            listElementPet[i].Init(this);

        for (int i = 0; i < listElementEquipment.Count; i++)
        {
            listElementEquipment[i].onClickEnchantment = OpenEnchantment;
            listElementEquipment[i].onClickWing = OpenWing;
            listElementEquipment[i].onClickCape = OpenCape;
        }

        EventDispatcher.Instance.RegisterListener(EventID.EquipmentChanged, OnPowerSourceChanged);
        EventDispatcher.Instance.RegisterListener(EventID.CompanionOwnedChanged, OnPowerSourceChanged);
        EventDispatcher.Instance.RegisterListener(EventID.EnchantmentChanged, OnPowerSourceChanged);
        EventDispatcher.Instance.RegisterListener(EventID.WingChanged, OnWingChanged);
        EventDispatcher.Instance.RegisterListener(EventID.CapeChanged, OnCapeChanged);
        LoadPowerTxt();

        if (pageEnchantment != null)
            pageEnchantment.onNotEnoughVial = () => OpenTab(TypeMenuLobby.Dungeon);

        UpdateLootTicketText();
        InitTabMenu();

        // Gốc GameplayUI → BossRushController.ekp lúc vào game: gửi lại snapshot đồ Boss Rush (1 lần/ngày UTC).
        BossRushController.Instance.OnGameStarted();
    }

    #region Tab menu

    // listElementMenu[i] <-> listObjTab[i] <-> (TypeMenuLobby)i
    private void InitTabMenu()
    {
        for (int i = 0; i < listElementMenu.Count; i++)
        {
            if (listElementMenu[i] != null)
                listElementMenu[i].Init((TypeMenuLobby)i, OnClickTab);
        }

        // Mặc định vào lobby: đóng hết, không tab nào mở.
        CloseAllTabs();
    }

    // Bấm tab đang đóng -> mở; bấm lại chính tab đang mở -> đóng.
    private void OnClickTab(TypeMenuLobby type)
    {
        int index = (int)type;
        if (index < listElementMenu.Count && listElementMenu[index] != null && listElementMenu[index].isOpen)
            CloseAllTabs();
        else
            OpenTab(type);
    }

    // Mở 1 tab thì đóng các tab còn lại (cả objSelect của element lẫn obj nội dung tab).
    public void OpenTab(TypeMenuLobby type)
    {
        SetActiveTab((int)type);
    }

    public void CloseAllTabs()
    {
        SetActiveTab(-1);
    }

    // index = -1 -> không tab nào mở.
    private void SetActiveTab(int index)
    {
        // Mở 1 tab menu thì đóng trang Enchantment / Wing (cùng nằm trong ContentPage).
        if (index >= 0 && pageEnchantment != null)
            pageEnchantment.Close();
        if (index >= 0 && pageWing != null)
            pageWing.Close();

        for (int i = 0; i < listElementMenu.Count; i++)
        {
            if (listElementMenu[i] != null)
                listElementMenu[i].SetOpen(i == index);
        }

        for (int i = 0; i < listObjTab.Count; i++)
        {
            if (listObjTab[i] != null)
                listObjTab[i].SetActive(i == index);
        }
    }

    // Mở trang Enchantment (đóng các tab menu đang mở).
    private void OpenEnchantment(GearSlotType slot)
    {
        if (pageEnchantment == null)
            return;

        CloseAllTabs();
        if (pageWing != null)
            pageWing.Close();
        pageEnchantment.Open();
    }

    // Mở trang Wing (đóng tab menu / trang Enchantment). Chưa tới level mở khoá thì báo.
    private void OpenWing()
    {
        if (pageWing == null)
            return;

        if (WingService.IsUnlocked() == false)
        {
            UIManager.Instance.ShowToastMessage("Mở Wing ở level " + WingService.UNLOCK_PLAYER_LEVEL, isLocalize: false);
            return;
        }

        CloseAllTabs();
        if (pageEnchantment != null)
            pageEnchantment.Close();
        pageWing.Open();
    }

    // Mở popup Cape. Chưa tới level mở khoá thì báo.
    private void OpenCape()
    {
        if (CapeService.IsUnlocked() == false)
        {
            UIManager.Instance.ShowToastMessage("Mở Cape ở level " + CapeService.UNLOCK_PLAYER_LEVEL, isLocalize: false);
            return;
        }

        UICapePopup ui = UIManager.Instance.LoadUI(UIKey.CapePopup) as UICapePopup;
        if (ui != null)
            ui.Show();
    }

    #endregion

    private void Update()
    {
        UpdatePetCooldowns();

#if UNITY_EDITOR
        // add more loot ticket for test
        if (Input.GetKeyDown(KeyCode.L))
        {
            GameData.userData.items.Receive(ItemType.LOOT_TICKET, 100);
            UpdateLootTicketText();
        }

        // test: +2000 Vial để thử summon Enchantment
        if (Input.GetKeyDown(KeyCode.V))
        {
            GameData.userData.items.Receive(ItemType.VIAL, 2000);
            this.PostEvent(EventID.EnchantmentChanged);
        }

        // test: +1000 mỗi loại quặng để thử Wing craft / reroll / upgrade
        if (Input.GetKeyDown(KeyCode.O))
        {
            foreach (MineOreType ore in System.Enum.GetValues(typeof(MineOreType)))
                GameData.userData.items.Receive(WingService.ToItemType(ore), 1000);
            this.PostEvent(EventID.WingChanged);
        }

        // test: +1000 Cloak để thử summon Cape
        if (Input.GetKeyDown(KeyCode.K))
        {
            GameData.userData.items.Receive(ItemType.CLOAK, 1000);
            this.PostEvent(EventID.CapeChanged);
        }

        // test: mở tất cả pet (sở hữu đủ 16 con, level 1)
        if (Input.GetKeyDown(KeyCode.P))
            CheatUnlockAllPets();

        // chỉnh nhanh debugForgeLevel khi đang chơi: mũi tên lên/xuống
        if (Input.GetKeyDown(KeyCode.UpArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {++debugForgeLevel}");
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {--debugForgeLevel}");

#endif

    }

    public void UpdateLootTicketText()
    {
        if (txtLootTicket != null)
            txtLootTicket.text = GameData.userData.items.GetQuantityHave(ItemType.LOOT_TICKET).ToString("0");
    }

    #region Pet

#if UNITY_EDITOR
    // Test: sở hữu toàn bộ companion + đẩy playerLevel lên mốc mở hệ pet (nếu chưa đủ) để ô pet lobby mở.
    private void CheatUnlockAllPets()
    {
        List<CompanionData> all = GameData.staticData.companions.companions;
        for (int i = 0; i < all.Count; i++)
            GameData.userData.companions.Own(all[i].assetName);

        UserPlayerData player = GameData.userData.player;
        if (player.playerLevel < CompanionSummonConfig.COMPANION_UNLOCK_PLAYER_LEVEL)
        {
            player.playerLevel = CompanionSummonConfig.COMPANION_UNLOCK_PLAYER_LEVEL;
            player.isDataChanged = true;
        }

        GameData.Save(true);
        Refresh();
        this.PostEvent(EventID.CompanionOwnedChanged);
        CompanionUI companionUI = FindAnyObjectByType<CompanionUI>();
        if (companionUI != null)
            companionUI.Refresh();

        DebugCustom.Log($"[Cheat] Mở tất cả pet: owned={GameData.userData.companions.owned.Count}/{all.Count}, playerLevel={player.playerLevel}");
    }
#endif

    // Đổ 3 ô pet theo danh sách equip (UserCompanionData) + trạng thái nút auto.
    // Gọi khi vào lobby và khi CompanionUI đổi equip.
    public void RefreshPets()
    {
        bool isUnlocked = GameData.userData.player.playerLevel >= CompanionSummonConfig.COMPANION_UNLOCK_PLAYER_LEVEL;
        List<string> equipped = GameData.userData.companions.GetEquipped();
        for (int i = 0; i < listElementPet.Count; i++)
        {
            CompanionData data = i < equipped.Count ? GameData.staticData.companions.GetData(equipped[i]) : null;
            listElementPet[i].SetData(data, isUnlocked);
        }

        UpdateAutoPetVisual();
    }

    // Thanh fill mỗi ô = tiến độ hồi chiêu của pet tương ứng đang ở trong trận.
    private void UpdatePetCooldowns()
    {
        for (int i = 0; i < listElementPet.Count; i++)
        {
            ElementPetUILobby element = listElementPet[i];
            element.UpdateCooldown(element.Data != null ? GetBattlePet(element.Data.assetName) : null);
        }
    }

    private static PetUnit GetBattlePet(string assetName)
    {
        BaseMode mode = GameController.Instance.mode;
        return mode != null ? mode.GetPet(assetName) : null;
    }

    // Bấm ô pet: khoá → báo level mở; trống → mở tab Pet; có pet → kích hoạt ra đòn (khi đã hồi).
    public void OnClickPetSlot(ElementPetUILobby element)
    {
        if (!element.IsUnlocked)
        {
            UIManager.Instance.ShowToastMessage(
                "Mở pet ở level " + CompanionSummonConfig.COMPANION_UNLOCK_PLAYER_LEVEL, isLocalize: false);
            return;
        }

        if (element.Data == null)
        {
            OpenTab(TypeMenuLobby.Pet);
            return;
        }

        PetUnit pet = GetBattlePet(element.Data.assetName);
        if (pet != null)
            pet.RequestActivate();
    }

    // Bật/tắt auto kích hoạt pet (lưu save).
    private void ClickTooglePet()
    {
        UserCompanionData companions = GameData.userData.companions;
        companions.isAutoActive = !companions.isAutoActive;
        companions.isDataChanged = true;
        GameData.Save();
        UpdateAutoPetVisual();
    }

    // Chữ trên nút auto: petAuto khi pet tự kích hoạt, petOff khi người chơi bấm tay.
    private void UpdateAutoPetVisual()
    {
        if (txtAutoPet != null)
            txtAutoPet.text = GameData.userData.companions.isAutoActive ? petAuto : petOff;
    }

    #endregion

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

        // Bảng rarity bám playerLevel (hệ exp). Row 0-based = playerLevel - 1 (Level 1 -> dòng 0).
        int forgeLevel = GameData.userData.player.playerLevel - 1;
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
        // Clan War: điểm "Loot {rarity} Equipment" (ClanWarController.fzk gốc).
        ClanWarController.Instance.RecordLoot(result.rarity);
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
        RefreshPets();
        SetlevelPlayer();
        UpdateProcessLevel();
        LoadResourceTxt();
        LoadPowerTxt();
        LoadInfoMap();
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

            // Wing/Cape không phải đồ loot (RefreshWingSlot/RefreshCapeSlot lo phần hiển thị).
            if (element.typeEquipment == GearSlotType.WING || element.typeEquipment == GearSlotType.CAPE)
            {
                element.SetLayout(null);
                continue;
            }

            string equipId = GameData.userData.equipment.GetEquipped(element.typeEquipment);
            LootResult result = LootService.BuildFromEquipId(element.typeEquipment, equipId);
            element.SetLayout(result);
        }

        RefreshWingSlot();
        RefreshCapeSlot();
    }

    // Ô Cape: hiện icon/level cape đang mặc (Loot không dựng được Cape).
    private void RefreshCapeSlot()
    {
        if (listElementEquipment == null)
            return;

        CapeModel model = CapeService.GetEquipped();
        CapeData cape = CapeService.GetData(model);
        for (int i = 0; i < listElementEquipment.Count; i++)
        {
            if (listElementEquipment[i] != null && listElementEquipment[i].typeEquipment == GearSlotType.CAPE)
                listElementEquipment[i].SetCape(cape, model != null ? model.level : 1);
        }
    }

    private void OnCapeChanged(object param)
    {
        RefreshCapeSlot();
        LoadPowerTxt();
    }

    // Ô Wing: Loot không dựng được Wing nên hiện icon/level wing đang mặc riêng.
    private void RefreshWingSlot()
    {
        if (listElementEquipment == null)
            return;

        EquippedItemData rec = GameData.userData.equipment.GetRecord(GearSlotType.WING);
        WingData wing = rec != null && int.TryParse(rec.equipId, out int wingId) ? GameData.staticData.wings.GetData(wingId) : null;
        for (int i = 0; i < listElementEquipment.Count; i++)
        {
            if (listElementEquipment[i] != null && listElementEquipment[i].typeEquipment == GearSlotType.WING)
                listElementEquipment[i].SetWing(wing, rec != null ? rec.level : 1);
        }
    }

    private void OnWingChanged(object param)
    {
        RefreshWingSlot();
        LoadPowerTxt();
    }
    #endregion

    #region Full Info Player

    private void LoadResourceTxt()
    {
        // số lượng gem hiện có (ItemType.GEM trong UserItemData). Gọi khi vào scene hoặc khi có thay đổi.
        if (txtGem != null)
            txtGem.text = GameData.userData.items.GetQuantityHave(ItemType.GEM).ToString("0");
    }

    // Power người chơi (rm.iqm gốc, xem PlayerPower). Cập nhật khi đổi đồ / pet sở hữu / relic.
    private void LoadPowerTxt()
    {
        if (txtPower != null)
            txtPower.text = PlayerPower.ToLong(PlayerPower.GetCurrent()).ToLetter();
    }

    private void OnPowerSourceChanged(object param)
    {
        LoadPowerTxt();
    }

    private void OnDestroy()
    {
        if (EventDispatcher.IsNull == false)
        {
            EventDispatcher.Instance.RemoveListener(EventID.EquipmentChanged, OnPowerSourceChanged);
            EventDispatcher.Instance.RemoveListener(EventID.CompanionOwnedChanged, OnPowerSourceChanged);
            EventDispatcher.Instance.RemoveListener(EventID.EnchantmentChanged, OnPowerSourceChanged);
            EventDispatcher.Instance.RemoveListener(EventID.WingChanged, OnWingChanged);
            EventDispatcher.Instance.RemoveListener(EventID.CapeChanged, OnCapeChanged);
        }
    }

    private void LoadInfoMap()
    {
        // Đang trong dungeon: bật objDungeonUI (có nút Exit), txtLevelMap = độ khó dungeon "1-1".
        DungeonMode dungeon = GameController.Instance.mode as DungeonMode;
        SetActiveDungeonUI(dungeon != null);
        if (txtLevelMap == null)
            return;

        if (dungeon != null)
        {
            txtLevelMap.text = StaticDungeonData.GetDifficultyText(dungeon.DungeonLevel);
            return;
        }

        // Màn campaign đang đánh: stageIdCurrent (101, 102...) hiển thị dạng "chương-màn" = "1-1".
        int stageId = GameData.userData.campaign.stageIdCurrent;
        StaticCampaignData campaign = GameData.staticData.campaign;
        txtLevelMap.text = campaign.GetChapter(stageId) + "-" + campaign.GetStageIndex(stageId);
    }

    public void SetActiveDungeonUI(bool isOn)
    {
        if (objDungeonUI != null)
            objDungeonUI.SetActive(isOn);
    }

    private void OnClickExitDungeon()
    {
        DungeonMode dungeon = GameController.Instance.mode as DungeonMode;
        if (dungeon != null)
            dungeon.Exit();
    }

    public void SetActiveEventBlocker(bool isOn)
    {
        if (objEventBlockerUI != null)
            objEventBlockerUI.SetActive(isOn);
        if (btExitEventBlocker != null)
            btExitEventBlocker.interactable = true;
    }

    // GameplayUI.kjv gốc: bấm Exit khi Boss Rush / PvP → khoá nút rồi bỏ trận (PvP: xử thua, GameController.hio).
    private void OnClickExitEventBlocker()
    {
        BossRushMode bossRush = GameController.Instance.mode as BossRushMode;
        PvPMode pvp = GameController.Instance.mode as PvPMode;
        bool exited = bossRush != null ? bossRush.ExitMidFight() : pvp != null && pvp.GiveUp();
        if (!exited)
            return;
        if (btExitEventBlocker != null)
            btExitEventBlocker.interactable = false;
    }

    // Level người chơi (hệ exp) = INDEX bảng rarity loot. OnClickLoot roll rarity theo playerLevel - 1.
    private void SetlevelPlayer()
    {
        if (txtLevelPlayer != null)
            txtLevelPlayer.text = GameData.userData.player.playerLevel.ToString();
    }

    // Thanh exp tiến tới level kế: fill = expHiệnTại / ngưỡngLênLevel, text = "cur/need".
    // Đạt MaxLevel (hết bảng experience_required_per_level) thì thanh đầy, hiện "MAX".
    private void UpdateProcessLevel()
    {
        UserPlayerData player = GameData.userData.player;
        StaticExperienceData exp = GameData.staticData.experience;

        if (player.playerLevel >= exp.MaxLevel)
        {
            if (imgProcessPlayer != null)
                imgProcessPlayer.fillAmount = 1f;
            if (txtProcess != null)
                txtProcess.text = "MAX";
            return;
        }

        int need = exp.GetXpRequired(player.playerLevel);
        int cur = player.playerExperience;

        if (imgProcessPlayer != null)
            imgProcessPlayer.fillAmount = need > 0 ? Mathf.Clamp01((float)cur / need) : 0f;
        if (txtProcess != null)
            txtProcess.text = cur + "/" + need;
    }



    #endregion
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup xem thông tin 1 người chơi khác (PlayerPopup gốc): tên, avatar, hình hero + pet, Power, 8 ô đồ (độ hiếm,
// level, huy hiệu relic +N), pet đang dùng, tổng chỉ số phụ. Bấm 1 ô đồ / ô pet → popup chi tiết món / pet đó.
// Nơi mở tự tải dữ liệu: ShowLoading(userId) trước, có hồ sơ thì Show(player). Hiện mở từ dòng người chơi ở sảnh Boss Rush.
public class UIPlayerPopup : BaseUI
{
    private const float LOADING_SPIN_SPEED = 360f;

    public Button btClose;
    public GameObject objLoaded;
    public GameObject objLoading;
    public TMP_Text txtName;
    public TMP_Text txtPower;
    public Image imgAvatar;
    public PlayerPreviewStage preview;
    public List<ElementPlayerItemUI> listItem;
    public List<ElementPlayerItemUI> listCompanion;
    public TMP_Text txtSubStats;
    public RectTransform rectContent;   // khung nền, cao theo nội dung
    public RectTransform rectLayout;    // cột nội dung (tự co giãn theo số pet / số dòng chỉ số phụ)

    private string userId;
    private float loadingHeight;
    private BossRushPlayerModel player;
    private UserEquipmentData equipment;
    // Pet đang hiện ở listCompanion, cùng thứ tự.
    private readonly List<BossRushCompanionModel> shownCompanions = new List<BossRushCompanionModel>();

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        for (int i = 0; i < listItem.Count; i++) listItem[i].Init(OnClickItem);
        for (int i = 0; i < listCompanion.Count; i++) listCompanion[i].Init(OnClickCompanion);
        loadingHeight = rectContent.rect.height;
    }

    private void Update()
    {
        if (objLoading.activeSelf)
        {
            objLoading.transform.Rotate(0f, 0f, -LOADING_SPIN_SPEED * Time.unscaledDeltaTime);
        }
    }

    // Popup còn mở và đang xem đúng người này (kết quả tải về trễ thì đối chiếu trước khi vẽ).
    public bool IsShowing(string userId)
    {
        return gameObject.activeInHierarchy && this.userId == userId;
    }

    public void ShowLoading(string userId)
    {
        this.userId = userId;
        txtName.text = string.Empty; // tên nằm trên thanh tiêu đề (ngoài objLoaded) → xoá tên người trước
        objLoaded.SetActive(false);
        objLoading.SetActive(true);
        rectContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, loadingHeight);
    }

    public void Show(BossRushPlayerModel player)
    {
        this.player = player;
        userId = player.UserId;
        equipment = BossRushController.ToEquipmentData(player.Items);
        objLoading.SetActive(false);
        objLoaded.SetActive(true);

        txtName.text = player.PlayerName;
        txtPower.text = "<sprite=0>" + player.Power.ToLetter();
        imgAvatar.sprite = AvatarConfig.Get(player.AvatarId);

        DrawItems(player);
        List<CompanionData> pets = DrawCompanions(player.Companions);
        txtSubStats.text = GetSubStatsText(player.Items);
        preview.Show(equipment, player.ShowCloak, pets);
        FitContentHeight();
    }

    // Khung nền bản gốc cao cố định, còn cột nội dung dài ngắn theo từng người → kéo khung theo cột, lề dưới = lề trên.
    private void FitContentHeight()
    {
        // Các khối con (lưới đồ, hàng pet, chỉ số phụ) tự co theo nội dung → dựng chúng trước rồi mới tới cột.
        for (int i = 0; i < rectLayout.childCount; i++)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)rectLayout.GetChild(i));
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectLayout);
        float margin = -rectLayout.anchoredPosition.y;
        rectContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectLayout.rect.height + margin * 2f);
    }

    private void DrawItems(BossRushPlayerModel player)
    {
        for (int i = 0; i < listItem.Count; i++)
        {
            ElementPlayerItemUI element = listItem[i];
            int slot = (int)element.typeEquipment;
            int relicTier = GetRelicTier(element.typeEquipment);

            BossRushItemModel item = FindItem(player.Items, slot);
            Sprite icon = item != null ? GetItemIcon(element.typeEquipment, item.ItemId) : null;
            if (icon != null)
            {
                element.SetData(icon, (Rarity)item.Rarity, item.ItemLevel, relicTier);
            }
            else
            {
                element.SetEmpty(relicTier);
            }
        }
    }

    // Vẽ các pet đang dùng, trả về data của chúng để dựng hình cạnh hero.
    private List<CompanionData> DrawCompanions(List<BossRushCompanionModel> companions)
    {
        List<CompanionData> pets = new List<CompanionData>();
        shownCompanions.Clear();
        int count = 0;
        for (int i = 0; companions != null && i < companions.Count && count < listCompanion.Count; i++)
        {
            BossRushCompanionModel model = companions[i];
            if (!model.Equipped) continue;

            CompanionData data = GameData.staticData.companions.GetData(model.CompanionId);
            if (data == null) continue;

            listCompanion[count].gameObject.SetActive(true);
            listCompanion[count].SetData(data.icon, data.rarity, model.CompanionLevel, 0);
            pets.Add(data);
            shownCompanions.Add(model);
            count++;
        }
        for (int i = count; i < listCompanion.Count; i++)
        {
            listCompanion[i].gameObject.SetActive(false);
        }
        return pets;
    }

    private int GetRelicTier(GearSlotType slot)
    {
        List<int> tiers = player.EnchantmentTiers;
        return tiers != null && (int)slot < tiers.Count ? tiers[(int)slot] : 0;
    }

    // ===== Bấm xem chi tiết =====

    // Chỉ số chính hiện đã nhân hệ số relic của slot, như ô đồ của chính mình (UIGearInfo).
    private void OnClickItem(ElementPlayerItemUI element)
    {
        GearSlotType slot = element.typeEquipment;
        EquippedItemData record = equipment.GetRecord(slot);
        if (record == null) return;

        float multiplier = EnchantmentConfig.GetStatMultiplier(GetRelicTier(slot));
        if (slot == GearSlotType.WING)
        {
            WingData wing = int.TryParse(record.equipId, out int wingId) ? GameData.staticData.wings.GetData(wingId) : null;
            if (wing == null) return;

            string mainStats = "Health " + (GearStatCalculator.GetWingHealth(wing, record.level) * multiplier).ToLetter(false)
                + "\nDamage " + (GearStatCalculator.GetWingDamage(wing, record.level) * multiplier).ToLetter(false);
            ShowItemInfo(wing.icon, wing.wingName, wing.rarity, record.level, mainStats, record.subStats);
            return;
        }
        if (slot == GearSlotType.CAPE)
        {
            CapeData cape = int.TryParse(record.equipId, out int capeId) ? GameData.staticData.capes.GetData(capeId) : null;
            if (cape == null) return;

            string mainStats = "Damage +" + (GearStatCalculator.GetCapeDamagePercent(cape, record.level) * multiplier).ToString("F1") + "%"
                + "\nHealth +" + (GearStatCalculator.GetCapeHealthPercent(cape, record.level) * multiplier).ToString("F1") + "%";
            ShowItemInfo(cape.icon, cape.capeName, cape.rarity, record.level, mainStats, record.subStats);
            return;
        }

        LootResult result = LootService.BuildFromRecord(slot, record.equipId, record);
        if (result == null) return;

        UIGearInfo ui = UIManager.Instance.LoadUI(UIKey.InfoGear) as UIGearInfo;
        if (ui != null) ui.Show(result, GetRelicTier(slot));
    }

    private static void ShowItemInfo(Sprite icon, string itemName, Rarity rarity, int level, string mainStats, List<GearSubStat> subStats)
    {
        UIGearInfo ui = UIManager.Instance.LoadUI(UIKey.InfoGear) as UIGearInfo;
        if (ui != null) ui.Show(icon, rarity, level, itemName, mainStats, subStats);
    }

    private void OnClickCompanion(ElementPlayerItemUI element)
    {
        BossRushCompanionModel model = shownCompanions[listCompanion.IndexOf(element)];
        CompanionData data = GameData.staticData.companions.GetData(model.CompanionId);
        UIPetInfo ui = UIManager.Instance.LoadUI(UIKey.PetInfo) as UIPetInfo;
        if (ui != null) ui.ShowOther(data, model.CompanionLevel);
    }

    private static BossRushItemModel FindItem(List<BossRushItemModel> items, int slot)
    {
        for (int i = 0; items != null && i < items.Count; i++)
        {
            if (items[i] != null && items[i].Slot == slot) return items[i];
        }
        return null;
    }

    // Vũ khí/cánh/áo choàng là module riêng; 5 slot còn lại nằm chung catalog gear. Id cánh/áo choàng là số.
    private static Sprite GetItemIcon(GearSlotType slot, string itemId)
    {
        StaticGameData data = GameData.staticData;
        switch (slot)
        {
            case GearSlotType.WEAPON:
                WeaponData weapon = data.weapons.GetData(itemId);
                return weapon != null ? weapon.icon : null;
            case GearSlotType.WING:
                WingData wing = int.TryParse(itemId, out int wingId) ? data.wings.GetData(wingId) : null;
                return wing != null ? wing.icon : null;
            case GearSlotType.CAPE:
                CapeData cape = int.TryParse(itemId, out int capeId) ? data.capes.GetData(capeId) : null;
                return cape != null ? cape.icon : null;
            default:
                GearItemData gear = data.gears.GetData(itemId);
                return gear != null ? gear.icon : null;
        }
    }

    // Cộng dồn chỉ số phụ của mọi món theo loại, mỗi loại 1 dòng theo thứ tự enum SubStatType.
    private static string GetSubStatsText(List<BossRushItemModel> items)
    {
        SortedDictionary<SubStatType, float> totals = new SortedDictionary<SubStatType, float>();
        for (int i = 0; items != null && i < items.Count; i++)
        {
            List<SubStatEntry> subStats = items[i] != null ? items[i].SubStats : null;
            for (int s = 0; subStats != null && s < subStats.Count; s++)
            {
                totals.TryGetValue(subStats[s].Type, out float total);
                totals[subStats[s].Type] = total + subStats[s].Value;
            }
        }

        List<GearSubStat> lines = new List<GearSubStat>();
        foreach (KeyValuePair<SubStatType, float> pair in totals)
        {
            lines.Add(new GearSubStat(pair.Key, pair.Value));
        }
        return UITabWing.GetSubStatsText(lines);
    }
}

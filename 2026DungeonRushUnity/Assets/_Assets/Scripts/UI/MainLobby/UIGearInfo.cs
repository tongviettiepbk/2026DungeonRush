using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGearInfo : BaseUI
{
    [Space(20)]
    public Button btClose;
    public Image imgBgRarity;
    public Image imgIcon;

    [Space(20)]
    public TMP_Text txtTypeRarity;
    public TMP_Text txtNameGear;
    public TMP_Text txtMainStats;
    public List<TMP_Text> listTxtSubStats;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
    }

    // enchantmentTier: relic đang đeo ở slot của món (ItemInfoPopup.EnchantmentTier gốc, chỉ khi mở từ ô
    // đang mặc) → main stat hiển thị × (1 + tier²/100) như ItemInfoPopup.igz. 0 = không có relic.
    public void Show(LootResult result, int enchantmentTier = 0)
    {
        string typeLabel = result.kind == LootItemKind.Weapon
            ? "Vũ khí (" + (result.weaponType == WeaponType.Melee ? "Cận chiến" : "Bắn xa") + ")"
            : UIMainLobby.SlotName(result.gearSlot);

        string mainLabel = result.mainStatKind == GearMainStatKind.Health ? "Máu" : "Sát thương";
        string mainStats = mainLabel + ": " + (result.mainStat * EnchantmentConfig.GetStatMultiplier(enchantmentTier)).ToString("0");   // game gốc hiện main stat làm tròn nguyên

        DebugCustom.ShowLog("subStats:", JsonConvert.SerializeObject(result.subStats));

        Show(result.icon, result.displayName, result.rarity + " - " + typeLabel, mainStats, result.subStats);
    }

    // Đổ thẳng chữ đã dựng sẵn — cho món không đi qua LootResult (Wing/Cape có 2 chỉ số chính).
    public void Show(Sprite icon, string itemName, string typeRarity, string mainStats, List<GearSubStat> subStats)
    {
        if (imgIcon != null)
            imgIcon.sprite = icon;

        if (txtNameGear != null)
            txtNameGear.text = itemName;

        if (txtTypeRarity != null)
            txtTypeRarity.text = typeRarity;

        if (txtMainStats != null)
            txtMainStats.text = mainStats;

        UIMainLobby.FillSubStats(subStats, listTxtSubStats);

        gameObject.SetActive(true);
    }
}

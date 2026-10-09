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
        if (imgIcon != null)
            imgIcon.sprite = result.icon;

        string typeLabel = result.kind == LootItemKind.Weapon
            ? "Vũ khí (" + (result.weaponType == WeaponType.Melee ? "Cận chiến" : "Bắn xa") + ")"
            : UIMainLobby.SlotName(result.gearSlot);

        if (txtNameGear != null)
            txtNameGear.text = result.displayName;

        if (txtTypeRarity != null)
            txtTypeRarity.text = result.rarity + " - " + typeLabel;

        string mainLabel = result.mainStatKind == GearMainStatKind.Health ? "Máu" : "Sát thương";
        if (txtMainStats != null)
            txtMainStats.text = mainLabel + ": " + (result.mainStat * EnchantmentConfig.GetStatMultiplier(enchantmentTier)).ToString("0");   // game gốc hiện main stat làm tròn nguyên

        DebugCustom.ShowLog("subStats:", JsonConvert.SerializeObject(result.subStats));

        UIMainLobby.FillSubStats(result.subStats, listTxtSubStats);

        gameObject.SetActive(true);
    }
}

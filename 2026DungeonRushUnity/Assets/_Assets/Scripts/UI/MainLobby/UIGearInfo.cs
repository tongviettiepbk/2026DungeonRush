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

    public void Show(LootResult result)
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
            txtMainStats.text = mainLabel + ": " + result.mainStat.ToString("0.##");

        DebugCustom.ShowLog("subStats:", JsonConvert.SerializeObject(result.subStats));

        UIMainLobby.FillSubStats(result.subStats, listTxtSubStats);

        gameObject.SetActive(true);
    }
}

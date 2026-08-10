using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILootGearInfo : BaseUI
{
    [Space(20)]
    public GameObject objEquip;
    public Image imgBgRarityEquip;
    public Image imgIconEquip;
    public TMP_Text txtTypeRarityEquip;
    public TMP_Text txtNameGearEquip;
    public TMP_Text txtMainStatsEquip;
    public List<TMP_Text> listTxtSubStatsEquip;

    [Space(20)]
    public GameObject objNew;
    public Image imgBgRarityNew;
    public Image imgIconNew;
    public TMP_Text txtTypeRarityNew;
    public TMP_Text txtNameGearNew;
    public TMP_Text txtMainStatsNew;
    public List<TMP_Text> listTxtSubStatsNew;

    [Space(20)]
    public Button btEquip;
    public Button btSell;

    private Action onEquip;
    private Action onSell;

    protected override void Awake()
    {
        base.Awake();
        btEquip.onClick.AddListener(OnClickEquip);
        btSell.onClick.AddListener(OnClickSell);
    }

    // newResult luôn hiển thị ở objNew. oldResult (đồ đang mặc, lấy từ ElementEquipmentUILobby)
    // null khi slot chưa có thông tin -> ẩn objEquip. Chọn Equip mới gọi onEquip (save + mặc đồ),
    // chọn Sell chỉ đóng popup, giữ nguyên đồ cũ.
    public void Show(LootResult newResult, LootResult oldResult, Action onEquip, Action onSell)
    {
        this.onEquip = onEquip;
        this.onSell = onSell;

        FillLayout(newResult, imgIconNew, txtTypeRarityNew, txtNameGearNew, txtMainStatsNew, listTxtSubStatsNew);

        bool hasOld = oldResult != null;
        if (objEquip != null)
            objEquip.SetActive(hasOld);
        if (hasOld)
            FillLayout(oldResult, imgIconEquip, txtTypeRarityEquip, txtNameGearEquip, txtMainStatsEquip, listTxtSubStatsEquip);

        gameObject.SetActive(true);
    }

    private void OnClickEquip()
    {
        onEquip?.Invoke();
        Close();
    }

    private void OnClickSell()
    {
        onSell?.Invoke();
        Close();
    }

    private static void FillLayout(LootResult result, Image imgIcon, TMP_Text txtTypeRarity, TMP_Text txtNameGear, TMP_Text txtMainStats, List<TMP_Text> listTxtSubStats)
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

        List<GearSubStat> subStats = result.subStats;
        for (int i = 0; i < listTxtSubStats.Count; i++)
        {
            if (listTxtSubStats[i] == null)
                continue;

            bool hasSubStat = i < subStats.Count;
            listTxtSubStats[i].gameObject.SetActive(hasSubStat);
            if (hasSubStat)
                listTxtSubStats[i].text = UIMainLobby.SubStatName(subStats[i].type) + ": +" + subStats[i].value.ToString("0.##") + "%";
        }
    }
}

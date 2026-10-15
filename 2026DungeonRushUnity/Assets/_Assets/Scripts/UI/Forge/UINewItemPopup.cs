using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup món loại MỚI — slot đó chưa mặc gì (NewItemPopup gốc, 1 thẻ). Nhiệm vụ duy nhất: bấm Equip
// để mặc món vào hero. Slot đã có đồ thì dùng UINewForgePopup (so sánh, mặc hoặc bán).
public class UINewItemPopup : BaseUI
{
    public ItemElementView itemElement;
    public TMP_Text txtName;
    public TMP_Text txtRarity;
    public TMP_Text txtBaseStat;
    public TMP_Text txtSubStats;
    public Button btEquip;

    private Action onEquip;
    private bool isHandled;

    protected override void Awake()
    {
        base.Awake();
        btEquip.onClick.AddListener(Close);
    }

    public void Show(LootResult item, Action onEquip)
    {
        this.onEquip = onEquip;
        isHandled = false;

        itemElement.SetItem(item);
        txtName.text = ItemTextUtil.Name(item);
        txtRarity.text = ItemTextUtil.RarityLine(item.rarity);
        txtBaseStat.text = ItemTextUtil.MainStat(item);

        string subStats = ItemTextUtil.SubStats(item.subStats);
        txtSubStats.gameObject.SetActive(subStats.Length > 0);
        txtSubStats.text = subStats;
    }

    // Không có lựa chọn bỏ: nút Back không đóng được.
    public override bool IsBackable()
    {
        return false;
    }

    // Mọi đường đóng (nút Equip, UIManager đóng hộ) đều mặc món → onEquip đúng 1 lần.
    public override void Close()
    {
        bool isFirst = isHandled == false;
        isHandled = true;
        base.Close();
        if (isFirst)
            onEquip?.Invoke();
    }
}

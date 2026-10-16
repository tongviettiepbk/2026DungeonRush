using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup món loại MỚI — slot đó chưa mặc gì (NewItemPopup gốc, 1 thẻ). Nhiệm vụ duy nhất: bấm Equip
// để mặc món vào hero. Slot đã có đồ thì dùng UINewForgePopup (so sánh, mặc hoặc bán).
// Chạm nền tối (CloseOnClickDark gốc) = BỎ QUA: món quay lại bục, bấm món đó để mở lại.
public class UINewItemPopup : BaseUI
{
    public ItemElementView itemElement;
    public TMP_Text txtName;
    public TMP_Text txtRarity;
    public TMP_Text txtBaseStat;
    public TMP_Text txtSubStats;
    public Button btEquip;
    public Button btBackground;

    private Action onEquip;
    private Action onDismiss;
    private bool isHandled;

    protected override void Awake()
    {
        base.Awake();
        btEquip.onClick.AddListener(Close);
        if (btBackground != null)
            btBackground.onClick.AddListener(OnClickBackground);
    }

    public void Show(LootResult item, Action onEquip, Action onDismiss = null)
    {
        this.onEquip = onEquip;
        this.onDismiss = onDismiss;
        isHandled = false;

        itemElement.SetItem(item);
        txtName.text = ItemTextUtil.Name(item);
        txtRarity.text = ItemTextUtil.RarityLine(item.rarity);
        txtBaseStat.text = ItemTextUtil.MainStat(item);

        string subStats = ItemTextUtil.SubStats(item.subStats);
        txtSubStats.gameObject.SetActive(subStats.Length > 0);
        txtSubStats.text = subStats;
    }

    private void OnClickBackground()
    {
        if (onDismiss == null || isHandled)
            return;

        isHandled = true;
        base.Close();
        onDismiss();
    }

    // Nút Back không đóng được (bỏ qua chỉ bằng chạm nền).
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

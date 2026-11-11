using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup món TRÙNG loại với đồ đang mặc (NewForgePopup gốc): thẻ trái = đồ đang mặc, thẻ phải = món còn lại.
//  - Equip: mặc món ở thẻ phải → 2 thẻ đổi chỗ ngay trong popup (món cũ sang thẻ phải), vẫn chọn tiếp.
//  - Sell: bán món ở thẻ phải lấy EXP rồi đóng. Món đó rarity CAO hơn đồ đang mặc → hỏi xác nhận trước
//    (OnSellButtonClicked gốc so rarity, câu Popup.NewForge.SellConfirm).
//  - Chạm nền tối khi chưa chọn gì (CloseOnClickDark gốc) = BỎ QUA: món quay lại bục, bấm món đó để mở lại.
public class UINewForgePopup : BaseUI
{
    private static readonly Color STAT_UP_COLOR = new Color32(98, 211, 74, 255);     // #62D34A (kit concept)
    private static readonly Color STAT_DOWN_COLOR = new Color32(255, 90, 90, 255);   // #FF5A5A

    [Header("Đồ đang mặc")]
    public ItemElementView equippedElement;
    public ItemStatsView equippedStats;
    public GameObject objEquippedNew;    // nhãn "NEW" trên thẻ trái: món vừa được mặc trong popup này

    [Header("Món còn lại")]
    public ItemElementView newElement;
    public ItemStatsView newStats;
    public GameObject objNewNew;         // nhãn "NEW" trên thẻ phải: món vừa loot
    public Image imgCompareArrow;        // cạnh chỉ số chính thẻ phải: xanh lên = mạnh hơn đồ đang mặc, đỏ xuống = yếu hơn
    public Sprite sprStatUp;
    public Sprite sprStatDown;

    [Header("Nút")]
    public Button btEquip;
    public Button btSell;
    public GameObject objConfirm;
    public CanvasGroup confirmGroup;
    public TMP_Text txtConfirm;
    public Button btConfirmYes;
    public Button btConfirmNo;
    public Button btBackground;

    private LootResult equipped;
    private LootResult other;
    private LootResult looted;
    private Action<LootResult> onEquip;
    private Action<LootResult> onSell;
    private Action onDismiss;
    private bool isHandled;
    private bool hasEquipped;

    protected override void Awake()
    {
        base.Awake();
        btEquip.onClick.AddListener(OnClickEquip);
        btSell.onClick.AddListener(OnClickSell);
        btConfirmYes.onClick.AddListener(SellAndClose);
        btConfirmNo.onClick.AddListener(() => SetConfirmVisible(false));
        if (btBackground != null)
            btBackground.onClick.AddListener(OnClickBackground);
    }

    // onEquip(món) = mặc món đó (lưu save + cập nhật hero); onSell(món) = bán món đó, gọi lúc popup đóng;
    // onDismiss = chạm nền khi chưa chọn gì (null thì chạm nền không làm gì).
    public void Show(LootResult equipped, LootResult looted, Action<LootResult> onEquip, Action<LootResult> onSell, Action onDismiss = null)
    {
        this.onDismiss = onDismiss;
        hasEquipped = false;
        this.equipped = equipped;
        this.other = looted;
        this.looted = looted;
        this.onEquip = onEquip;
        this.onSell = onSell;
        isHandled = false;
        SetConfirmVisible(false);
        Refresh();
    }

    private void Refresh()
    {
        equippedElement.SetItem(equipped);
        equippedStats.Set(equipped);
        newElement.SetItem(other);
        newStats.Set(other);
        objEquippedNew.SetActive(equipped == looted);
        objNewNew.SetActive(other == looted);
        RefreshCompareArrow();
    }

    // So chỉ số chính (cùng slot nên cùng loại Damage/Health): mạnh hơn = số xanh + mũi tên lên, yếu hơn = số đỏ +
    // mũi tên xuống, bằng nhau thì ẩn. Mũi tên đặt ngay sau số (mũi tên là con của ô giá trị).
    private void RefreshCompareArrow()
    {
        bool isEqual = other.mainStat == equipped.mainStat;
        imgCompareArrow.gameObject.SetActive(isEqual == false);
        if (isEqual)
            return;

        bool isUp = other.mainStat > equipped.mainStat;
        imgCompareArrow.sprite = isUp ? sprStatUp : sprStatDown;
        TMP_Text value = newStats.txtMainValues;
        value.color = isUp ? STAT_UP_COLOR : STAT_DOWN_COLOR;
        value.ForceMeshUpdate(true);
        Bounds bounds = value.textBounds;
        imgCompareArrow.rectTransform.anchoredPosition = new Vector2(bounds.max.x + 22f, bounds.center.y);
    }

    // ihn gốc: mặc món thẻ phải rồi đổi chỗ 2 thẻ.
    private void OnClickEquip()
    {
        AudioManager.Instance.PlayItemEquip();   // kaf gốc (AudioConfig.ItemEquipSound gốc để trống)
        onEquip?.Invoke(other);
        hasEquipped = true;
        LootResult temp = equipped;
        equipped = other;
        other = temp;
        Refresh();
    }

    private void OnClickSell()
    {
        if (other.rarity > equipped.rarity)
        {
            txtConfirm.text = Loc.L("Popup.NewForge.SellConfirm");
            SetConfirmVisible(true);
            return;
        }

        SellAndClose();
    }

    // kai gốc: bán (Sell trực tiếp hoặc xác nhận Yes) kêu ItemSellSound rồi đóng.
    private void SellAndClose()
    {
        AudioManager.Instance.PlayItemSell();
        Close();
    }

    private void SetConfirmVisible(bool visible)
    {
        objConfirm.SetActive(visible);
        confirmGroup.alpha = visible ? 1f : 0f;
        confirmGroup.interactable = visible;
        confirmGroup.blocksRaycasts = visible;
    }

    // Đang hỏi xác nhận bán → chạm nền chỉ tắt hộp xác nhận. Đã Equip trong popup → chạm nền = đóng (bán món còn lại).
    private void OnClickBackground()
    {
        if (objConfirm.activeSelf)
        {
            SetConfirmVisible(false);
            return;
        }

        if (hasEquipped)
        {
            Close();
            return;
        }

        if (onDismiss == null || isHandled)
            return;

        isHandled = true;
        base.Close();
        onDismiss();
    }

    // Phải chọn Equip hoặc Sell: nút Back không đóng được.
    public override bool IsBackable()
    {
        return false;
    }

    // Đóng = bán món thẻ phải (Sell, xác nhận Yes, hoặc UIManager đóng hộ) → onSell đúng 1 lần.
    public override void Close()
    {
        bool isFirst = isHandled == false;
        isHandled = true;
        base.Close();
        if (isFirst)
            onSell?.Invoke(other);
    }
}

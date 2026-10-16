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
    private const string SELL_CONFIRM = "The item you are selling is stronger than your character are you sure?";

    [Header("Đồ đang mặc")]
    public ItemElementView equippedElement;
    public TMP_Text txtEquippedName;
    public TMP_Text txtEquippedRarity;
    public TMP_Text txtEquippedBaseStat;
    public TMP_Text txtEquippedSubStats;
    public GameObject objEquippedNew;    // nhãn "NEW" trên thẻ trái: món vừa được mặc trong popup này

    [Header("Món còn lại")]
    public ItemElementView newElement;
    public TMP_Text txtNewName;
    public TMP_Text txtNewRarity;
    public TMP_Text txtNewBaseStat;
    public TMP_Text txtNewSubStats;
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
        btConfirmYes.onClick.AddListener(Close);
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
        Fill(equipped, equippedElement, txtEquippedName, txtEquippedRarity, txtEquippedBaseStat, txtEquippedSubStats);
        Fill(other, newElement, txtNewName, txtNewRarity, txtNewBaseStat, txtNewSubStats);
        objEquippedNew.SetActive(equipped == looted);
        objNewNew.SetActive(other == looted);
        RefreshCompareArrow();
    }

    // So chỉ số chính (cùng slot nên cùng loại Damage/Health); bằng nhau thì ẩn. Mũi tên đặt ngay sau chữ.
    private void RefreshCompareArrow()
    {
        bool isEqual = other.mainStat == equipped.mainStat;
        imgCompareArrow.gameObject.SetActive(isEqual == false);
        if (isEqual)
            return;

        imgCompareArrow.sprite = other.mainStat > equipped.mainStat ? sprStatUp : sprStatDown;
        txtNewBaseStat.ForceMeshUpdate(true);
        Bounds bounds = txtNewBaseStat.textBounds;
        imgCompareArrow.rectTransform.anchoredPosition = new Vector2(bounds.max.x + 10f, bounds.center.y);
    }

    private static void Fill(LootResult item, ItemElementView element, TMP_Text name, TMP_Text rarity, TMP_Text baseStat, TMP_Text subStats)
    {
        element.SetItem(item);
        name.text = ItemTextUtil.Name(item);
        rarity.text = ItemTextUtil.RarityLine(item.rarity);
        baseStat.text = ItemTextUtil.MainStat(item);
        subStats.text = ItemTextUtil.SubStats(item.subStats);
    }

    // ihn gốc: mặc món thẻ phải rồi đổi chỗ 2 thẻ.
    private void OnClickEquip()
    {
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
            txtConfirm.text = SELL_CONFIRM;
            SetConfirmVisible(true);
            return;
        }

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

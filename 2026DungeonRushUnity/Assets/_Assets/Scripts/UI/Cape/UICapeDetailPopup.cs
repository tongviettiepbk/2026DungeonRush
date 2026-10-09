using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup chi tiết 1 cape (CapeDetailPopup gốc) — mở từ ô kho UICapePopup.
// Tên + [Rarity] + "+X% Damage/Health" + substat; Equip/Unequip (đóng popup, toast Power); Upgrade → UICapeSalvagePopup.
public class UICapeDetailPopup : BaseUI
{
    public CapeElementUI elementCape;
    public TMP_Text txtName;
    public TMP_Text txtRarity;
    public TMP_Text txtDamage;
    public TMP_Text txtHealth;
    public TMP_Text txtSubStats;
    public Button btClose;
    public Button btUpgrade;
    public Button btEquip;
    public Image imgEquip;
    public TMP_Text txtEquip;

    private CapeModel model;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btUpgrade.onClick.AddListener(OnClickUpgrade);
        btEquip.onClick.AddListener(OnClickEquip);
    }

    public void Show(CapeModel model)
    {
        this.model = model;
        Refresh();
        gameObject.SetActive(true);
    }

    // erq: err/ers/ert/eru/erv
    private void Refresh()
    {
        CapeData data = CapeService.GetData(model);
        bool isEquipped = CapeService.IsEquipped(model);
        elementCape.SetData(data, model, isEquipped);

        string color = ColorUtility.ToHtmlStringRGB(UITabWing.GetRarityColor(data.rarity));
        txtName.text = "<color=#" + color + ">" + data.capeName + "</color>";
        txtRarity.text = "<color=#" + color + ">[" + data.rarity + "]</color>";
        txtDamage.text = UICapePopup.GetDamageText(data, model.level);
        txtHealth.text = UICapePopup.GetHealthText(data, model.level);

        txtSubStats.gameObject.SetActive(model.subStats.Count > 0);
        txtSubStats.text = UITabWing.GetSubStatsText(model.subStats);

        txtEquip.text = isEquipped ? "Unequip" : "Equip";
        imgEquip.color = isEquipped ? UITabWing.DANGER_BUTTON_COLOR : UITabWing.DEFAULT_BUTTON_COLOR;
    }

    // erx: đang mặc → cởi, không thì mặc. Hero mặc lại slot CAPE + toast Power rồi đóng popup.
    private void OnClickEquip()
    {
        double powerBefore = PlayerPower.GetCurrent();
        if (CapeService.IsEquipped(model))
        {
            CapeService.Unequip();
        }
        else
        {
            CapeService.Equip(model);
        }

        this.PostEvent(EventID.EquipmentChanged, GearSlotType.CAPE);
        PlayerPower.NotifyChange(powerBefore);
        this.PostEvent(EventID.CapeChanged);
        Close();
    }

    // ery: mở Salvage cho cape này; xong thì cập nhật lại popup.
    private void OnClickUpgrade()
    {
        UICapeSalvagePopup ui = UIManager.Instance.LoadUI(UIKey.CapeSalvagePopup) as UICapeSalvagePopup;
        if (ui != null)
        {
            ui.Show(model, Refresh);
        }
    }
}

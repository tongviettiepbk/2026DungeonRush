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
    public TMP_Text txtStatLabels;   // cột nhãn nâu Damage/Health (Batch4Reskin tạo); null = chữ gộp như cũ
    public TMP_Text txtSubValues;    // cột số substat bên phải (Batch4Reskin tạo)
    public Button btClose;
    public Button btUpgrade;
    public Button btEquip;
    public Image imgEquip;
    // Reskin: chưa mặc = sprite xanh (Equip), đang mặc = sprite đỏ (Unequip).
    public Sprite sprEquip;
    public Sprite sprUnequip;
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
        txtName.text = "<color=#" + color + ">" + Loc.Or(data.localizationKey, data.capeName) + "</color>";
        txtRarity.text = "<color=#" + color + ">[" + Loc.Rarity(data.rarity) + "]</color>";
        if (txtStatLabels != null)
        {
            txtDamage.text = UICapePopup.GetDamageValue(data, model.level);
            txtHealth.text = UICapePopup.GetHealthValue(data, model.level);
            UICapePopup.SetStatLabels(txtStatLabels, txtDamage, txtHealth);
        }
        else
        {
            txtDamage.text = UICapePopup.GetDamageText(data, model.level);
            txtHealth.text = UICapePopup.GetHealthText(data, model.level);
        }

        if (txtSubValues != null)
        {
            // Ô substat cao cố định 2 dòng (tối đa của Cape) — luôn hiện, chỉ đổi chữ.
            txtSubStats.text = ItemTextUtil.SubStatNames(model.subStats);
            txtSubValues.text = ItemTextUtil.SubStatValues(model.subStats);
        }
        else
        {
            txtSubStats.gameObject.SetActive(model.subStats.Count > 0);
            txtSubStats.text = UITabWing.GetSubStatsText(model.subStats);
        }

        txtEquip.text = isEquipped ? Loc.L("Common.Unequip") : Loc.L("Common.Equip");
        UITabWing.SetButtonState(imgEquip, !isEquipped, sprEquip, sprUnequip);
        if (sprEquip == null || sprUnequip == null)
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

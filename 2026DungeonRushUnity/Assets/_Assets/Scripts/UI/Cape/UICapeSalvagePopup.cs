using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup nâng cấp cape bằng cách gộp cape khác (CapeSalvagePopup gốc) — mở từ nút Upgrade của UICapeDetailPopup.
// Chọn cape trong kho làm nguyên liệu (cape đang mặc bị khoá) → xem trước level/XP/chỉ số (+xanh) → Upgrade:
// các cape được chọn biến mất, XP = SalvageBaseXP + XP đã lên của từng cape (CapeConfig.eqm) cộng cho cape này.
public class UICapeSalvagePopup : BaseUI
{
    public CapeElementUI elementCape;
    public TMP_Text txtName;
    public TMP_Text txtDamage;
    public TMP_Text txtHealth;
    public RectTransform rectXPFill;
    public TMP_Text txtXP;
    public Transform transContent;
    public GameObject objPrefabElement;
    public Button btUpgrade;
    public Image imgUpgrade;
    public Button btClose;

    private readonly List<CapeElementUI> listElement = new List<CapeElementUI>();
    private readonly List<string> selected = new List<string>();
    private CapeModel model;
    private Action onChanged;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btUpgrade.onClick.AddListener(OnClickUpgrade);
    }

    public void Show(CapeModel model, Action onChanged)
    {
        this.model = model;
        this.onChanged = onChanged;
        selected.Clear();
        RefreshInventory();
        RefreshPreview();
        gameObject.SetActive(true);
    }

    // euo: mọi cape khác cape đang nâng. Cape đang mặc hiện nhưng không chọn được.
    // Sắp xếp rarity thấp → cao, level thấp → cao (nguyên liệu rẻ lên đầu).
    private void RefreshInventory()
    {
        List<CapeModel> list = new List<CapeModel>();
        List<CapeModel> owned = GameData.userData.capes.owned;
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i] != model && CapeService.GetData(owned[i]) != null)
            {
                list.Add(owned[i]);
            }
        }
        list.Sort((a, b) =>
        {
            int rarity = CapeService.GetData(a).rarity.CompareTo(CapeService.GetData(b).rarity);
            return rarity != 0 ? rarity : a.level.CompareTo(b.level);
        });

        for (int i = 0; i < list.Count; i++)
        {
            if (i >= listElement.Count)
            {
                CapeElementUI created = Instantiate(objPrefabElement, transContent).GetComponent<CapeElementUI>();
                created.Init(OnClickElement);
                listElement.Add(created);
            }

            CapeElementUI element = listElement[i];
            bool isEquipped = CapeService.IsEquipped(list[i]);
            element.gameObject.SetActive(true);
            element.SetData(CapeService.GetData(list[i]), list[i], isEquipped);
            element.SetInteractable(isEquipped == false);
        }

        for (int i = list.Count; i < listElement.Count; i++)
        {
            listElement[i].gameObject.SetActive(false);
        }
    }

    // eup: bật/tắt chọn.
    private void OnClickElement(CapeElementUI element)
    {
        string id = element.Model.instanceId;
        bool isChecked = selected.Contains(id) == false;
        if (isChecked)
        {
            selected.Add(id);
        }
        else
        {
            selected.Remove(id);
        }

        element.SetChecked(isChecked);
        RefreshPreview();
    }

    // euq/eus/eut/euv/eux: level + chỉ số + thanh XP sau khi gộp các cape đang chọn.
    private void RefreshPreview()
    {
        CapeData data = CapeService.GetData(model);
        bool isEquipped = CapeService.IsEquipped(model);
        elementCape.SetData(data, model, isEquipped);
        txtName.text = UICapePopup.GetCapeName(data);

        int addXP = CapeService.GetSalvageXP(selected);
        CapeService.SimulateXP(data.rarity, model.level, model.currentXP, addXP, out int level, out int xp);
        int gained = level - model.level;

        elementCape.txtLevel.text = "Lvl " + model.level + (gained > 0 ? " <color=green>(+" + gained + ")</color>" : string.Empty);
        txtDamage.text = UICapePopup.GetDamageText(data, model.level) + GetGainText(GearStatCalculator.GetCapeDamagePercent(data, level) - GearStatCalculator.GetCapeDamagePercent(data, model.level));
        txtHealth.text = UICapePopup.GetHealthText(data, model.level) + GetGainText(GearStatCalculator.GetCapeHealthPercent(data, level) - GearStatCalculator.GetCapeHealthPercent(data, model.level));

        if (level >= CapeService.MaxLevel)
        {
            txtXP.text = "MAX";
            rectXPFill.localScale = Vector3.one;
        }
        else
        {
            int need = CapeService.GetLevelUpXP(data.rarity, level);
            txtXP.text = xp + "/" + need + " xp";
            rectXPFill.localScale = new Vector3(need > 0 ? Mathf.Clamp01((float)xp / need) : 0f, 1f, 1f);
        }

        imgUpgrade.color = selected.Count > 0 ? UITabWing.DEFAULT_BUTTON_COLOR : UITabWing.DISABLED_BUTTON_COLOR;
    }

    private static string GetGainText(float gain)
    {
        return gain > 0f ? " <color=green>(+" + gain.ToString("F1") + "%)</color>" : string.Empty;
    }

    // euy
    private void OnClickUpgrade()
    {
        if (selected.Count == 0)
        {
            return;
        }

        bool isEquipped = CapeService.IsEquipped(model);
        double powerBefore = PlayerPower.GetCurrent();
        CapeService.Salvage(model, new List<string>(selected));
        selected.Clear();

        if (isEquipped)
        {
            this.PostEvent(EventID.EquipmentChanged, GearSlotType.CAPE);
            PlayerPower.NotifyChange(powerBefore);
        }
        this.PostEvent(EventID.CapeChanged);

        RefreshInventory();
        RefreshPreview();
        onChanged?.Invoke();
    }
}

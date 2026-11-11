using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup Cape (CapePopup gốc) — mở từ ô Cape ở lobby (mở khoá PlayerLevel ≥ 15).
// Cloak + Summon Level/tiến độ + nút Summon (x multiplier, chỉ hiện khi Cloak > 99) + kho cape + Show Cloak.
// Summon 1 → ClaimPage "New Cloak Obtained!"; nhiều → bảng kết quả (bấm để đóng). Bấm ô kho → UICapeDetailPopup.
// Logic ở CapeService / CapeSummonConfig (reverse il2cpp v41, xem DecodedData/CAPE_MODEL.md).
public class UICapePopup : BaseUI
{
    public TMP_Text txtCloak;
    public Button btClose;
    public Button btInfo;

    [Header("Summon")]
    public Button btSummon;
    public Image imgSummon;
    // Reskin: nút đủ tiền = sprite xanh, thiếu = sprite xám (thay cho tô DEFAULT/DISABLED_BUTTON_COLOR).
    public Sprite sprButtonOn;
    public Sprite sprButtonOff;
    public TMP_Text txtSummon;
    public TMP_Text txtSummonLevel;
    public TMP_Text txtSummonProgress;
    public Image imgSummonFill;           // Filled ngang (kit Companion), fillAmount = tiến độ
    public GameObject objSummonNotification;
    public Button btMultiplier;
    public TMP_Text txtMultiplier;

    [Header("Kho")]
    public Transform transInventory;
    public GameObject objPrefabElement;

    [Header("Show Cloak")]
    public GameObject objShowCloakRoot;
    public Button btShowCloak;
    public Image imgShowCloak;
    public TMP_Text txtShowCloak;
    // Công tắc kit Setting (Batch4Reskin gán): núm đổi sprite + chạy trái/phải, vệt xanh hiện khi bật. null = tô màu như cũ.
    public Sprite sprKnobOn;
    public Sprite sprKnobOff;
    public GameObject objShowCloakFill;
    public float knobOffset = 40f;

    [Header("Kết quả summon nhiều")]
    public GameObject objSummonPanel;        // bấm bất kỳ đâu trên panel để đóng (TapToCloseExtension gốc)
    public Transform transSummonContent;

    [Header("Claim (New Cloak Obtained)")]
    public GameObject objClaimPage;
    public Image imgClaimIcon;
    public Image imgClaimGlow;
    public TMP_Text txtClaimName;
    public Transform transClaimStats;
    public Button btClaim;

    private readonly List<CapeElementUI> listElement = new List<CapeElementUI>();
    private readonly List<CapeElementUI> listSummonElement = new List<CapeElementUI>();
    private readonly List<TMP_Text> listClaimStat = new List<TMP_Text>();
    private int multiplierIndex;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btInfo.onClick.AddListener(OnClickInfo);
        btSummon.onClick.AddListener(OnClickSummon);
        btMultiplier.onClick.AddListener(OnClickMultiplier);
        btShowCloak.onClick.AddListener(OnClickShowCloak);
        btClaim.onClick.AddListener(() => objClaimPage.SetActive(false));

        Button panelButton = objSummonPanel.GetComponent<Button>();
        if (panelButton == null)
        {
            panelButton = objSummonPanel.AddComponent<Button>();
            panelButton.transition = Selectable.Transition.None;
        }
        panelButton.onClick.AddListener(() => objSummonPanel.SetActive(false));

        if (objSummonNotification != null)
        {
            objSummonNotification.SetActive(false);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventDispatcher.Instance.RegisterListener(EventID.CapeChanged, OnCapeChanged);
    }

    private void OnDisable()
    {
        EventDispatcher.Instance.RemoveListener(EventID.CapeChanged, OnCapeChanged);
    }

    public void Show()
    {
        objSummonPanel.SetActive(false);
        objClaimPage.SetActive(false);
        gameObject.SetActive(true);
        Refresh();
    }

    private void OnCapeChanged(object param)
    {
        Refresh();
    }

    // esx
    public void Refresh()
    {
        int cloak = CapeService.GetCloak();
        txtCloak.text = "<sprite=0>" + cloak;   // sprite asset IsCrown = icon Cloak
        RefreshMultiplier(cloak);
        RefreshSummonLevel();
        RefreshSummonButton(cloak);
        RefreshInventory();
        RefreshShowCloak();
    }

    // esy/etc: nút multiplier chỉ hiện khi Cloak > 99; dưới mức này về x1.
    private void RefreshMultiplier(int cloak)
    {
        bool canMultiply = cloak >= CapeSummonConfig.MULTIPLIER_UNLOCK_CLOAK;
        btMultiplier.gameObject.SetActive(canMultiply);
        if (canMultiply == false)
        {
            multiplierIndex = 0;
        }
        txtMultiplier.text = "x" + CapeSummonConfig.MULTIPLIERS[multiplierIndex];
    }

    // eta: "Lvl N" + "cur/req" + thanh (fillAmount), max → "Max Level".
    private void RefreshSummonLevel()
    {
        int total = GameData.userData.capes.totalSummons;
        txtSummonLevel.text = Loc.L("Common.Level.Abbrev", CapeSummonConfig.GetLevel(total));
        CapeSummonConfig.GetProgress(total, out int current, out int required, out bool isMax);
        if (isMax)
        {
            txtSummonProgress.text = "Max Level";
            imgSummonFill.fillAmount = 1f;
            return;
        }

        txtSummonProgress.text = current + "/" + required;
        imgSummonFill.fillAmount = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;
    }

    // etb: "<giá>\nSummon x<lượt>", giá đỏ khi thiếu Cloak; nút màu Default/Disabled.
    private void RefreshSummonButton(int cloak)
    {
        int multiplier = CapeSummonConfig.MULTIPLIERS[multiplierIndex];
        int cost = CapeService.GetSummonCost(multiplier);
        bool isEnough = cloak >= cost;
        string costText = "<sprite=0>" + (isEnough ? cost.ToString() : "<color=red>" + cost + "</color>");
        txtSummon.text = costText + "\nSummon x" + multiplier;
        UITabWing.SetButtonState(imgSummon, isEnough, sprButtonOn, sprButtonOff);
    }

    // ete: mỗi cape sở hữu 1 ô. Sắp xếp: đang mặc trước, rồi rarity cao → thấp, level cao → thấp.
    private void RefreshInventory()
    {
        List<CapeModel> owned = new List<CapeModel>(GameData.userData.capes.owned);
        CapeModel equipped = CapeService.GetEquipped();
        owned.Sort((a, b) =>
        {
            bool ea = a == equipped;
            bool eb = b == equipped;
            if (ea != eb)
            {
                return ea ? -1 : 1;
            }
            CapeData da = CapeService.GetData(a);
            CapeData db = CapeService.GetData(b);
            int rarity = (db != null ? (int)db.rarity : 0).CompareTo(da != null ? (int)da.rarity : 0);
            if (rarity != 0)
            {
                return rarity;
            }
            int level = b.level.CompareTo(a.level);
            return level != 0 ? level : a.capeId.CompareTo(b.capeId);
        });

        int index = 0;
        for (int i = 0; i < owned.Count; i++)
        {
            CapeData data = CapeService.GetData(owned[i]);
            if (data == null)
            {
                continue;
            }

            CapeElementUI element = GetElement(listElement, transInventory, index, OnClickElement);
            element.SetData(data, owned[i], owned[i] == equipped);
            index++;
        }

        for (int i = index; i < listElement.Count; i++)
        {
            listElement[i].gameObject.SetActive(false);
        }
    }

    private CapeElementUI GetElement(List<CapeElementUI> list, Transform parent, int index, System.Action<CapeElementUI> onClick)
    {
        if (index >= list.Count)
        {
            CapeElementUI element = Instantiate(objPrefabElement, parent).GetComponent<CapeElementUI>();
            element.Init(onClick);
            list.Add(element);
        }

        list[index].gameObject.SetActive(true);
        return list[index];
    }

    // ett: chỉ hiện khi có ít nhất 1 cape.
    private void RefreshShowCloak()
    {
        objShowCloakRoot.SetActive(GameData.userData.capes.owned.Count > 0);
        bool isShow = CapeService.IsShowCloak();
        txtShowCloak.text = isShow ? "ON" : "OFF";
        if (imgShowCloak != null && sprKnobOn != null && sprKnobOff != null)
        {
            imgShowCloak.sprite = isShow ? sprKnobOn : sprKnobOff;
            imgShowCloak.color = Color.white;
            RectTransform knob = imgShowCloak.rectTransform;
            knob.anchoredPosition = new Vector2(isShow ? knobOffset : -knobOffset, knob.anchoredPosition.y);
            if (objShowCloakFill != null)
            {
                objShowCloakFill.SetActive(isShow);
            }
        }
        else if (imgShowCloak != null)
        {
            imgShowCloak.color = isShow ? UITabWing.DEFAULT_BUTTON_COLOR : UITabWing.DISABLED_BUTTON_COLOR;
        }
    }

    // ----- Nút -----

    // etd
    private void OnClickMultiplier()
    {
        multiplierIndex = (multiplierIndex + 1) % CapeSummonConfig.MULTIPLIERS.Length;
        Refresh();
    }

    private void OnClickInfo()
    {
        UICapeUpgradeInfoPopup ui = UIManager.Instance.LoadUI(UIKey.CapeUpgradeInfoPopup) as UICapeUpgradeInfoPopup;
        if (ui != null)
        {
            ui.Show();
        }
    }

    // etg: thiếu Cloak → báo; đủ → summon. 1 cape → ClaimPage, nhiều → bảng kết quả.
    private void OnClickSummon()
    {
        int multiplier = CapeSummonConfig.MULTIPLIERS[multiplierIndex];
        List<CapeModel> results = CapeService.TrySummon(multiplier);
        if (results == null)
        {
            UIManager.Instance.ShowToastMessage("Không đủ Pipe", isLocalize: false);
            return;
        }

        this.PostEvent(EventID.CapeChanged);
        if (results.Count == 1)
        {
            ShowClaim(results[0]);
        }
        else if (results.Count > 1)
        {
            ShowSummonPanel(results);
        }
    }

    private void OnClickShowCloak()
    {
        CapeService.SetShowCloak(CapeService.IsShowCloak() == false);
        this.PostEvent(EventID.EquipmentChanged, GearSlotType.CAPE);
        this.PostEvent(EventID.CapeChanged);
    }

    // etq: bấm ô kho → popup chi tiết.
    private void OnClickElement(CapeElementUI element)
    {
        UICapeDetailPopup ui = UIManager.Instance.LoadUI(UIKey.CapeDetailPopup) as UICapeDetailPopup;
        if (ui != null)
        {
            ui.Show(element.Model);
        }
    }

    // ----- Kết quả summon -----

    // eth: bảng các cape vừa ra (nút ô bị tắt để click nổi lên panel → đóng).
    private void ShowSummonPanel(List<CapeModel> results)
    {
        CapeModel equipped = CapeService.GetEquipped();
        for (int i = 0; i < results.Count; i++)
        {
            CapeElementUI element = GetElement(listSummonElement, transSummonContent, i, null);
            element.SetData(CapeService.GetData(results[i]), results[i], results[i] == equipped);
            element.btElement.enabled = false;
        }
        for (int i = results.Count; i < listSummonElement.Count; i++)
        {
            listSummonElement[i].gameObject.SetActive(false);
        }

        objSummonPanel.SetActive(true);
    }

    // CapeClaimPage.Show: tên + "+X% Damage/Health" (lv1) + substat. Dòng chỉ số clone từ txtClaimName.
    private void ShowClaim(CapeModel model)
    {
        CapeData data = CapeService.GetData(model);
        imgClaimIcon.sprite = data.icon;
        if (imgClaimGlow != null)
        {
            imgClaimGlow.color = UITabWing.GetRarityColor(data.rarity);
        }
        txtClaimName.text = GetCapeName(data);

        List<string> lines = new List<string> { GetDamageText(data, model.level), GetHealthText(data, model.level) };
        for (int i = 0; i < model.subStats.Count; i++)
        {
            lines.Add(UITabWing.GetSubStatText(model.subStats[i]));
        }

        for (int i = 0; i < lines.Count; i++)
        {
            if (i >= listClaimStat.Count)
            {
                listClaimStat.Add(Instantiate(txtClaimName, transClaimStats));
            }
            listClaimStat[i].gameObject.SetActive(true);
            listClaimStat[i].text = lines[i];
        }
        for (int i = lines.Count; i < listClaimStat.Count; i++)
        {
            listClaimStat[i].gameObject.SetActive(false);
        }

        objClaimPage.SetActive(true);
        Transform icon = imgClaimIcon.transform;
        icon.DOKill();
        icon.localScale = Vector3.zero;
        icon.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
    }

    // ----- Helper dùng chung cho các UI Cape -----

    // "<color=#RRGGBB>[Rarity]</color> Tên"
    public static string GetCapeName(CapeData data)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(UITabWing.GetRarityColor(data.rarity)) + ">[" + Loc.Rarity(data.rarity) + "]</color> " + Loc.Or(data.localizationKey, data.capeName);
    }

    // "+X.X% Damage" (fu.eqx gốc)
    // Chỉ phần số "+55.2%" — dùng khi nhãn Damage/Health nằm ở cột riêng (Detail/Salvage, Batch4Reskin).
    public static string GetDamageValue(CapeData data, int level)
    {
        return "+" + GearStatCalculator.GetCapeDamagePercent(data, level).ToString("F1") + "%";
    }

    public static string GetHealthValue(CapeData data, int level)
    {
        return "+" + GearStatCalculator.GetCapeHealthPercent(data, level).ToString("F1") + "%";
    }

    // Nhãn "Damage\nHealth" (cột nâu) + đặt 2 ô số ngay sau nhãn dài nhất (nhãn đổi theo ngôn ngữ).
    public static void SetStatLabels(TMP_Text labels, TMP_Text damage, TMP_Text health)
    {
        labels.text = Loc.L("Items.SubStat.Damage") + "\n" + Loc.L("Items.SubStat.Health");
        float x = labels.rectTransform.anchoredPosition.x + labels.GetPreferredValues(labels.text).x + 14f;
        damage.rectTransform.anchoredPosition = new Vector2(x, damage.rectTransform.anchoredPosition.y);
        health.rectTransform.anchoredPosition = new Vector2(x, health.rectTransform.anchoredPosition.y);
    }

    public static string GetDamageText(CapeData data, int level)
    {
        return "+" + GearStatCalculator.GetCapeDamagePercent(data, level).ToString("F1") + "% Damage";
    }

    // "+X.X% Health" (fu.eqy gốc)
    public static string GetHealthText(CapeData data, int level)
    {
        return "+" + GearStatCalculator.GetCapeHealthPercent(data, level).ToString("F1") + "% Health";
    }
}

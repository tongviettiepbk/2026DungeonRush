using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup reroll substat wing (WingRerollPopup gốc) — mở từ nút Reroll trang Wing khi đã sở hữu wing.
// Mỗi substat 1 dòng có nút khoá; giá = RerollCosts[số dòng khoá] (quặng RerollOreType). Reroll: dòng không khoá
// roll lại loại mới (khác mọi loại đang có). Khoá hết → báo phải mở ít nhất 1 dòng.
public class UIWingRerollPopup : BaseUI
{
    public Image imgItemBackground;
    public Image imgItemIcon;
    public TMP_Text txtItemName;
    public TMP_Text txtItemLevel;
    public TMP_Text txtItemBaseStat;
    public GameObject objEquipped;
    public Transform transSubStatContent;
    public GameObject objPrefabSubStat;
    public Button btReroll;
    public Image imgReroll;
    public TMP_Text txtRerollResource;
    public Button btClose;

    private readonly List<ElementSubStatRerollUI> listElement = new List<ElementSubStatRerollUI>();
    private WingData wing;
    private int lockedCount;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btReroll.onClick.AddListener(OnClickReroll);
    }

    // eip/laa/lab: dựng lại các dòng substat (tất cả mở khoá) + info wing.
    public void Show(WingData wing, Sprite bgRarity)
    {
        this.wing = wing;
        imgItemBackground.sprite = bgRarity;
        WingModel model = WingService.GetOwned(wing);
        List<GearSubStat> subStats = model != null ? model.subStats : new List<GearSubStat>();

        for (int i = 0; i < subStats.Count; i++)
        {
            if (i >= listElement.Count)
            {
                GameObject obj = Instantiate(objPrefabSubStat, transSubStatContent);
                listElement.Add(obj.GetComponent<ElementSubStatRerollUI>());
            }
            listElement[i].gameObject.SetActive(true);
            listElement[i].Init(subStats[i], OnClickLock);
        }
        for (int i = subStats.Count; i < listElement.Count; i++)
        {
            listElement[i].gameObject.SetActive(false);
        }

        lockedCount = 0;
        imgItemIcon.sprite = wing.icon;
        txtItemName.text = UITabWing.GetWingName(wing);
        int level = model != null ? model.level : 1;
        txtItemLevel.text = "Lv " + level;
        txtItemBaseStat.text = UITabWing.GetBaseStatText(wing, level);
        if (objEquipped != null)
        {
            objEquipped.SetActive(WingService.IsEquipped(wing));
        }

        RefreshCost();
        gameObject.SetActive(true);
    }

    // lae: đếm lại số dòng khoá → giá mới.
    private void OnClickLock(ElementSubStatRerollUI element)
    {
        lockedCount = 0;
        for (int i = 0; i < listElement.Count; i++)
        {
            if (listElement[i].gameObject.activeSelf && listElement[i].IsLocked)
            {
                lockedCount++;
            }
        }
        RefreshCost();
    }

    // lac: "<quặng> có/cần" đỏ khi thiếu; nút xanh khi đủ quặng và còn ít nhất 1 dòng mở.
    private void RefreshCost()
    {
        WingModel model = WingService.GetOwned(wing);
        int count = model != null ? model.subStats.Count : 0;
        int ore = WingService.GetOre(wing.rerollOreType);
        int cost = WingService.GetRerollCost(wing, lockedCount);

        txtRerollResource.text = UITabWing.GetOreText(wing.rerollOreType, ore, cost);
        txtRerollResource.color = ore >= cost ? Color.white : Color.red;
        imgReroll.color = ore >= cost && lockedCount < count ? UITabWing.DEFAULT_BUTTON_COLOR : UITabWing.DISABLED_BUTTON_COLOR;
    }

    // laf
    private void OnClickReroll()
    {
        WingModel model = WingService.GetOwned(wing);
        if (model == null)
        {
            return;
        }

        if (lockedCount >= model.subStats.Count)
        {
            UIManager.Instance.ShowToastMessage("Phải mở khoá ít nhất 1 dòng chỉ số", isLocalize: false);
            return;
        }

        if (WingService.GetOre(wing.rerollOreType) < WingService.GetRerollCost(wing, lockedCount))
        {
            UIManager.Instance.ShowToastMessage("Không đủ tài nguyên", isLocalize: false);
            return;
        }

        List<bool> locked = new List<bool>();
        for (int i = 0; i < model.subStats.Count; i++)
        {
            locked.Add(i < listElement.Count && listElement[i].IsLocked);
        }

        bool isEquipped = WingService.IsEquipped(wing);
        double powerBefore = PlayerPower.GetCurrent();
        if (WingService.Reroll(wing, locked) == false)
        {
            return;
        }

        float delay = 0f;
        for (int i = 0; i < model.subStats.Count; i++)
        {
            if (locked[i])
            {
                continue;
            }
            listElement[i].SetData(model.subStats[i], delay);
            delay += 0.125f;
        }

        RefreshCost();
        if (isEquipped)
        {
            this.PostEvent(EventID.EquipmentChanged, GearSlotType.WING);
            PlayerPower.NotifyChange(powerBefore);
        }
        this.PostEvent(EventID.WingChanged);
    }
}

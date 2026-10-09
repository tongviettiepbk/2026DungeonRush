using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang Enchantment (EnchantmentTabPage gốc) — page trong UiMainGame, mở từ nút enchantment ở ô trang bị lobby.
// 8 slot đeo relic + kho relic + Summon (Vial) + Quick Equip / Merge All. Logic ở EnchantmentService;
// số liệu reverse xem DecodedData/ENCHANTMENT_MODEL.md.
// Luồng đeo (gốc): bấm relic trong kho → popup Info → Equip → các slot sáng "đeo vào đây", kho chỉ còn
// hiện relic đang chọn → bấm slot để đeo / bấm relic đó để huỷ.
public class UITabEnchantment : MonoBehaviour
{
    public Button btClose;
    public TMP_Text txtQuantityVial;
    public List<ElementEnchantmentEquipUI> listElementEquipHero;
    // Template ô kho (nằm sẵn trong Content) — clone ra mỗi relic 1 ô, bản gốc ẩn đi.
    public GameObject objPrefabElementEnchantment;
    public Transform transParrentElement;

    public Button btQuickEquip;
    public Button btMergeAll;
    public TMP_Text txtLvl;
    public Button btInfoRateSummon;
    public Image imgFill;
    public TMP_Text txtFill;
    public Button btSummonEnchantment;
    public TMP_Text txtSummon;            // "<giá>\nSummon x<lượt>"
    public Button btMultiplier;           // chỉ hiện khi Vial >= 2000
    public TMP_Text txtMultiplier;        // "x<multiplier>"

    // UIMainLobby gán: thiếu Vial khi summon → chuyển sang tab Dungeon (gốc TabBar.kvq(Dungeon)).
    public Action onNotEnoughVial;

    private readonly List<ElementEchantmentUI> listElementEnchant = new List<ElementEchantmentUI>();
    private int multiplierIndex;
    private int selectingTier;            // > 0: đang chờ chọn slot để đeo relic tier này

    private void Awake()
    {
        btClose.onClick.AddListener(Close);
        btQuickEquip.onClick.AddListener(OnClickQuickEquip);
        btMergeAll.onClick.AddListener(OnClickMergeAll);
        btInfoRateSummon.onClick.AddListener(OnClickInfoRate);
        btSummonEnchantment.onClick.AddListener(OnClickSummon);
        if (btMultiplier != null)
        {
            btMultiplier.onClick.AddListener(OnClickMultiplier);
        }

        for (int i = 0; i < listElementEquipHero.Count; i++)
        {
            listElementEquipHero[i].Init(OnClickSlot);
        }

        objPrefabElementEnchantment.SetActive(false);
    }

    private void OnEnable()
    {
        EventDispatcher.Instance.RegisterListener(EventID.EnchantmentChanged, OnEnchantmentChanged);
        SetSelectingTier(0);
        Refresh();
    }

    private void OnDisable()
    {
        EventDispatcher.Instance.RemoveListener(EventID.EnchantmentChanged, OnEnchantmentChanged);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnEnchantmentChanged(object param)
    {
        Refresh();
    }

    public void Refresh()
    {
        txtQuantityVial.text = EnchantmentService.GetVial().ToString();
        RefreshSlots();
        RefreshInventory();
        RefreshSummon();
        btMergeAll.interactable = EnchantmentService.CanMergeAny();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < listElementEquipHero.Count; i++)
        {
            listElementEquipHero[i].Refresh();
        }
    }

    // Kho (EnchantmentTabPage.hyu gốc): mỗi relic 1 ô, tier TĂNG dần (relic đang đeo không nằm trong kho).
    // Đang chọn slot để đeo → chỉ hiện 1 ô là relic đang chọn.
    private void RefreshInventory()
    {
        UserEnchantmentData user = GameData.userData.enchantments;
        int index = 0;
        if (selectingTier > 0)
        {
            GetElement(index).SetData(selectingTier);
            index++;
        }

        for (int tier = 1; tier <= EnchantmentConfig.MAX_TIER && selectingTier <= 0; tier++)
        {
            int count = user.GetOwned(tier);
            for (int k = 0; k < count; k++)
            {
                GetElement(index).SetData(tier);
                index++;
            }
        }

        for (int i = index; i < listElementEnchant.Count; i++)
        {
            listElementEnchant[i].gameObject.SetActive(false);
        }
    }

    private ElementEchantmentUI GetElement(int index)
    {
        if (index >= listElementEnchant.Count)
        {
            GameObject obj = Instantiate(objPrefabElementEnchantment, transParrentElement);
            ElementEchantmentUI element = obj.GetComponent<ElementEchantmentUI>();
            element.Init(OnClickOwned);
            listElementEnchant.Add(element);
        }

        ElementEchantmentUI result = listElementEnchant[index];
        result.gameObject.SetActive(true);
        return result;
    }

    // Summon Level + tiến độ + nút summon (EnchantmentTabPage.hyp/hyq/hys gốc).
    private void RefreshSummon()
    {
        int total = GameData.userData.enchantments.totalSummons;
        txtLvl.text = "Lvl " + EnchantmentConfig.GetLevel(total);
        EnchantmentConfig.GetProgress(total, out int current, out int required, out bool isMax);
        if (isMax)
        {
            txtFill.text = "MAX";
            imgFill.fillAmount = 1f;
        }
        else
        {
            txtFill.text = current + "/" + required;
            imgFill.fillAmount = (float)current / required;
        }

        // Nút multiplier chỉ hiện khi đủ 2000 Vial; dưới mức này multiplier về x1.
        int vial = EnchantmentService.GetVial();
        bool canMultiply = vial >= EnchantmentConfig.MULTIPLIER_UNLOCK_VIAL;
        if (canMultiply == false)
        {
            multiplierIndex = 0;
        }
        if (btMultiplier != null)
        {
            btMultiplier.gameObject.SetActive(canMultiply);
        }

        int multiplier = EnchantmentConfig.MULTIPLIERS[multiplierIndex];
        if (txtMultiplier != null)
        {
            txtMultiplier.text = "x" + multiplier;
        }

        int cost = EnchantmentService.GetSummonCost(multiplier);
        if (txtSummon != null)
        {
            string costText = vial >= cost ? cost.ToString() : "<color=red>" + cost + "</color>";
            txtSummon.text = costText + "\nSummon x" + EnchantmentService.GetSummonCount(multiplier);
        }
    }

    // ----- Đeo -----

    // Bấm relic trong kho: đang chọn slot thì huỷ chọn; không thì mở popup Info (Equip/Merge/Dismantle).
    private void OnClickOwned(ElementEchantmentUI element)
    {
        if (selectingTier > 0)
        {
            SetSelectingTier(0);
            return;
        }

        UIEnchantmentElementInfo ui = UIManager.Instance.LoadUI(UIKey.EnchantmentInfo) as UIEnchantmentElementInfo;
        if (ui != null)
        {
            ui.ShowOwned(element.Tier, SetSelectingTier);
        }
    }

    // Bấm slot: đang chọn → đeo relic đã chọn vào slot; không thì slot có relic → popup Info (Unequip/Merge/Dismantle).
    private void OnClickSlot(ElementEnchantmentEquipUI slot)
    {
        if (selectingTier > 0)
        {
            int tier = selectingTier;
            SetSelectingTier(0);
            double powerBefore = PlayerPower.GetCurrent();
            if (EnchantmentService.Equip(slot.slotType, tier))
            {
                this.PostEvent(EventID.EnchantmentChanged);
                PlayerPower.NotifyChange(powerBefore);
            }
            return;
        }

        if (slot.Tier < 1)
        {
            return;
        }

        UIEnchantmentElementInfo ui = UIManager.Instance.LoadUI(UIKey.EnchantmentInfo) as UIEnchantmentElementInfo;
        if (ui != null)
        {
            ui.ShowEquipped(slot.slotType);
        }
    }

    private void SetSelectingTier(int tier)
    {
        selectingTier = tier;
        for (int i = 0; i < listElementEquipHero.Count; i++)
        {
            listElementEquipHero[i].SetSelectMode(tier > 0);
        }
        RefreshInventory();
    }

    private void OnClickQuickEquip()
    {
        SetSelectingTier(0);
        double powerBefore = PlayerPower.GetCurrent();
        EnchantmentService.QuickEquip();
        this.PostEvent(EventID.EnchantmentChanged);
        PlayerPower.NotifyChange(powerBefore);
    }

    private void OnClickMergeAll()
    {
        SetSelectingTier(0);
        if (EnchantmentService.MergeAll())
        {
            this.PostEvent(EventID.EnchantmentChanged);
        }
    }

    // ----- Summon -----

    private void OnClickMultiplier()
    {
        multiplierIndex = (multiplierIndex + 1) % EnchantmentConfig.MULTIPLIERS.Length;
        RefreshSummon();
    }

    private void OnClickInfoRate()
    {
        UIUpgradeEnchantment ui = UIManager.Instance.LoadUI(UIKey.UpgradeEnchantment) as UIUpgradeEnchantment;
        if (ui != null)
        {
            ui.Show();
        }
    }

    private void OnClickSummon()
    {
        SetSelectingTier(0);
        List<int> results = EnchantmentService.TrySummon(EnchantmentConfig.MULTIPLIERS[multiplierIndex]);
        if (results == null)
        {
            onNotEnoughVial?.Invoke();
            return;
        }

        UISumonEnchantment ui = UIManager.Instance.LoadUI(UIKey.SummonEnchantment) as UISumonEnchantment;
        if (ui != null)
        {
            ui.Show(results);
        }

        this.PostEvent(EventID.EnchantmentChanged);
    }
}

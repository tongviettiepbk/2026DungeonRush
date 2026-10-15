using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup cài đặt Auto Loot (AutoForgePopup gốc):
//  - Mỗi dòng rarity: tick "Keep" (bỏ tick = auto bán ngay) + tỉ lệ ra ở level hiện tại; tỉ lệ 0 → ẩn dòng.
//  - Lọc substat (mở ở level 7): bật thì món phải có ít nhất 1 substat được tick mới giữ.
//  - Số hộp mở mỗi lượt: 8 ô 1..8, ô vượt ForgeService.GetMaxHammerCount bị khoá.
//  - Start: lọc bật mà chưa tick substat nào → tự tắt lọc; đóng popup rồi gọi onStart.
// Ô "Auto sell lower level" của bản gốc là tính năng chết (node tắt sẵn) — không dùng.
public class UIAutoForgePopup : BaseUI
{
    [Header("Rarity (index = Rarity)")]
    public List<GameObject> rarityRows;
    public List<Button> keepButtons;
    public List<GameObject> keepChecks;
    public List<TMP_Text> rarityRateTexts;

    [Header("Substat filter")]
    public Button btFilterToggle;
    public GameObject objFilterOn;
    public GameObject objFilterLock;
    public GameObject objFilterList;
    public RectTransform layoutRoot;
    public List<SubStatType> subStatTypes;
    public List<Button> subStatButtons;
    public List<GameObject> subStatChecks;

    [Header("Hammer count")]
    public Button btHammerCount;
    public TMP_Text txtSelectedHammer;
    public CanvasGroup hammerPanel;
    public Button btHammerPanelBackground;
    public Button btHammerPanelClose;
    public TMP_Text txtPanelSelectedHammer;
    public List<Button> hammerOptions;
    public List<TMP_Text> hammerOptionTexts;
    public List<GameObject> hammerOptionLocks;
    public Color hammerOptionHighlight = new Color(0.435f, 0.435f, 0.435f, 1f);   // HammerOptionElementUI.HighlightColor
    public Color hammerOptionDefault = new Color(0.141f, 0.141f, 0.141f, 1f);     // HammerOptionElementUI.DefaultColor

    [Header("Bottom")]
    public Button btStart;
    public Button btClose;
    public Button btBackground;

    private Action onStart;

    private static UserForgeData User => GameData.userData.forge;

    protected override void Awake()
    {
        base.Awake();
        for (int i = 0; i < keepButtons.Count; i++)
        {
            int index = i;
            keepButtons[i].onClick.AddListener(() => OnClickKeep(index));
        }

        for (int i = 0; i < subStatButtons.Count; i++)
        {
            int index = i;
            subStatButtons[i].onClick.AddListener(() => OnClickSubStat(index));
        }

        for (int i = 0; i < hammerOptions.Count; i++)
        {
            int count = i + 1;
            hammerOptions[i].onClick.AddListener(() => OnClickHammerOption(count));
        }

        btFilterToggle.onClick.AddListener(OnClickFilterToggle);
        btHammerCount.onClick.AddListener(() => SetHammerPanelVisible(true));
        btHammerPanelBackground.onClick.AddListener(() => SetHammerPanelVisible(false));
        btHammerPanelClose.onClick.AddListener(() => SetHammerPanelVisible(false));
        btStart.onClick.AddListener(OnClickStart);
        btClose.onClick.AddListener(Close);
        if (btBackground != null)
            btBackground.onClick.AddListener(Close);
    }

    public void Show(Action onStart)
    {
        this.onStart = onStart;
        SetHammerPanelVisible(false);
        Refresh();
    }

    // igb gốc.
    private void Refresh()
    {
        List<float> probs = ForgeService.GetRarityProbabilities();
        for (int i = 0; i < rarityRows.Count; i++)
        {
            float prob = i < probs.Count ? probs[i] : 0f;
            rarityRows[i].SetActive(prob > 0f);
            rarityRateTexts[i].text = "%" + prob.ToString("0.##");
            keepChecks[i].SetActive(User.IsRarityKept((Rarity)i));
        }

        bool filterUnlocked = ForgeService.IsSubStatFilterUnlocked();
        bool filterOn = filterUnlocked && User.subStatFilterEnabled;
        objFilterLock.SetActive(filterUnlocked == false);
        objFilterOn.SetActive(filterOn);
        objFilterList.SetActive(filterOn);
        for (int i = 0; i < subStatTypes.Count; i++)
            subStatChecks[i].SetActive(User.IsSubStatEnabled(subStatTypes[i]));

        int max = ForgeService.GetMaxHammerCount();
        int selected = ForgeService.GetSelectedHammerCount();
        string selectedText = selected + "<sprite=0>";
        txtSelectedHammer.text = selectedText;
        txtPanelSelectedHammer.text = selectedText;
        for (int i = 0; i < hammerOptions.Count; i++)
        {
            int count = i + 1;
            hammerOptionTexts[i].text = count + "<sprite=0>";
            hammerOptionLocks[i].SetActive(count > max);
            hammerOptionTexts[i].gameObject.SetActive(count <= max);
            hammerOptions[i].image.color = count == selected ? hammerOptionHighlight : hammerOptionDefault;
        }

        if (layoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }

    private void OnClickKeep(int index)
    {
        Rarity rarity = (Rarity)index;
        User.SetRarityKept(rarity, User.IsRarityKept(rarity) == false);
        GameData.Save();
        Refresh();
    }

    private void OnClickFilterToggle()
    {
        if (ForgeService.IsSubStatFilterUnlocked() == false)
        {
            UIManager.Instance.ShowToastMessage("Mở ở level " + ForgeService.SUBSTAT_FILTER_UNLOCK_LEVEL, isLocalize: false);
            return;
        }

        User.subStatFilterEnabled = User.subStatFilterEnabled == false;
        User.isDataChanged = true;
        GameData.Save();
        Refresh();
    }

    private void OnClickSubStat(int index)
    {
        SubStatType type = subStatTypes[index];
        User.SetSubStatEnabled(type, User.IsSubStatEnabled(type) == false);
        GameData.Save();
        Refresh();
    }

    // ige gốc.
    private void OnClickHammerOption(int count)
    {
        if (count > ForgeService.GetMaxHammerCount())
        {
            UIManager.Instance.ShowToastMessage("Nâng Mastery Auto-Loot để mở thêm", isLocalize: false);
            return;
        }

        User.selectedAutoHammerCount = count;
        User.isDataChanged = true;
        GameData.Save();
        SetHammerPanelVisible(false);
        Refresh();
    }

    private void SetHammerPanelVisible(bool visible)
    {
        hammerPanel.alpha = visible ? 1f : 0f;
        hammerPanel.interactable = visible;
        hammerPanel.blocksRaycasts = visible;
        hammerPanel.gameObject.SetActive(visible);
    }

    // OnStartAutoForgeButtonClicked gốc.
    private void OnClickStart()
    {
        if (User.subStatFilterEnabled && User.enabledSubStats.Count == 0)
        {
            User.subStatFilterEnabled = false;
            User.isDataChanged = true;
            GameData.Save();
        }

        Action callback = onStart;
        Close();
        callback?.Invoke();
    }
}

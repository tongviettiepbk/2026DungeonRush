using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Trang Wing (WingCraftTabPage gốc) — page trong UiMainGame, mở từ ô Wing ở lobby.
// Carousel 10 wing (mỗi rarity 1 mẫu, kéo/nút trái phải) + bảng info wing đang chọn + nút:
//   Craft (chưa có) / Reroll (đã có) → UIWingCraftPopup / UIWingRerollPopup; Upgrade (tốn quặng); Equip/Unequip.
// Chế tạo xong hiện ClaimPage "New Wing Obtained!". Logic ở WingService (reverse il2cpp v41).
public class UITabWing : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Serializable]
    public class RarityPatternEntry
    {
        public Rarity rarity;
        public GameObject objPattern;
    }

    // GameResources gốc: DisabledButtonColor / DefaultButtonColor / DangerButtonColor.
    public static readonly Color DISABLED_BUTTON_COLOR = new Color(0.6705883f, 0.6705883f, 0.6705883f, 1f);
    public static readonly Color DEFAULT_BUTTON_COLOR = new Color(0.2078431f, 0.6f, 0.8666667f, 1f);
    public static readonly Color DANGER_BUTTON_COLOR = new Color(1f, 0.2392157f, 0.2705882f, 1f);

    // GameResources.RarityColors gốc (index = Rarity).
    private static readonly string[] RARITY_COLORS =
    {
        "CACACA", "E0C69D", "B5E05C", "45C4FF", "FFDD61", "A288F2", "49A872", "3C4AC2", "E66161", "FFB236",
    };

    public Button btClose;

    [Header("Carousel")]
    public Transform transContent;
    public GameObject objPrefabElement;
    public Button btLeft;
    public Button btRight;
    public float itemSpacing = 320f;
    public float animationDuration = 0.3f;
    public float centerScale = 1.5f;
    public float sideScale = 0.6f;
    public float dragSensitivity = 1f;

    [Header("Nút")]
    public Button btCraft;
    public Image imgCraft;
    public GameObject objCraftArea;
    public GameObject objRerollArea;
    public TMP_Text txtCraftResource;
    public Button btLevelUp;
    public Image imgLevelUp;
    public TMP_Text txtLevelUpResource;
    public Button btEquip;
    public TMP_Text txtEquip;
    public Button btMining;
    public GameObject objMiningNotification;

    [Header("Info")]
    public Image imgInfoBackground;
    public Image imgInfoIcon;
    public TMP_Text txtInfoName;
    public TMP_Text txtInfoBaseStat;
    public TMP_Text txtInfoSubStats;
    public GameObject objEquipped;
    public TMP_Text txtInfoLevel;
    public List<RarityPatternEntry> listRarityPattern;
    // Nền ô icon theo rarity (GameResources.RarityBackgroundSprites gốc), index = Rarity.
    public List<Sprite> listSpriteBgRarity;

    [Header("Claim (New Wing Obtained)")]
    public GameObject objClaimPage;
    public Image imgClaimWing;
    public Image imgClaimGlow;
    public TMP_Text txtClaimName;
    public Transform transClaimStats;
    public Button btClaim;

    private readonly List<WingData> listWing = new List<WingData>();
    private readonly List<WingElementUI> listElement = new List<WingElementUI>();
    private readonly List<TMP_Text> listClaimStat = new List<TMP_Text>();
    private int currentIndex;
    private bool isAnimating;
    private bool isDragging;
    private float dragStartX;
    private float dragOffset;
    private int dragPreviewIndex;
    private WingData claimWing;
    private Color baseStatColor;

    private void Awake()
    {
        btClose.onClick.AddListener(Close);
        btLeft.onClick.AddListener(() => MoveBy(-1));
        btRight.onClick.AddListener(() => MoveBy(1));
        btCraft.onClick.AddListener(OnClickCraft);
        btLevelUp.onClick.AddListener(OnClickLevelUp);
        btEquip.onClick.AddListener(OnClickEquip);
        btMining.onClick.AddListener(OnClickMining);
        btClaim.onClick.AddListener(OnClickClaim);
        baseStatColor = txtInfoBaseStat.color;

        if (objMiningNotification != null)
        {
            objMiningNotification.SetActive(false);
        }
        objClaimPage.SetActive(false);
    }

    private void OnEnable()
    {
        EventDispatcher.Instance.RegisterListener(EventID.WingChanged, OnWingChanged);
        BuildElements();
        currentIndex = GetDefaultIndex();
        LayoutElements();
        HighlightElements(currentIndex);
        ShowInfo(currentIndex);
    }

    private void OnDisable()
    {
        EventDispatcher.Instance.RemoveListener(EventID.WingChanged, OnWingChanged);
        DOTween.Kill(this);
        isAnimating = false;
        isDragging = false;
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnWingChanged(object param)
    {
        Refresh();
    }

    // kzb/kze gốc: dựng lại carousel (icon/trạng thái sở hữu) + info, giữ wing đang chọn.
    public void Refresh()
    {
        BuildElements();
        LayoutElements();
        HighlightElements(currentIndex);
        ShowInfo(currentIndex);
    }

    // ----- Carousel (kyn/kyo/kyq/kyt/kyz) -----

    // Danh sách wing theo rarity tăng dần, mỗi wing 1 ô (template objPrefabElement).
    private void BuildElements()
    {
        listWing.Clear();
        listWing.AddRange(GameData.staticData.wings.wings);
        listWing.Sort((a, b) => a.rarity.CompareTo(b.rarity));

        for (int i = 0; i < listWing.Count; i++)
        {
            if (i >= listElement.Count)
            {
                GameObject obj = Instantiate(objPrefabElement, transContent);
                RectTransform rect = (RectTransform)obj.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                listElement.Add(obj.GetComponent<WingElementUI>());
            }

            listElement[i].gameObject.SetActive(true);
            listElement[i].SetData(listWing[i], WingService.GetOwned(listWing[i]));
        }

        for (int i = listWing.Count; i < listElement.Count; i++)
        {
            listElement[i].gameObject.SetActive(false);
        }

        if (currentIndex >= listWing.Count)
        {
            currentIndex = 0;
        }
    }

    // kyq: wing đang mặc; chưa mặc thì wing sở hữu rarity cao nhất; chưa có gì thì ô đầu.
    private int GetDefaultIndex()
    {
        int equippedId = WingService.GetEquippedId();
        if (equippedId >= 0)
        {
            for (int i = 0; i < listWing.Count; i++)
            {
                if (listWing[i].wingId == equippedId)
                {
                    return i;
                }
            }
        }

        for (int i = listWing.Count - 1; i >= 0; i--)
        {
            if (WingService.GetOwned(listWing[i]) != null)
            {
                return i;
            }
        }
        return 0;
    }

    private int Wrap(int index)
    {
        int count = listWing.Count;
        return ((index % count) + count) % count;
    }

    // Khoảng cách vòng tròn từ ô `center` tới ô `index` (−count/2 .. count/2).
    private int CircularOffset(int index, int center)
    {
        int count = listWing.Count;
        int offset = index - center;
        if (offset > count / 2)
        {
            offset -= count;
        }
        else if (offset < -(count / 2))
        {
            offset += count;
        }
        return offset;
    }

    private void LayoutElements()
    {
        for (int i = 0; i < listWing.Count; i++)
        {
            int offset = CircularOffset(i, currentIndex);
            RectTransform rect = (RectTransform)listElement[i].transform;
            rect.anchoredPosition = new Vector2(itemSpacing * offset, 0f);
            rect.localScale = Vector3.one * (offset == 0 ? centerScale : sideScale);
        }
    }

    private void HighlightElements(int index)
    {
        for (int i = 0; i < listWing.Count; i++)
        {
            listElement[i].SetSelected(i == index);
        }
    }

    // kyw/kyx/kyy: cần >= 3 ô và không đang chạy animation.
    private void MoveBy(int direction)
    {
        if (isAnimating || listWing.Count < 3)
        {
            return;
        }
        AnimateTo(Wrap(currentIndex + direction));
    }

    // Trượt các ô về vị trí quanh `target` (ô nhảy vòng >= 3 bậc thì đặt thẳng, không tween). Ease OutCubic.
    private void AnimateTo(int target)
    {
        isAnimating = true;
        HighlightElements(target);
        ShowInfo(target);

        Sequence sequence = DOTween.Sequence().SetId(this);
        for (int i = 0; i < listWing.Count; i++)
        {
            RectTransform rect = (RectTransform)listElement[i].transform;
            int oldOffset = CircularOffset(i, currentIndex);
            int newOffset = CircularOffset(i, target);
            float x = itemSpacing * newOffset;
            float scale = newOffset == 0 ? centerScale : sideScale;
            if (Mathf.Abs(newOffset - oldOffset) >= 3)
            {
                rect.anchoredPosition = new Vector2(x, 0f);
                rect.localScale = Vector3.one * scale;
                continue;
            }

            sequence.Join(rect.DOAnchorPosX(x, animationDuration).SetEase(Ease.OutCubic));
            sequence.Join(rect.DOScale(scale, animationDuration).SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            currentIndex = target;
            isAnimating = false;
        });
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isAnimating || listWing.Count < 3)
        {
            return;
        }

        DOTween.Kill(this);
        isDragging = true;
        dragStartX = eventData.position.x;
        dragOffset = 0f;
        dragPreviewIndex = currentIndex;
    }

    // Kéo: các ô đi theo tay, scale nội suy CenterScale→SideScale theo khoảng cách tới tâm;
    // ô gần tâm nhất đổi thì cập nhật highlight + info ngay.
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging == false)
        {
            return;
        }

        dragOffset = (eventData.position.x - dragStartX) * dragSensitivity;
        int step = Mathf.RoundToInt(-dragOffset / itemSpacing);
        int preview = Wrap(currentIndex + step);
        float baseX = dragOffset + itemSpacing * step;

        for (int i = 0; i < listWing.Count; i++)
        {
            float x = baseX + itemSpacing * CircularOffset(i, preview);
            float t = Mathf.Min(Mathf.Abs(x) / itemSpacing, 1f);
            RectTransform rect = (RectTransform)listElement[i].transform;
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.localScale = Vector3.one * (centerScale + (sideScale - centerScale) * t);
        }

        if (preview != dragPreviewIndex)
        {
            dragPreviewIndex = preview;
            HighlightElements(preview);
            ShowInfo(preview);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging == false)
        {
            return;
        }

        isDragging = false;
        int step = Mathf.RoundToInt(-dragOffset / itemSpacing);
        AnimateTo(Wrap(currentIndex + step));
    }

    // ----- Info (kzo) -----

    // Đang trượt/kéo carousel thì chưa chốt ô → chặn thao tác nút.
    private WingData SelectedWing => listWing.Count > 0 && isAnimating == false && isDragging == false ? listWing[currentIndex] : null;

    private void ShowInfo(int index)
    {
        WingData wing = listWing[index];
        WingModel model = WingService.GetOwned(wing);
        int level = model != null ? model.level : 1;

        imgInfoBackground.sprite = GetBgRarity(wing.rarity);
        imgInfoIcon.sprite = wing.icon;
        for (int i = 0; i < listRarityPattern.Count; i++)
        {
            listRarityPattern[i].objPattern.SetActive(listRarityPattern[i].rarity == wing.rarity);
        }

        txtInfoName.text = GetWingName(wing);
        txtInfoBaseStat.text = GetBaseStatText(wing, level);
        txtInfoSubStats.text = GetSubStatsText(model != null ? model.subStats : GetDefaultSubStats(wing));

        // Chưa sở hữu: nút Craft + giá chế tạo; đã có: nút Reroll + Upgrade + Equip.
        int ore = WingService.GetOre(wing.craftOreType);
        txtCraftResource.text = GetOreText(wing.craftOreType, ore, wing.craftOreCost);
        objCraftArea.SetActive(model == null);
        objRerollArea.SetActive(model != null);
        imgCraft.color = model != null || ore >= wing.craftOreCost ? DEFAULT_BUTTON_COLOR : DISABLED_BUTTON_COLOR;

        bool isEquipped = WingService.IsEquipped(wing);
        objEquipped.SetActive(isEquipped);
        txtInfoLevel.gameObject.SetActive(model != null);
        txtInfoLevel.text = "Lv " + level;

        btEquip.gameObject.SetActive(model != null);
        txtEquip.text = isEquipped ? "Unequip" : "Equip";

        btLevelUp.gameObject.SetActive(model != null);
        if (model == null)
        {
            return;
        }

        if (WingService.IsMaxLevel(wing, model))
        {
            txtLevelUpResource.text = "Max Level";
            txtLevelUpResource.color = Color.white;
            imgLevelUp.color = DISABLED_BUTTON_COLOR;
            return;
        }

        int levelUpOre = WingService.GetOre(wing.levelUpOreType);
        int cost = WingService.GetLevelUpCost(wing, model.level);
        txtLevelUpResource.text = GetOreText(wing.levelUpOreType, levelUpOre, cost);
        txtLevelUpResource.color = levelUpOre >= cost ? Color.white : Color.red;
        imgLevelUp.color = levelUpOre >= cost ? DEFAULT_BUTTON_COLOR : DISABLED_BUTTON_COLOR;
    }

    // ----- Nút -----

    // kza: chưa có → popup chế tạo; đã có → popup reroll substat.
    private void OnClickCraft()
    {
        WingData wing = SelectedWing;
        if (wing == null)
        {
            return;
        }

        if (WingService.GetOwned(wing) == null)
        {
            UIWingCraftPopup ui = UIManager.Instance.LoadUI(UIKey.WingCraftPopup) as UIWingCraftPopup;
            if (ui != null)
            {
                ui.Show(wing, GetBgRarity(wing.rarity), () => OnCrafted(wing));
            }
            return;
        }

        UIWingRerollPopup reroll = UIManager.Instance.LoadUI(UIKey.WingRerollPopup) as UIWingRerollPopup;
        if (reroll != null)
        {
            reroll.Show(wing, GetBgRarity(wing.rarity));
        }
    }

    // kzd: chế tạo xong → cập nhật info + hiện ClaimPage.
    private void OnCrafted(WingData wing)
    {
        ShowInfo(currentIndex);
        ShowClaim(wing);
    }

    // kzc: trừ quặng, +1 level; wing đang mặc → Hero tính lại + toast Power. Hiệu ứng nảy chữ level/chỉ số.
    private void OnClickLevelUp()
    {
        WingData wing = SelectedWing;
        WingModel model = WingService.GetOwned(wing);
        if (model == null || WingService.IsMaxLevel(wing, model))
        {
            return;
        }

        if (WingService.GetOre(wing.levelUpOreType) < WingService.GetLevelUpCost(wing, model.level))
        {
            UIManager.Instance.ShowToastMessage("Không đủ tài nguyên", isLocalize: false);
            return;
        }

        bool isEquipped = WingService.IsEquipped(wing);
        double powerBefore = PlayerPower.GetCurrent();
        WingService.LevelUp(wing);
        if (isEquipped)
        {
            this.PostEvent(EventID.EquipmentChanged, GearSlotType.WING);
            PlayerPower.NotifyChange(powerBefore);
        }
        this.PostEvent(EventID.WingChanged);
        PlayLevelUpEffect();
    }

    private void PlayLevelUpEffect()
    {
        Transform level = txtInfoLevel.transform;
        Transform stat = txtInfoBaseStat.transform;
        level.DOKill();
        stat.DOKill();
        txtInfoBaseStat.DOKill();
        level.localScale = Vector3.one;
        stat.localScale = Vector3.one;

        DOTween.Sequence().SetId(txtInfoBaseStat)
            .Join(level.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad))
            .Join(stat.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad))
            .Join(txtInfoBaseStat.DOColor(Color.green, 0.1f))
            .Append(level.DOScale(1f, 0.15f).SetEase(Ease.InQuad))
            .Join(stat.DOScale(1f, 0.15f).SetEase(Ease.InQuad))
            .Join(txtInfoBaseStat.DOColor(baseStatColor, 0.15f));
    }

    // kzb: đang mặc → cởi, không thì mặc. Hero mặc lại slot WING + toast Power.
    private void OnClickEquip()
    {
        WingData wing = SelectedWing;
        if (wing == null || WingService.GetOwned(wing) == null)
        {
            return;
        }

        double powerBefore = PlayerPower.GetCurrent();
        if (WingService.IsEquipped(wing))
        {
            WingService.Unequip();
        }
        else
        {
            WingService.Equip(wing);
        }

        this.PostEvent(EventID.EquipmentChanged, GearSlotType.WING);
        PlayerPower.NotifyChange(powerBefore);
        this.PostEvent(EventID.WingChanged);
    }

    // Hệ Mining (nguồn quặng) chưa làm.
    private void OnClickMining()
    {
        UIManager.Instance.ShowToastMessage("Tính năng Đào mỏ chưa mở", isLocalize: false);
    }

    // ----- Claim (WingClaimPage gốc) -----

    // Tên + chỉ số nền level 1 + substat khởi điểm. Dòng chỉ số clone từ txtClaimName vào transClaimStats.
    private void ShowClaim(WingData wing)
    {
        claimWing = wing;
        imgClaimWing.sprite = wing.icon;
        if (imgClaimGlow != null)
        {
            imgClaimGlow.color = GetRarityColor(wing.rarity);
        }
        txtClaimName.text = GetWingName(wing);

        List<string> lines = new List<string>
        {
            "Health " + GearStatCalculator.GetWingHealth(wing, 1).ToLetter(false),
            "Damage " + GearStatCalculator.GetWingDamage(wing, 1).ToLetter(false),
        };
        List<GearSubStat> subStats = GetDefaultSubStats(wing);
        for (int i = 0; i < subStats.Count; i++)
        {
            lines.Add(GetSubStatText(subStats[i]));
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
        Transform wingTransform = imgClaimWing.transform;
        wingTransform.DOKill();
        wingTransform.localScale = Vector3.zero;
        wingTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
    }

    // kze: nhận → dựng lại carousel và chọn wing vừa chế tạo.
    private void OnClickClaim()
    {
        objClaimPage.SetActive(false);
        if (claimWing == null)
        {
            return;
        }

        BuildElements();
        int index = listWing.IndexOf(claimWing);
        currentIndex = index >= 0 ? index : 0;
        claimWing = null;
        LayoutElements();
        HighlightElements(currentIndex);
        ShowInfo(currentIndex);
    }

    // ----- Helper dùng chung cho các UI Wing -----

    private Sprite GetBgRarity(Rarity rarity)
    {
        int index = (int)rarity;
        return listSpriteBgRarity != null && index < listSpriteBgRarity.Count ? listSpriteBgRarity[index] : imgInfoBackground.sprite;
    }

    public static Color GetRarityColor(Rarity rarity)
    {
        int index = Mathf.Clamp((int)rarity, 0, RARITY_COLORS.Length - 1);
        ColorUtility.TryParseHtmlString("#" + RARITY_COLORS[index], out Color color);
        return color;
    }

    // "<color=#RRGGBB>[Rarity]</color> Tên" (kzo/kxq gốc).
    public static string GetWingName(WingData wing)
    {
        int index = Mathf.Clamp((int)wing.rarity, 0, RARITY_COLORS.Length - 1);
        return "<color=#" + RARITY_COLORS[index] + ">[" + wing.rarity + "]</color> " + wing.wingName;
    }

    // "Health X\nDamage Y" theo level (vh.lal / vh.lak).
    public static string GetBaseStatText(WingData wing, int level)
    {
        return "Health " + GearStatCalculator.GetWingHealth(wing, level).ToLetter(false)
            + "\nDamage " + GearStatCalculator.GetWingDamage(wing, level).ToLetter(false);
    }

    public static string GetSubStatText(GearSubStat subStat)
    {
        return UIMainLobby.SubStatName(subStat.type) + ": +" + subStat.value.ToString("0.##") + "%";
    }

    public static string GetSubStatsText(List<GearSubStat> subStats)
    {
        List<string> lines = new List<string>();
        for (int i = 0; i < subStats.Count; i++)
        {
            lines.Add(GetSubStatText(subStats[i]));
        }
        return string.Join("\n", lines);
    }

    public static List<GearSubStat> GetDefaultSubStats(WingData wing)
    {
        List<GearSubStat> result = new List<GearSubStat>();
        for (int i = 0; i < wing.subStats.Count; i++)
        {
            result.Add(new GearSubStat(wing.subStats[i].type, wing.subStats[i].value));
        }
        return result;
    }

    public static string GetOreName(MineOreType ore)
    {
        switch (ore)
        {
            case MineOreType.CoalMine: return "Than";
            case MineOreType.DiamondMine: return "Kim cương";
            case MineOreType.EmeraldMine: return "Ngọc lục bảo";
            case MineOreType.GoldMine: return "Vàng";
            case MineOreType.IronMine: return "Sắt";
            case MineOreType.RubyMine: return "Hồng ngọc";
            case MineOreType.Dirt: return "Đất";
            case MineOreType.Stone: return "Đá";
            default: return ore.ToString();
        }
    }

    // "<quặng> có/cần" (gốc: "<sprite=0> có/cần" với spriteAsset của quặng — chưa có sprite asset quặng).
    public static string GetOreText(MineOreType ore, int have, int need)
    {
        return GetOreName(ore) + " " + have + "/" + need;
    }
}

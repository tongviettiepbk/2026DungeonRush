using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup thông tin 1 pet (CompanionInfoPopup gốc) — mở từ ô pet (ElementPetEquimentUI.btInfo) qua CompanionUI.OnClickInfo.
//   • Tên + rarity; mô tả kỹ năng điền <Value> theo level; Own Effect = Damage/Health riêng của pet.
//   • Chưa sở hữu → chỉ hiện khoá (ẩn icon, level, tiến độ, nút); số liệu tính ở level 1.
//   • Đã sở hữu  → icon + "Lvl N" + tiến độ thẻ; btEquip = Equip/Remove tuỳ trạng thái; btUpgrade bật khi đủ thẻ.
// Mọi giá trị = Base + Scaler × level (cùng công thức PetUnit.ScaleByLevel, KHÔNG nhân CompanionDamage của hero).
// Đối chiếu game thật: Flame Wing lv1 → 252 / Damage 47 Health 63 ; Magic Wool lv6 → 78 / Damage 6 Health 65.
public class UIPetInfo : BaseUI
{
    // Màu số trong mô tả: xanh đậm đọc được trên nền kem (concept batch3; gốc vàng #E5F36B trên nền tối).
    private const string VALUE_COLOR = "#2F8F1F";
    private const string VALUE_TAG = "<Value>";

    public Button btClose;
    public TMP_Text txtName;
    public TMP_Text txtRarity;

    [Space(20)]
    public Image imgBackground;     // nền ô = khung rarity (RarityBackgroundConfig)
    public Image imgIcon;
    public Image imgLock;
    public TMP_Text textLv;

    public Image imgProcess;
    public TMP_Text textProcess;

    [Space(20)]
    public TMP_Text txtDescription;
    public TMP_Text txtOwnLabels;   // cột nhãn nâu "Damage\nHealth"
    public TMP_Text txtOwnEffect;   // cột số chữ viền "+1.2k\n+2.2k", đặt ngay sau nhãn dài nhất
    public Button btEquip;
    public Button btUpgrade;
    public TMP_Text txtHint;        // chữ mờ thay chỗ nút: chưa sở hữu / xem pet người khác
    public Button btBackground;     // chạm nền để đóng

    [Space(20)]
    // Trạng thái nút bằng sprite (Batch3Reskin gán): Equip xanh lá, Unequip tím, Upgrade cam, khoá xám.
    public Sprite spriteEquip;
    public Sprite spriteUnequip;
    public Sprite spriteUpgrade;
    public Sprite spriteDisabled;

    private CompanionUI owner;
    private CompanionData data;
    // > 0: đang xem pet của NGƯỜI KHÁC ở level này (popup thông tin người chơi) — không đọc save, không có nút.
    private int viewLevel;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btEquip.onClick.AddListener(OnClickEquip);
        btUpgrade.onClick.AddListener(OnClickUpgrade);
        if (btBackground != null)
            btBackground.onClick.AddListener(Close);
    }

    public void Show(CompanionData data, CompanionUI owner)
    {
        this.data = data;
        this.owner = owner;
        viewLevel = 0;
        Refresh();
        gameObject.SetActive(true);
    }

    public void ShowOther(CompanionData data, int level)
    {
        this.data = data;
        owner = null;
        viewLevel = Mathf.Max(1, level);
        Refresh();
        gameObject.SetActive(true);
    }

    private void Refresh()
    {
        UserCompanionData user = GameData.userData.companions;
        CompanionModel model = user.GetModel(data.assetName);
        bool isViewOther = viewLevel > 0;
        bool isOwned = isViewOther || model != null;
        bool isEquipped = isViewOther == false && isOwned && user.IsEquipped(data.assetName);
        int level = isViewOther ? viewLevel : user.GetLevel(data.assetName); // chưa sở hữu → 1

        txtName.text = Loc.Or(data.localizationKey, data.companionName);
        txtRarity.text = ShopUIUtil.ColoredRarity(data.rarity);

        txtDescription.text = BuildDescription(level);
        txtOwnLabels.text = Loc.L("Items.SubStat.Damage") + "\n" + Loc.L("Items.SubStat.Health");
        txtOwnEffect.text = "+" + FormatNumber(CompanionService.GetOwnAttack(data, level))
                          + "\n+" + FormatNumber(CompanionService.GetOwnHealth(data, level));
        PlaceOwnValues();

        if (imgBackground != null)
        {
            imgBackground.sprite = RarityBackgroundConfig.Get(data.rarity);
        }
        imgIcon.sprite = data.icon;
        imgIcon.gameObject.SetActive(isOwned);
        imgLock.gameObject.SetActive(isOwned == false);

        bool isMine = isOwned && isViewOther == false;
        textLv.gameObject.SetActive(isOwned);
        textProcess.gameObject.SetActive(isMine);
        imgProcess.gameObject.SetActive(isMine);
        imgProcess.transform.parent.gameObject.SetActive(isMine);   // cả thanh (track) — không để track rỗng khi khoá
        btEquip.gameObject.SetActive(isMine);
        btUpgrade.gameObject.SetActive(isMine);
        textLv.text = Loc.L("Common.Level.Abbrev", level);
        if (txtHint != null)
        {
            txtHint.gameObject.SetActive(isMine == false);
            txtHint.text = isViewOther ? Loc.L("Common.TapToClose") : Loc.L("Popup.Companion.SummonToUnlock");
        }
        if (isMine == false)
        {
            return;
        }

        bool isMax = model.level >= CompanionUpgradeConfig.MAX_LEVEL;
        if (isMax)
        {
            textProcess.text = Loc.L("Common.Level.Max");
            imgProcess.fillAmount = 1f;
        }
        else
        {
            int need = CompanionUpgradeConfig.GetCardsRequired(model.level);
            textProcess.text = model.cardCount + "/" + need;
            imgProcess.fillAmount = Mathf.Clamp01((float)model.cardCount / need);
        }

        TMP_Text txtEquip = btEquip.GetComponentInChildren<TMP_Text>(true);
        if (txtEquip != null)
        {
            txtEquip.text = isEquipped ? Loc.L("Common.Unequip") : Loc.L("Common.Equip");
        }
        SetSprite(btEquip, isEquipped ? spriteUnequip : spriteEquip);

        bool canUpgrade = user.CanUpgrade(data.assetName);
        btUpgrade.interactable = canUpgrade;
        SetSprite(btUpgrade, canUpgrade ? spriteUpgrade : spriteDisabled);
        TMP_Text txtUpgrade = btUpgrade.GetComponentInChildren<TMP_Text>(true);
        if (txtUpgrade != null)
        {
            txtUpgrade.text = isMax ? Loc.L("Common.Level.Max") : Loc.L("Common.Upgrade");
        }
    }

    private static void SetSprite(Button button, Sprite sprite)
    {
        if (sprite != null)
        {
            button.image.sprite = sprite;
        }
    }

    // Cột số đứng ngay sau nhãn dài nhất (nhãn đổi theo ngôn ngữ). Hai cột cùng neo/pivot trái.
    private void PlaceOwnValues()
    {
        RectTransform labels = txtOwnLabels.rectTransform;
        RectTransform values = txtOwnEffect.rectTransform;
        float width = txtOwnLabels.GetPreferredValues(txtOwnLabels.text).x;
        values.anchoredPosition = new Vector2(labels.anchoredPosition.x + width + 14f, values.anchoredPosition.y);
    }

    // Điền lần lượt từng <Value> trong data.details theo kiểu kỹ năng (khớp field các lớp PetCompanion*).
    private string BuildDescription(int level)
    {
        List<string> values = GetDescriptionValues(level);
        string text = Loc.Or(data.descriptionKey, data.details);
        for (int i = 0; i < values.Count; i++)
        {
            int index = text.IndexOf(VALUE_TAG);
            if (index < 0)
            {
                break;
            }
            text = text.Substring(0, index) + Highlight(values[i]) + text.Substring(index + VALUE_TAG.Length);
        }
        return text;
    }

    private List<string> GetDescriptionValues(int level)
    {
        string damage = FormatNumber(data.damageBase + data.damageScaler * level);
        string heal = FormatNumber(data.healBase + data.healScaler * level);
        // slowAmount là hệ số NHÂN tốc chạy (0.5 → chậm 50%).
        string slowPercent = FormatDecimal((1f - data.slowAmount) * 100f);

        switch (data.type)
        {
            case CompanionType.Healer:
            case CompanionType.ChainHealer:
            case CompanionType.Guardian:
                return new List<string> { heal };
            case CompanionType.Slower:
            case CompanionType.MultiSlower:
                return new List<string> { damage, slowPercent };
            case CompanionType.Bomber: // Blaze Tail có thêm "burns targets for <Value> seconds"
                return new List<string> { damage, FormatDecimal(data.burnDuration) };
            case CompanionType.Siphon:
                return new List<string> { damage, heal };
            case CompanionType.HealNova:
                return new List<string> { heal, FormatNumber(data.healNovaDamageBase + data.healNovaDamageScaler * level) };
            case CompanionType.MirrorClone:
                return new List<string> { FormatDecimal(data.cloneHealthPercent + data.cloneHealthPercentScaler * level),
                                          FormatDecimal(data.cloneLifetime) };
            case CompanionType.Immortality:
                return new List<string> { FormatDecimal(data.immortalityDuration + data.immortalityDurationScaler * level) };
            default: // DPS, Lightning, Beam, AoeSlower, Blaster, Meteor
                return new List<string> { damage };
        }
    }

    private static string Highlight(string value)
    {
        return "<color=" + VALUE_COLOR + ">" + value + "</color>";
    }

    // Số nguyên (cắt phần lẻ: 47.25 → 47, 6.5 → 6 như game gốc); từ 1000 rút gọn k/M/B/T, 3 chữ số có nghĩa (1.2k, 7.08M).
    private static string FormatNumber(double value)
    {
        value = System.Math.Floor(value);
        if (value < 1000)
        {
            return value.ToString("0");
        }

        string[] suffixes = { "k", "M", "B", "T" };
        int i = -1;
        while (value >= 1000 && i < suffixes.Length - 1)
        {
            value /= 1000;
            i++;
        }

        string format = value < 10 ? "0.##" : value < 100 ? "0.#" : "0";
        return value.ToString(format) + suffixes[i];
    }

    // Giây / phần trăm: giữ tối đa 2 số lẻ (2.02s, 30.5%).
    private static string FormatDecimal(float value)
    {
        return value.ToString("0.##");
    }

    // Đang equip → gỡ; chưa → trang bị (CompanionUI tự toast nếu đã đủ slot).
    private void OnClickEquip()
    {
        if (GameData.userData.companions.IsEquipped(data.assetName))
        {
            owner.OnClickUnequip(data);
        }
        else
        {
            owner.OnClickEquip(data);
        }
        Refresh();
    }

    private void OnClickUpgrade()
    {
        owner.OnClickUpgrade(data);
        Refresh();
    }
}

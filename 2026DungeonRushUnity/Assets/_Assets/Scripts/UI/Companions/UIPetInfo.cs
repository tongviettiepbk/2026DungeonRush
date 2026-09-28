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
    // Màu số trong text — lấy từ prefab gốc (<color=#E5F36B>).
    private const string VALUE_COLOR = "#E5F36B";
    private const string VALUE_TAG = "<Value>";

    public Button btClose;
    public TMP_Text txtName;
    public TMP_Text txtRarity;

    [Space(20)]
    public Image imgIcon;
    public Image imgLock;
    public TMP_Text textLv;

    public Image imgProcess;
    public TMP_Text textProcess;

    [Space(20)]
    public TMP_Text txtDescription;
    public TMP_Text txtOwnEffect;
    public Button btEquip;
    public Button btUpgrade;

    private CompanionUI owner;
    private CompanionData data;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btEquip.onClick.AddListener(OnClickEquip);
        btUpgrade.onClick.AddListener(OnClickUpgrade);
    }

    public void Show(CompanionData data, CompanionUI owner)
    {
        this.data = data;
        this.owner = owner;
        Refresh();
        gameObject.SetActive(true);
    }

    private void Refresh()
    {
        UserCompanionData user = GameData.userData.companions;
        CompanionModel model = user.GetModel(data.assetName);
        bool isOwned = model != null;
        bool isEquipped = isOwned && user.IsEquipped(data.assetName);
        int level = user.GetLevel(data.assetName); // chưa sở hữu → 1

        txtName.text = data.companionName;
        // TODO: màu chữ theo rarity — chưa có bảng màu gốc, đang giữ màu mặc định của prefab.
        txtRarity.text = data.rarity.ToString();

        txtDescription.text = BuildDescription(level);
        txtOwnEffect.text = "Damage " + Highlight(FormatNumber(CompanionService.GetOwnAttack(data, level)))
                          + " Health " + Highlight(FormatNumber(CompanionService.GetOwnHealth(data, level)));

        imgIcon.sprite = data.icon;
        imgIcon.gameObject.SetActive(isOwned);
        imgLock.gameObject.SetActive(isOwned == false);

        textLv.gameObject.SetActive(isOwned);
        textProcess.gameObject.SetActive(isOwned);
        imgProcess.gameObject.SetActive(isOwned);
        btEquip.gameObject.SetActive(isOwned);
        btUpgrade.gameObject.SetActive(isOwned);
        if (isOwned == false)
        {
            return;
        }

        textLv.text = "Lvl " + model.level;
        if (model.level >= CompanionUpgradeConfig.MAX_LEVEL)
        {
            textProcess.text = "MAX";
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
            txtEquip.text = isEquipped ? "Remove" : "Equip";
        }
        btUpgrade.interactable = user.CanUpgrade(data.assetName);
    }

    // Điền lần lượt từng <Value> trong data.details theo kiểu kỹ năng (khớp field các lớp PetCompanion*).
    private string BuildDescription(int level)
    {
        List<string> values = GetDescriptionValues(level);
        string text = data.details;
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

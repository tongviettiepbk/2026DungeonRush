using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup chi tiết 1 nhánh mastery ĐÃ MỞ KHOÁ (mở từ ElementMasteryUI.btInfoMastery).
//   • Tên, icon, "Lvl N", mô tả, giá trị hiện tại > cấp kế (VD "x1,10 > x1,20", "3<box> > 4<box>").
//   • Nút nâng cấp: phí Gem cấp kế, chữ đỏ khi không đủ Gem; hết cấp → MAX.
// Logic nâng cấp nằm ở MasteryService; class này chỉ hiển thị + gắn nút.
public class MasterUpgradeUI : MonoBehaviour
{
    public Button btClose;
    public TMP_Text txtName;
    public Image imgIcon;
    public TMP_Text txtLevel;
    public TMP_Text txtDescirpts;

    public TMP_Text txtProcess;

    public Button btUpgrade;
    public TMP_Text txtQuantityRequest;

    private MasteryUpgradeData data;
    private bool isTestFree;
    private Action onUpgraded;
    private Color colorEnough;

    private void Awake()
    {
        colorEnough = txtQuantityRequest.color;
        btClose.onClick.AddListener(Hide);
        btUpgrade.onClick.AddListener(OnClickUpgrade);
    }

    // isTestFree: nâng miễn phí (đồng bộ cờ TEST của MasteryUI). onUpgraded: báo MasteryUI refresh lưới.
    public void Show(MasteryUpgradeData data, bool isTestFree, Action onUpgraded)
    {
        this.data = data;
        this.isTestFree = isTestFree;
        this.onUpgraded = onUpgraded;

        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        int level = GameData.userData.mastery.GetLevel(data.upgradeType);
        bool isMax = level >= data.MaxLevel;

        txtName.text = data.upgradeName;
        imgIcon.sprite = data.icon;
        txtLevel.text = "Lvl " + level;
        txtDescirpts.text = data.description;

        txtProcess.spriteAsset = data.valueSpriteAsset;
        string current = FormatValue(data.GetValueAtLevel(level));
        txtProcess.text = isMax ? current : current + " > " + FormatValue(data.GetValueAtLevel(level + 1));

        if (isMax)
        {
            txtQuantityRequest.text = "MAX";
            txtQuantityRequest.color = colorEnough;
            btUpgrade.interactable = false;
            return;
        }

        // Game gốc: nút vẫn bấm được, thiếu Gem thì phí hiện chữ đỏ.
        // Luôn hiện phí thật; cờ TEST chỉ bỏ qua việc trừ Gem.
        int cost = MasteryService.GetUpgradeCost(data.upgradeType);
        bool isEnough = isTestFree || MasteryService.CanUpgrade(data.upgradeType);
        txtQuantityRequest.text = cost.ToString();
        txtQuantityRequest.color = isEnough ? colorEnough : Color.red;
        btUpgrade.interactable = true;
    }

    // prefix + số + suffix theo data gốc. Số nguyên in không lẻ ("%13", "+1", "3"),
    // số lẻ in 2 chữ số thập phân theo culture máy ("x1,10" trên máy dùng dấu phẩy).
    private string FormatValue(float value)
    {
        string number = Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.Round(value).ToString("0") : value.ToString("0.00");
        return data.valuePrefix + number + data.valueSuffix;
    }

    private void OnClickUpgrade()
    {
        if (isTestFree)
        {
            // TEST: nâng thẳng, không trừ Gem.
            UserMasteryData user = GameData.userData.mastery;
            user.SetLevel(data.upgradeType, user.GetLevel(data.upgradeType) + 1);
            GameData.Save();
        }
        else if (MasteryService.TryUpgrade(data.upgradeType) == false)
        {
            UIManager.Instance.ShowToastMessage("Không đủ Gem", isLocalize: false);
            return;
        }

        this.PostEvent(EventID.MasteryChanged);
        Refresh();
        onUpgraded?.Invoke();
    }
}

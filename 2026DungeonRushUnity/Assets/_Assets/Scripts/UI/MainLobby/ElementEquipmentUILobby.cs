using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementEquipmentUILobby : MonoBehaviour
{
    public GearSlotType typeEquipment;
    public Button btEquipment;
    public TMP_Text txtLevel;
    public TMP_Text txtLevelEquipment;
    public Image imgEquipment;

    private LootResult dataGear;

    // Đồ đang mặc ở slot này (null nếu chưa được set layout trong phiên chơi này).
    public LootResult GetDataGear()
    {
        return dataGear;
    }

    private void Start()
    {
        btEquipment.onClick.AddListener(ClickBtInfoGear);
    }

    // Gắn item vừa loot vào slot: icon + level. Null-guard từng ref vì prefab có thể chưa wire hết.
    public void SetLayout(LootResult result)
    {
        this.dataGear = result;

        if (result == null)
            return;

        if (imgEquipment != null)
        {
            DebugCustom.Log("set layout");

            imgEquipment.sprite = result.icon;
            //imgEquipment.enabled = result.icon != null; // ẩn Image nếu item không có icon.
        }

        string levelText = "Lv." + result.level;
        if (txtLevel != null)
            txtLevel.text = levelText;
        if (txtLevelEquipment != null)
            txtLevelEquipment.text = levelText;
    }

    private void ClickBtInfoGear()
    {
        if (this.dataGear == null)
        {
            UIManager.Instance.ShowToastMessage("Chưa có trang bị", isLocalize: false);
        }
        else
        {
            UIGearInfo uiGearInfo = UIManager.Instance.LoadUI(UIKey.InfoGear) as UIGearInfo;
            if (uiGearInfo != null)
                uiGearInfo.Show(this.dataGear);
        }
    }
}

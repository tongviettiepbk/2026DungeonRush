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

    // Gắn item vừa loot vào slot: icon + level. Null-guard từng ref vì prefab có thể chưa wire hết.
    public void SetLayout(LootResult result)
    {
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
}

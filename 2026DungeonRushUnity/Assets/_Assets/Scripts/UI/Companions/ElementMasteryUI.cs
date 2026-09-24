using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô mastery trong MasteryUI.
//   • Đã mở khoá → objUnlock + level hiện tại.
//   • Chưa mở    → objLock.
public class ElementMasteryUI : MonoBehaviour
{
    public GameObject objUnlock;
    public Image imgIcon;
    public TMP_Text txtLv;

    public GameObject objLock;

    private MasteryUpgradeData data;

    public void SetData(MasteryUpgradeData data)
    {
        this.data = data;
        imgIcon.sprite = data.icon;
        Refresh();
    }

    public void Refresh()
    {
        UserMasteryData user = GameData.userData.mastery;
        bool isUnlocked = user.IsUnlocked(data.upgradeType);

        objUnlock.SetActive(isUnlocked);
        objLock.SetActive(isUnlocked == false);

        txtLv.gameObject.SetActive(isUnlocked);
        if (isUnlocked)
        {
            txtLv.text = "Lv." + user.GetLevel(data.upgradeType);
        }
    }
}

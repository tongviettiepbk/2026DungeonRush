using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô mastery trong MasteryUI.
//   • Đã mở khoá → objUnlock + level hiện tại.
//   • Chưa mở    → objLock, bấm vào không mở popup info.
public class ElementMasteryUI : MonoBehaviour
{
    public Button btInfoMastery;
    public GameObject objUnlock;
    public Image imgIcon;
    public TMP_Text txtLv;

    public GameObject objLock;
    public TutorialUI tutorial;   // MasteryUpgradeCard.Tutorial gốc

    private MasteryUpgradeData data;
    private Action<MasteryUpgradeData> onClickInfo;

    private void Awake()
    {
        btInfoMastery.onClick.AddListener(OnClickInfo);
    }

    // onClickInfo: MasteryUI mở popup chi tiết (MasterUpgradeUI) cho nhánh này.
    public void SetData(MasteryUpgradeData data, Action<MasteryUpgradeData> onClickInfo)
    {
        this.data = data;
        this.onClickInfo = onClickInfo;
        imgIcon.sprite = data.icon;
        Refresh();
    }

    private void OnClickInfo()
    {
        if (GameData.userData.mastery.IsUnlocked(data.upgradeType) == false)
        {
            return;
        }

        onClickInfo?.Invoke(data);
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
            txtLv.text = "Lvl " + user.GetLevel(data.upgradeType);
        }

        // iwe gốc: đã mở AutoLootHammerCount (15) mà chưa nâng (17) → chỉ tay vào ô này.
        if (tutorial != null)
            tutorial.SetVisible(data.upgradeType == MasteryUpgradeType.AutoLootHammerCount
                && TutorialService.IsCompleted(FtueStep.UnlockMastery)
                && TutorialService.IsCompleted(FtueStep.UpgradeMastery) == false);
    }
}

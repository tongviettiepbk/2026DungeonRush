using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang Mastery: lưới toàn bộ nhánh mastery (sinh từ data) + nút mở khoá nhánh kế tiếp.
// Game gốc mở TUẦN TỰ theo MasteryConfig.Upgrades, phí = unlockGemCost của nhánh kế (tăng dần).
// Logic nằm ở MasteryService; class này chỉ gắn nút + hiển thị.
// Tutorial (MasteryTabPage gốc): tay chỉ nút mở khoá tới khi mở nhánh AutoLootHammerCount (step 15, nhánh mở đầu tiên).
public class MasteryUI : MonoBehaviour
{
    public Button BtSummonMastery;
    public TMP_Text txtQuantityGem;

    // Template ô mastery (nằm sẵn trong Content) — clone ra mỗi nhánh 1 ô, bản gốc ẩn đi.
    public GameObject objPrefabElementMastery;

    public MasterUpgradeUI infoMasterUpgradeUI;
    public TutorialUI unlockTutorial;   // UnlockTutorialUI gốc

    // TEST: true = mở khoá miễn phí (bỏ qua trừ Gem).
    public bool isTestFree = false;

    private List<ElementMasteryUI> listElementMastery;
    private Color colorEnough;

    private void Awake()
    {
        colorEnough = txtQuantityGem.color;
        BtSummonMastery.onClick.AddListener(OnClickSummonMastery);
        CreateElements();
    }

    private void OnEnable()
    {
        SyncTutorialSteps();
        Refresh();
    }

    // ivr gốc: save cũ đã mở / đã nâng nhánh AutoLootHammerCount thì coi như xong step 15 / 17.
    private static void SyncTutorialSteps()
    {
        int level = GameData.userData.mastery.GetLevel(MasteryUpgradeType.AutoLootHammerCount);
        if (level >= 1)
            TutorialService.Complete(FtueStep.UnlockMastery);
        if (level >= 2)
            TutorialService.Complete(FtueStep.UpgradeMastery);
    }

    // Sinh 1 ô cho mỗi nhánh trong config (thứ tự gốc MasteryConfig.Upgrades).
    private void CreateElements()
    {
        listElementMastery = new List<ElementMasteryUI>();
        objPrefabElementMastery.SetActive(false);

        Transform content = objPrefabElementMastery.transform.parent;
        List<MasteryUpgradeData> upgrades = GameData.staticData.mastery.upgrades;
        for (int i = 0; i < upgrades.Count; i++)
        {
            GameObject obj = Instantiate(objPrefabElementMastery, content);
            obj.SetActive(true);

            ElementMasteryUI element = obj.GetComponent<ElementMasteryUI>();
            element.SetData(upgrades[i], OnClickInfoMastery);
            listElementMastery.Add(element);
        }
    }

    public void Refresh()
    {
        for (int i = 0; i < listElementMastery.Count; i++)
        {
            listElementMastery[i].Refresh();
        }

        RefreshSummon();

        // ivs gốc.
        if (unlockTutorial != null)
            unlockTutorial.SetVisible(BtSummonMastery.gameObject.activeSelf && TutorialService.IsCompleted(FtueStep.UnlockMastery) == false);
    }

    // Nút mở khoá: hiện phí Gem của nhánh sẽ mở kế tiếp; hết nhánh khoá thì tắt nút.
    private void RefreshSummon()
    {
        MasteryUpgradeData next = GetNextLocked();
        if (next == null)
        {
            txtQuantityGem.text = "MAX";
            txtQuantityGem.color = colorEnough;
            BtSummonMastery.interactable = false;
            return;
        }

        // Game gốc: nút vẫn bấm được, thiếu Gem thì phí hiện chữ đỏ.
        // Luôn hiện phí thật; cờ TEST chỉ bỏ qua việc trừ Gem.
        int cost = MasteryService.GetUnlockCost(next.upgradeType);
        bool isEnough = isTestFree || MasteryService.CanUnlock(next.upgradeType);
        txtQuantityGem.text = cost.ToString();
        txtQuantityGem.color = isEnough ? colorEnough : Color.red;
        BtSummonMastery.interactable = true;
    }

    // Nhánh chưa mở khoá đầu tiên theo thứ tự gốc (null = đã mở hết).
    private MasteryUpgradeData GetNextLocked()
    {
        UserMasteryData user = GameData.userData.mastery;
        List<MasteryUpgradeData> upgrades = GameData.staticData.mastery.upgrades;
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (user.IsUnlocked(upgrades[i].upgradeType) == false)
            {
                return upgrades[i];
            }
        }

        return null;
    }

    // Mở popup chi tiết/nâng cấp; nâng xong thì refresh lại lưới.
    private void OnClickInfoMastery(MasteryUpgradeData data)
    {
        infoMasterUpgradeUI.Show(data, isTestFree, Refresh);
    }

    private void OnClickSummonMastery()
    {
        MasteryUpgradeData next = GetNextLocked();
        if (next == null)
        {
            UIManager.Instance.ShowToastMessage("Đã mở hết Mastery", isLocalize: false);
            return;
        }

        if (isTestFree)
        {
            // TEST: mở thẳng, không trừ Gem.
            GameData.userData.mastery.Unlock(next.upgradeType);
            GameData.Save();
        }
        else if (MasteryService.TryUnlock(next.upgradeType) == false)
        {
            UIManager.Instance.ShowToastMessage("Không đủ Gem", isLocalize: false);
            return;
        }

        // ivw gốc: mở nhánh AutoLootHammerCount = step 15.
        if (next.upgradeType == MasteryUpgradeType.AutoLootHammerCount)
            TutorialService.Complete(FtueStep.UnlockMastery);

        this.PostEvent(EventID.MasteryChanged);
        Refresh();
    }
}

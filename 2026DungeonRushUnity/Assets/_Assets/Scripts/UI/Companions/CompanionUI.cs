using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang Companion: 3 slot equip + inventory toàn bộ pet + summon (Ads / Bone nhỏ / Bone lớn) + Summon Level.
// Logic nằm ở CompanionService; class này chỉ gắn nút + hiển thị. Số liệu reverse xem DecodedData/COMPANION_MODEL.md.
public class CompanionUI : MonoBehaviour
{
    public TMP_Text txtQuantityBone;
    public List<ElementPetEquimentUI> listPetEquip;
    // Template ô inventory (nằm sẵn trong Content) — clone ra mỗi pet 1 ô, bản gốc ẩn đi.
    public GameObject objPrefabElementPetInventory;

    public Button btQuickQuip;
    public Button btUpgradeAll;

    public TMP_Text txtLvl;
    public TMP_Text txtProcess;
    public Image imgProcess;
    public Button btInfo;

    [Space(5)]
    public Button btAds;
    public TMP_Text txtQuantityAds;
    public TMP_Text txtQuantitySumonByAds;
    [Space(5)]
    public Button btSummonX100;
    public TMP_Text txtQuantityX100;
    public TMP_Text txtQuantitySumonByX100;
    [Space(5)]
    public Button btSummonX200;
    public TMP_Text txtQuantityX200;
    public TMP_Text txtQuantitySumonByX200;

    private List<ElementPetEquimentUI> listPetInventory;

    private void Awake()
    {
        btQuickQuip.onClick.AddListener(OnClickQuickEquip);
        btUpgradeAll.onClick.AddListener(OnClickUpgradeAll);
        btAds.onClick.AddListener(OnClickSummonAds);
        btSummonX100.onClick.AddListener(OnClickSummonX100);
        btSummonX200.onClick.AddListener(OnClickSummonX200);

        for (int i = 0; i < listPetEquip.Count; i++)
        {
            listPetEquip[i].Init(this);
        }

        CreateInventory();
    }

    private void OnEnable()
    {
        Refresh();
    }

    // Sinh 1 ô cho mỗi pet trong config (kể cả chưa sở hữu → hiện khoá).
    private void CreateInventory()
    {
        listPetInventory = new List<ElementPetEquimentUI>();
        objPrefabElementPetInventory.SetActive(false);

        Transform content = objPrefabElementPetInventory.transform.parent;
        List<CompanionData> companions = GameData.staticData.companions.companions;
        for (int i = 0; i < companions.Count; i++)
        {
            GameObject obj = Instantiate(objPrefabElementPetInventory, content);
            obj.SetActive(true);

            ElementPetEquimentUI element = obj.GetComponent<ElementPetEquimentUI>();
            element.Init(this);
            listPetInventory.Add(element);
        }
    }

    public void Refresh()
    {
        RefreshEquip();
        RefreshInventory();
        RefreshSummon();
        btUpgradeAll.interactable = CompanionService.CanUpgradeAny();
    }

    private void RefreshEquip()
    {
        List<string> equipped = GameData.userData.companions.GetEquipped();
        for (int i = 0; i < listPetEquip.Count; i++)
        {
            CompanionData data = i < equipped.Count ? GameData.staticData.companions.GetData(equipped[i]) : null;
            listPetEquip[i].SetData(data);
        }
    }

    // Thứ tự hiển thị: đã sở hữu trước, rồi rarity giảm dần, rồi level giảm dần.
    private void RefreshInventory()
    {
        UserCompanionData user = GameData.userData.companions;
        List<CompanionData> sorted = new List<CompanionData>(GameData.staticData.companions.companions);
        sorted.Sort((a, b) =>
        {
            bool ownA = user.IsOwned(a.assetName);
            bool ownB = user.IsOwned(b.assetName);
            if (ownA != ownB)
            {
                return ownB.CompareTo(ownA);
            }
            if (a.rarity != b.rarity)
            {
                return b.rarity.CompareTo(a.rarity);
            }
            return user.GetLevel(b.assetName).CompareTo(user.GetLevel(a.assetName));
        });

        for (int i = 0; i < listPetInventory.Count; i++)
        {
            listPetInventory[i].SetData(sorted[i]);
            listPetInventory[i].transform.SetSiblingIndex(i + 1); // +1: template ẩn đứng đầu
        }
    }

    private void RefreshSummon()
    {
        double bone = GameData.userData.items.GetQuantityHave(ItemType.BONE);
        txtQuantityBone.text = bone.ToString("0");

        // Summon Level + tiến độ trong level.
        int total = GameData.userData.companions.totalSummons;
        txtLvl.text = "Lv." + CompanionSummonLevelConfig.GetLevel(total);
        CompanionSummonLevelConfig.GetProgress(total, out int current, out int required, out bool isMax);
        if (isMax)
        {
            txtProcess.text = "MAX";
            imgProcess.fillAmount = 1f;
        }
        else
        {
            txtProcess.text = current + "/" + required;
            imgProcess.fillAmount = (float)current / required;
        }

        // Nút summon: số lượt (đã cộng Summon Capacity mastery) + giá Bone.
        txtQuantitySumonByAds.text = "x" + CompanionService.GetAdSummonCount();

        txtQuantityX100.text = CompanionSummonConfig.BONE_SMALL_COST.ToString();
        txtQuantitySumonByX100.text = "x" + CompanionService.GetBoneSmallSummonCount();
        btSummonX100.interactable = bone >= CompanionSummonConfig.BONE_SMALL_COST;

        int bigCost = CompanionService.GetBoneBigCost();
        txtQuantityX200.text = bigCost.ToString();
        txtQuantitySumonByX200.text = "x" + CompanionService.GetBoneBigSummonCount();
        btSummonX200.interactable = bone >= bigCost;
    }

    // ----- Summon -----

    private void OnClickSummonAds()
    {
        // TODO: chưa có hệ quảng cáo + giới hạn/ngày (CompanionAdSummonDailyCount gốc) → summon thẳng.
        OnSummonDone(CompanionService.Summon(CompanionService.GetAdSummonCount()));
    }

    private void OnClickSummonX100()
    {
        OnSummonDone(CompanionService.TrySummonByBone(CompanionSummonConfig.BONE_SMALL_COST,
                                                      CompanionService.GetBoneSmallSummonCount()));
    }

    private void OnClickSummonX200()
    {
        OnSummonDone(CompanionService.TrySummonByBone(CompanionService.GetBoneBigCost(),
                                                      CompanionService.GetBoneBigSummonCount()));
    }

    private void OnSummonDone(List<CompanionData> results)
    {
        if (results == null)
        {
            UIManager.Instance.ShowToastMessage("Không đủ Bone", isLocalize: false);
            return;
        }

        // TODO: panel hiển thị kết quả summon (SummonPanel gốc) — tạm toast số lượt.
        UIManager.Instance.ShowToastMessage("Summon x" + results.Count, isLocalize: false);
        Refresh();
    }

    // ----- Nâng cấp / trang bị -----

    private void OnClickQuickEquip()
    {
        CompanionService.QuickEquip();
        Refresh();
    }

    private void OnClickUpgradeAll()
    {
        CompanionService.UpgradeAll();
        Refresh();
    }

    // Click ô pet: tắt cờ New, đủ thẻ thì nâng 1 cấp.
    public void OnClickPet(CompanionData data)
    {
        UserCompanionData user = GameData.userData.companions;
        if (user.IsOwned(data.assetName) == false)
        {
            UIManager.Instance.ShowToastMessage("Chưa sở hữu", isLocalize: false);
            return;
        }

        user.ClearNew(data.assetName);
        CompanionService.TryUpgrade(data.assetName);
        GameData.Save();
        Refresh();
    }

    public void OnClickEquip(CompanionData data)
    {
        if (GameData.userData.companions.Equip(data.assetName) == false)
        {
            UIManager.Instance.ShowToastMessage("Đã đủ " + UserCompanionData.MAX_EQUIPPED + " pet", isLocalize: false);
            return;
        }

        GameData.Save();
        Refresh();
    }

    public void OnClickUnequip(CompanionData data)
    {
        GameData.userData.companions.Unequip(data.assetName);
        GameData.Save();
        Refresh();
    }
}

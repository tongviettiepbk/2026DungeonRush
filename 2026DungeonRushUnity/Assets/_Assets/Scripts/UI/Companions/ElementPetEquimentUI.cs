using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô pet — dùng chung cho 3 slot đang equip VÀ các ô trong inventory (cùng prefab CompanionPageElementUI).
//   • Slot equip trống      → chỉ hiện objAddArea.
//   • Pet chưa sở hữu        → icon + objLock.
//   • Pet đã sở hữu          → level + tiến độ thẻ nâng cấp; đang equip → objEquip + objRemove, chưa → objAddButotn.
//   • objNew                 → pet vừa nhận, chưa click xem.
public class ElementPetEquimentUI : MonoBehaviour
{
    public Image imgIcon;
    public GameObject objLock;
    public TMP_Text txtLv;

    public GameObject objEquip;
    public GameObject objAddArea;
    public GameObject objRemove;
    public GameObject objAddButotn;

    public TMP_Text txtProcess;
    public Image imgProcess;

    public GameObject objNew;
    public GameObject ObjDownArrow;

    private CompanionUI owner;
    private CompanionData data;

    public CompanionData GetData()
    {
        return data;
    }

    // Gắn listener 1 lần. Nút Add/Remove là Button nằm trên chính objAddButotn/objRemove.
    public void Init(CompanionUI owner)
    {
        this.owner = owner;

        GetComponent<Button>().onClick.AddListener(OnClickElement);
        objAddButotn.GetComponent<Button>().onClick.AddListener(OnClickAdd);
        objRemove.GetComponent<Button>().onClick.AddListener(OnClickRemove);
    }

    // data null = slot equip trống.
    public void SetData(CompanionData data)
    {
        this.data = data;

        // Tạm ẩn, chưa xử lý logic.
        ObjDownArrow.SetActive(false);

        if (data == null)
        {
            SetEmpty();
            return;
        }

        UserCompanionData user = GameData.userData.companions;
        CompanionModel model = user.GetModel(data.assetName);
        bool isOwned = model != null;
        bool isEquipped = isOwned && user.IsEquipped(data.assetName);

        objAddArea.SetActive(false);
        imgIcon.gameObject.SetActive(true);
        imgIcon.sprite = data.icon;

        objLock.SetActive(isOwned == false);
        objNew.SetActive(isOwned && model.isNew);
        objEquip.SetActive(isEquipped);
        objRemove.SetActive(isEquipped);
        objAddButotn.SetActive(isOwned && isEquipped == false);

        txtLv.gameObject.SetActive(isOwned);
        txtProcess.gameObject.SetActive(isOwned);
        imgProcess.gameObject.SetActive(isOwned);
        if (isOwned)
        {
            txtLv.text = "Lv." + model.level;
            SetUpgradeProgress(model);
        }
    }

    private void SetEmpty()
    {
        objAddArea.SetActive(true);
        imgIcon.gameObject.SetActive(false);
        objLock.SetActive(false);
        objNew.SetActive(false);
        objEquip.SetActive(false);
        objRemove.SetActive(false);
        objAddButotn.SetActive(false);
        txtLv.gameObject.SetActive(false);
        txtProcess.gameObject.SetActive(false);
        imgProcess.gameObject.SetActive(false);
    }

    // Tiến độ thẻ: cardCount / số thẻ cần lên cấp kế.
    private void SetUpgradeProgress(CompanionModel model)
    {
        if (model.level >= CompanionUpgradeConfig.MAX_LEVEL)
        {
            txtProcess.text = "MAX";
            imgProcess.fillAmount = 1f;
            return;
        }

        int need = CompanionUpgradeConfig.GetCardsRequired(model.level);
        txtProcess.text = model.cardCount + "/" + need;
        imgProcess.fillAmount = Mathf.Clamp01((float)model.cardCount / need);
    }

    private void OnClickElement()
    {
        if (data != null)
        {
            owner.OnClickPet(data);
        }
    }

    private void OnClickAdd()
    {
        if (data != null)
        {
            owner.OnClickEquip(data);
        }
    }

    private void OnClickRemove()
    {
        if (data != null)
        {
            owner.OnClickUnequip(data);
        }
    }
}

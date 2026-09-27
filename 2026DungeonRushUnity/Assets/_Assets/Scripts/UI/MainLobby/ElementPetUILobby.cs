using UnityEngine;
using UnityEngine.UI;

// 1 ô pet ở thanh lobby (UIMainLobby.listElementPet) — ô thứ i = pet equip thứ i (UserCompanionData).
//   • Chưa mở hệ companion (playerLevel < COMPANION_UNLOCK_PLAYER_LEVEL) → objLock.
//   • Slot trống   → objPlusImage (bấm → mở tab Pet).
//   • Có pet       → icon + imgFillBar = tiến độ hồi chiêu của pet ĐANG TRONG TRẬN; bấm → kích hoạt
//                    ra đòn (chế độ thủ công, xem PetUnit.RequestActivate).
public class ElementPetUILobby : MonoBehaviour
{
    public Image imgIcon;
    public GameObject objPlusImage;
    public Image imgFillBar;
    public GameObject objLock;
    public GameObject objUnlock;

    private UIMainLobby owner;
    private CompanionData data;
    private bool isUnlocked;

    public CompanionData Data => data;
    public bool IsUnlocked => isUnlocked;

    // Gắn listener 1 lần. Button nằm trên root prefab (Pet1Element).
    public void Init(UIMainLobby owner)
    {
        this.owner = owner;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    // data null = slot trống.
    public void SetData(CompanionData data, bool isUnlocked)
    {
        this.data = data;
        this.isUnlocked = isUnlocked;

        objLock.SetActive(!isUnlocked);
        objUnlock.SetActive(isUnlocked);

        bool hasPet = isUnlocked && data != null;
        objPlusImage.SetActive(isUnlocked && data == null);
        imgIcon.gameObject.SetActive(hasPet);
        if (hasPet)
        {
            imgIcon.sprite = data.icon;
        }

        imgFillBar.fillAmount = 0f;
    }

    // Gọi mỗi frame từ UIMainLobby. pet null = pet chưa có trong trận (VD vừa equip, chờ màn sau).
    public void UpdateCooldown(PetUnit pet)
    {
        imgFillBar.fillAmount = pet != null ? pet.CooldownProgress : 0f;
    }

    private void OnClick()
    {
        owner.OnClickPetSlot(this);
    }
}

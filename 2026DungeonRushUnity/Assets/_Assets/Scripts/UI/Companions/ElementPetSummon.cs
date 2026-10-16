using UnityEngine;
using UnityEngine.UI;

// 1 ô kết quả trong UISumonPet: icon pet vừa summon ra + chữ NEW khi lần đầu sở hữu (CompanionElementUI.newTextObj gốc).
public class ElementPetSummon : MonoBehaviour
{
    public Image iconPet;
    public GameObject objNew;

    public void SetData(CompanionSummonResult result)
    {
        // TODO: khung/màu theo rarity — chưa có bảng màu gốc (giống UIPetInfo).
        iconPet.sprite = result.data.icon;
        if (objNew != null)
            objNew.SetActive(result.isNew);
    }
}

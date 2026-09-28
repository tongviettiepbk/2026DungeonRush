using UnityEngine;
using UnityEngine.UI;

// 1 ô kết quả trong UISumonPet: icon pet vừa summon ra.
public class ElementPetSummon : MonoBehaviour
{
    public Image iconPet;

    public void SetData(CompanionData data)
    {
        // TODO: khung/màu theo rarity — chưa có bảng màu gốc (giống UIPetInfo).
        iconPet.sprite = data.icon;
    }
}

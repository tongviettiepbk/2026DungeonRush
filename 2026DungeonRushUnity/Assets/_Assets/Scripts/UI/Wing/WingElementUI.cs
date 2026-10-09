using UnityEngine;
using UnityEngine.UI;

// 1 ô wing trong carousel trang Wing (WingElementUI gốc, prefab WingElementUIPrefab).
// Icon = WingData.icon; GlowImage màu rarity, chỉ bật ở ô đang chọn (kzv/kzw).
public class WingElementUI : MonoBehaviour
{
    public Image imgIcon;
    public Image imgGlow;

    public WingData Data { get; private set; }
    public WingModel Model { get; private set; }

    // kzu
    public void SetData(WingData data, WingModel model)
    {
        Data = data;
        Model = model;
        imgIcon.sprite = data.icon;
        imgGlow.color = UITabWing.GetRarityColor(data.rarity);
    }

    public void SetSelected(bool isSelected)
    {
        imgGlow.gameObject.SetActive(isSelected);
    }
}

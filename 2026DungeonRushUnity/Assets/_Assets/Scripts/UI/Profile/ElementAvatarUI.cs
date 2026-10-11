using System;
using UnityEngine;
using UnityEngine.UI;

// 1 ô avatar trong popup chọn avatar (AvatarItemUI gốc): hình + dấu tích ở ô đang dùng.
public class ElementAvatarUI : MonoBehaviour
{
    public Image imgAvatar;
    public GameObject objSelected;
    public Button btSelect;

    private int avatarId;
    private Action<int> onClick;

    private void Awake()
    {
        btSelect.onClick.AddListener(() => onClick?.Invoke(avatarId));
    }

    // fig gốc.
    public void Show(int avatarId, Sprite sprite, bool isSelected, Action<int> onClick)
    {
        this.avatarId = avatarId;
        this.onClick = onClick;
        imgAvatar.sprite = sprite;
        objSelected.SetActive(isSelected);
    }
}

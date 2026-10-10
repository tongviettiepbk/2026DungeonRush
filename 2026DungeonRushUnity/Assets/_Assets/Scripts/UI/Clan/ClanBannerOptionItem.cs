using System;
using UnityEngine;
using UnityEngine.UI;

// 1 ô lựa chọn trong ClanBannerEditorPopup (ClanBannerOptionItem gốc): ô màu hoặc ô hình, khung chọn.
public class ClanBannerOptionItem : MonoBehaviour
{
    public Image ColorPreviewImage;
    public Image SpritePreviewImage;
    public GameObject SelectedIndicator;
    public Button Button;

    private int index;
    private Action<int> onClick;

    private void Awake()
    {
        if (Button != null) Button.onClick.AddListener(() => onClick?.Invoke(index));
    }

    // fsu gốc: (index, sprite, color, isColorOption, selected, callback).
    public void Setup(int index, Sprite sprite, Color color, bool isColorOption, bool selected, Action<int> onClick)
    {
        this.index = index;
        this.onClick = onClick;
        if (ColorPreviewImage != null)
        {
            ColorPreviewImage.gameObject.SetActive(isColorOption);
            ColorPreviewImage.color = color;
        }
        if (SpritePreviewImage != null)
        {
            SpritePreviewImage.gameObject.SetActive(!isColorOption);
            SpritePreviewImage.sprite = sprite;
            SpritePreviewImage.color = Color.white;
            SpritePreviewImage.preserveAspect = true;
        }
        SetSelected(selected);
    }

    // fsv gốc.
    public void SetSelected(bool selected)
    {
        if (SelectedIndicator != null) SelectedIndicator.SetActive(selected);
    }
}

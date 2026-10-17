using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô ngôn ngữ trong popup chọn ngôn ngữ (LanguageItemUI gốc): cờ, tên ngôn ngữ (viết bằng chính ngôn ngữ đó),
// dấu tích ở ô đang dùng.
public class ElementLanguageUI : MonoBehaviour
{
    public Image imgFlag;
    public TMP_Text txtName;
    public GameObject objSelected;
    public Button btSelect;

    private string code;
    private Action<string> onClick;

    // kme gốc: cờ null thì tắt Image cờ.
    public void Show(string code, string displayName, Sprite flag, bool isSelected, Action<string> onClick)
    {
        this.code = code;
        this.onClick = onClick;
        if (imgFlag != null)
        {
            if (flag != null)
                imgFlag.sprite = flag;
            imgFlag.enabled = flag != null;
        }
        if (txtName != null)
            txtName.text = displayName;
        SetSelected(isSelected);
        btSelect.onClick.RemoveAllListeners();
        btSelect.onClick.AddListener(() => this.onClick?.Invoke(this.code));
    }

    // kmf gốc.
    public void SetSelected(bool isSelected)
    {
        if (objSelected != null)
            objSelected.SetActive(isSelected);
    }
}

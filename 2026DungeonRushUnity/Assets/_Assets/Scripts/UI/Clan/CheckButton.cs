using System;
using UnityEngine;
using UnityEngine.UI;

// Ô tick (CheckButton gốc): bấm đảo trạng thái, CheckImage hiện khi bật.
public class CheckButton : MonoBehaviour
{
    public Image CheckImage;
    public Action CheckChanged;

    private bool isChecked;

    public bool IsChecked => isChecked;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnButtonClicked);
        Refresh();
    }

    // jxw gốc (không phát CheckChanged).
    public void SetChecked(bool value)
    {
        isChecked = value;
        Refresh();
    }

    public void OnButtonClicked()
    {
        isChecked = !isChecked;
        Refresh();
        CheckChanged?.Invoke();
    }

    private void Refresh()
    {
        if (CheckImage != null) CheckImage.enabled = isChecked;
    }
}

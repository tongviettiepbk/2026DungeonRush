using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 tab trong TabSelector (TabSelectorItem gốc, prefab TabSelectorItemUI).
public class TabSelectorItem : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private Image _icon;
    [SerializeField] private Color _selectedColor = Color.white;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _disabledColor = new Color(0.5377358f, 0.5377358f, 0.5377358f, 1f);

    private int index;
    private Action<int> onClick;
    private bool isSelected;
    private bool isInteractable = true;

    public RectTransform RectTransform => (RectTransform)transform;

    // kwg gốc.
    public void Init(int index, string label, Action<int> onClick)
    {
        this.index = index;
        this.onClick = onClick;
        SetLabel(label);
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => { if (isInteractable) this.onClick?.Invoke(this.index); });
        }
    }

    public void SetLabel(string label)
    {
        if (_label != null) _label.text = label;
    }

    public void SetIcon(Sprite sprite)
    {
        if (_icon == null) return;
        _icon.sprite = sprite;
        _icon.enabled = sprite != null;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        Refresh();
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        Refresh();
    }

    private void Refresh()
    {
        if (_label != null) _label.color = !isInteractable ? _disabledColor : (isSelected ? _selectedColor : _normalColor);
    }
}

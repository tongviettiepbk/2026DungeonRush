using System;
using System.Collections.Generic;
using UnityEngine;

// Thanh chọn tab (TabSelector gốc): sinh TabSelectorItem từ _itemPrefab vào _itemContainer, khung _selectorTransform
// trượt theo tab đang chọn. Dùng cho Members|Battle (ClanTabPage) và Daily|Weekly (ClanContributionLeaderboardPopup).
public class TabSelector : MonoBehaviour
{
    [SerializeField] private RectTransform _selectorTransform;
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private TabSelectorItem _itemPrefab;

    public event Action<int> OnTabSelected;

    private readonly List<TabSelectorItem> items = new List<TabSelectorItem>();
    private int selectedIndex = -1;

    public int SelectedIndex => selectedIndex;

    // kvy gốc.
    public void Setup(string[] labels, int selected = 0)
    {
        Clear();
        if (_itemPrefab != null) _itemPrefab.gameObject.SetActive(false);
        for (int i = 0; i < labels.Length; i++)
        {
            TabSelectorItem item = Instantiate(_itemPrefab, _itemContainer);
            item.gameObject.SetActive(true);
            item.Init(i, labels[i], OnItemClicked);
            items.Add(item);
        }
        Select(selected, false);
    }

    // kvz gốc.
    public void Select(int index, bool notify = true)
    {
        if (items.Count == 0) return;
        selectedIndex = Mathf.Clamp(index, 0, items.Count - 1);
        for (int i = 0; i < items.Count; i++) items[i].SetSelected(i == selectedIndex);
        if (notify) OnTabSelected?.Invoke(selectedIndex);
    }

    public void SetLabel(int index, string label)
    {
        if (index >= 0 && index < items.Count) items[index].SetLabel(label);
    }

    public void SetInteractable(int index, bool interactable)
    {
        if (index >= 0 && index < items.Count) items[index].SetInteractable(interactable);
    }

    private void OnItemClicked(int index)
    {
        if (index == selectedIndex) return;
        Select(index);
    }

    // Khung chọn bám tab (sau khi layout chia bề rộng).
    private void LateUpdate()
    {
        if (_selectorTransform == null || selectedIndex < 0 || selectedIndex >= items.Count) return;
        RectTransform tab = items[selectedIndex].RectTransform;
        if (tab.rect.width <= 0f) return;
        _selectorTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tab.rect.width);
        _selectorTransform.position = new Vector3(tab.position.x, _selectorTransform.position.y, _selectorTransform.position.z);
    }

    private void Clear()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null) Destroy(items[i].gameObject);
        }
        items.Clear();
        selectedIndex = -1;
    }
}

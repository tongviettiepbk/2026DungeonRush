using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Clan Banner Editor" (ClanBannerEditorPopup gốc): xem trước + 4 lưới chọn Shape / Background Color / Emblem Shape /
// Emblem Color sinh từ ClanBannerCatalog, Apply → trả banner về popup gọi.
public class UIClanBannerEditorPopup : BaseUI
{
    public ClanBannerView Preview;
    public ClanBannerCatalog Catalog;
    public Transform BackgroundTypeContent;
    public Transform BackgroundColorContent;
    public Transform ImageTypeContent;
    public Transform ImageColorContent;
    public ClanBannerOptionItem OptionPrefab;
    public Button ApplyButton;
    public Button CloseButton;
    public TMP_Text TitleText;
    public TMP_Text ShapeLabel;
    public TMP_Text BackgroundColorLabel;
    public TMP_Text EmblemShapeLabel;
    public TMP_Text EmblemColorLabel;
    public TMP_Text ApplyButtonText;

    private ClanBannerData data = new ClanBannerData();
    private Action<ClanBannerData> onApply;
    private readonly List<ClanBannerOptionItem> backgroundTypes = new List<ClanBannerOptionItem>();
    private readonly List<ClanBannerOptionItem> backgroundColors = new List<ClanBannerOptionItem>();
    private readonly List<ClanBannerOptionItem> imageTypes = new List<ClanBannerOptionItem>();
    private readonly List<ClanBannerOptionItem> imageColors = new List<ClanBannerOptionItem>();
    private bool isBuilt;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "Clan Banner Editor");
        ClanUIUtil.SetText(ShapeLabel, "Shape");
        ClanUIUtil.SetText(BackgroundColorLabel, "Background Color");
        ClanUIUtil.SetText(EmblemShapeLabel, "Emblem Shape");
        ClanUIUtil.SetText(EmblemColorLabel, "Emblem Color");
        ClanUIUtil.SetText(ApplyButtonText, "Apply");
        if (ApplyButton != null) ApplyButton.onClick.AddListener(OnApplyClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        if (OptionPrefab != null) OptionPrefab.gameObject.SetActive(false);
    }

    // fsj gốc.
    public void Setup(ClanBannerData current, Action<ClanBannerData> onApply)
    {
        data = current != null ? current.Clone() : new ClanBannerData();
        this.onApply = onApply;
        if (!isBuilt) Build();
        RefreshSelection();
        RefreshPreview();
    }

    // fsk gốc.
    private void Build()
    {
        isBuilt = true;
        if (Catalog == null || OptionPrefab == null) return;
        for (int i = 0; i < Catalog.BackgroundTypeCount; i++)
            backgroundTypes.Add(NewOption(BackgroundTypeContent, i, Catalog.GetBackground(i), Color.white, false, OnBackgroundType));
        for (int i = 0; i < Catalog.BackgroundColorCount; i++)
            backgroundColors.Add(NewOption(BackgroundColorContent, i, null, Catalog.GetBackgroundColor(i), true, OnBackgroundColor));
        for (int i = 0; i < Catalog.ImageTypeCount; i++)
            imageTypes.Add(NewOption(ImageTypeContent, i, Catalog.GetImage(i), Color.white, false, OnImageType));
        for (int i = 0; i < Catalog.ImageColorCount; i++)
            imageColors.Add(NewOption(ImageColorContent, i, null, Catalog.GetImageColor(i), true, OnImageColor));
    }

    private ClanBannerOptionItem NewOption(Transform parent, int index, Sprite sprite, Color color, bool isColor, Action<int> onClick)
    {
        ClanBannerOptionItem item = Instantiate(OptionPrefab, parent);
        item.gameObject.SetActive(true);
        item.Setup(index, sprite, color, isColor, false, onClick);
        return item;
    }

    private void OnBackgroundType(int i) { data.BackgroundTypeId = i; RefreshSelection(); RefreshPreview(); }
    private void OnBackgroundColor(int i) { data.BackgroundColorId = i; RefreshSelection(); RefreshPreview(); }
    private void OnImageType(int i) { data.ImageTypeId = i; RefreshSelection(); RefreshPreview(); }
    private void OnImageColor(int i) { data.ImageColorId = i; RefreshSelection(); RefreshPreview(); }

    // fsp gốc.
    private void RefreshSelection()
    {
        Mark(backgroundTypes, data.BackgroundTypeId);
        Mark(backgroundColors, data.BackgroundColorId);
        Mark(imageTypes, data.ImageTypeId);
        Mark(imageColors, data.ImageColorId);
    }

    private static void Mark(List<ClanBannerOptionItem> list, int selected)
    {
        for (int i = 0; i < list.Count; i++) list[i].SetSelected(i == selected);
    }

    private void RefreshPreview()
    {
        if (Preview != null) Preview.SetBanner(data);
    }

    private void OnApplyClicked()
    {
        onApply?.Invoke(data.Clone());
        Close();
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô cape (CapePageElementUI gốc) — dùng ở kho UICapePopup, kết quả summon, Detail/Salvage popup.
// Nền theo rarity, icon, "Lvl N", dấu Equipped, dấu tick khi được chọn làm nguyên liệu Salvage.
public class CapeElementUI : MonoBehaviour
{
    private static readonly Color DISABLED_COLOR = new Color(0.5f, 0.5f, 0.5f, 1f);

    public Button btElement;
    public Image imgBackground;
    public Image imgIcon;
    public TMP_Text txtLevel;
    public GameObject objEquipped;
    public GameObject objCheck;

    public CapeData Data { get; private set; }
    public CapeModel Model { get; private set; }

    private Action<CapeElementUI> onClick;

    private void Awake()
    {
        btElement.onClick.AddListener(() => onClick?.Invoke(this));
    }

    public void Init(Action<CapeElementUI> onClick)
    {
        this.onClick = onClick;
    }

    // esm
    public void SetData(CapeData data, CapeModel model, bool isEquipped)
    {
        Data = data;
        Model = model;
        Sprite bg = RarityBackgroundConfig.Get(data.rarity);
        if (bg != null)
        {
            imgBackground.sprite = bg;
        }
        imgIcon.sprite = data.icon;
        txtLevel.text = "Lvl " + model.level;
        objEquipped.SetActive(isEquipped);
        SetChecked(false);
        SetInteractable(true);
    }

    // esl
    public void SetChecked(bool isChecked)
    {
        if (objCheck != null)
        {
            objCheck.SetActive(isChecked);
        }
    }

    // esn: ô không chọn được (cape đang mặc trong Salvage) — tắt nút + làm xám.
    public void SetInteractable(bool isInteractable)
    {
        btElement.interactable = isInteractable;
        imgBackground.color = isInteractable ? Color.white : DISABLED_COLOR;
        imgIcon.color = isInteractable ? Color.white : DISABLED_COLOR;
    }
}

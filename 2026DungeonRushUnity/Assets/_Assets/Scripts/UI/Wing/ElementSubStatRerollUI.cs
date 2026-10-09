using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng substat có nút khoá trong popup Reroll Wing (SubStatUIElement gốc, prefab SubstatUIElementPrefab).
// Khoá = giữ nguyên dòng khi reroll; bấm nút đổi icon khoá + màu nền rồi báo popup tính lại giá (kxo).
public class ElementSubStatRerollUI : MonoBehaviour
{
    public Button btLock;
    public Image imgLockIcon;
    public Image imgBackground;
    public Image imgFlash;
    public TMP_Text txtStat;
    public Color colorLocked = new Color(0.50980395f, 0.50980395f, 0.50980395f, 1f);
    public Color colorUnlocked = new Color(0.7137255f, 0.7294118f, 0.23921569f, 1f);
    public Sprite spriteLocked;
    public Sprite spriteUnlocked;

    public bool IsLocked { get; private set; }

    private Action<ElementSubStatRerollUI> onClickLock;

    private void Awake()
    {
        btLock.onClick.AddListener(OnClickLock);
    }

    // kxl: dòng mới luôn ở trạng thái mở khoá.
    public void Init(GearSubStat subStat, Action<ElementSubStatRerollUI> onClickLock)
    {
        this.onClickLock = onClickLock;
        IsLocked = false;
        txtStat.text = UITabWing.GetSubStatText(subStat);
        imgLockIcon.sprite = spriteUnlocked;
        imgBackground.color = colorUnlocked;
        if (imgFlash != null)
        {
            imgFlash.DOKill();
            imgFlash.color = new Color(imgFlash.color.r, imgFlash.color.g, imgFlash.color.b, 0f);
        }
    }

    // kxm: đổi chữ sau khi reroll + chớp sáng.
    public void SetData(GearSubStat subStat, float delay)
    {
        txtStat.text = UITabWing.GetSubStatText(subStat);
        if (imgFlash == null)
        {
            return;
        }

        imgFlash.DOKill();
        Color color = imgFlash.color;
        imgFlash.color = new Color(color.r, color.g, color.b, 0f);
        DOTween.Sequence().SetTarget(imgFlash)
            .AppendInterval(delay)
            .Append(imgFlash.DOFade(1f, 0.125f))
            .Append(imgFlash.DOFade(0f, 0.25f));
    }

    // kxn
    public void SetInteractable(bool isInteractable)
    {
        btLock.interactable = isInteractable;
    }

    // kxo
    private void OnClickLock()
    {
        IsLocked = !IsLocked;
        imgLockIcon.sprite = IsLocked ? spriteLocked : spriteUnlocked;
        imgBackground.color = IsLocked ? colorLocked : colorUnlocked;
        onClickLock?.Invoke(this);
    }
}

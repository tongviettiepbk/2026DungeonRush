using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 1 tab ở thanh tab đáy lobby (TabBarElementUI gốc). Chưa tới TabData.UnlockPlayerLevel thì khoá:
// icon đổi sang hình khoá màu tối (kul gốc), không chỉ tay tutorial; bấm báo "Unlock at Level N" (TabBarUI.kvc).
public class ElementTabMenuUILobby : MonoBehaviour
{
    private static readonly Color LOCKED_ICON_COLOR = new Color(0.1019608f, 0.1019608f, 0.1019608f, 1f);   // zcj gốc
    private static readonly Vector3 SELECT_BOUNCE_FROM = new Vector3(1.1f, 0.75f, 1f);
    private const float SELECT_BOUNCE_DURATION = 0.35f;

    public Button btTab;
    public GameObject objNotice;
    public GameObject objSelect;
    public TutorialUI tutorial;   // TabBarElementUI.TutorialUI gốc
    public Image imgIcon;
    public Sprite sprLocked;      // TabData.LockedIconSprite gốc

    private bool wantTutorial;
    private Sprite sprIcon;
    private Color iconColor;
    private bool isIconCached;

    public bool isLocked { get; private set; }

    public TypeMenuLobby typeMenu { get; private set; }
    public bool isOpen { get; private set; }

    // UIMainLobby gán loại tab + callback khi bấm nút.
    public void Init(TypeMenuLobby type, Action<TypeMenuLobby> onClick)
    {
        typeMenu = type;
        if (btTab != null)
        {
            btTab.onClick.RemoveAllListeners();
            btTab.onClick.AddListener(() => onClick?.Invoke(typeMenu));
        }
    }

    // Trạng thái mở/đóng: mở -> bật objSelect, đóng -> tắt.
    public void SetOpen(bool open)
    {
        bool justOpened = open && isOpen == false;
        isOpen = open;
        if (objSelect != null)
        {
            objSelect.SetActive(open);
            if (justOpened)
                PlaySelectBounce();
        }
        RefreshTutorial();
    }

    // Ô vàng hiện ra thì nhún: bẹp ngang rồi bật về 1 (OutBack vọt quá chút cho dẻo).
    private void PlaySelectBounce()
    {
        Transform tf = objSelect.transform;
        tf.DOKill();
        tf.localScale = SELECT_BOUNCE_FROM;
        tf.DOScale(Vector3.one, SELECT_BOUNCE_DURATION).SetEase(Ease.OutBack, 3f);
    }

    // TabBarElementUI.kup gốc: tab đang mở thì không chỉ tay.
    public void SetTutorial(bool want)
    {
        wantTutorial = want;
        RefreshTutorial();
    }

    private void RefreshTutorial()
    {
        if (tutorial != null)
            tutorial.SetVisible(wantTutorial && isOpen == false && isLocked == false);
    }

    // kuk + kul gốc.
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (imgIcon != null)
        {
            if (isIconCached == false)
            {
                isIconCached = true;
                sprIcon = imgIcon.sprite;
                iconColor = imgIcon.color;
            }
            imgIcon.sprite = locked && sprLocked != null ? sprLocked : sprIcon;
            imgIcon.color = locked ? LOCKED_ICON_COLOR : iconColor;
        }
        RefreshTutorial();
    }
}

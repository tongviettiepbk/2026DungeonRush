using System;
using UnityEngine;
using UnityEngine.UI;

public class ElementTabMenuUILobby : MonoBehaviour
{
    public Button btTab;
    public GameObject objNotice;
    public GameObject objSelect;
    public TutorialUI tutorial;   // TabBarElementUI.TutorialUI gốc

    private bool wantTutorial;

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
        isOpen = open;
        if (objSelect != null)
            objSelect.SetActive(open);
        RefreshTutorial();
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
            tutorial.SetVisible(wantTutorial && isOpen == false);
    }
}

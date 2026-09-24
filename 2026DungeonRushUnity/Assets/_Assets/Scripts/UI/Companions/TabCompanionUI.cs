using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITabCompanion : MonoBehaviour
{
    public GameObject objCompanion;
    public GameObject objMasteryContent;

    public Button btPetSelect;
    public GameObject objSelectPet;

    public Button btMasterySelect;
    public GameObject objSelectMastery;

    private void Awake()
    {
        btPetSelect.onClick.AddListener(OnClickPetSelect);
        btMasterySelect.onClick.AddListener(OnClickMasterySelect);
    }

    // Mỗi lần mở tab mặc định hiện phần Companion
    private void OnEnable()
    {
        ShowContent(true);
    }

    private void OnClickPetSelect()
    {
        ShowContent(true);
    }

    private void OnClickMasterySelect()
    {
        ShowContent(false);
    }

    // Bật 1 content + selector tương ứng, tắt phần còn lại
    private void ShowContent(bool isCompanion)
    {
        objCompanion.SetActive(isCompanion);
        objSelectPet.SetActive(isCompanion);

        objMasteryContent.SetActive(!isCompanion);
        objSelectMastery.SetActive(!isCompanion);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Popup kết quả summon pet (SummonPanel gốc) — mở từ 3 nút summon của CompanionUI.
// Mỗi lượt ra 1 ô (ElementPetSummon) theo đúng thứ tự roll; bấm nền (btCloseAll) để đóng.
// objPrefabElementPet là template nằm sẵn trong transParent (ẩn) — clone ra và tái dùng giữa các lần mở.
public class UISumonPet : BaseUI
{
    public Button btCloseAll;
    public Transform transParent;
    public GameObject objPrefabElementPet;

    private readonly List<ElementPetSummon> listElement = new List<ElementPetSummon>();

    protected override void Awake()
    {
        base.Awake();
        btCloseAll.onClick.AddListener(Close);
        objPrefabElementPet.SetActive(false);
    }

    public void Show(List<CompanionData> results)
    {
        for (int i = 0; i < results.Count; i++)
        {
            if (i >= listElement.Count)
            {
                GameObject obj = Instantiate(objPrefabElementPet, transParent);
                listElement.Add(obj.GetComponent<ElementPetSummon>());
            }

            listElement[i].gameObject.SetActive(true);
            listElement[i].SetData(results[i]);
        }

        for (int i = results.Count; i < listElement.Count; i++)
        {
            listElement[i].gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }
}

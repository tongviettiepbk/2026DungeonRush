using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Popup kết quả summon relic (SummonResultPanel gốc) — mở từ nút Summon trang Enchantment.
// Mỗi lượt ra 1 ô (ElementEchantmentUI) theo đúng thứ tự roll; bấm nền (btCloseAll) để đóng.
// objPrefabElement là template nằm sẵn trong transParent (ẩn) — clone ra và tái dùng giữa các lần mở.
public class UISumonEnchantment : BaseUI
{
    public Button btCloseAll;
    public Transform transParent;
    public GameObject objPrefabElement;

    private readonly List<ElementEchantmentUI> listElement = new List<ElementEchantmentUI>();

    protected override void Awake()
    {
        base.Awake();
        btCloseAll.onClick.AddListener(Close);
        objPrefabElement.SetActive(false);
    }

    public void Show(List<int> tiers)
    {
        for (int i = 0; i < tiers.Count; i++)
        {
            if (i >= listElement.Count)
            {
                GameObject obj = Instantiate(objPrefabElement, transParent);
                listElement.Add(obj.GetComponent<ElementEchantmentUI>());
            }

            listElement[i].gameObject.SetActive(true);
            listElement[i].SetData(tiers[i]);
        }

        for (int i = tiers.Count; i < listElement.Count; i++)
        {
            listElement[i].gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }
}

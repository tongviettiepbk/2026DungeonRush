using UnityEngine;

// Tab Shop ở lobby (PageShop trong scene MainGame). Nạp StoreTabPage (prefab dựng từ bản gốc) lần đầu mở.
public class UITabShop : MonoBehaviour
{
    private const string PAGE_PATH = "Prefabs/UI/StoreTabPage";

    private StoreTabPage page;

    private void OnEnable()
    {
        if (page != null) return;

        StoreTabPage prefab = Resources.Load<StoreTabPage>(PAGE_PATH);
        if (prefab == null)
        {
            DebugCustom.LogError("[UITabShop] Thiếu prefab Resources/" + PAGE_PATH);
            return;
        }
        page = Instantiate(prefab, transform);
        RectTransform rt = (RectTransform)page.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

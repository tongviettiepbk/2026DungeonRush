using UnityEngine;

// Tab Clan ở lobby (ClanPage trong scene MainGame). Nạp ClanTabPage (prefab dựng từ bản gốc) lần đầu mở;
// chưa tới PlayerLevel 15 (ClanController.vhk gốc) thì báo + đóng tab.
public class UITabClan : MonoBehaviour
{
    private const string PAGE_PATH = "Prefabs/UI/ClanTabPage";

    private ClanTabPage page;

    private void OnEnable()
    {
        if (!ClanController.IsUnlocked())
        {
            UIManager.Instance.ShowToastMessage("Unlock at level " + StaticClanData.UNLOCK_PLAYER_LEVEL, isLocalize: false);
            GameController.Instance.uiLobby.CloseAllTabs();
            return;
        }
        if (page == null)
        {
            ClanTabPage prefab = Resources.Load<ClanTabPage>(PAGE_PATH);
            if (prefab == null)
            {
                DebugCustom.LogError("[UITabClan] Thiếu prefab Resources/" + PAGE_PATH);
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
}

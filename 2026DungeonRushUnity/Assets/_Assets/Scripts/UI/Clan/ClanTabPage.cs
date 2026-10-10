using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Trang tab Clan ở lobby (ClanTabPage gốc). Chưa có clan → ClanSearchPanel; có clan → TabSelector Members | Battle.
// Tự đổi khi ClanController.ClanUpdated (vào/rời/bị kick). PendingOpenWarTab: sau trận PvP Clan War quay về tab Battle.
public class ClanTabPage : MonoBehaviour
{
    public static bool PendingOpenWarTab;

    public TabSelector TabSelector;
    public GameObject MemberBattleSwitcherRoot;
    public GameObject ClanSearchPanel;
    public ClanSearchTab SearchTab;
    public GameObject ClanMembersPanel;
    public ClanMembersTab MembersTab;
    public GameObject ClanBattlePanel;
    public ClanBattleTab BattleTab;
    public ClanLoadingView LoadingView;
    public Button CloseButton;

    private bool isInitialized;
    private bool hasClanShown;
    private bool isLoading;

    private const int TAB_MEMBERS = 0;
    private const int TAB_BATTLE = 1;

    private void Awake()
    {
        if (TabSelector != null)
        {
            TabSelector.Setup(new[] { "Members", "Battle" });
            TabSelector.OnTabSelected += OnTabSelected;
        }
        if (CloseButton != null) CloseButton.onClick.AddListener(() => GameController.Instance.uiLobby.CloseAllTabs());
        ClanController.ClanUpdated += OnClanUpdated;
    }

    private void OnDestroy()
    {
        ClanController.ClanUpdated -= OnClanUpdated;
    }

    // eji/ejj gốc: mở tab.
    private void OnEnable()
    {
        if (ClanController.Instance.CurrentClan != null || GameData.userData.clan.HasClan)
        {
            ShowHasClan(true);
        }
        else
        {
            ShowHasClan(false);
        }
        Load();
    }

    // ejk gốc: đóng tab.
    private void OnDisable()
    {
        if (SearchTab != null) SearchTab.ResetState();
    }

    // fxo gốc: tải clan của mình rồi điều hướng.
    private void Load()
    {
        if (isLoading) return;
        isLoading = true;
        if (!isInitialized && LoadingView != null) LoadingView.ShowLoading();
        ClanController.Instance.LoadMyClan((ok, clan, error) =>
        {
            isLoading = false;
            if (this == null) return;
            if (!ok)
            {
                if (LoadingView != null) LoadingView.ShowError(error ?? "Failed to load clan data", Load);
                return;
            }
            isInitialized = true;
            if (LoadingView != null) LoadingView.Hide();
            ShowHasClan(clan != null);
        });
    }

    private void OnClanUpdated(ClanModel clan)
    {
        if (this != null && gameObject.activeInHierarchy && hasClanShown != (clan != null)) ShowHasClan(clan != null);
    }

    // fxq gốc.
    private void ShowHasClan(bool hasClan)
    {
        bool changed = hasClanShown != hasClan || !isInitialized;
        hasClanShown = hasClan;
        ClanUIUtil.SetActive(ClanSearchPanel, !hasClan);
        ClanUIUtil.SetActive(MemberBattleSwitcherRoot, hasClan);
        if (!hasClan)
        {
            ClanUIUtil.SetActive(ClanMembersPanel, false);
            ClanUIUtil.SetActive(ClanBattlePanel, false);
            if (SearchTab != null) SearchTab.Show();
            return;
        }
        if (PendingOpenWarTab)
        {
            PendingOpenWarTab = false;
            SelectTab(TAB_BATTLE);
        }
        else if (changed || TabSelector == null || TabSelector.SelectedIndex < 0)
        {
            SelectTab(TAB_MEMBERS);
        }
        else
        {
            OnTabSelected(TabSelector.SelectedIndex);
        }
    }

    private void SelectTab(int index)
    {
        if (TabSelector != null) TabSelector.Select(index, false);
        OnTabSelected(index);
    }

    // fxs gốc.
    private void OnTabSelected(int index)
    {
        ClanUIUtil.SetActive(ClanMembersPanel, index == TAB_MEMBERS);
        ClanUIUtil.SetActive(ClanBattlePanel, index != TAB_MEMBERS);
        if (index == TAB_MEMBERS)
        {
            if (MembersTab != null) MembersTab.Show();
        }
        else if (BattleTab != null)
        {
            BattleTab.Show();
        }
    }

    // Sau trận PvP Clan War: chờ đổi mode xong rồi mở tab Clan > Battle.
    public static void OpenWarTabAfterModeChange()
    {
        PendingOpenWarTab = true;
        ClanWarController.Instance.StartCoroutine(RoutineOpenAfterModeChange());
    }

    private static IEnumerator RoutineOpenAfterModeChange()
    {
        yield return null;
        while (GameController.Instance.isChangingMode)
        {
            yield return null;
        }
        GameController.Instance.uiLobby.OpenTab(TypeMenuLobby.Clan);
    }
}

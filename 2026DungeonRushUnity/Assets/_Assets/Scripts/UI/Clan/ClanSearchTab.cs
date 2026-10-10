using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Màn "Join a Clan" khi chưa có clan (ClanSearchTab gốc): ô tên + nút tìm, Advanced Search, Create Clan,
// danh sách ClanSearchCard (Join / Request / Requested), trống → "No clans found".
public class ClanSearchTab : MonoBehaviour
{
    public TMP_InputField NameSearchInput;
    public Button AdvancedSearchButton;
    public Button CreateClanButton;
    public ScrollRect ScrollRect;
    public Transform ListContent;
    public ClanSearchCard CardPrefab;
    public ClanLoadingView LoadingView;
    public GameObject EmptyState;
    public TMP_Text TitleText;
    public TMP_Text AdvancedSearchButtonText;
    public TMP_Text CreateClanButtonText;
    public TMP_Text EmptyStateText;

    private ClanSearchFilter filter = new ClanSearchFilter();
    private readonly List<ClanSearchCard> cards = new List<ClanSearchCard>();
    private bool hasSearched;
    private bool isSearching;

    private static ClanController Controller => ClanController.Instance;

    private void Awake()
    {
        ClanUIUtil.SetText(TitleText, "Join a Clan");
        ClanUIUtil.SetText(AdvancedSearchButtonText, "Advanced Search");
        ClanUIUtil.SetText(CreateClanButtonText, "Create Clan");
        ClanUIUtil.SetText(EmptyStateText, "No clans found");
        if (NameSearchInput != null)
        {
            NameSearchInput.characterLimit = StaticClanData.NAME_MAX;
            NameSearchInput.onSubmit.AddListener(OnNameSubmitted);
            if (NameSearchInput.placeholder is TMP_Text ph) ph.text = "Search for a clan...";
        }
        if (AdvancedSearchButton != null) AdvancedSearchButton.onClick.AddListener(OnAdvancedSearchClicked);
        if (CreateClanButton != null) CreateClanButton.onClick.AddListener(OnCreateClicked);
        // Nút kính lúp cạnh ô tên (không phải field gốc — PressButtonUI) → tìm theo tên.
        Transform searchButton = transform.Find("TopControls/SearchParent/ClanTabPageSearchButton");
        if (searchButton != null && searchButton.GetComponent<Button>() != null)
        {
            searchButton.GetComponent<Button>().onClick.AddListener(() => OnNameSubmitted(NameSearchInput != null ? NameSearchInput.text : string.Empty));
        }
        if (CardPrefab != null) CardPrefab.gameObject.SetActive(false);
    }

    // Show gốc: lần đầu mở → tìm với bộ lọc hiện tại.
    public void Show()
    {
        gameObject.SetActive(true);
        if (!hasSearched) Refresh();
    }

    // fwt gốc (đóng tab).
    public void ResetState()
    {
        hasSearched = false;
    }

    // fwz gốc: áp bộ lọc từ AdvancedSearchPopup.
    public void ApplyFilter(ClanSearchFilter newFilter)
    {
        filter = newFilter ?? new ClanSearchFilter();
        if (NameSearchInput != null) NameSearchInput.SetTextWithoutNotify(filter.ClanName ?? string.Empty);
        Refresh();
    }

    public void Refresh()
    {
        if (isSearching) return;
        isSearching = true;
        hasSearched = true;
        ClanUIUtil.SetActive(EmptyState, false);
        if (LoadingView != null) LoadingView.ShowLoading();
        Controller.Search(filter, OnSearchResult);
    }

    // fxe gốc.
    private void OnSearchResult(bool ok, List<ClanModel> clans, List<string> requested, string error)
    {
        isSearching = false;
        if (this == null) return;
        if (!ok)
        {
            if (LoadingView != null) LoadingView.ShowError(error ?? "Search failed", Refresh);
            return;
        }
        if (LoadingView != null) LoadingView.Hide();
        Draw(clans, requested);
    }

    // fxa gốc.
    private void Draw(List<ClanModel> clans, List<string> requested)
    {
        Clear();
        for (int i = 0; i < clans.Count; i++)
        {
            ClanSearchCard card = Instantiate(CardPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Setup(clans[i], requested.Contains(clans[i].ClanId), c => OnJoin(c, card), c => OnRequest(c, card), OnCardClicked);
            cards.Add(card);
        }
        ClanUIUtil.SetActive(EmptyState, clans.Count == 0);
        if (ScrollRect != null) ScrollRect.verticalNormalizedPosition = 1f;
    }

    // fxb gốc: Join clan Open.
    private void OnJoin(ClanModel clan, ClanSearchCard card)
    {
        card.SetBusy(true);
        Controller.JoinOpen(clan.ClanId, (ok, joined, error) =>
        {
            if (card != null) card.SetBusy(false);
            if (!ok) ClanUIUtil.Toast(error);
            // Thành công → ClanController.ClanUpdated → ClanTabPage chuyển sang Members.
        });
    }

    // fxc gốc: xin vào clan Approval Only.
    private void OnRequest(ClanModel clan, ClanSearchCard card)
    {
        card.SetBusy(true);
        Controller.RequestJoin(clan.ClanId, (ok, error) =>
        {
            if (card == null) return;
            card.SetBusy(false);
            if (ok) card.SetRequested(true);
            else ClanUIUtil.Toast(error);
        });
    }

    private void OnCardClicked(ClanModel clan)
    {
        UIClanInfoPopup popup = UIManager.Instance.LoadUI(UIKey.ClanInfoPopup) as UIClanInfoPopup;
        ClanSearchCard card = cards.Find(c => c.Clan == clan);
        if (popup != null) popup.Setup(clan, card != null && IsRequested(card), joinedOrRequested =>
        {
            if (card != null && joinedOrRequested) card.SetRequested(true);
        });
    }

    private static bool IsRequested(ClanSearchCard card)
    {
        return card.RequestedState != null && card.RequestedState.activeSelf;
    }

    private void OnNameSubmitted(string text)
    {
        filter.ClanName = (text ?? string.Empty).Trim();
        Refresh();
    }

    private void OnAdvancedSearchClicked()
    {
        UIAdvancedSearchPopup popup = UIManager.Instance.LoadUI(UIKey.AdvancedSearchPopup) as UIAdvancedSearchPopup;
        if (popup != null) popup.Setup(filter, ApplyFilter);
    }

    private void OnCreateClicked()
    {
        UIManager.Instance.LoadUI(UIKey.CreateClanPopup);
    }

    private void Clear()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
    }
}

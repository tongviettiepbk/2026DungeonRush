using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Requests" (ClanRequestsPopup gốc): danh sách đơn xin vào, Accept / Deny; trống → "No pending requests".
public class UIClanRequestsPopup : BaseUI
{
    public ScrollRect ScrollRect;
    public Transform ListContent;
    public ClanRequestCard CardPrefab;
    public ClanLoadingView LoadingView;
    public GameObject EmptyState;
    public Button CloseButton;
    public TMP_Text TitleText;
    public TMP_Text EmptyStateText;

    private readonly List<ClanRequestCard> cards = new List<ClanRequestCard>();
    private Action onChanged;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "Requests");
        ClanUIUtil.SetText(EmptyStateText, "No pending requests");
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        if (CardPrefab != null) CardPrefab.gameObject.SetActive(false);
    }

    // fvw + eip gốc.
    public void Setup(Action onChanged)
    {
        this.onChanged = onChanged;
        Load();
    }

    // fvx gốc.
    private void Load()
    {
        ClanUIUtil.SetActive(EmptyState, false);
        if (LoadingView != null) LoadingView.ShowLoading();
        ClanController.Instance.GetRequests((ok, list, error) =>
        {
            if (this == null) return;
            if (!ok)
            {
                if (LoadingView != null) LoadingView.ShowError(error ?? "Failed to load clan data", Load);
                return;
            }
            if (LoadingView != null) LoadingView.Hide();
            Draw(list);
        });
    }

    // fvy gốc.
    private void Draw(List<ClanRequestModel> list)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            ClanRequestCard card = Instantiate(CardPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Setup(list[i], r => OnAccept(r, card), r => OnDeny(r, card), null);
            cards.Add(card);
        }
        ClanUIUtil.SetActive(EmptyState, list.Count == 0);
    }

    // fvz gốc.
    private void OnAccept(ClanRequestModel request, ClanRequestCard card)
    {
        card.SetInteractable(false);
        ClanController.Instance.AcceptRequest(request.UserId, (ok, error) => OnHandled(ok, error, card));
    }

    // fwa gốc.
    private void OnDeny(ClanRequestModel request, ClanRequestCard card)
    {
        card.SetInteractable(false);
        ClanController.Instance.DenyRequest(request.UserId, (ok, error) => OnHandled(ok, error, card));
    }

    private void OnHandled(bool ok, string error, ClanRequestCard card)
    {
        if (!ok) ClanUIUtil.Toast(error);
        if (this == null) return;
        if (card != null)
        {
            if (ok)
            {
                cards.Remove(card);
                Destroy(card.gameObject);
            }
            else
            {
                card.SetInteractable(true);
            }
        }
        ClanUIUtil.SetActive(EmptyState, cards.Count == 0);
        onChanged?.Invoke();
    }
}

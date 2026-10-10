using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Clan Leaderboard" (ClanWarLeaderboardPopup gốc): hàng Champion (hoặc "No champion clan yet") + bảng clan theo tier;
// nút thông tin Champion → "Championship Ranking" (LeadershipRankPopup).
public class UIClanWarLeaderboardPopup : BaseUI
{
    public Button CloseButton;
    public ClanLoadingView LoadingView;
    public GameObject LeaderRowRoot;
    public ClanWarClanRowCard LeaderRow;
    public GameObject NoLeaderRoot;
    public Button ChampionClanInfoButton;
    public ClanWarClanRowCard RowPrefab;
    public Transform ListContent;
    public TMP_Text TitleText;
    public TMP_Text NoLeaderText;

    private readonly List<ClanWarClanRowCard> rows = new List<ClanWarClanRowCard>();

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "Clan Leaderboard");
        ClanUIUtil.SetText(NoLeaderText, "No champion clan yet");
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        if (ChampionClanInfoButton != null) ChampionClanInfoButton.onClick.AddListener(() => UIManager.Instance.LoadUI(UIKey.LeadershipRankPopup));
        if (RowPrefab != null) RowPrefab.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Load();
    }

    // gci/gcj gốc.
    private void Load()
    {
        Clear();
        if (LoadingView != null) LoadingView.ShowLoading();
        ClanWarController.Instance.GetClanLeaderboard(res =>
        {
            if (this == null) return;
            if (res == null)
            {
                if (LoadingView != null) LoadingView.ShowError("Failed to load clan war data.", Load);
                return;
            }
            if (LoadingView != null) LoadingView.Hide();
            Draw(res);
        });
    }

    // gck gốc.
    private void Draw(ClanWarClanLeaderboardResponseDTO res)
    {
        bool hasLeader = res.leader != null && !string.IsNullOrEmpty(res.leader.clanId);
        ClanUIUtil.SetActive(LeaderRowRoot, hasLeader);
        ClanUIUtil.SetActive(NoLeaderRoot, !hasLeader);
        if (hasLeader && LeaderRow != null) LeaderRow.Set(res.leader, res.leader.clanId == res.myClanId, true);
        if (res.rows == null) return;
        for (int i = 0; i < res.rows.Count; i++)
        {
            ClanWarClanRowDTO r = res.rows[i];
            ClanWarClanRowCard card = Instantiate(RowPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Set(r, r.clanId == res.myClanId, hasLeader && r.clanId == res.leader.clanId);
            rows.Add(card);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null) Destroy(rows[i].gameObject);
        }
        rows.Clear();
    }
}

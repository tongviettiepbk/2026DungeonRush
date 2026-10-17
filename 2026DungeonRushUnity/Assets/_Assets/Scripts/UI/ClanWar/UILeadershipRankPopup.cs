using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Championship Ranking" (LeadershipRankPopup gốc): xếp hạng clan theo điểm đóng góp tuần — clan dẫn đầu thách đấu
// Champion tuần sau. "Updates every 10 minutes"; chưa có war → "Rankings will be displayed when war is active.".
public class UILeadershipRankPopup : BaseUI
{
    public Button CloseButton;
    public ClanLoadingView LoadingView;
    public ClanWarClanRowCard RowPrefab;
    public Transform ListContent;
    public TMP_Text TitleText;
    public TMP_Text DescriptionText;
    public TMP_Text UpdateInfoText;
    public TMP_Text NoRankingsText;

    private readonly List<ClanWarClanRowCard> rows = new List<ClanWarClanRowCard>();

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, Loc.L("Clan.War.LeadershipRank.Title"));
        ClanUIUtil.SetText(DescriptionText, Loc.L("Clan.War.LeadershipRank.Description"));
        ClanUIUtil.SetText(UpdateInfoText, Loc.L("Clan.War.LeadershipRank.UpdateInfo"));
        ClanUIUtil.SetText(NoRankingsText, Loc.L("Clan.War.LeadershipRank.NoRankings"));
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Load();
    }

    // gfb gốc.
    private void Load()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null) Destroy(rows[i].gameObject);
        }
        rows.Clear();
        ClanUIUtil.SetActive(NoRankingsText, false);
        if (LoadingView != null) LoadingView.ShowLoading();
        ClanWarController.Instance.GetLeadershipRanking(res =>
        {
            if (this == null) return;
            if (res == null)
            {
                if (LoadingView != null) LoadingView.ShowError(Loc.L("Errors.ClanWar.LoadFailed"), Load);
                return;
            }
            if (LoadingView != null) LoadingView.Hide();
            Draw(res);
        });
    }

    // gfc gốc: tierPoints ở đây = điểm đóng góp tuần.
    private void Draw(ClanWarLeadershipRankingResponseDTO res)
    {
        bool empty = res.rows == null || res.rows.Count == 0;
        ClanUIUtil.SetActive(NoRankingsText, empty);
        if (empty) return;
        for (int i = 0; i < res.rows.Count; i++)
        {
            ClanWarClanRowDTO r = res.rows[i];
            ClanWarClanRowCard card = Instantiate(RowPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Set(r, r.clanId == res.myClanId);
            rows.Add(card);
        }
    }
}

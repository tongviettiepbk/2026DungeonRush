using System.Collections.Generic;

// Save Clan phía client (UserData gốc: ClanId / ClanRole / ClanJoinedAt / ClanBanner*) + hàng đợi Clan War
// (PlayerPrefs gốc "clanwar_pending_actions" / "clanwar_pending_pvp_report"). Server là nguồn sự thật — đây là cache.
public class UserClanData : BaseUserData
{
    public string clanId { get; set; } = string.Empty;
    public string clanRole { get; set; } = string.Empty;
    public long clanJoinedAt { get; set; }
    public int bannerBackgroundTypeId { get; set; }
    public int bannerBackgroundColorId { get; set; }
    public int bannerImageTypeId { get; set; }
    public int bannerImageColorId { get; set; }

    // ClanWarController.PersistedActions gốc {weekId, uid, actions}.
    public string pendingWarWeekId { get; set; } = string.Empty;
    public List<ClanWarActionDTO> pendingWarActions { get; set; } = new List<ClanWarActionDTO>();
    // ClanWarController.PendingPvpReport gốc {warId, battleToken, won}.
    public string pendingPvpWarId { get; set; } = string.Empty;
    public string pendingPvpToken { get; set; } = string.Empty;
    public bool pendingPvpWon { get; set; }

    public bool HasClan => !string.IsNullOrEmpty(clanId);

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_CLAN;
    }

    public override void InitData()
    {
        base.InitData();
        clanId = string.Empty;
        clanRole = string.Empty;
        clanJoinedAt = 0;
        pendingWarWeekId = string.Empty;
        pendingWarActions = new List<ClanWarActionDTO>();
        pendingPvpWarId = string.Empty;
        pendingPvpToken = string.Empty;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        clanId = clanId ?? string.Empty;
        clanRole = clanRole ?? string.Empty;
        pendingWarWeekId = pendingWarWeekId ?? string.Empty;
        pendingPvpWarId = pendingPvpWarId ?? string.Empty;
        pendingPvpToken = pendingPvpToken ?? string.Empty;
        if (pendingWarActions == null)
        {
            pendingWarActions = new List<ClanWarActionDTO>();
            isDataChanged = true;
        }
    }

    // fqg gốc: lưu clan hiện tại vào UserData.
    public void SetClan(ClanModel clan)
    {
        clanId = clan != null ? clan.ClanId : string.Empty;
        clanRole = clan != null ? clan.MyRole.ToWire() : string.Empty;
        if (clan != null)
        {
            bannerBackgroundTypeId = clan.Banner.BackgroundTypeId;
            bannerBackgroundColorId = clan.Banner.BackgroundColorId;
            bannerImageTypeId = clan.Banner.ImageTypeId;
            bannerImageColorId = clan.Banner.ImageColorId;
        }
        isDataChanged = true;
    }
}

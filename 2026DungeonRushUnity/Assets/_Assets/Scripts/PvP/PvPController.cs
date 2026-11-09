using System;
using System.Collections.Generic;
using UnityEngine;

// Trận PvP đang chuẩn bị/đang đánh — port class tĩnh `tf` gốc (jlx: đối thủ + battleToken + startLeagueIndex).
public static class PvPBattleSession
{
    public static bool IsActive { get; private set; }
    public static PvPPlayerModel Opponent { get; private set; }
    public static string BattleToken { get; private set; }
    public static int StartLeagueIndex { get; private set; }
    // Khác null = trận PvP ngày 6 của Clan War (ClanWarController.gae gốc), báo kết quả qua ClanWarController.
    public static string ClanWarId { get; private set; }
    public static bool IsClanWar => !string.IsNullOrEmpty(ClanWarId);

    public static void Begin(PvPPlayerModel opponent, string battleToken, int startLeagueIndex)
    {
        Opponent = opponent;
        BattleToken = battleToken;
        StartLeagueIndex = startLeagueIndex;
        ClanWarId = null;
        IsActive = true;
    }

    public static void BeginClanWar(PvPPlayerModel opponent, string battleToken, string warId)
    {
        Begin(opponent, battleToken, 0);
        ClanWarId = warId;
    }

    public static void Clear()
    {
        IsActive = false;
        Opponent = null;
        BattleToken = null;
        ClanWarId = null;
    }
}

// Cổng nghiệp vụ PvP phía client: gọi Cloud Function qua FirebaseManager, cache trạng thái mở (league, cúp, vé, mùa,
// bảng xếp hạng nhóm, bảng thưởng) + roster đối thủ, lưu báo cáo trận lỗi mạng để gửi lại.
public class PvPController : Singleton<PvPController>
{
    public const string FN_OPEN = "openpvp";
    public const string FN_CLAIM_SEASON = "claimpvpseason";
    public const string FN_FIND_OPPONENTS = "findpvpopponents";
    public const string FN_START_BATTLE = "startpvpbattle";
    public const string FN_REPORT_BATTLE = "reportpvpbattle";

    private const int MAX_PENDING_ATTEMPTS = 5;

    public static event Action<PvPOpenResponseDTO> StateUpdated;
    public static event Action<PvPTicketsDTO> TicketsUpdated;

    public PvPOpenResponseDTO State { get; private set; }
    public List<PvPLeagueRewardsDTO> RewardTable { get; private set; } = new List<PvPLeagueRewardsDTO>();
    public bool IsBusy { get; private set; }

    private static UserPvPData User => GameData.userData.pvp;
    private static StaticPvPData Static => GameData.staticData.pvp;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===== Trạng thái (jnm/jnn/jno/jnp) =====

    public int Trophy => User.trophy;
    public int LeagueIndex => User.leagueIndex;
    public string LeagueName => Static.GetLeagueName(LeagueIndex);
    public int TotalTickets => User.TotalTickets;

    // ===== openPvP (jmx/jmy) =====

    public void Open(Action<bool, string> callback)
    {
        PvPOpenRequestDTO request = BuildOpenRequest();
        IsBusy = true;

        FirebaseManager.Instance.Call<PvPOpenResponseDTO>(FN_OPEN, request, (ok, res, raw) =>
        {
            IsBusy = false;
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[PvP] openPvP failed | " + raw);
                callback?.Invoke(false, res != null && !string.IsNullOrEmpty(res.message) ? res.message : Loc.L("Errors.PvP.LoadFailed"));
                return;
            }

            State = res;
            RewardTable = res.rewardTable ?? new List<PvPLeagueRewardsDTO>();
            User.trophy = res.trophy;
            User.leagueIndex = res.leagueIndex;
            string seasonKey = res.season != null ? res.season.key : string.Empty;
            if (User.seasonKey != seasonKey)
            {
                // Sang mùa/nhóm khác: roster cũ không còn giá trị.
                User.seasonKey = seasonKey;
                User.cachedRoster = null;
            }
            User.lastSnapshotHash = request.snapshotHash;
            User.lastSnapshotUploadTime = Now();
            SetTickets(res.tickets);
            User.isDataChanged = true;
            GameData.Save();

            StateUpdated?.Invoke(res);
            RetryPendingReport(null);
            callback?.Invoke(true, null);
        });
    }

    // ===== findPvPOpponents (jna) — roster còn hạn thì dùng lại =====

    public void FindOpponents(Action<PvPCachedRosterModel> onSuccess, Action<string> onError)
    {
        PvPCachedRosterModel cached = User.cachedRoster;
        if (cached != null && cached.ExpiresAt > Now() && cached.Opponents != null && cached.Opponents.Count > 0)
        {
            onSuccess?.Invoke(cached);
            return;
        }

        FirebaseManager.Instance.Call<PvPFindOpponentsResponseDTO>(FN_FIND_OPPONENTS,
            new PvPFindOpponentsRequestDTO { server = FirebaseSettings.ServerId }, (ok, res, raw) =>
            {
                if (!ok || res == null || !res.success)
                {
                    DebugCustom.LogWarning("[PvP] findPvPOpponents failed | " + raw);
                    onError?.Invoke(res != null && !string.IsNullOrEmpty(res.message) ? res.message : Loc.L("Errors.PvP.FindOpponentsFailed"));
                    return;
                }

                SetTickets(res.tickets);
                User.cachedRoster = new PvPCachedRosterModel
                {
                    RosterToken = res.rosterToken,
                    ExpiresAt = res.rosterExpiresAt,
                    Opponents = res.opponents ?? new List<PvPPlayerModel>(),
                };
                User.isDataChanged = true;
                GameData.Save();
                onSuccess?.Invoke(User.cachedRoster);
            });
    }

    // ===== startPvPBattle (jnb + jog) =====

    public void StartBattle(PvPPlayerModel opponent, Action<bool, string> callback)
    {
        PvPCachedRosterModel roster = User.cachedRoster;
        if (roster == null || string.IsNullOrEmpty(roster.RosterToken) || roster.ExpiresAt <= Now())
        {
            callback?.Invoke(false, Loc.L("Errors.PvP.RosterExpired"));
            return;
        }
        if (opponent == null)
        {
            callback?.Invoke(false, Loc.L("Errors.PvP.OpponentNotFound"));
            return;
        }
        if (TotalTickets <= 0)
        {
            callback?.Invoke(false, Loc.L("Errors.PvP.NoTicketsRemaining"));
            return;
        }

        IsBusy = true;
        PvPStartBattleRequestDTO request = new PvPStartBattleRequestDTO
        {
            server = FirebaseSettings.ServerId,
            rosterToken = roster.RosterToken,
            opponentUserId = opponent.UserId,
        };
        FirebaseManager.Instance.Call<PvPStartBattleResponseDTO>(FN_START_BATTLE, request, (ok, res, raw) =>
        {
            IsBusy = false;
            if (res != null) SetTickets(res.tickets);

            if (!ok || res == null || !res.success || string.IsNullOrEmpty(res.battleToken))
            {
                DebugCustom.LogWarning("[PvP] startPvPBattle failed | " + raw);
                if (res != null && res.message == "Opponent roster expired.")
                {
                    User.cachedRoster = null;
                }
                GameData.Save();
                callback?.Invoke(false, res != null && !string.IsNullOrEmpty(res.message) ? res.message : Loc.L("Errors.PvP.StartBattleFailed"));
                return;
            }

            // Đã đánh → gỡ khỏi roster cache (server cũng gỡ).
            roster.Opponents.RemoveAll(x => x.UserId == opponent.UserId);
            User.isDataChanged = true;
            GameData.Save();

            PvPBattleSession.Begin(res.opponent ?? opponent, res.battleToken, res.startLeagueIndex);
            callback?.Invoke(true, null);

            GameController.Instance.uiLobby.CloseAllTabs();
            GameController.Instance.ChangeMode(ModeType.PvP);
        });
    }

    // ===== reportPvPBattle (jnc + jnd) =====

    public void ReportBattle(bool won, Action<bool, PvPBattleResultResponseDTO> callback)
    {
        string token = PvPBattleSession.BattleToken;
        if (string.IsNullOrEmpty(token))
        {
            DebugCustom.LogError("[PvP] ReportBattle: missing battleToken");
            callback?.Invoke(false, null);
            return;
        }

        // Lưu trước — mạng lỗi/app tắt giữa chừng thì lần sau gửi lại.
        User.pendingReport = new PendingPvPReport
        {
            BattleToken = token,
            Won = won,
            AttemptedAt = Now(),
            AttemptCount = 0,
            StartLeagueIndex = PvPBattleSession.StartLeagueIndex,
        };
        User.isDataChanged = true;
        GameData.Save();

        SendPendingReport(callback);
    }

    private void SendPendingReport(Action<bool, PvPBattleResultResponseDTO> callback)
    {
        PendingPvPReport report = User.pendingReport;
        if (report == null)
        {
            callback?.Invoke(false, null);
            return;
        }

        report.AttemptCount++;
        report.AttemptedAt = Now();
        PvPBattleResultRequestDTO request = new PvPBattleResultRequestDTO { battleToken = report.BattleToken, won = report.Won };

        FirebaseManager.Instance.Call<PvPBattleResultResponseDTO>(FN_REPORT_BATTLE, request, (ok, res, raw) =>
        {
            if (!ok || res == null)
            {
                DebugCustom.LogWarning("[PvP] reportPvPBattle failed | " + raw);
                if (report.AttemptCount >= MAX_PENDING_ATTEMPTS)
                {
                    User.pendingReport = null;
                }
                User.isDataChanged = true;
                GameData.Save();
                callback?.Invoke(false, res);
                return;
            }

            // Token đã dùng/không hợp lệ cũng bỏ báo cáo (server không nhận lại được nữa).
            User.pendingReport = null;
            SetTickets(res.tickets);
            if (res.success)
            {
                User.trophy = res.newTrophy;
                User.pendingSettlement = BuildSettlement(res);
            }
            User.isDataChanged = true;
            GameData.Save();
            callback?.Invoke(res.success, res);
        });
    }

    // Gửi lại báo cáo treo từ lần trước (jns).
    public void RetryPendingReport(Action<bool, PvPBattleResultResponseDTO> callback)
    {
        if (User.pendingReport == null)
        {
            callback?.Invoke(false, null);
            return;
        }
        SendPendingReport(callback);
    }

    public bool HasPendingReport => User.pendingReport != null;

    // Kết quả server → hiện ở popup cuối trận.
    private static PendingPvPRewardSettlement BuildSettlement(PvPBattleResultResponseDTO res)
    {
        return new PendingPvPRewardSettlement
        {
            Won = res.won,
            OldTrophy = res.oldTrophy,
            NewTrophy = res.newTrophy,
            TrophyDelta = res.trophyDelta,
        };
    }

    // Đã xem xong popup cuối trận.
    public void ClearSettlement()
    {
        if (User.pendingSettlement == null)
        {
            return;
        }
        User.pendingSettlement = null;
        User.isDataChanged = true;
        GameData.Save();
    }

    // ===== Thưởng mùa =====

    public void ClaimSeason(Action<List<RewardEntry>, string> callback)
    {
        FirebaseManager.Instance.Call<PvPClaimSeasonResponseDTO>(FN_CLAIM_SEASON, new object(), (ok, res, raw) =>
        {
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[PvP] claimPvPSeason failed | " + raw);
                callback?.Invoke(null, res != null && !string.IsNullOrEmpty(res.message) ? res.message : Loc.L("Errors.PvP.LoadFailed"));
                return;
            }

            List<RewardEntry> rewards = res.rewards ?? new List<RewardEntry>();
            using (Analytics.Scope("pvp_season")) BossRushController.GrantRewards(rewards);
            if (State != null) State.pendingSeasonResult = null;
            GameData.Save(true);
            callback?.Invoke(rewards, null);
        });
    }

    public List<PvPRewardBandDTO> GetRewardBands(int leagueIndex)
    {
        for (int i = 0; i < RewardTable.Count; i++)
        {
            if (RewardTable[i].LeagueIndex == leagueIndex)
            {
                return RewardTable[i].Bands;
            }
        }
        return new List<PvPRewardBandDTO>();
    }

    // ===== Snapshot đồ (jnv/job/joc/jod/joe/jof) =====

    private static PvPOpenRequestDTO BuildOpenRequest()
    {
        PvPOpenRequestDTO request = new PvPOpenRequestDTO
        {
            server = FirebaseSettings.ServerId,
            playerName = GameData.userData.profile.userName,
            countryCode = GetCountryCode(),
            avatarId = GameData.userData.profile.avatarId,
            power = BossRushPower.GetPlayerPower(),
            contentVersion = StaticPvPData.CONTENT_VERSION,
            items = BossRushController.BuildItems(GameData.userData.equipment),
            companions = BossRushController.BuildCompanions(GameData.userData.companions),
            enchantmentTiers = BossRushController.BuildEnchantmentTiers(GameData.userData.enchantments),
            showCloak = GameData.userData.capes.showCloak,
        };
        request.snapshotHash = Newtonsoft.Json.JsonConvert.SerializeObject(new object[]
        {
            request.power, request.items, request.companions, request.enchantmentTiers, request.showCloak,
        }).GetHashCode().ToString("X8");
        return request;
    }

    // Mã quốc gia 2 chữ (gốc lấy từ CountryFlagController).
    private static string GetCountryCode()
    {
        try
        {
            return System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ===== Nội bộ =====

    private static void SetTickets(PvPTicketsDTO tickets)
    {
        if (tickets == null)
        {
            return;
        }
        User.SetTickets(tickets);
        SyncEventTickets();
        TicketsUpdated?.Invoke(tickets);
    }

    // Vé PvP hiển thị ở tab Events (UserEventData) lấy theo server.
    private static void SyncEventTickets()
    {
        EventTickets tickets = GameData.userData.events.Get(EventModeType.PvP);
        tickets.freeTickets = User.tickets.remaining;
        tickets.adTickets = 0;
        tickets.adClaimedToday = 0;
        GameData.userData.events.isDataChanged = true;
    }

    public static long Now()
    {
        // Giờ server: so với ExpiresAt server trả về.
        return new DateTimeOffset(ServerTime.UtcNow).ToUnixTimeSeconds();
    }
}

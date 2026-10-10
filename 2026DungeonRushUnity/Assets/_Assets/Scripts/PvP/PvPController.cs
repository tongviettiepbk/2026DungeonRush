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

// Cổng nghiệp vụ PvP phía client — port PvPController gốc. Gọi 6 Cloud Function qua FirebaseManager,
// cache trạng thái mở (trophy/league/vé/bảng thưởng) + roster đối thủ, lưu báo cáo trận lỗi mạng để gửi lại,
// giữ kết quả chưa nhận thưởng (PendingPvPRewardSettlement) tới khi bấm Claim.
public class PvPController : Singleton<PvPController>
{
    public const string FN_INIT_PROFILE = "initpvpprofile";
    public const string FN_OPEN = "openpvp";
    public const string FN_FIND_OPPONENTS = "findpvpopponents";
    public const string FN_START_BATTLE = "startpvpbattle";
    public const string FN_REPORT_BATTLE = "reportpvpbattle";
    public const string FN_GRANT_AD_TICKET = "grantpvpadticket";
    public const string FN_LEADERBOARD = "getpvpleaderboard";
    public const string FN_SEED_BOTS = "seedpvpdummyplayers";

    public const string AD_PLACEMENT = "pvp_ticket_ad";     // placement gốc (RewardedType.PvPTicket = 13)
    private const int MAX_PENDING_ATTEMPTS = 5;

    // StateUpdated / TicketsUpdated gốc.
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
    public int LeagueIndex => Static.GetLeagueIndex(User.trophy);
    public string LeagueName => Static.GetLeagueName(LeagueIndex);
    public int TotalTickets => User.TotalTickets;
    public bool CanWatchAdTicket => User.tickets.adClaimedToday < Mathf.Max(1, User.tickets.maxAdPerDay);

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
                callback?.Invoke(false, res != null && !string.IsNullOrEmpty(res.message) ? res.message : "Failed to load PvP.");
                return;
            }

            State = res;
            RewardTable = res.rewardTable ?? new List<PvPLeagueRewardsDTO>();
            User.trophy = res.trophy;
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
            new PvPFindOpponentsRequestDTO { server = FirebaseSettings.PROJECT_ID }, (ok, res, raw) =>
            {
                if (!ok || res == null || !res.success)
                {
                    DebugCustom.LogWarning("[PvP] findPvPOpponents failed | " + raw);
                    onError?.Invoke("Failed to find opponents.");
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
            callback?.Invoke(false, "Opponent roster expired.");
            return;
        }
        if (opponent == null)
        {
            callback?.Invoke(false, "Opponent not found.");
            return;
        }
        if (TotalTickets <= 0)
        {
            callback?.Invoke(false, "No PvP tickets remaining.");
            return;
        }

        IsBusy = true;
        PvPStartBattleRequestDTO request = new PvPStartBattleRequestDTO
        {
            server = FirebaseSettings.PROJECT_ID,
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
                callback?.Invoke(false, res != null && !string.IsNullOrEmpty(res.message) ? res.message : "Failed to start PvP battle.");
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

    // jnd: kết quả server → phần thưởng chờ nhận.
    private static PendingPvPRewardSettlement BuildSettlement(PvPBattleResultResponseDTO res)
    {
        return new PendingPvPRewardSettlement
        {
            Won = res.won,
            OldTrophy = res.oldTrophy,
            NewTrophy = res.newTrophy,
            TrophyDelta = res.trophyDelta,
            OldLeagueIndex = res.oldLeagueIndex,
            NewLeagueIndex = res.newLeagueIndex,
            Rewards = ParseRewards(res.rewards),
        };
    }

    // Nút Claim ở PvPEndPopup: cộng thưởng + xoá kết quả chờ.
    public void ClaimSettlement()
    {
        PendingPvPRewardSettlement settlement = User.pendingSettlement;
        if (settlement == null)
        {
            return;
        }
        BossRushController.GrantRewards(settlement.Rewards);
        User.pendingSettlement = null;
        User.isDataChanged = true;
        GameData.Save(true);
    }

    // jne: lần mở game/popup sau còn thưởng chưa nhận (tắt app trước khi bấm Claim) → tự cộng + báo.
    public bool ApplyPendingSettlement(bool showToast)
    {
        if (User.pendingSettlement == null)
        {
            return false;
        }
        ClaimSettlement();
        if (showToast)
        {
            UIManager.Instance.ShowToastMessage("Applied pending PvP rewards.", isLocalize: false);
        }
        return true;
    }

    // ===== grantPvPAdTicket (jng) — gọi SAU khi xem xong ads =====

    public void GrantAdTicket(Action<bool, string> callback)
    {
        FirebaseManager.Instance.Call<PvPAdTicketResponseDTO>(FN_GRANT_AD_TICKET, new object(), (ok, res, raw) =>
        {
            if (res != null) SetTickets(res.tickets);
            GameData.Save();
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[PvP] grantPvPAdTicket failed | " + raw);
                callback?.Invoke(false, res != null && !string.IsNullOrEmpty(res.message) ? res.message : "Failed to grant ticket.");
                return;
            }
            callback?.Invoke(true, null);
        });
    }

    // ===== getPvPLeaderboard (jnf) =====

    public void GetLeaderboard(string scope, Action<PvPLeaderboardResponseDTO> callback)
    {
        FirebaseManager.Instance.Call<PvPLeaderboardResponseDTO>(FN_LEADERBOARD, new PvPLeaderboardRequestDTO { scope = scope }, (ok, res, raw) =>
        {
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[PvP] getPvPLeaderboard failed | " + raw);
                callback?.Invoke(null);
                return;
            }
            callback?.Invoke(res);
        });
    }

    // Admin/test: tạo bot quanh trophy mình (chỉ chạy được trên emulator hoặc uid admin).
    public void SeedBots(int count, Action<bool> callback)
    {
        FirebaseManager.Instance.Call<object>(FN_SEED_BOTS, new { count }, (ok, res, raw) =>
        {
            DebugCustom.Log("[PvP] Seed bots: " + ok + " | " + raw);
            User.cachedRoster = null;
            callback?.Invoke(ok);
        });
    }

    // ===== Bảng thưởng (jnj/jnk/jnl) =====

    public List<RewardEntry> GetRewards(int leagueIndex, bool won)
    {
        for (int i = 0; i < RewardTable.Count; i++)
        {
            if (RewardTable[i].leagueIndex == leagueIndex)
            {
                return ParseRewards(won ? RewardTable[i].winRewards : RewardTable[i].loseRewards);
            }
        }
        return new List<RewardEntry>();
    }

    // joa: Type chuỗi → RewardType (bỏ loại lạ), Amount kẹp ≥ 0.
    public static List<RewardEntry> ParseRewards(List<PvPRewardEntryDTO> list)
    {
        List<RewardEntry> result = new List<RewardEntry>();
        if (list == null)
        {
            return result;
        }
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && Enum.TryParse(list[i].Type, true, out RewardType type))
            {
                result.Add(new RewardEntry { Type = type, Amount = Mathf.Max(0, list[i].Amount) });
            }
        }
        return result;
    }

    // ===== Snapshot đồ (jnv/job/joc/jod/joe/jof) =====

    private static PvPOpenRequestDTO BuildOpenRequest()
    {
        PvPOpenRequestDTO request = new PvPOpenRequestDTO
        {
            server = FirebaseSettings.PROJECT_ID,
            playerName = GameData.userData.profile.userName,
            countryCode = GetCountryCode(),
            avatarId = 0,
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

    // Mã quốc gia 2 chữ cho tab leaderboard "country" (gốc lấy từ CountryFlagController).
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
        tickets.freeTickets = User.tickets.freeRemaining;
        tickets.adTickets = User.tickets.adRemaining;
        tickets.adClaimedToday = User.tickets.adClaimedToday;
        GameData.userData.events.isDataChanged = true;
    }

    public static long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

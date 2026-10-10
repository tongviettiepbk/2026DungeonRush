using System;
using System.Collections.Generic;
using UnityEngine;

// Clan War phía client — port ClanWarController gốc. Lịch tuần (fze/fzf), lọc nguồn điểm theo ngày (fzg),
// gom action đóng góp rồi gửi theo lô (fzs/fzu/fzv: chờ 8s, lỗi thử lại 30s, tối đa 200 action, lưu save),
// tải state war (fzy) + cấu hình, leaderboard (gaa/gab/gac), PvP ngày 6 (gad/gaf), nhận thưởng (gak/gal/gam).
public class ClanWarController : Singleton<ClanWarController>
{
    public const string FN_STATE = "getclanwarstate";
    public const string FN_RECORD = "recordclanwarcontributions";
    public const string FN_CONTRIBUTION_LB = "getclanwarcontributionleaderboard";
    public const string FN_CLAN_LB = "getclanwarclanleaderboard";
    public const string FN_LEADERSHIP = "getclanwarleadershipranking";
    public const string FN_PVP_START = "startclanwarpvpbattle";
    public const string FN_PVP_REPORT = "reportclanwarpvpbattle";
    public const string FN_CLAIM_MILESTONE = "claimclanwarmilestone";
    public const string FN_CLAIM_BUNDLE = "claimclanwarpersonalbundle";
    public const string FN_CLAIM_CLAN = "claimclanwarclanreward";
    public const string FN_MATCH_NOW = "matchclanwarsnow";

    // WarStateUpdated / ContributionPointsAccepted gốc.
    public static event Action<ClanWarStateResponseDTO> WarStateUpdated;
    public static event Action<int> ContributionPointsAccepted;

    public ClanWarStateResponseDTO State { get; private set; }
    public ClanWarConfigDTO Config { get; private set; }

    private readonly List<Action<ClanWarStateResponseDTO>> stateWaiters = new List<Action<ClanWarStateResponseDTO>>();
    private bool isFetching;
    private bool isFlushing;
    private float flushTimer = -1f;

    private static UserClanData User => GameData.userData.clan;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        ClanController.ClanUpdated += OnClanUpdated;
    }

    private void OnDestroy()
    {
        ClanController.ClanUpdated -= OnClanUpdated;
    }

    private void Update()
    {
        if (flushTimer < 0f) return;
        flushTimer -= Time.unscaledDeltaTime;
        if (flushTimer <= 0f)
        {
            flushTimer = -1f;
            Flush();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Flush();
    }

    // fzd gốc: đổi/rời clan → bỏ state cũ.
    private void OnClanUpdated(ClanModel clan)
    {
        if (clan == null || State?.war?.myClan == null || State.war.myClan.clanId != clan.ClanId)
        {
            State = null;
        }
    }

    // ===== Lịch tuần (fze / fzf) =====

    // Day 1..7: 1 = Thứ Ba ... 6 = Chủ Nhật (PvP), 7 = Thứ Hai (cooldown).
    public static int DayOf(DateTime utc)
    {
        return ((int)utc.DayOfWeek + 5) % 7 + 1;
    }

    public static string WeekIdOf(DateTime utc)
    {
        return utc.Date.AddDays(-(((int)utc.DayOfWeek + 5) % 7)).ToString("yyyy-MM-dd");
    }

    public static int Today => DayOf(DateTime.UtcNow);

    // fzg gốc: nguồn điểm có hiệu lực trong ngày (ưu tiên config.sourcesByDay, mặc định lẻ/chẵn).
    public bool IsSourceActive(string source)
    {
        int day = State?.war != null && State.war.activeDay >= 1 && State.war.activeDay <= 6 ? State.war.activeDay : Today;
        if (day < 1 || day > 5) return false;
        if (Config?.sourcesByDay != null && Config.sourcesByDay.TryGetValue(day.ToString(), out List<string> list) && list != null)
        {
            return list.Contains(source);
        }
        return Array.IndexOf(day % 2 == 1 ? StaticClanData.ODD_DAY_SOURCES : StaticClanData.EVEN_DAY_SOURCES, source) >= 0;
    }

    // fzj gốc: có war đang chạy để ghi điểm (không ở cooldown/concluded, có clan).
    private bool CanRecord()
    {
        if (!User.HasClan) return false;
        if (State != null && (State.war == null || State.war.IsCooldown || State.war.IsConcluded)) return false;
        return true;
    }

    // ===== Ghi action đóng góp (fzk..fzp) =====

    public void RecordLoot(Rarity rarity) => Record(StaticClanData.SOURCE_LOOT, RarityKey(rarity), null, 0, 1);
    public void RecordSummonCompanion(Rarity rarity, int count = 1) => Record(StaticClanData.SOURCE_SUMMON_COMPANION, RarityKey(rarity), null, 0, count);
    public void RecordSummonCape(Rarity rarity, int count = 1) => Record(StaticClanData.SOURCE_SUMMON_CAPE, RarityKey(rarity), null, 0, count);
    public void RecordDungeonKey(int count = 1) => Record(StaticClanData.SOURCE_DUNGEON_KEY, null, null, 0, count);
    public void RecordLevelUp(int newLevel) => Record(StaticClanData.SOURCE_LEVEL_UP, null, null, newLevel, 1);
    // Project chưa có Mining — giữ API như gốc (fzp) để nối khi có.
    public void RecordMining(string ore, int count = 1) => Record(StaticClanData.SOURCE_MINING, null, (ore ?? string.Empty).ToLowerInvariant(), 0, count);

    private static string RarityKey(Rarity rarity) => rarity.ToString().ToLowerInvariant();   // fzq gốc

    // fzs gốc.
    private void Record(string source, string rarity, string resource, int newLevel, int count)
    {
        if (!CanRecord() || !IsSourceActive(source)) return;

        string weekId = WeekIdOf(DateTime.UtcNow);
        if (User.pendingWarWeekId != weekId)
        {
            User.pendingWarWeekId = weekId;
            User.pendingWarActions.Clear();
        }
        if (User.pendingWarActions.Count >= StaticClanData.WAR_MAX_PENDING_ACTIONS)
        {
            User.pendingWarActions.RemoveAt(0);
        }
        User.pendingWarActions.Add(new ClanWarActionDTO
        {
            actionId = Guid.NewGuid().ToString("N"),
            source = source,
            rarity = rarity,
            resource = resource,
            newLevel = newLevel,
            count = Mathf.Max(1, count),
            eventTimestamp = ClanController.Now(),
        });
        User.isDataChanged = true;
        ScheduleFlush(StaticClanData.WAR_FLUSH_DELAY);
    }

    private void ScheduleFlush(float delay)
    {
        if (flushTimer < 0f || flushTimer > delay) flushTimer = delay;
    }

    // fzu/fzv gốc: gửi lô action đang chờ.
    public void Flush()
    {
        if (isFlushing || User.pendingWarActions.Count == 0 || !User.HasClan) return;
        if (User.pendingWarWeekId != WeekIdOf(DateTime.UtcNow))
        {
            User.pendingWarActions.Clear();
            User.isDataChanged = true;
            return;
        }
        if (State?.war == null)
        {
            // Chưa có warId → tải state trước rồi gửi.
            FetchState(true, s =>
            {
                if (s?.war != null) Flush();
                else if (s != null && s.success && s.inClan) DropPending();
            });
            return;
        }

        isFlushing = true;
        List<ClanWarActionDTO> batch = new List<ClanWarActionDTO>(User.pendingWarActions);
        var request = new
        {
            op = "record",
            uid = FirebaseManager.Instance.Uid,
            warId = State.war.warId,
            batchId = Guid.NewGuid().ToString("N"),
            actions = batch,
        };
        FirebaseManager.Instance.Call<ClanWarRecordResponseDTO>(FN_RECORD, request, (ok, res, raw) =>
        {
            isFlushing = false;
            if (!ok || res == null)
            {
                DebugCustom.LogWarning("ClanWar contribution flush failed: " + raw);
                ScheduleFlush(StaticClanData.WAR_FLUSH_RETRY);
                return;
            }
            if (!res.success)
            {
                // Bị từ chối ổn định (không còn war / sai ngày) → bỏ lô + làm mới state (bvz gốc).
                DebugCustom.LogWarning("ClanWar contribution flush rejected: " + res.code + " " + res.message);
                RemoveSent(batch);
                State = null;
                return;
            }
            RemoveSent(batch);
            if (res.points > 0)
            {
                if (State?.war != null)
                {
                    State.war.myDailyPoints += res.points;
                    State.war.myWeeklyPoints += res.points;
                    if (State.war.milestone != null) State.war.milestone.weeklyContribution += res.points;
                }
                ContributionPointsAccepted?.Invoke(res.points);
            }
        });
    }

    private void RemoveSent(List<ClanWarActionDTO> batch)
    {
        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < batch.Count; i++) ids.Add(batch[i].actionId);
        User.pendingWarActions.RemoveAll(a => ids.Contains(a.actionId));
        User.isDataChanged = true;
        GameData.Save();
    }

    private void DropPending()
    {
        User.pendingWarActions.Clear();
        User.isDataChanged = true;
    }

    // ===== getClanWarState (fzy / fzz) =====

    public void FetchState(bool force, Action<ClanWarStateResponseDTO> callback, bool includeConfig = true)
    {
        if (!force && State != null)
        {
            callback?.Invoke(State);
            return;
        }
        if (callback != null) stateWaiters.Add(callback);
        if (isFetching) return;
        isFetching = true;

        var request = new { includeConfig = includeConfig || Config == null, includeDailyResults = true };
        FirebaseManager.Instance.Call<ClanWarStateResponseDTO>(FN_STATE, request, (ok, res, raw) =>
        {
            isFetching = false;
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[ClanWar] getClanWarState failed | " + raw);
                InvokeWaiters(null);
                return;
            }
            if (res.config != null) Config = res.config;
            State = res;
            InvokeWaiters(res);
            WarStateUpdated?.Invoke(res);
            if (User.pendingWarActions.Count > 0) ScheduleFlush(1f);
            RetryPendingPvpReport();
        });
    }

    private void InvokeWaiters(ClanWarStateResponseDTO res)
    {
        List<Action<ClanWarStateResponseDTO>> list = new List<Action<ClanWarStateResponseDTO>>(stateWaiters);
        stateWaiters.Clear();
        for (int i = 0; i < list.Count; i++) list[i]?.Invoke(res);
    }

    // ===== Leaderboard (gaa / gab / gac) =====

    public void GetContributionLeaderboard(string clanId, string mode, Action<ClanWarContributionLeaderboardResponseDTO> callback)
    {
        int day = State?.war != null ? Mathf.Clamp(State.war.activeDay, 1, 6) : Mathf.Clamp(Today, 1, 6);
        var request = new { clanId, mode, day };
        FirebaseManager.Instance.Call<ClanWarContributionLeaderboardResponseDTO>(FN_CONTRIBUTION_LB, request, (ok, res, raw) =>
        {
            callback?.Invoke(ok && res != null && res.success ? res : null);
        });
    }

    public void GetClanLeaderboard(Action<ClanWarClanLeaderboardResponseDTO> callback)
    {
        FirebaseManager.Instance.Call<ClanWarClanLeaderboardResponseDTO>(FN_CLAN_LB, new object(), (ok, res, raw) =>
        {
            callback?.Invoke(ok && res != null && res.success ? res : null);
        });
    }

    public void GetLeadershipRanking(Action<ClanWarLeadershipRankingResponseDTO> callback)
    {
        FirebaseManager.Instance.Call<ClanWarLeadershipRankingResponseDTO>(FN_LEADERSHIP, new object(), (ok, res, raw) =>
        {
            callback?.Invoke(ok && res != null && res.success ? res : null);
        });
    }

    // ===== PvP ngày 6 (gad / gae / gaf / gag) =====

    public void StartPvp(string targetUserId, Action<bool, string> callback)
    {
        if (State?.war == null)
        {
            callback?.Invoke(false, "No active clan war.");
            return;
        }
        string warId = State.war.warId;
        var request = new { warId, targetUserId, requestId = Guid.NewGuid().ToString("N") };
        FirebaseManager.Instance.Call<ClanWarPvpStartResponseDTO>(FN_PVP_START, request, (ok, res, raw) =>
        {
            if (!ok || res == null || !res.success || res.opponent == null || string.IsNullOrEmpty(res.battleToken))
            {
                callback?.Invoke(false, res != null && !res.success ? ErrorText(res.code, "Failed to start the battle.") : "Failed to start the battle.");
                return;
            }
            if (State?.war?.pvp != null) State.war.pvp.myTickets = res.tickets;
            callback?.Invoke(true, null);

            // gae gốc: vào trận PvP với snapshot đối thủ (dùng lại PvPMode, đánh dấu Clan War).
            PvPBattleSession.BeginClanWar(res.opponent, res.battleToken, warId);
            GameController.Instance.uiLobby.CloseAllTabs();
            GameController.Instance.ChangeMode(ModeType.PvP);
        });
    }

    // gaf gốc: báo kết quả, lưu trước để gửi lại khi mạng lỗi.
    public void ReportPvp(bool won, Action<bool, ClanWarPvpReportResponseDTO> callback)
    {
        User.pendingPvpWarId = PvPBattleSession.ClanWarId ?? string.Empty;
        User.pendingPvpToken = PvPBattleSession.BattleToken ?? string.Empty;
        User.pendingPvpWon = won;
        User.isDataChanged = true;
        GameData.Save();
        SendPendingPvpReport(callback);
    }

    private void SendPendingPvpReport(Action<bool, ClanWarPvpReportResponseDTO> callback)
    {
        if (string.IsNullOrEmpty(User.pendingPvpToken))
        {
            callback?.Invoke(false, null);
            return;
        }
        var request = new { warId = User.pendingPvpWarId, battleToken = User.pendingPvpToken, won = User.pendingPvpWon };
        FirebaseManager.Instance.Call<ClanWarPvpReportResponseDTO>(FN_PVP_REPORT, request, (ok, res, raw) =>
        {
            if (!ok || res == null)
            {
                DebugCustom.LogWarning("[ClanWar] reportClanWarPvpBattle failed | " + raw);
                callback?.Invoke(false, res);
                return;
            }
            User.pendingPvpWarId = string.Empty;
            User.pendingPvpToken = string.Empty;
            User.isDataChanged = true;
            GameData.Save();
            if (res.success)
            {
                State = null;     // điểm/vé/đối thủ đã đổi → tải lại khi mở tab
                if (res.pointsAwarded > 0) ContributionPointsAccepted?.Invoke(res.pointsAwarded);
            }
            callback?.Invoke(res.success, res);
        });
    }

    public void RetryPendingPvpReport()
    {
        if (!string.IsNullOrEmpty(User.pendingPvpToken)) SendPendingPvpReport(null);
    }

    // ===== Nhận thưởng (gak / gal / gam → gan / gao) =====

    public void ClaimMilestone(int threshold, Action<bool, string, ClanWarRewardsDTO> callback)
    {
        Claim(FN_CLAIM_MILESTONE, new { warId = State?.war?.warId, threshold }, callback, res =>
        {
            ClanWarMilestoneDTO m = State?.war?.milestone;
            if (m != null)
            {
                if (m.claimedMilestones == null) m.claimedMilestones = new List<int>();
                m.claimedMilestones.Add(threshold);
            }
        });
    }

    public void ClaimPersonalBundle(Action<bool, string, ClanWarRewardsDTO> callback)
    {
        Claim(FN_CLAIM_BUNDLE, new { warId = State?.war?.warId }, callback, res =>
        {
            if (State?.war?.claims != null) State.war.claims.personalBundleClaimed = true;
            ClanWarMilestoneDTO m = State?.war?.milestone;
            if (m != null)
            {
                m.bundleClaimed = true;
                if (m.claimedMilestones == null) m.claimedMilestones = new List<int>();
                if (res.thresholds != null) m.claimedMilestones.AddRange(res.thresholds);
            }
        });
    }

    public void ClaimClanReward(Action<bool, string, ClanWarRewardsDTO> callback)
    {
        Claim(FN_CLAIM_CLAN, new { warId = State?.war?.warId }, callback, res =>
        {
            if (State?.war?.claims != null) State.war.claims.clanRewardClaimed = true;
        });
    }

    private void Claim(string fn, object request, Action<bool, string, ClanWarRewardsDTO> callback, Action<ClanWarClaimResponseDTO> onSuccess)
    {
        FirebaseManager.Instance.Call<ClanWarClaimResponseDTO>(fn, request, (ok, res, raw) =>
        {
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[ClanWar] " + fn + " failed | " + raw);
                callback?.Invoke(false, res != null ? ErrorText(res.code, "Failed to claim the reward.") : "Failed to claim the reward.", null);
                return;
            }
            onSuccess?.Invoke(res);
            // gao gốc: cộng thưởng vào túi đồ.
            if (res.rewards != null) BossRushController.GrantRewards(res.rewards.ToRewardEntries());
            GameData.Save(true);
            callback?.Invoke(true, null, res.rewards);
            if (State != null) WarStateUpdated?.Invoke(State);
        });
    }

    // Admin/test: ghép cặp war tuần này ngay (emulator).
    public void MatchNow(Action<bool> callback)
    {
        FirebaseManager.Instance.Call<object>(FN_MATCH_NOW, new object(), (ok, res, raw) =>
        {
            DebugCustom.Log("[ClanWar] matchNow: " + ok + " | " + raw);
            State = null;
            callback?.Invoke(ok);
        });
    }

    // ===== Thưởng tính sẵn ở client =====

    // gdl gốc: tổng thưởng các mốc đã đạt mà chưa nhận (gói "Remaining Personal Rewards").
    public static ClanWarRewardsDTO RemainingPersonalRewards(ClanWarMilestoneDTO milestone, ClanWarConfigDTO config)
    {
        ClanWarRewardsDTO total = new ClanWarRewardsDTO();
        if (milestone == null || config?.individualMilestones == null || milestone.bundleClaimed) return total;
        for (int i = 0; i < config.individualMilestones.Count; i++)
        {
            ClanWarMilestoneRowDTO row = config.individualMilestones[i];
            bool claimed = milestone.claimedMilestones != null && milestone.claimedMilestones.Contains(row.threshold);
            if (milestone.weeklyContribution >= row.threshold && !claimed) total.Add(row.ToRewards());
        }
        return total;
    }

    // gek gốc: thưởng clan theo tier + kết quả.
    public static ClanWarRewardsDTO ClanReward(ClanWarConfigDTO config, string tier, string outcome)
    {
        if (config?.clanRewards == null || string.IsNullOrEmpty(tier)) return null;
        if (!config.clanRewards.TryGetValue(tier, out Dictionary<string, ClanWarRewardsDTO> byOutcome) || byOutcome == null) return null;
        return byOutcome.TryGetValue(outcome, out ClanWarRewardsDTO r) ? r : null;
    }

    // Mã lỗi Clan War → câu báo (Errors.ClanWar.* tiếng Anh).
    public static string ErrorText(string code, string fallback)
    {
        switch (code)
        {
            case "ERR_NO_WAR": return "No active clan war.";
            case "ERR_WAR_STATE": return "The war state has changed. Refresh and try again.";
            case "ERR_RATE_LIMITED": return "Too many requests. Please wait a moment.";
            case "ERR_NO_TICKETS": return "No tickets remaining.";
            case "ERR_ALREADY_DEFEATED": return "This enemy has already been defeated.";
            case "ERR_TARGET_NOT_IN_ROUND": return "This target is not part of the current round.";
            case "ERR_NOT_ELIGIBLE": return "You are not eligible for this action.";
            case "ERR_MILESTONE_NOT_REACHED": return "Milestone not reached yet.";
            case "ERR_ALREADY_CLAIMED": return "Already claimed.";
            case "ERR_NOTHING_TO_CLAIM": return "Nothing to claim.";
            case "ERR_NOT_IN_CLAN": return "You are not in a clan";
            default: return fallback;
        }
    }

    // jz.gex gốc: "{d}d {hh:mm:ss}" hoặc "{hh:mm:ss}".
    public static string FormatCountdown(long seconds)
    {
        if (seconds < 0) seconds = 0;
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.Days > 0) return string.Format("{0}d {1:00}:{2:00}:{3:00}", t.Days, t.Hours, t.Minutes, t.Seconds);
        return string.Format("{0:00}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
    }

    // jz.gey gốc: số có dấu phân cách "N0".
    public static string FormatNumber(long value)
    {
        return value.ToString("N0");
    }
}

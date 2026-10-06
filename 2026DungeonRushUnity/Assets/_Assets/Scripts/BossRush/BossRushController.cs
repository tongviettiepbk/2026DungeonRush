using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dữ liệu trận Boss Rush đang chuẩn bị/đang đánh — port class tĩnh `fo` gốc (emx: allies + boss + poolId + fightToken).
public static class BossRushFightSession
{
    public static bool IsActive { get; private set; }
    public static List<BossRushPlayerModel> Allies { get; private set; } = new List<BossRushPlayerModel>();
    public static BossRushBossModel Boss { get; private set; }
    public static string PoolId { get; private set; }
    public static string FightToken { get; private set; }
    public static int Tier { get; private set; }

    public static void Begin(List<BossRushPlayerModel> allies, BossRushBossModel boss, string poolId, string fightToken, int tier)
    {
        Allies = allies ?? new List<BossRushPlayerModel>();
        Boss = boss;
        PoolId = poolId;
        FightToken = fightToken;
        Tier = tier;
        IsActive = true;
    }

    public static void Clear()
    {
        IsActive = false;
        Allies = new List<BossRushPlayerModel>();
        Boss = null;
        FightToken = null;
    }
}

// Cổng nghiệp vụ Boss Rush phía client — port BossRushController gốc. Gọi 6 Cloud Function qua FirebaseManager,
// cache nhóm (BossRushPoolModel) + bảng thưởng, đồng bộ vé về save, lưu báo cáo damage lỗi mạng để gửi lại.
public class BossRushController : Singleton<BossRushController>
{
    public const string FN_JOIN = "joinbossrush";
    public const string FN_GET_POOL = "getbossrushpool";
    public const string FN_START_FIGHT = "startbossrushfight";
    public const string FN_REPORT_DAMAGE = "reportbossrushdamage";
    public const string FN_CLAIM = "claimbossrushrewards";
    public const string FN_UPDATE_PLAYER = "updatebossrushplayer";
    public const string FN_SEED_BOTS = "seedbossrushdummyplayers";

    private const int MAX_PENDING_ATTEMPTS = 5;
    private const int SECONDS_PER_DAY = 24 * 60 * 60;
    // Chờ hero campaign spawn xong (Power tính từ hero trong trận) trước khi gửi snapshot lúc vào game.
    private const float WAIT_HERO_TIMEOUT = 10f;

    public static event Action<BossRushPoolModel> PoolUpdated;

    public BossRushPoolModel Pool { get; private set; }
    public BossRushRewardTableDTO RewardTable { get; private set; }
    public bool IsBusy { get; private set; }

    private static UserBossRushData User => GameData.userData.bossRush;
    private static StaticBossRushData Static => GameData.staticData.bossRush;

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

    // ===== Trạng thái =====

    public bool IsEventActive => BossRushSchedule.IsActive(BossRushSchedule.UtcNow);
    public int TotalFightsRemaining => User.tickets.freeRemaining + User.tickets.adRemaining;
    public bool HasFreeFight => User.tickets.freeRemaining > 0;

    // ===== Join (ekr) =====

    public void Join(Action<bool, BossRushJoinResponseDTO> callback)
    {
        BossRushJoinRequestDTO request = new BossRushJoinRequestDTO();
        FillSnapshot(request);
        IsBusy = true;

        FirebaseManager.Instance.Call<BossRushJoinResponseDTO>(FN_JOIN, request, (ok, res, raw) =>
        {
            IsBusy = false;
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[BossRush] Failed to join Boss Rush | httpSuccess=" + ok + " | response=" + raw);
                callback?.Invoke(false, res);
                return;
            }

            User.SetTickets(res.tickets);
            User.unclaimedPoolId = res.hasUnclaimed ? res.unclaimedPoolId : string.Empty;
            if (res.tier > 0) User.tier = res.tier;
            if (!string.IsNullOrEmpty(res.poolId))
            {
                User.currentPoolId = res.poolId;
                User.lastJoinEventKey = res.eventKey;
                User.lastItemUpdateTime = Now();
                RewardTable = res.rewardTable;
                SetPool(res.poolId, res.tier, res.eventKey, res.players, res.isFinalized, res.bossNumber, res.bossHP, res.maxBossHP);
            }
            GameData.Save();
            SyncEventTickets();
            RetryPendingReport(null);
            callback?.Invoke(true, res);
        });
    }

    // ===== Pool (eks) =====

    public void RefreshPool(Action<bool, BossRushPoolResponseDTO> callback)
    {
        string poolId = Pool != null ? Pool.PoolId : User.currentPoolId;
        FirebaseManager.Instance.Call<BossRushPoolResponseDTO>(FN_GET_POOL, new BossRushPoolRequestDTO { poolId = poolId }, (ok, res, raw) =>
        {
            if (!ok || res == null)
            {
                DebugCustom.LogWarning("[BossRush] Failed to get Boss Rush pool | httpSuccess=" + ok + " | response=" + raw);
                callback?.Invoke(false, res);
                return;
            }

            User.SetTickets(res.tickets);
            User.unclaimedPoolId = res.unclaimedPoolId ?? string.Empty;
            if (res.success)
            {
                RewardTable = res.rewardTable;
                SetPool(res.poolId, res.tier, res.eventKey, res.players, res.isFinalized, res.bossNumber, res.bossHP, res.maxBossHP);
            }
            else if (res.removed)
            {
                Pool = null;
                User.currentPoolId = string.Empty;
            }
            GameData.Save();
            SyncEventTickets();
            callback?.Invoke(res.success, res);
        });
    }

    // ===== Start fight (ekt + fi.eki) =====

    // Thành công → dựng BossRushFightSession (≤7 người khác làm ghost) rồi đổi sang BossRushMode.
    public void StartFight(Action<bool, string> callback)
    {
        if (Pool == null)
        {
            callback?.Invoke(false, "Couldn't load Boss Rush. Please try again.");
            return;
        }

        IsBusy = true;
        FirebaseManager.Instance.Call<BossRushStartFightResponseDTO>(FN_START_FIGHT, new BossRushStartFightRequestDTO { poolId = Pool.PoolId }, (ok, res, raw) =>
        {
            IsBusy = false;
            if (res != null) User.SetTickets(res.tickets);
            SyncEventTickets();

            if (!ok || res == null || !res.success || string.IsNullOrEmpty(res.fightToken))
            {
                DebugCustom.LogWarning("[BossRush] Failed to start Boss Rush fight | response=" + raw);
                string message = res != null && res.message == "No fights remaining." ? "No fights remaining today!" : "Failed to start fight. Please try again.";
                callback?.Invoke(false, message);
                return;
            }

            Pool.CurrentBossNumber = res.bossNumber;
            Pool.CurrentBossHP = res.bossHP;
            Pool.MaxBossHP = res.maxBossHP;

            BossRushBossModel boss = new BossRushBossModel
            {
                BossNumber = res.bossNumber,
                CurrentHP = res.bossHP,
                MaxHP = res.maxBossHP,
                AttackPower = Static.GetBossDamage(Pool.Tier),
            };
            BossRushFightSession.Begin(PickAllies(StaticBossRushData.MAX_GHOSTS), boss, Pool.PoolId, res.fightToken, Pool.Tier);
            GameData.Save();
            callback?.Invoke(true, null);

            GameController.Instance.uiLobby.CloseAllTabs();
            GameController.Instance.ChangeMode(ModeType.BossRush);
        });
    }

    // ekz(7): chọn ngẫu nhiên tối đa n người khác (không phải mình) trong nhóm.
    public List<BossRushPlayerModel> PickAllies(int count)
    {
        List<BossRushPlayerModel> others = new List<BossRushPlayerModel>();
        string myId = FirebaseManager.Instance.Uid;
        if (Pool != null)
        {
            for (int i = 0; i < Pool.Players.Count; i++)
            {
                if (Pool.Players[i].UserId != myId)
                {
                    others.Add(Pool.Players[i]);
                }
            }
        }

        List<BossRushPlayerModel> result = new List<BossRushPlayerModel>();
        while (result.Count < count && others.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, others.Count);
            result.Add(others[index]);
            others.RemoveAt(index);
        }
        return result;
    }

    // ===== Report damage (eku/ekw/ekx) =====

    public void ReportDamage(long damageDealt, long totalDamageDealt, Action<bool, BossRushFightResultResponseDTO> callback)
    {
        string token = BossRushFightSession.FightToken;
        if (string.IsNullOrEmpty(token))
        {
            DebugCustom.LogError("[BossRush] ReportDamage: missing fightToken");
            callback?.Invoke(false, null);
            return;
        }

        // Lưu trước — nếu mạng lỗi/app tắt giữa chừng thì lần sau gửi lại.
        User.pendingReport = new BossRushPendingReport
        {
            FightToken = token,
            DamageDealt = damageDealt,
            TotalDamageDealt = totalDamageDealt,
            AttemptedAtUnix = Now(),
            AttemptCount = 0,
        };
        User.isDataChanged = true;
        GameData.Save();

        SendPendingReport(callback);
    }

    private void SendPendingReport(Action<bool, BossRushFightResultResponseDTO> callback)
    {
        BossRushPendingReport report = User.pendingReport;
        if (report == null)
        {
            callback?.Invoke(false, null);
            return;
        }

        report.AttemptCount++;
        report.AttemptedAtUnix = Now();
        BossRushFightResultRequestDTO request = new BossRushFightResultRequestDTO
        {
            fightToken = report.FightToken,
            damageDealt = report.DamageDealt,
            totalDamageDealt = report.TotalDamageDealt,
        };

        FirebaseManager.Instance.Call<BossRushFightResultResponseDTO>(FN_REPORT_DAMAGE, request, (ok, res, raw) =>
        {
            if (!ok || res == null)
            {
                DebugCustom.LogWarning("[BossRush] Failed to report Boss Rush damage | " + raw);
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
            User.SetTickets(res.tickets);
            if (res.success && !res.autoLoss && Pool != null)
            {
                if (res.players != null) Pool.Players = res.players;
                Pool.CurrentBossNumber = res.newBossNumber;
                Pool.CurrentBossHP = res.newBossHP;
                Pool.MaxBossHP = res.newMaxBossHP;
                PoolUpdated?.Invoke(Pool);
            }
            GameData.Save();
            SyncEventTickets();
            callback?.Invoke(res.success, res);
        });
    }

    // ekx: còn báo cáo treo từ lần trước → gửi lại.
    public void RetryPendingReport(Action callback)
    {
        if (User.pendingReport == null)
        {
            callback?.Invoke();
            return;
        }
        SendPendingReport((ok, res) => callback?.Invoke());
    }

    // ===== Claim (eky) =====

    public void Claim(string poolId, Action<BossRushClaimResponseDTO, string> callback)
    {
        FirebaseManager.Instance.Call<BossRushClaimResponseDTO>(FN_CLAIM, new BossRushClaimRequestDTO { poolId = poolId }, (ok, res, raw) =>
        {
            if (!ok || res == null || !res.success)
            {
                DebugCustom.LogWarning("[BossRush] Failed to claim Boss Rush rewards | httpSuccess=" + ok + " | response=" + raw);
                callback?.Invoke(res, "Failed to claim rewards. Please try again.");
                return;
            }

            DebugCustom.Log("[BossRush] Crediting rewards for pool " + poolId);
            GrantRewards(res.rewards);
            User.SetTickets(res.tickets);
            User.tier = res.newTier > 0 ? res.newTier : User.tier;
            User.unclaimedPoolId = string.Empty;
            if (User.currentPoolId == poolId)
            {
                User.currentPoolId = string.Empty;
                Pool = null;
            }
            User.isDataChanged = true;
            GameData.Save(true);
            SyncEventTickets();
            callback?.Invoke(res, null);
        });
    }

    // Cộng thưởng vào túi đồ. Loại chưa có hệ tương ứng trong DungeonRush → log để bổ sung sau.
    public static void GrantRewards(List<RewardEntry> rewards)
    {
        if (rewards == null)
        {
            return;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            RewardEntry r = rewards[i];
            switch (r.Type)
            {
                case RewardType.Bone: GameData.userData.items.Receive(ItemType.BONE, r.Amount); break;
                case RewardType.Gem: GameData.userData.items.Receive(ItemType.GEM, r.Amount); break;
                case RewardType.Vial: GameData.userData.items.Receive(ItemType.VIAL, r.Amount); break;
                case RewardType.Exp: GameData.userData.player.AddExperience(r.Amount); break;
                default:
                    DebugCustom.LogWarning("[BossRush] RewardType chưa hỗ trợ: " + r.Type + " x" + r.Amount);
                    break;
            }
        }
    }

    // ===== Update player (elm/eln) =====

    // ekp/ekq gốc: GameplayUI gọi lúc vào game → elm. Chờ hero spawn để có Power rồi mới gửi.
    public void OnGameStarted()
    {
        StartCoroutine(RoutineUpdateOnGameStarted());
    }

    private IEnumerator RoutineUpdateOnGameStarted()
    {
        float waited = 0f;
        while (waited < WAIT_HERO_TIMEOUT && (GameController.Instance.mode == null || GameController.Instance.mode.Hero == null))
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        UpdatePlayerIfNeeded();
    }

    // elm gốc: đang ở trong nhóm → gửi lại snapshot đồ tối đa 1 lần / NGÀY UTC (chưa gửi bao giờ hoặc ngày
    // hiện tại khác ngày lần gửi trước). Gốc KHÔNG gửi khi đổi đồ; Join cũng gửi snapshot.
    public void UpdatePlayerIfNeeded()
    {
        if (string.IsNullOrEmpty(User.currentPoolId))
        {
            return;
        }
        long now = Now();
        if (User.lastItemUpdateTime >= 1 && now / SECONDS_PER_DAY == User.lastItemUpdateTime / SECONDS_PER_DAY)
        {
            return;
        }

        BossRushUpdateRequestDTO request = new BossRushUpdateRequestDTO { poolId = User.currentPoolId };
        FillSnapshot(request);
        User.lastItemUpdateTime = now;
        User.isDataChanged = true;
        FirebaseManager.Instance.Call<object>(FN_UPDATE_PLAYER, request, (ok, res, raw) =>
        {
            if (!ok) DebugCustom.LogWarning("[BossRush] Failed to update Boss Rush player data | response=" + raw);
        });
    }

    // Admin/test: lấp nhóm bằng bot (chỉ chạy được trên emulator hoặc uid admin).
    public void SeedBots(Action<bool> callback)
    {
        FirebaseManager.Instance.Call<object>(FN_SEED_BOTS, new { count = StaticBossRushData.POOL_SIZE }, (ok, res, raw) =>
        {
            DebugCustom.Log("[BossRush] Seed bots: " + ok + " | " + raw);
            callback?.Invoke(ok);
        });
    }

    // ===== Snapshot đồ (elh/eli/elj/elk/ell) =====

    private static void FillSnapshot(BossRushJoinRequestDTO request)
    {
        request.server = FirebaseSettings.PROJECT_ID;
        request.playerName = GameData.userData.profile.userName;
        request.power = BossRushPower.GetPlayerPower();
        request.items = BuildItems(GameData.userData.equipment);
        request.companions = BuildCompanions(GameData.userData.companions);
    }

    public static List<BossRushItemModel> BuildItems(UserEquipmentData equipment)
    {
        List<BossRushItemModel> list = new List<BossRushItemModel>();
        if (equipment == null || equipment.equipped == null)
        {
            return list;
        }

        foreach (KeyValuePair<string, EquippedItemData> pair in equipment.equipped)
        {
            if (pair.Value == null || string.IsNullOrEmpty(pair.Value.equipId) || !int.TryParse(pair.Key, out int slot))
            {
                continue;
            }

            BossRushItemModel item = new BossRushItemModel
            {
                Slot = slot,
                ItemId = pair.Value.equipId,
                Rarity = (int)pair.Value.rarity,
                ItemLevel = pair.Value.level,
            };
            if (pair.Value.subStats != null)
            {
                for (int i = 0; i < pair.Value.subStats.Count; i++)
                {
                    item.SubStats.Add(new SubStatEntry { Type = pair.Value.subStats[i].type, Value = pair.Value.subStats[i].value });
                }
            }
            list.Add(item);
        }
        return list;
    }

    public static List<BossRushCompanionModel> BuildCompanions(UserCompanionData companions)
    {
        List<BossRushCompanionModel> list = new List<BossRushCompanionModel>();
        if (companions == null || companions.owned == null)
        {
            return list;
        }

        for (int i = 0; i < companions.owned.Count; i++)
        {
            CompanionModel m = companions.owned[i];
            if (m == null || string.IsNullOrEmpty(m.companionId))
            {
                continue;
            }
            list.Add(new BossRushCompanionModel
            {
                CompanionId = m.companionId,
                CompanionLevel = m.level,
                Equipped = companions.equipped != null && companions.equipped.Contains(m.companionId),
            });
        }
        return list;
    }

    // Đổi snapshot server → dữ liệu save tạm để dựng ghost (HeroUnit đọc như save của chính mình).
    public static UserEquipmentData ToEquipmentData(List<BossRushItemModel> items)
    {
        UserEquipmentData data = new UserEquipmentData();
        if (items == null)
        {
            return data;
        }

        for (int i = 0; i < items.Count; i++)
        {
            BossRushItemModel it = items[i];
            if (it == null || string.IsNullOrEmpty(it.ItemId))
            {
                continue;
            }

            List<GearSubStat> subs = new List<GearSubStat>();
            if (it.SubStats != null)
            {
                for (int s = 0; s < it.SubStats.Count; s++)
                {
                    subs.Add(new GearSubStat(it.SubStats[s].Type, it.SubStats[s].Value));
                }
            }
            data.Equip((GearSlotType)it.Slot, it.ItemId, (Rarity)it.Rarity, it.ItemLevel, subs);
        }
        return data;
    }

    // ===== Nội bộ =====

    private void SetPool(string poolId, int tier, string eventKey, List<BossRushPlayerModel> players, bool isFinalized,
        int bossNumber, double bossHp, double maxBossHp)
    {
        Pool = new BossRushPoolModel
        {
            PoolId = poolId,
            Tier = tier,
            EventKey = eventKey,
            Players = players ?? new List<BossRushPlayerModel>(),
            IsFinalized = isFinalized,
            CurrentBossNumber = bossNumber,
            CurrentBossHP = bossHp,
            MaxBossHP = maxBossHp,
        };
        PoolUpdated?.Invoke(Pool);
    }

    // Vé Boss Rush hiển thị ở tab Events (UserEventData) lấy theo server.
    private static void SyncEventTickets()
    {
        EventTickets tickets = GameData.userData.events.Get(EventModeType.BossRush);
        tickets.freeTickets = User.tickets.freeRemaining;
        tickets.adTickets = 0;
        tickets.adClaimedToday = User.tickets.adClaimedToday;
        GameData.userData.events.isDataChanged = true;
    }

    private static long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

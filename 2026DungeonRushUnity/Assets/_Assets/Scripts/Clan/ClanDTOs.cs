using System;
using System.Collections.Generic;
using UnityEngine;

// DTO Clan + Clan War — đúng tên/field gốc (dump il2cpp v41, xem DecodedData/CLAN_MODEL.md).

public enum ClanRole
{
    None = 0,
    Member = 1,
    Captain = 2,
    Leader = 3,
}

public enum ClanJoinSetting
{
    Open = 0,
    ApprovalOnly = 1,
}

[Serializable]
public class ClanBannerData
{
    public int BackgroundTypeId;
    public int BackgroundColorId;
    public int ImageTypeId;
    public int ImageColorId;

    public ClanBannerData() { }

    public ClanBannerData(int backgroundTypeId, int backgroundColorId, int imageTypeId, int imageColorId)
    {
        BackgroundTypeId = backgroundTypeId;
        BackgroundColorId = backgroundColorId;
        ImageTypeId = imageTypeId;
        ImageColorId = imageColorId;
    }

    // frb gốc: bản sao.
    public ClanBannerData Clone()
    {
        return new ClanBannerData(BackgroundTypeId, BackgroundColorId, ImageTypeId, ImageColorId);
    }
}

// ===== Request =====

public class ClanCreateRequestDTO
{
    public string clanName;
    public string joinSetting;
    public int bannerBackgroundTypeId;
    public int bannerBackgroundColorId;
    public int bannerImageTypeId;
    public int bannerImageColorId;
    public string server;
    public string playerName;
    public long power;
    public int avatarId;
}

public class ClanUpdateSettingsRequestDTO
{
    public string clanId;
    public string description;
    public string joinSetting;
    public int bannerBackgroundTypeId;
    public int bannerBackgroundColorId;
    public int bannerImageTypeId;
    public int bannerImageColorId;
}

public class ClanUpdateAnnouncementRequestDTO
{
    public string clanId;
    public string announcement;
}

public class ClanSearchRequestDTO
{
    public string server;
    public string clanName;
    public int? minMemberCount;
    public int? maxMemberCount;
    public bool hideApprovalOnly;
}

public class ClanGetDetailsRequestDTO
{
    public string clanId;
    public long power;
    public string playerName;
}

public class ClanJoinActionRequestDTO
{
    public string clanId;
    public string server;
    public string playerName;
    public long power;
    public int avatarId;
}

public class ClanTargetRequestDTO
{
    public string clanId;
    public string targetUserId;
}

public class ClanIdRequestDTO
{
    public string clanId;
}

// ===== Response =====

public class ClanBaseResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public string contentVersion;
}

public class ClanResponseDTO : ClanBaseResponseDTO
{
    public ClanWireDTO clan;
}

public class ClanSearchResponseDTO : ClanBaseResponseDTO
{
    public List<ClanWireDTO> clans;
    public List<string> requestedClanIds;
}

public class ClanAnnouncementResponseDTO : ClanBaseResponseDTO
{
    public string announcement;
}

public class ClanRequestsListResponseDTO : ClanBaseResponseDTO
{
    public List<ClanRequestWireDTO> requests;
}

public class ClanCreateRequestResponseDTO : ClanBaseResponseDTO
{
    public bool pending;
    public string clanId;
}

public class ClanPromoteResponseDTO : ClanBaseResponseDTO
{
    public string targetUserId;
    public string newRole;
}

public class ClanLeaveResponseDTO : ClanBaseResponseDTO
{
    public bool disbanded;
}

public class ClanWireDTO
{
    public string clanId;
    public string clanName;
    public string clanTier;
    public string joinSetting;
    public string server;
    public string description;
    public string announcement;
    public string myRole;
    public int bannerBackgroundTypeId;
    public int bannerBackgroundColorId;
    public int bannerImageTypeId;
    public int bannerImageColorId;
    public int memberCount;
    public long totalPower;
    public List<ClanMemberWireDTO> members;
}

public class ClanMemberWireDTO
{
    public string userId;
    public string playerName;
    public long power;
    public string role;
    public int avatarId;
    public long joinedAt;
}

public class ClanRequestWireDTO
{
    public string userId;
    public string playerName;
    public long power;
    public int avatarId;
    public long requestedAt;
}

// ===== Model client (io / in / im / ip gốc) =====

public class ClanMemberModel
{
    public string UserId;
    public string PlayerName;
    public long Power;
    public ClanRole Role;
    public int AvatarId;
    public long JoinedAt;

    public static ClanMemberModel From(ClanMemberWireDTO w)
    {
        return new ClanMemberModel
        {
            UserId = w.userId,
            PlayerName = w.playerName,
            Power = w.power,
            Role = ClanRoleExt.ParseRole(w.role),
            AvatarId = w.avatarId,
            JoinedAt = w.joinedAt,
        };
    }
}

public class ClanRequestModel
{
    public string UserId;
    public string PlayerName;
    public long Power;
    public int AvatarId;
    public long RequestedAt;

    public static ClanRequestModel From(ClanRequestWireDTO w)
    {
        return new ClanRequestModel
        {
            UserId = w.userId,
            PlayerName = w.playerName,
            Power = w.power,
            AvatarId = w.avatarId,
            RequestedAt = w.requestedAt,
        };
    }
}

public class ClanModel
{
    public string ClanId;
    public string ClanName;
    public string ClanTier;
    public string Server;
    public ClanJoinSetting JoinSetting;
    public ClanBannerData Banner;
    public string Description;
    public string Announcement;
    public int MemberCount;
    public long TotalPower;
    public List<ClanMemberModel> Members = new List<ClanMemberModel>();
    public ClanRole MyRole;

    // frr gốc: memberCount > 49.
    public bool IsFull => MemberCount >= StaticClanData.MAX_MEMBERS;

    public static ClanModel From(ClanWireDTO w)
    {
        ClanModel m = new ClanModel
        {
            ClanId = w.clanId,
            ClanName = w.clanName,
            ClanTier = string.IsNullOrEmpty(w.clanTier) ? "D" : w.clanTier,
            Server = w.server,
            JoinSetting = ClanRoleExt.ParseJoinSetting(w.joinSetting),
            Banner = new ClanBannerData(w.bannerBackgroundTypeId, w.bannerBackgroundColorId, w.bannerImageTypeId, w.bannerImageColorId),
            Description = w.description ?? string.Empty,
            Announcement = w.announcement ?? string.Empty,
            MemberCount = w.memberCount,
            TotalPower = w.totalPower,
            MyRole = ClanRoleExt.ParseRole(w.myRole),
        };
        if (w.members != null)
        {
            for (int i = 0; i < w.members.Count; i++) m.Members.Add(ClanMemberModel.From(w.members[i]));
        }
        return m;
    }
}

// Bộ lọc tìm kiếm (ip gốc).
public class ClanSearchFilter
{
    public string ClanName;
    public int? MinMemberCount;
    public int? MaxMemberCount;
    public bool HideApprovalOnly;

    // frt gốc: không lọc gì.
    public bool IsEmpty => string.IsNullOrEmpty(ClanName) && !MinMemberCount.HasValue && !MaxMemberCount.HasValue && !HideApprovalOnly;
}

// Helper role/join/tier (class il gốc).
public static class ClanRoleExt
{
    public const string ROLE_LEADER = "leader";
    public const string ROLE_CAPTAIN = "captain";
    public const string ROLE_MEMBER = "member";
    public const string JOIN_OPEN = "open";
    public const string JOIN_APPROVAL = "approval";

    // Màu gốc (il.cctor).
    private static readonly Color COLOR_DEFAULT = Hex("9E9E9E");
    private static readonly Color COLOR_D = Hex("4CAF50");
    private static readonly Color COLOR_C = Hex("29B6F6");
    private static readonly Color COLOR_B = Hex("AB47BC");
    private static readonly Color COLOR_A = Hex("FF9800");
    private static readonly Color COLOR_S = Hex("FFD54F");
    private static readonly Color COLOR_OPEN = Hex("4CAF50");
    private static readonly Color COLOR_APPROVAL = Hex("FFA726");

    public static ClanRole ParseRole(string s)
    {
        switch ((s ?? string.Empty).Trim().ToLowerInvariant())
        {
            case ROLE_LEADER: return ClanRole.Leader;
            case ROLE_CAPTAIN: return ClanRole.Captain;
            case ROLE_MEMBER: return ClanRole.Member;
            default: return ClanRole.None;
        }
    }

    public static string ToWire(this ClanRole role)
    {
        switch (role)
        {
            case ClanRole.Leader: return ROLE_LEADER;
            case ClanRole.Captain: return ROLE_CAPTAIN;
            case ClanRole.Member: return ROLE_MEMBER;
            default: return string.Empty;
        }
    }

    public static ClanJoinSetting ParseJoinSetting(string s)
    {
        string v = (s ?? string.Empty).Trim().ToLowerInvariant();
        return v == JOIN_APPROVAL || v == "approvalonly" ? ClanJoinSetting.ApprovalOnly : ClanJoinSetting.Open;
    }

    public static string ToWire(this ClanJoinSetting setting)
    {
        return setting == ClanJoinSetting.ApprovalOnly ? JOIN_APPROVAL : JOIN_OPEN;
    }

    public static bool IsLeader(ClanRole role) => role == ClanRole.Leader;

    // frh/fri gốc: Captain hoặc Leader.
    public static bool CanManage(ClanRole role) => role == ClanRole.Captain || role == ClanRole.Leader;

    // frk gốc: Captain → Member; Leader → Member/Captain.
    public static bool CanActOn(ClanRole actor, ClanRole target)
    {
        if (actor == ClanRole.Captain) return target == ClanRole.Member;
        if (actor == ClanRole.Leader) return target == ClanRole.Member || target == ClanRole.Captain;
        return false;
    }

    public static string RoleName(ClanRole role)
    {
        switch (role)
        {
            case ClanRole.Leader: return "Leader";
            case ClanRole.Captain: return "Captain";
            case ClanRole.Member: return "Member";
            default: return string.Empty;
        }
    }

    // frl gốc.
    public static Color TierColor(string tier)
    {
        switch ((tier ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "S": return COLOR_S;
            case "A": return COLOR_A;
            case "B": return COLOR_B;
            case "C": return COLOR_C;
            case "D": return COLOR_D;
            default: return COLOR_DEFAULT;
        }
    }

    // frm gốc: "<color=#hex>tier</color>".
    public static string TierText(string tier)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(TierColor(tier)) + ">" + tier + "</color>";
    }

    // frn / fro gốc.
    public static string JoinSettingText(ClanJoinSetting setting)
    {
        return setting == ClanJoinSetting.ApprovalOnly ? "Approval Only" : "Open";
    }

    public static Color JoinSettingColor(ClanJoinSetting setting)
    {
        return setting == ClanJoinSetting.ApprovalOnly ? COLOR_APPROVAL : COLOR_OPEN;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}

// ===================== CLAN WAR =====================

public class ClanWarStateResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public bool inClan;
    public ClanWarWireDTO war;
    public long nextWeekStartsAt;
    public long serverTime;
    public ClanWarConfigDTO config;
    public string contentVersion;
}

public class ClanWarWireDTO
{
    public string warId;
    public string weekId;
    public string type;
    public string state;
    public int activeDay;
    public long dayEndsAt;
    public long cooldownEndsAt;
    public ClanWarClanSideDTO myClan;
    public ClanWarClanSideDTO enemyClan;
    public int myWarScore;
    public int enemyWarScore;
    public string winnerClanId;
    public string leaderClanId;
    public bool? won;
    public ClanWarLiveBarDTO liveBar;
    public int myDailyPoints;
    public int myWeeklyPoints;
    public ClanWarMvpDTO myMvp;
    public ClanWarMvpDTO enemyMvp;
    public int myMemberCount;
    public int enemyMemberCount;
    public ClanWarPvpDTO pvp;
    public ClanWarClaimsDTO claims;
    public ClanWarMilestoneDTO milestone;
    public List<ClanWarDailyResultDTO> dailyResults;

    // gax/gay/gaz/gba gốc.
    public bool IsLeadership => type == "leadership";
    public bool IsDay => state == "day";
    public bool IsCooldown => state == "cooldown";
    public bool IsDay6 => state == "day6";
    public bool IsConcluded => state == "concluded";
}

public class ClanWarClanSideDTO
{
    public string clanId;
    public string clanName;
    public int bannerBackgroundTypeId;
    public int bannerBackgroundColorId;
    public int bannerImageTypeId;
    public int bannerImageColorId;
    public string server;
    public string rewardTier;
    public bool isBot;

    public ClanBannerData Banner => new ClanBannerData(bannerBackgroundTypeId, bannerBackgroundColorId, bannerImageTypeId, bannerImageColorId);
}

public class ClanWarLiveBarDTO
{
    public int myDaily;
    public int enemyDaily;
    public int myWeekly;
    public int enemyWeekly;
}

public class ClanWarMvpDTO
{
    public string userId;
    public string playerName;
    public int avatarId;
}

public class ClanWarPvpDTO
{
    public int round;
    public int resetCount;
    public int maxResets;
    public bool resetEnabled;
    public int resetThreshold;
    public int targetCount;
    public int defeatedCount;
    public int myTickets;
    public int ticketsMax;
    public bool canAttack;
    public List<ClanWarPvpTargetDTO> targets;
}

public class ClanWarPvpTargetDTO
{
    public string userId;
    public string playerName;
    public double power;
    public int avatarId;
    public bool defeated;
}

public class ClanWarClaimsDTO
{
    public string outcome;
    public bool clanRewardClaimed;
    public bool clanRewardEligible;
    public bool personalBundleClaimed;
    public ClanWarRewardsDTO clanReward;
}

public class ClanWarMilestoneDTO
{
    public string weekId;
    public int weeklyContribution;
    public List<int> claimedMilestones;
    public bool bundleClaimed;
}

public class ClanWarDailyResultDTO
{
    public int day;
    public int warScore;
    public string winnerClanId;
    public string winnerClanName;
    public ClanWarMvpDTO winnerMvp;
    public bool myClanWon;
    public int myDailyTotal;
    public int enemyDailyTotal;
}

public class ClanWarConfigDTO
{
    public Dictionary<string, List<string>> sourcesByDay;
    public ClanWarAwardsDTO awards;
    public List<ClanWarMilestoneRowDTO> individualMilestones;
    public Dictionary<string, Dictionary<string, ClanWarRewardsDTO>> clanRewards;
    public Dictionary<string, int> warScoreByDay;
    public ClanWarPvpConfigDTO pvp;
}

public class ClanWarAwardsDTO
{
    public Dictionary<string, int> lootEquipment;
    public Dictionary<string, int> summonCompanion;
    public int levelUpMultiplier;
    public Dictionary<string, int> mining;
    public int dungeonKey;
    public Dictionary<string, int> summonCape;
    public int pvpWin;
}

public class ClanWarMilestoneRowDTO
{
    public int threshold;
    public int lootBox;
    public int bones;
    public int pickaxe;
    public int experience;
    public int cloakCurrency;

    public ClanWarRewardsDTO ToRewards()
    {
        return new ClanWarRewardsDTO { lootBox = lootBox, bones = bones, pickaxe = pickaxe, experience = experience, cloakCurrency = cloakCurrency };
    }
}

public class ClanWarPvpConfigDTO
{
    public int startTickets;
    public int winPoints;
    public int maxRoundResets;
    public int minMembersForReset;
}

[Serializable]
public class ClanWarActionDTO
{
    public string actionId;
    public string source;
    public string rarity;
    public string resource;
    public int newLevel;
    public int count;
    public long eventTimestamp;
}

public class ClanWarRecordResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public bool duplicate;
    public int accepted;
    public int dropped;
    public int points;
    public long serverTime;
}

public class ClanWarPvpStartResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public string battleToken;
    public long battleTokenExpiresAt;
    public PvPPlayerModel opponent;
    public int tickets;
    public long serverTime;
}

public class ClanWarPvpReportResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public bool won;
    public int pointsAwarded;
    public bool alreadyDefeated;
    public int defeatedCount;
    public bool roundReset;
    public int round;
    public int tickets;
    public long serverTime;
}

public class ClanWarRewardsDTO
{
    public int lootBox;
    public int bones;
    public int pickaxe;
    public int experience;
    public int cloakCurrency;
    public int goldenPickaxe;
    public int drill;
    public int vial;

    // gbc gốc.
    public bool IsEmpty => lootBox <= 0 && bones <= 0 && pickaxe <= 0 && experience <= 0 && cloakCurrency <= 0
                           && goldenPickaxe <= 0 && drill <= 0 && vial <= 0;

    public void Add(ClanWarRewardsDTO o)
    {
        if (o == null) return;
        lootBox += o.lootBox; bones += o.bones; pickaxe += o.pickaxe; experience += o.experience;
        cloakCurrency += o.cloakCurrency; goldenPickaxe += o.goldenPickaxe; drill += o.drill; vial += o.vial;
    }

    // Đổi sang RewardEntry để cộng bằng BossRushController.GrantRewards.
    public List<RewardEntry> ToRewardEntries()
    {
        List<RewardEntry> list = new List<RewardEntry>();
        AddEntry(list, RewardType.Lootbox, lootBox);
        AddEntry(list, RewardType.Bone, bones);
        AddEntry(list, RewardType.MiningPickaxe, pickaxe);
        AddEntry(list, RewardType.Exp, experience);
        AddEntry(list, RewardType.CloakCurrency, cloakCurrency);
        AddEntry(list, RewardType.MiningGoldenPickaxe, goldenPickaxe);
        AddEntry(list, RewardType.MiningDrill, drill);
        AddEntry(list, RewardType.Vial, vial);
        return list;
    }

    private static void AddEntry(List<RewardEntry> list, RewardType type, int amount)
    {
        if (amount > 0) list.Add(new RewardEntry { Type = type, Amount = amount });
    }
}

public class ClanWarClaimResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public int threshold;
    public List<int> thresholds;
    public string outcome;
    public ClanWarRewardsDTO rewards;
    public long serverTime;
}

public class ClanWarContributionEntryDTO
{
    public int rank;
    public string userId;
    public string playerName;
    public int avatarId;
    public int points;
}

public class ClanWarContributionLeaderboardResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public string mode;
    public int day;
    public string clanId;
    public List<ClanWarContributionEntryDTO> entries;
}

public class ClanWarClanRowDTO
{
    public int rank;
    public string clanId;
    public string clanName;
    public int bannerBackgroundTypeId;
    public int bannerBackgroundColorId;
    public int bannerImageTypeId;
    public int bannerImageColorId;
    public string tier;
    public int tierPoints;
    public long totalPower;
    public string server;

    public ClanBannerData Banner => new ClanBannerData(bannerBackgroundTypeId, bannerBackgroundColorId, bannerImageTypeId, bannerImageColorId);
}

public class ClanWarClanLeaderboardResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public ClanWarClanRowDTO leader;
    public List<ClanWarClanRowDTO> rows;
    public string myClanId;
}

public class ClanWarLeadershipRankingResponseDTO
{
    public bool success;
    public string code;
    public string message;
    public string weekId;
    public List<ClanWarClanRowDTO> rows;
    public string myClanId;
}

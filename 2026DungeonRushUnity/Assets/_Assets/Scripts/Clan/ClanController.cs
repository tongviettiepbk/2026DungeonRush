using System;
using System.Collections.Generic;
using UnityEngine;

// Cổng nghiệp vụ Clan phía client — port ClanController gốc (fps..fql). Gọi 13 Cloud Function qua FirebaseManager,
// giữ clan hiện tại (vhp), phát ClanUpdated khi đổi, lưu ClanId/Role/Banner vào UserClanData.
public class ClanController : Singleton<ClanController>
{
    public const string FN_CREATE = "createclan";
    public const string FN_UPDATE_SETTINGS = "updateclansettings";
    public const string FN_UPDATE_ANNOUNCEMENT = "updateclanannouncement";
    public const string FN_SEARCH = "searchclans";
    public const string FN_DETAILS = "getclandetails";
    public const string FN_JOIN_OPEN = "joinopenclan";
    public const string FN_CREATE_REQUEST = "createclanjoinrequest";
    public const string FN_GET_REQUESTS = "getclanjoinrequests";
    public const string FN_ACCEPT = "acceptclanjoinrequest";
    public const string FN_DENY = "denyclanjoinrequest";
    public const string FN_PROMOTE = "promoteordemoteclanmember";
    public const string FN_KICK = "kickclanmember";
    public const string FN_LEAVE = "leaveclan";

    // ClanUpdated gốc (null = rời/không có clan).
    public static event Action<ClanModel> ClanUpdated;

    public ClanModel CurrentClan { get; private set; }

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
    }

    // fpo / fpp gốc.
    public bool HasClan => CurrentClan != null || User.HasClan;
    public ClanRole MyRole => CurrentClan != null ? CurrentClan.MyRole : ClanRoleExt.ParseRole(User.clanRole);
    public string MyUserId => FirebaseManager.Instance.Uid;

    public static bool IsUnlocked()
    {
        return GameData.userData.player.playerLevel >= StaticClanData.UNLOCK_PLAYER_LEVEL;
    }

    // ===== createClan (fps) =====

    public void Create(string clanName, ClanJoinSetting joinSetting, ClanBannerData banner, Action<bool, ClanModel, string> callback)
    {
        ClanCreateRequestDTO request = new ClanCreateRequestDTO
        {
            clanName = clanName,
            joinSetting = joinSetting.ToWire(),
            bannerBackgroundTypeId = banner.BackgroundTypeId,
            bannerBackgroundColorId = banner.BackgroundColorId,
            bannerImageTypeId = banner.ImageTypeId,
            bannerImageColorId = banner.ImageColorId,
            server = FirebaseSettings.PROJECT_ID,
            playerName = PlayerName(),
            power = Power(),
            avatarId = 0,
        };
        CallClan<ClanResponseDTO>(FN_CREATE, request, "Failed to create clan", (ok, res, error) =>
        {
            if (ok) SetCurrent(ClanModel.From(res.clan));
            callback?.Invoke(ok, ok ? CurrentClan : null, error);
        });
    }

    // ===== updateClanSettings (fpt) =====

    public void UpdateSettings(string description, ClanJoinSetting joinSetting, ClanBannerData banner, Action<bool, ClanModel, string> callback)
    {
        if (CurrentClan == null)
        {
            callback?.Invoke(false, null, ErrorText("ERR_NOT_IN_CLAN", null));
            return;
        }
        ClanUpdateSettingsRequestDTO request = new ClanUpdateSettingsRequestDTO
        {
            clanId = CurrentClan.ClanId,
            description = description,
            joinSetting = joinSetting.ToWire(),
            bannerBackgroundTypeId = banner.BackgroundTypeId,
            bannerBackgroundColorId = banner.BackgroundColorId,
            bannerImageTypeId = banner.ImageTypeId,
            bannerImageColorId = banner.ImageColorId,
        };
        CallClan<ClanResponseDTO>(FN_UPDATE_SETTINGS, request, "Failed to save changes", (ok, res, error) =>
        {
            if (ok) SetCurrent(ClanModel.From(res.clan));
            callback?.Invoke(ok, ok ? CurrentClan : null, error);
        });
    }

    // ===== updateClanAnnouncement (fpu) =====

    public void UpdateAnnouncement(string announcement, Action<bool, string> callback)
    {
        if (CurrentClan == null)
        {
            callback?.Invoke(false, ErrorText("ERR_NOT_IN_CLAN", null));
            return;
        }
        ClanUpdateAnnouncementRequestDTO request = new ClanUpdateAnnouncementRequestDTO { clanId = CurrentClan.ClanId, announcement = announcement };
        CallClan<ClanAnnouncementResponseDTO>(FN_UPDATE_ANNOUNCEMENT, request, "Failed to save changes", (ok, res, error) =>
        {
            if (ok)
            {
                CurrentClan.Announcement = res.announcement ?? string.Empty;
                ClanUpdated?.Invoke(CurrentClan);
            }
            callback?.Invoke(ok, error);
        });
    }

    // ===== searchClans (fpv) =====

    public void Search(ClanSearchFilter filter, Action<bool, List<ClanModel>, List<string>, string> callback)
    {
        filter = filter ?? new ClanSearchFilter();
        ClanSearchRequestDTO request = new ClanSearchRequestDTO
        {
            server = FirebaseSettings.PROJECT_ID,
            clanName = string.IsNullOrEmpty(filter.ClanName) ? null : filter.ClanName.Trim(),
            minMemberCount = filter.MinMemberCount,
            maxMemberCount = filter.MaxMemberCount,
            hideApprovalOnly = filter.HideApprovalOnly,
        };
        CallClan<ClanSearchResponseDTO>(FN_SEARCH, request, "Search failed", (ok, res, error) =>
        {
            List<ClanModel> clans = new List<ClanModel>();
            if (ok && res.clans != null)
            {
                for (int i = 0; i < res.clans.Count; i++) clans.Add(ClanModel.From(res.clans[i]));
            }
            callback?.Invoke(ok, clans, ok && res.requestedClanIds != null ? res.requestedClanIds : new List<string>(), error);
        });
    }

    // ===== getClanDetails (fpw = clan khác, fpx = clan của mình) =====

    public void GetDetails(string clanId, Action<bool, ClanModel, string> callback)
    {
        ClanGetDetailsRequestDTO request = new ClanGetDetailsRequestDTO { clanId = clanId, power = Power(), playerName = PlayerName() };
        CallClan<ClanResponseDTO>(FN_DETAILS, request, "Failed to load clan data", (ok, res, error) =>
        {
            callback?.Invoke(ok, ok ? ClanModel.From(res.clan) : null, error);
        });
    }

    public void LoadMyClan(Action<bool, ClanModel, string> callback)
    {
        ClanGetDetailsRequestDTO request = new ClanGetDetailsRequestDTO { clanId = string.Empty, power = Power(), playerName = PlayerName() };
        CallClan<ClanResponseDTO>(FN_DETAILS, request, "Failed to load clan data", (ok, res, error, code) =>
        {
            if (ok)
            {
                SetCurrent(ClanModel.From(res.clan));
            }
            else if (code == "ERR_NOT_IN_CLAN" || code == "ERR_NOT_FOUND")
            {
                // Không còn clan (bị kick / giải tán) → xoá cache, coi như tải thành công (hiện màn tìm clan).
                SetCurrent(null);
                callback?.Invoke(true, null, null);
                return;
            }
            callback?.Invoke(ok, ok ? CurrentClan : null, error);
        });
    }

    // ===== joinOpenClan (fpy) / createClanJoinRequest (fpz) =====

    public void JoinOpen(string clanId, Action<bool, ClanModel, string> callback)
    {
        CallClan<ClanResponseDTO>(FN_JOIN_OPEN, BuildJoin(clanId), "Failed to join clan", (ok, res, error) =>
        {
            if (ok) SetCurrent(ClanModel.From(res.clan));
            callback?.Invoke(ok, ok ? CurrentClan : null, error);
        });
    }

    public void RequestJoin(string clanId, Action<bool, string> callback)
    {
        CallClan<ClanCreateRequestResponseDTO>(FN_CREATE_REQUEST, BuildJoin(clanId), "Failed to send request", (ok, res, error) =>
        {
            callback?.Invoke(ok, error);
        });
    }

    // ===== Request (fqa/fqb/fqc) =====

    public void GetRequests(Action<bool, List<ClanRequestModel>, string> callback)
    {
        if (CurrentClan == null)
        {
            callback?.Invoke(false, null, ErrorText("ERR_NOT_IN_CLAN", null));
            return;
        }
        CallClan<ClanRequestsListResponseDTO>(FN_GET_REQUESTS, new ClanIdRequestDTO { clanId = CurrentClan.ClanId }, "Failed to load clan data", (ok, res, error) =>
        {
            List<ClanRequestModel> list = new List<ClanRequestModel>();
            if (ok && res.requests != null)
            {
                for (int i = 0; i < res.requests.Count; i++) list.Add(ClanRequestModel.From(res.requests[i]));
            }
            callback?.Invoke(ok, list, error);
        });
    }

    public void AcceptRequest(string targetUserId, Action<bool, string> callback)
    {
        TargetAction(FN_ACCEPT, targetUserId, "Failed to accept request", true, callback);
    }

    public void DenyRequest(string targetUserId, Action<bool, string> callback)
    {
        TargetAction(FN_DENY, targetUserId, "Failed to deny request", false, callback);
    }

    // ===== promoteOrDemote (fqd) / kick (fqe) =====

    public void PromoteOrDemote(string targetUserId, Action<bool, ClanRole, string> callback)
    {
        if (CurrentClan == null)
        {
            callback?.Invoke(false, ClanRole.None, ErrorText("ERR_NOT_IN_CLAN", null));
            return;
        }
        ClanTargetRequestDTO request = new ClanTargetRequestDTO { clanId = CurrentClan.ClanId, targetUserId = targetUserId };
        CallClan<ClanPromoteResponseDTO>(FN_PROMOTE, request, "Failed to change role", (ok, res, error) =>
        {
            ClanRole role = ok ? ClanRoleExt.ParseRole(res.newRole) : ClanRole.None;
            if (ok) UpdateMemberRole(targetUserId, role);
            callback?.Invoke(ok, role, error);
        });
    }

    public void Kick(string targetUserId, Action<bool, string> callback)
    {
        if (CurrentClan == null)
        {
            callback?.Invoke(false, ErrorText("ERR_NOT_IN_CLAN", null));
            return;
        }
        ClanTargetRequestDTO request = new ClanTargetRequestDTO { clanId = CurrentClan.ClanId, targetUserId = targetUserId };
        CallClan<ClanBaseResponseDTO>(FN_KICK, request, "Failed to kick member", (ok, res, error) =>
        {
            if (ok) RemoveMember(targetUserId);
            callback?.Invoke(ok, error);
        });
    }

    // ===== leaveClan (fqf) =====

    public void Leave(Action<bool, bool, string> callback)
    {
        CallClan<ClanLeaveResponseDTO>(FN_LEAVE, new object(), "Failed to leave clan", (ok, res, error, code) =>
        {
            if (ok || code == "ERR_NOT_IN_CLAN")
            {
                SetCurrent(null);
                callback?.Invoke(true, ok && res.disbanded, null);
                return;
            }
            callback?.Invoke(false, false, error);
        });
    }

    // ===== Nội bộ =====

    private void TargetAction(string fn, string targetUserId, string fallback, bool reloadAfter, Action<bool, string> callback)
    {
        if (CurrentClan == null)
        {
            callback?.Invoke(false, ErrorText("ERR_NOT_IN_CLAN", null));
            return;
        }
        ClanTargetRequestDTO request = new ClanTargetRequestDTO { clanId = CurrentClan.ClanId, targetUserId = targetUserId };
        CallClan<ClanBaseResponseDTO>(fn, request, fallback, (ok, res, error) =>
        {
            callback?.Invoke(ok, error);
            if (ok && reloadAfter) LoadMyClan(null);
        });
    }

    private ClanJoinActionRequestDTO BuildJoin(string clanId)
    {
        return new ClanJoinActionRequestDTO
        {
            clanId = clanId,
            server = FirebaseSettings.PROJECT_ID,
            playerName = PlayerName(),
            power = Power(),
            avatarId = 0,
        };
    }

    // fqg gốc: lưu clan + phát ClanUpdated; null = rời clan (fqh).
    private void SetCurrent(ClanModel clan)
    {
        CurrentClan = clan;
        User.SetClan(clan);
        GameData.Save();
        ClanUpdated?.Invoke(clan);
    }

    // fqi gốc.
    private void UpdateMemberRole(string userId, ClanRole role)
    {
        if (CurrentClan == null) return;
        for (int i = 0; i < CurrentClan.Members.Count; i++)
        {
            if (CurrentClan.Members[i].UserId == userId) CurrentClan.Members[i].Role = role;
        }
        ClanUpdated?.Invoke(CurrentClan);
    }

    // fqj gốc.
    private void RemoveMember(string userId)
    {
        if (CurrentClan == null) return;
        CurrentClan.Members.RemoveAll(x => x.UserId == userId);
        CurrentClan.MemberCount = CurrentClan.Members.Count;
        long total = 0;
        for (int i = 0; i < CurrentClan.Members.Count; i++) total += CurrentClan.Members[i].Power;
        CurrentClan.TotalPower = total;
        ClanUpdated?.Invoke(CurrentClan);
    }

    private void CallClan<T>(string fn, object request, string fallback, Action<bool, T, string> callback) where T : ClanBaseResponseDTO
    {
        CallClan<T>(fn, request, fallback, (ok, res, error, code) => callback(ok, res, error));
    }

    private void CallClan<T>(string fn, object request, string fallback, Action<bool, T, string, string> callback) where T : ClanBaseResponseDTO
    {
        FirebaseManager.Instance.Call<T>(fn, request, (ok, res, raw) =>
        {
            if (!ok || res == null)
            {
                DebugCustom.LogWarning("[Clan] " + fn + " failed | " + raw);
                callback(false, res, "Connection failed", res != null ? res.code : null);
                return;
            }
            if (!res.success)
            {
                DebugCustom.LogWarning("[Clan] " + fn + " rejected | " + res.code + " " + res.message);
                callback(false, res, ErrorText(res.code, fallback), res.code);
                return;
            }
            callback(true, res, null, null);
        });
    }

    // ij.fqp gốc: ERR_* → câu báo lỗi (Errors.Clan.* tiếng Anh trong strings_en).
    public static string ErrorText(string code, string fallback)
    {
        switch (code)
        {
            case "ERR_NOT_FOUND": return "Clan not found";
            case "ERR_CONFLICT_STATE_CHANGED": return "State changed, refresh and retry";
            case "ERR_NOT_IN_CLAN": return "You are not in a clan";
            case "ERR_FORBIDDEN": return "You don't have permission to do that";
            case "ERR_VALIDATION": return "Invalid input";
            case "ERR_ALREADY_IN_CLAN": return "You are already in a clan";
            case "ERR_REQUEST_ALREADY_PENDING": return "Request already pending";
            case "ERR_REQUEST_COOLDOWN": return "Daily join request limit reached";
            case "ERR_REQUEST_NOT_PENDING": return "This request is no longer pending";
            case "ERR_CLAN_FULL": return "Clan is full";
            case "ERR_CAPTAIN_LIMIT": return "Already have the maximum number of captains";
            case "ERR_NAME_TAKEN": return "This clan name is already taken";
            case "ERR_JOIN_COOLDOWN": return "You must wait 24 hours after leaving a clan before joining another.";
            case "ERR_INTERNAL": return "Something went wrong, please try again";
            default: return string.IsNullOrEmpty(fallback) ? "Something went wrong, please try again" : fallback;
        }
    }

    public static string PlayerName()
    {
        string name = GameData.userData.profile.userName;
        return string.IsNullOrEmpty(name) ? "Player" : name;
    }

    // fpq gốc: Power hiện tại (rm.iqm).
    public static long Power()
    {
        return (long)Math.Max(0d, BossRushPower.GetPlayerPower());
    }

    public static long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

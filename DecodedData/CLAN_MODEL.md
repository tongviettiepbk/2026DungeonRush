# CLAN + CLAN WAR — mô hình gốc (reverse v41) + cách làm lại

Nguồn: `libil2cpp.so` + `global-metadata.dat` (xapk v41) qua Il2CppDumper + capstone (chuỗi mã hoá giải bằng `decstr`);
prefab rip `AssetRipper/ExportedProject/Assets/GameObject/Clan*.prefab` (CÒN NGUYÊN field wiring gốc theo fileID —
trích bằng `tools/rip_fieldmap.py` → `tools/_rewire/clan_fieldmap.json`); `localization/strings_en.json`; `tables/ClanBannerCatalog.json`.
Ký hiệu: **[CHẮC]** = đọc thẳng từ code/data; **[SUY]** = logic nằm ở server gốc (không có trong APK) → tự thiết kế, để data-driven.

---

## 1. Clan

### 1.1 Hằng số (`ik`, `ClanController.vhk`, CreateClanPopup) [CHẮC]

| Giá trị | Ý nghĩa | Bằng chứng |
|---|---|---|
| 15 | PlayerLevel mở tab Clan | `ClanController.vhk = 15` |
| 100 gem | Phí tạo clan | `CreateClanPopup.fxy`: `gems > 99`, trừ `0x64` gem (`clan_create`) ; chữ nút `Clan.Create.Button` = "Create \n<sprite=0>{0}" |
| 3..15, `^[A-Za-z0-9]+$` | Tên clan | `ik.vht/vhv`, `Errors.Clan.InvalidName` "3-15 characters (letters and numbers only)" + lọc từ bậy `rp.ire` → `Errors.Clan.Profanity` |
| 300 | Ký tự announcement | `EditAnnouncementPopup.eip` `characterLimit = 0x12c`, bộ đếm "{0}/{1}" |
| 200 | Ký tự description | `ClanSettingsPopup.eip` `characterLimit = 0xc8` |
| 50 | Thành viên tối đa | `ik.vhs`, `AdvancedSearchPopup.frv = 0x32` (max của bộ lọc), `io.frr`: `memberCount > 49` = đầy |
| 3 | Captain tối đa | `ik.vhu` (+ lỗi `ERR_CAPTAIN_LIMIT`) [SUY gán] |
| 5 | Lượt xin vào / ngày | `ik.via`, `Errors.Clan.RequestCooldown` "Daily join request limit reached" [SUY gán] |
| 24h | Chờ sau khi rời clan | `Errors.Clan.JoinCooldown` "You must wait 24 hours after leaving a clan" |
| 20 | Kết quả tìm kiếm | `ik.vhy` [SUY gán] |

### 1.2 Vai trò / kiểu gia nhập [CHẮC]
`ClanRole` None 0 / Member 1 / Captain 2 / Leader 3 (chuỗi wire `leader|captain|member`).
`ClanJoinSetting` Open 0 / ApprovalOnly 1 (wire `open|approval`, nhận cả `approvalonly`).
- `il.frg/frj` = Leader; `il.frh/fri` = Captain hoặc Leader (quản lý request, sửa announcement).
- `il.frk(actor, target)`: Captain → chỉ target Member; Leader → target Member|Captain; khác → không.
- Sửa Settings (description/approval/banner): **chỉ Leader** (`ClanSettingsPopup.eip`: `role == 3`).
- `ClanPlayerPopup`: xem chính mình → chỉ nút Leave. Người khác: Promote/Demote hiện khi mình Leader và target ≠ Leader
  (nhãn Promote nếu target Member, ngược lại Demote); Kick hiện khi `frk(mình, target)`.
- Danh sách thành viên sắp role giảm dần (`Enum.CompareTo`) rồi power giảm dần; dòng của mình đổi màu `SelfColor`.

### 1.3 Màu [CHẮC] (`il.cctor`)
Tier: **S #FFD54F, A #FF9800, B #AB47BC, C #29B6F6, D #4CAF50**, khác #9E9E9E; chữ tier = `<color=#hex>{tier}</color>`.
Join: Open #4CAF50, Approval Only #FFA726.

### 1.4 Banner (`ClanBannerCatalog`: 8 nền clan_flag_0N + 8 overlay clan_flag_0N_color + 10 màu nền + 20 icon clan_icon_* + 8 màu icon — thứ tự ở `tables/ClanBannerCatalog.names.json`) [CHẮC]
`ClanBannerData {BackgroundTypeId, BackgroundColorId, ImageTypeId, ImageColorId}`. `ClanBannerView`: BackgroundImage = backgroundSprites[type]
tô màu backgroundColors[color], ColorOverlay = backgroundOverlaySprites[type], Icon = imageSprites[type] tô imageColors[color],
Crown bật cho clan vô địch (leadership). Editor: 4 lưới option (Shape/Background Color/Emblem Shape/Emblem Color) + Apply.

### 1.5 Endpoint (Cloud Functions, tên gốc) [CHẮC tên + DTO]
`createClan` {clanName, joinSetting, banner×4, server, playerName, power, avatarId} → ClanResponseDTO{clan}
`updateClanSettings` {clanId, description, joinSetting, banner×4} · `updateClanAnnouncement` {clanId, announcement} → {announcement}
`searchClans` {server, clanName, minMemberCount?, maxMemberCount?, hideApprovalOnly} → {clans, requestedClanIds}
`getClanDetails` {clanId, power, playerName} (clanId rỗng = clan của mình) · `joinOpenClan` / `createClanJoinRequest` {clanId, server, playerName, power, avatarId}
`getClanJoinRequests` {clanId} → {requests} · `acceptClanJoinRequest` / `denyClanJoinRequest` / `kickClanMember` {clanId, targetUserId}
`promoteOrDemoteClanMember` → {targetUserId, newRole} · `leaveClan` {} → {disbanded}
Base response `{success, code, message, contentVersion}`; mã lỗi `ERR_*` (NOT_FOUND, CONFLICT_STATE_CHANGED, NOT_IN_CLAN, FORBIDDEN,
VALIDATION, ALREADY_IN_CLAN, REQUEST_ALREADY_PENDING, REQUEST_COOLDOWN, REQUEST_NOT_PENDING, CLAN_FULL, CAPTAIN_LIMIT, NAME_TAKEN,
INTERNAL, JOIN_COOLDOWN) → key `Errors.Clan.*` (`ij.fqp`).
`ClanWireDTO` {clanId, clanName, clanTier, joinSetting, server, description, announcement, myRole, banner×4, memberCount, totalPower, members}.
Save client gốc (UserData): `ClanId, ClanRole, ClanJoinedAt, ClanBanner{Background,Image}{Type,Color}Id`.

### 1.6 UI [CHẮC]
`ClanTabPage` (tab lobby): chưa có clan → `ClanSearchTab` (ô tên, Advanced Search, Create Clan, danh sách `ClanSearchCard`);
có clan → `TabSelector` 2 tab Members | Battle → `ClanMembersTab` (banner, tên, Type/Tier/Members/Power, description,
nút Settings + Requests (chấm đỏ), announcement bấm để sửa (Captain/Leader), danh sách `ClanMemberCard`) / `ClanBattleTab`.
Popup: CreateClan, ClanSettings, ClanBannerEditor, EditAnnouncement, ClanRequests (ClanRequestCard Accept/Deny), ClanInfo (xem clan khác + Join/Request),
ClanPlayer (Promote/Demote/Kick/Leave), AdvancedSearch (Min/Max member 0..50, Hide Approval Only).

## 2. Clan War

### 2.1 Lịch tuần [CHẮC] (`ClanWarController.fze/fzf`)
`day = (DayOfWeek + 5) % 7 + 1` → **Day1 = Thứ Ba … Day5 = Thứ Bảy, Day6 = Chủ Nhật (PvP), Day7 = Thứ Hai (cooldown/nhận thưởng)**,
`weekId` = ngày thứ Ba đầu tuần "yyyy-MM-dd" (UTC). `state`: `day` (1-5) / `day6` / `cooldown` / `concluded`; `type` `normal|leadership`.
"Waiting For War — Your clan will be matched when the next war week begins" → clan ghép cặp lúc đầu tuần.

### 2.2 Nguồn điểm theo ngày [CHẮC cơ chế, SUY số]
`fzg(source)`: chỉ ngày 1..5; ưu tiên `config.sourcesByDay[day]`, thiếu thì mặc định
**ngày lẻ: lootEquipment, summonCompanion, levelUp — ngày chẵn: mining, dungeonKey, summonCape**.
Action gửi lên `recordClanWarContributions` {op, uid, warId, batchId, actions:[{actionId, source, rarity, resource, newLevel, count, eventTimestamp}]}
(gom lô, tối đa 200 action chờ, gửi lại khi lỗi; lưu `clanwar_pending_actions`). Server tính điểm theo `config.awards`:
lootEquipment[rarity 10 bậc Common..Divine], summonCompanion[6 bậc], summonCape[6 bậc], levelUp = newLevel × levelUpMultiplier
("New Level x 100 pts" ⇒ **100**), mining[7 quặng Stone..Diamond], dungeonKey (client mặc định **500** khi config thiếu), pvpWin.
Bảng "Day {0} Actions" liệt kê từng action + "{pts} pts" (`ClanWarDayActionsSection.gbo`).

### 2.3 Ngày 6 PvP [CHẮC cơ chế]
Đánh snapshot thành viên clan địch (PvPPlayerModel), vé `myTickets/ticketsMax` (`startTickets`), thắng +`winPoints` điểm đóng góp
("+{0} Contribution Points earned!"), mục tiêu đã bị clan mình hạ thì không được điểm (`ERR_ALREADY_DEFEATED`).
Hạ đủ `resetThreshold` → **reset vòng** (hồi vé + bỏ trạng thái hạ), tối đa `maxRoundResets`, chỉ khi cả 2 clan ≥ `minMembersForReset` = **30**
("…because one clan started the round below 30 members"). Endpoint `startClanWarPvpBattle` {warId, targetUserId, requestId} / `reportClanWarPvpBattle` {warId, battleToken, won}.

### 2.4 Kết quả, thưởng [CHẮC cơ chế, SUY số]
Mỗi ngày clan có tổng điểm cao hơn thắng ngày, nhận `warScoreByDay[day]` War Score; MVP = người đóng góp nhiều nhất ngày.
Thắng tuần = War Score cao hơn. Thưởng: **mốc cá nhân** `individualMilestones[threshold, lootBox, bones, pickaxe, experience, cloakCurrency]`
theo điểm tuần (nhận ngay `claimClanWarMilestone`, hoặc gộp "Collect Remaining Personal Rewards" lúc cooldown `claimClanWarPersonalBundle`);
**thưởng clan** `clanRewards[tier][win|lose]` (`claimClanWarClanReward`, cần đã đóng góp: `clanRewardEligible`).
`ClanWarRewardsDTO` {lootBox, bones, pickaxe, experience, cloakCurrency, goldenPickaxe, drill, vial}.
Leaderboard: `getClanWarContributionLeaderboard` (daily|weekly, cập nhật mỗi phút), `getClanWarClanLeaderboard` (tier, champion),
`getClanWarLeadershipRanking` (clan đóng góp nhiều nhất tuần → thách đấu Champion tuần sau; "Updates every 10 minutes").
Client gốc dùng phong bì ký HMAC (`issueClanWarSessionKey`, ClanWarEnvelopeDTO) chống gian lận — bản làm lại dùng callable có auth.

## 3. Làm lại trong project
- Server: `server/functions/src/clan.ts` + `clanWar.ts` + `clanWarConfig.ts` (Firestore `clans`, `clanPlayers`, `clanRequests`,
  `clanWars`, `config/clanWar`) — số [SUY] để mặc định trong `clanWarConfig.ts`, ghi đè được bằng Firestore.
- Client: `Scripts/Clan/` (DTO, StaticClanData, UserClanData, ClanController, ClanWarController, ClanWarHooks),
  UI `Scripts/UI/Clan/` (tên class + tên field GIỐNG GỐC để nối tự động), builder `Scripts/Clan/Editor/ClanPrefabBuilder.cs`
  đọc `clan_fieldmap.json` → prefab `Resources/Prefabs/UI/UIClan*` + `ClanTabPage`.

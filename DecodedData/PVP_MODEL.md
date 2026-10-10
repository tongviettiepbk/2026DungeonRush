# PVP ARENA — mô hình gốc (reverse v41) + cách làm lại

Nguồn: `libil2cpp.so` + `global-metadata.dat` (xapk v41) qua Il2CppDumper + capstone; bảng `tables/PvPConfig.json`;
`localization/strings_en.json`; save gốc `appcache/drdump/playerprefs.xml`; scene `GameplayScene.unity` (rip).
Ký hiệu: **[CHẮC]** = đọc thẳng từ code/data; **[SUY]** = logic nằm ở server gốc (không có trong APK) → tự thiết kế.

---

## 1. Tổng quan

| Mục | Giá trị gốc | Độ tin |
|---|---|---|
| Mở khoá | PlayerLevel **15** (`EventsTabPage.cctor`, `PvPController.yev = 15`) | CHẮC |
| Kiểu | PvP **bất đồng bộ**: đánh với SNAPSHOT đồ + pet của người khác (AI điều khiển), không realtime | CHẮC |
| Điểm | **Trophy** (Elo), người mới **1000** (save gốc `PvPTrophy:1000`), tối thiểu 0 (`PvPConfig.MinimumTrophy`) | CHẮC |
| League | 10 bậc theo trophy (bảng mục 3) — tên `Common.PvP.League.{1..10}` | CHẮC |
| Vé | **5 free/ngày** + tối đa **4 vé ads/ngày** (`PvPConfig.TicketConfig`), reset theo `dayKey` (ngày UTC) do server cấp | CHẮC (số) / SUY (reset) |
| Trận | **30 giây** (`GameController.hia`: `wtz = 30f`) | CHẮC |
| Thưởng | theo league **lúc bắt đầu trận**, bảng win/lose do SERVER trả (`PvPLeagueRewardsDTO`) | CHẮC (cơ chế) / SUY (số) |

## 2. Server gốc — 6 endpoint (Cloud Functions v2, `https://{fn}-umgnfrxyuq-uc.a.run.app/`)

| Endpoint | Gọi từ | Request | Response |
|---|---|---|---|
| `openPvP` | `PvPController.jmx/jmy` (mở popup) | `PvPOpenRequestDTO` {server, playerName, countryCode, avatarId, power, snapshotHash, contentVersion, items, companions, wing, capeId, capeLevel, capeSubStats, showCloak, enchantmentTiers} | `PvPOpenResponseDTO` {success, message, userId, hasName, playerName, trophy, leagueIndex, contentVersion, tickets, rewardTable: List<PvPLeagueRewardsDTO{leagueIndex, winRewards, loseRewards}>} |
| `findPvPOpponents` | `jna` | {server} | `PvPFindOpponentsResponseDTO` {success, message, rosterToken, rosterExpiresAt, opponents: List<PvPPlayerModel>, tickets} |
| `startPvPBattle` | `jnb(opponent)` | {server, rosterToken, opponentUserId} | `PvPStartBattleResponseDTO` {success, message, battleToken, battleTokenExpiresAt, startedAt, startLeagueIndex, opponent, tickets} |
| `reportPvPBattle` | `jnc(token, won)` | `PvPBattleResultRequestDTO` {battleToken, won} | `PvPBattleResultResponseDTO` {success, message, won, forfeit, oldTrophy, newTrophy, trophyDelta, oldLeagueIndex, newLeagueIndex, startLeagueIndex, opponentOldTrophy, opponentNewTrophy, opponent, rewards: List<{Type: string, Amount}>, tickets} |
| `grantPvPAdTicket` | `jng` (xem ads, `RewardedType.PvPTicket = 13`) | {} | {success, message, tickets} |
| `getPvPLeaderboard` | `jnf(scope)` | {scope: `"world"` \| `"country"`} | {success, message, scope, generatedAt, stale, playerRank, playerTrophy, topPlayers, nearbyPlayers: List<PvPLeaderboardEntryModel>} |
| `initPvPProfile` | đặt tên | {playerName, countryCode, server} | `RegisterPlayerResponseDTO` {success, userId, playerName, hasName, created, message} |
| Admin | — | `seedPvPDummyPlayers`, `removePvPDummyPlayers`, `removeAllPvPPlayers`, `resetPvPAndBossRush` | — |

`PvPTicketsDTO` {freeRemaining, adRemaining, adClaimedToday, maxAdPerDay, dailyFreeTickets, dayKey}.
`PvPPlayerModel` {UserId, PlayerName, CountryCode, AvatarId, Position, Trophy, Power, SnapshotContentVersion,
**ProjectedWinTrophy, ProjectedLossTrophy**, Items, Companions, Wing, CapeId, CapeLevel, CapeSubStats, ShowCloak, AttacksUsed, EnchantmentTiers}.
`RewardEntryDTO.Type` là **chuỗi** tên enum `RewardType` (client `Enum.TryParse`), Amount kẹp ≥ 0.

DTO nhóm cũ (`PvPJoin/Group/Claim*`, `PvPGroupModel`, `PvPResultModel`, save `PvPCurrentGroupId/UnclaimedGroupId`) **không còn dùng** trong luồng v41 (bản PvP theo nhóm cũ).

Save client gốc: `PvPTrophy`, `PvPRank`, `PvPDailyTicketsRemaining`, `PvPAdTicketsRemaining`, `PvPAdTicketsClaimedToday`,
`PvPBonusTickets`, `PvPLastSnapshotHash`, `PvPLastSnapshotUploadTime` (throttle `SnapshotUploadMinIntervalSeconds = 1800`),
`PvPCachedRoster` {RosterToken, ExpiresAt, Opponents}, `PvPPendingReport` {BattleToken, Won, AttemptedAt, AttemptCount, StartLeagueIndex}
(báo lỗi mạng → gửi lại), `PvPPendingRewardSettlement` {Won, OldTrophy, NewTrophy, TrophyDelta, OldLeagueIndex, NewLeagueIndex, Rewards}.

## 3. League + công thức Trophy (`PvPConfig`) [CHẮC]

| # | League | Trophy | KWin | KLoss |
|---|---|---|---|---|
| 1 | Iron League | 0 – 1199 | 40 | −10 |
| 2 | Bronze League | 1200 – 1399 | 40 | −10 |
| 3 | Silver League | 1400 – 1599 | 35 | −15 |
| 4 | Gold League | 1600 – 1799 | 30 | −15 |
| 5 | Platinum League | 1800 – 1999 | 30 | −20 |
| 6 | Emerald League | 2000 – 2199 | 30 | −20 |
| 7 | Diamond League | 2200 – 2399 | 25 | −25 |
| 8 | Master | 2400 – 2599 | 25 | −25 |
| 9 | Grandmaster | 2600 – 2799 | 20 | −20 |
| 10 | Legend | 2800 – ∞ | 20 | −20 |

WeeklyDecay = 0 mọi league. League của trophy t (`jkp`): league đầu tiên có Min ≤ max(t,0) ≤ Max (không có → league 1).

Elo (`jlb/jkx/jky/jkz/jla`), K theo league của **trophy MÌNH**:
```
E          = 1 / (1 + 10^((oppTrophy - myTrophy) / 400))
winDelta   = max( 1, RoundToEven(KWin       × (1 − E)) )      // jkx
lossDelta  = min(−1, RoundToEven(−|KLoss|   × E      ) )      // jky
ProjectedWinTrophy  = max(0, my + winDelta)                   // jkz
ProjectedLossTrophy = max(0, my + lossDelta)                  // jla
```
Ví dụ 1000 vs 1000: thắng +20, thua −5.

## 4. Luồng client [CHẮC]

1. **Tab Events → PvP** → `PvPPopup` (“PvP Arena”): `openPvP` gửi snapshot → hiện RankNameText (tên league, màu league),
   TrophyText `<sprite=0>{trophy}`, thanh trophy `{trophy−min}/{max−min}` của league, “Tickets refreshed in: {0}” (đếm tới 0h UTC),
   nút Battle `<sprite=0>{free+ad}/{dailyFree}\nBattle`, nút Ad Ticket (hết vé, còn lượt ads), Leaderboard, Rewards.
2. **Battle** → `PvPFindOpponentPopup` (“Searching for opponents” → “Choose Opponent”): roster cache theo `rosterExpiresAt`;
   sắp xếp đối thủ theo **ProjectedWin (jkx) GIẢM dần**; mỗi dòng `PvPPlayerElementUI`: tên, Power `<sprite=0>{power}`, avatar,
   nút đánh `<sprite=0>+{winDelta}`. Trống → “No opponents found.\nTry again later.”
3. **Chọn** → `startPvPBattle` (tiêu vé free trước, hết free dùng vé ads) → `tf.jlx` lưu đối thủ + battleToken + startLeagueIndex →
   load `GameplayScene` (transition “PvP <sprite=0> …”).
4. **Trong trận** (`GameController.hia`, `SpawnController.hvg/hvh`):
   - Đội nhà: hero + pet của mình (pet VẪN ra trận — khác Boss Rush).
   - Đối thủ: `Soldier` phe **Away**, `IsPvPOpponent = true`, đứng **hàng trên cùng lùi `PvPEnemyYOffset = 2` ô** (cột giữa),
     **xoay 180°**, mặc đúng snapshot (đồ+substat, cánh, áo choàng/ShowCloak, relic), **pet của đối thủ cũng spawn** quanh nó (xoay 180°).
   - HUD `PvPUI`: đồng hồ đếm ngược + ClockAnimation; GameplayUI bật tấm phủ EventBlocker (nút Exit = đầu hàng).
   - **Kết thúc**:
     * Hero đối thủ chết → pet đối thủ bị gỡ (`hvi`) → Away hết → **Completed → THẮNG** (sau 1.5s).
     * Hero mình chết → **Failed → THUA** (sau 1.5s).
     * **Hết 30s** (`hib`): so **tổng (HP/MaxHP)** các Character còn sống mỗi phe (không tính pet) → mình ≥ đối thủ ⇒ THẮNG (hoà = thắng).
     * Exit giữa trận (`hio`) → THUA (sau 0.5s).
   - Chiến đấu: đối thủ crit + lifesteal như người chơi; **mình chặn (Block) được đòn đối thủ, đối thủ KHÔNG chặn được đòn mình**
     (`Character.ewb`: bên đánh IsHome → bỏ qua block).
5. **Báo kết quả** → `reportPvPBattle(token, won)` (lỗi → `PvPPendingReport` gửi lại) → `PvPEndPopup`:
   “Victory!” / “Defeated”, `<sprite=0>{newTrophy} <color=#E3EC42|#FF4444>(+delta)</color>`, danh sách thưởng `<sprite=0>{amount}`,
   nút Claim (cộng thưởng) / Retry (báo lại khi lỗi) / Close.
6. **Rewards** (`PvPRewardsPopup` “PvP Rewards”): lật trái/phải theo league, cột Win Rewards / Lose Rewards từ `rewardTable`.
7. **Leaderboard** (`PvPLeaderboardPopup`): 2 tab `world` / `country`, top + quanh mình (separator giữa), dòng mình highlight.

## 5. Phần CHỈ server biết (tự thiết kế, để data-driven)

- **Số thưởng win/lose từng league**: client không có số → Firestore `config/pvp.rewardTable` (mặc định placeholder, cần ảnh từ game gốc).
- **Ghép đối thủ**: gần trophy nhất, loại chính mình; số người/roster = 5 (giả định); roster hết hạn 600s (`yew = 600`, giả định là TTL).
- **Trophy phòng thủ**: đối thủ bị đánh cũng đổi trophy (response có `opponentOldTrophy/NewTrophy`) — dùng cùng công thức Elo với K của league đối thủ.
- **Bot**: gốc có `seedPvPDummyPlayers` (admin) → server mình có bot lấp chỗ khi ít người.

## 6. Làm lại trong project

- Server: `server/functions/src/pvp.ts` + `pvpConfig.ts` (Firestore `pvpPlayers/{uid}`, `config/pvp`).
- Client: `Scripts/PvP/` (DTO, StaticPvPData = Elo/league, UserPvPData save, PvPController, PvPBattleSession, PvPMode),
  UI `Scripts/UI/PvP/`, prefab `Resources/Prefabs/UI/UIPvP*`.

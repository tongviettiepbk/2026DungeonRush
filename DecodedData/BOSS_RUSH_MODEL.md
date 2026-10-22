# BOSS RUSH — mô hình gốc (reverse v41) + kế hoạch làm lại

Nguồn: `libil2cpp.so` + `global-metadata.dat` (xapk v41) qua Il2CppDumper + capstone; bảng `tables/BossRushConfig.json`,
`tables/BossRushLeagueConfig.json`, `tables/PvPConfig.json`; `localization/strings_en.json`; save gốc `appcache/drdump/playerprefs.xml`.
Ký hiệu độ tin cậy: **[CHẮC]** = đọc thẳng từ code/data; **[SUY]** = suy ra từ DTO/luồng, logic thật nằm ở server (không có trong APK).

---

## 1. Tổng quan cách vận hành

| Mục | Giá trị gốc | Độ tin |
|---|---|---|
| Mở khoá | PlayerLevel **15** (`EventsTabPage.cctor`, `BossRushController.ujo = 15`) | CHẮC |
| Lịch sự kiện | **Theo tuần**: mở **Thứ 3 00:00 → Chủ nhật 23:59:59**, **Thứ 2 nghỉ** (chốt/nhận thưởng) | CHẮC (`fn.ely/ema/emb/emc`) |
| Event key | ngày Thứ 3 bắt đầu, dạng `yyyy-MM-dd` (`fn.elz`) | CHẮC |
| Nhóm (pool) | **8 người** ("Fight with an 8-player team"), cùng **tier** | CHẮC (localization + `ekz(7)`) |
| Tier | 1..10 = tên league PvP (`Common.PvP.League.{tier}`): Iron, Bronze, Silver, Gold, Platinum, Emerald, Diamond, Master, Grandmaster, Legend. Người mới = tier **1** | CHẮC (save gốc `BossRushTier:1`, `MaxTierCount=10`) |
| Lên/xuống tier | cuối tuần, theo hạng trong pool (`promoted/demoted/newTier` trong ClaimResponse) | SUY (luật cụ thể ở server) |
| Vé | `BossRushConfig`: MaxFreeEntries **3**, MaxAdEntries **3**. Nhưng save gốc mặc định `BossRushDailyTicketsRemaining = 5`; số thật do server trả (`BossRushTicketsDTO.dailyFreeTickets`) | ⚠ lệch — xem mục 7 |
| Reset vé | theo `dayKey` server cấp (ngày UTC) | SUY |
| Một trận | **30 giây** (`FightDuration`), arena **12×12**, camera offset (0,-5,0) | CHẮC |
| Đồng đội trong trận | tối đa **7 người chơi khác** trong pool (chọn ngẫu nhiên) xuất hiện dưới dạng **ghost** đánh boss cùng | CHẮC (`ekz(7)`, `SpawnController.hvl`, `Soldier.IsGhost`) |
| Boss | dùng chung cả pool: boss #1 → #20, máu chung lưu ở server; giết boss → boss kế | CHẮC (DTO) + SUY (trừ máu ở server) |

> Đoán ban đầu "người chơi sức mạnh nào nhóm vào boss đấy": **không phải theo power** — nhóm theo **tier (league)**.
> `power` chỉ gửi lên để hiển thị trong danh sách (`BossRushPlayerModel.Power`).

## 2. Server gốc (Firebase Cloud Functions v2 / Cloud Run)

Base URL (chuỗi đã giải mã): `https://{0}-umgnfrxyuq-uc.a.run.app/` — `{0}` = tên hàm viết thường. Gọi HTTP POST JSON (BestHTTP),
qua `FunctionsController` + `FirebaseAuthController` (token Firebase Auth). Chuỗi trong game bị mã hoá
(`<PrivateImplementationDetails>.a`: byte[] trong metadata @0x7AD340, XOR `(i & 0xFF) ^ 0xAA`) — đã giải 5455 chuỗi.

| Endpoint | Gọi từ | Request | Response |
|---|---|---|---|
| `joinBossRush` | `BossRushController.ekr` | `BossRushJoinRequestDTO` {server, playerName, power, items, companions, wing, capeId, capeLevel, capeSubStats, showCloak, enchantmentTiers} | `BossRushJoinResponseDTO` {success, alreadyJoined, inactive, poolId, tier, eventKey, nextEventKey, players, bossNumber, bossHP, maxBossHP, isFinalized, autoLoss, contentVersion, tickets, rewardTable, hasUnclaimed, unclaimedPoolId} |
| `getBossRushPool` | `eks(poolId)` | {poolId} | `BossRushPoolResponseDTO` {success, removed, inactive, concluding, autoLoss, poolId, tier, eventKey, nextEventKey, message, players, bossNumber, bossHP, maxBossHP, isFinalized, contentVersion, tickets, rewardTable, unclaimedPoolId} |
| `startBossRushFight` | `ekt` | {poolId} | `BossRushStartFightResponseDTO` {success, message, **fightToken**, bossNumber, bossHP, maxBossHP, **fightExpiresAt**, tickets} |
| `reportBossRushDamage` | `eku(token, dmg, total)` | {fightToken, **damageDealt**, **totalDamageDealt**} | `BossRushFightResultResponseDTO` {success, autoLoss, damagePoints, totalDamagePoints, newPosition, bossKilled, newBossNumber, newBossHP, newMaxBossHP, players, rewards, tickets} |
| `claimBossRushRewards` | `eky(poolId)` | {poolId} | `BossRushClaimResponseDTO` {success, expired, rank, promoted, demoted, bossesKilled, tier, newTier, rewards, tickets} |
| `updateBossRushPlayer` | `elm/eln` (khi đổi đồ, có throttle) | `BossRushUpdateRequestDTO` (= Join + poolId) | — |
| Admin/test | — | `seedBossRushDummyPlayers`, `shuffleBossRushPool`, `removeBossRushDummyPlayers`, `removeAllBossRushPools`, `finalizeBossRushEndedEvents`, `resetPvPAndBossRush` | — |

→ Gốc có **dummy players** (bot) để lấp pool, và một job **finalize** sự kiện đã hết.

Model phụ: `BossRushPlayerModel` {UserId, PlayerName, Position, Power(double), TotalDamagePoints(long), Items: List<PvPItemModel{ItemId, ItemLevel, SubStats}>,
Companions: List<PvPCompanionModel{CompanionId, CompanionLevel}>, Wing: PvPWingModel{WingId, WingLevel, SubStats}, CapeId, CapeLevel, CapeSubStats, ShowCloak, EnchantmentTiers}.
`BossRushTicketsDTO` {freeRemaining, adRemaining, adClaimedToday, dailyFreeTickets, dayKey}. `RewardEntry` {Type: RewardType, Amount}.
`BossRushRewardTableDTO` {guaranteedRewardsByBossesKilled: List<List<RewardEntry>>, placementBrackets: List<{minRank, maxRank, rewards}>}.
`RewardType`: Bone 0, Gem 1, DragonBossDungeonKey 2, ZombieHordeDungeonKey 3, Lootbox 4, Exp 5, MiningGoldenPickaxe 6, MiningPickaxe 7, MiningDrill 8, CloakCurrency 9, CultistDungeonKey 10, Vial 11.

Save client gốc (UserData): `BossRushTier`, `BossRushCurrentPoolId`, `BossRushLastJoinEventKey`, `BossRushUnclaimedPoolId`, `BossRushLastItemUpdateTime`,
`BossRushDailyTicketsRemaining`, `BossRushAdTicketsRemaining`, `BossRushAdTicketsClaimedToday`, `BossRushPendingReport` {FightToken, DamageDealt, TotalDamageDealt, AttemptedAtUnix, AttemptCount}
(báo damage lỗi mạng → lưu lại gửi lại), `BossRushPendingRewardSettlement`. Ads: `RewardedType.BossRushEntry = 12`, placement `boss_rush_ad`.

## 3. Bảng máu boss — `BossRushConfig.BossHPTable` (double[10][20]) **[CHẮC]**

`elw(bossNumber, tier)` = `BossHPTable[clamp(tier-1, 0, 9)][clamp(bossNumber-1, 0, 19)]`. Mỗi boss ×1.5 so với boss trước (làm tròn).
Thứ tự hàng đã xác minh qua fieldRef của `.cctor` (không phải sắp theo độ lớn).

| Boss # | T1 Iron | T2 Bronze | T3 Silver | T4 Gold | T5 Platinum | T6 Emerald | T7 Diamond | T8 Master | T9 Grandmaster | T10 Legend |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 500M | 1B | 2B | 3B | 5B | 10B | 20B | 30B | 50B | 100B |
| 2 | 750M | 1.5B | 3B | 4.5B | 8B | 15B | 30B | 45B | 75B | 150B |
| 3 | 1B | 2.25B | 4.5B | 6.75B | 11B | 23B | 45B | 68B | 113B | 225B |
| 4 | 2B | 3.375B | 6.75B | 10B | 17B | 34B | 68B | 101B | 169B | 338B |
| 5 | 3B | 5B | 10B | 15B | 25B | 51B | 101B | 152B | 253B | 506B |
| 6 | 4B | 8B | 15B | 23B | 38B | 76B | 152B | 228B | 380B | 759B |
| 7 | 6B | 11B | 23B | 34B | 57B | 114B | 228B | 342B | 570B | 1.139T |
| 8 | 9B | 17B | 34B | 51B | 85B | 171B | 342B | 513B | 854B | 1.709T |
| 9 | 13B | 26B | 51B | 77B | 128B | 256B | 513B | 769B | 1.281T | 2.563T |
| 10 | 19B | 38B | 77B | 115B | 192B | 384B | 769B | 1.153T | 1.922T | 3.844T |
| 11 | 29B | 58B | 115B | 173B | 288B | 577B | 1.153T | 1.73T | 2.883T | 5.767T |
| 12 | 43B | 86B | 173B | 259B | 432B | 865B | 1.73T | 2.595T | 4.325T | 8.65T |
| 13 | 65B | 130B | 259B | 389B | 649B | 1.297T | 2.595T | 3.892T | 6.487T | 12.975T |
| 14 | 97B | 195B | 389B | 584B | 973B | 1.946T | 3.892T | 5.839T | 9.731T | 19.462T |
| 15 | 146B | 292B | 584B | 876B | 1.46T | 2.919T | 5.839T | 8.758T | 14.596T | 29.193T |
| 16 | 219B | 438B | 876B | 1.314T | 2.189T | 4.379T | 8.758T | 13.137T | 21.895T | 43.789T |
| 17 | 328B | 657B | 1.314T | 1.971T | 3.284T | 6.568T | 13.137T | 19.705T | 32.842T | 65.684T |
| 18 | 493B | 985B | 1.971T | 2.956T | 4.926T | 9.853T | 19.705T | 29.558T | 49.263T | 98.526T |
| 19 | 739B | 1.478T | 2.956T | 4.434T | 7.389T | 14.779T | 29.558T | 44.337T | 73.895T | 147.789T |
| 20 | 1.108T | 2.217T | 4.434T | 6.651T | 11.084T | 22.168T | 44.337T | 66.505T | 110.842T | 221.684T |

Giá trị đầy đủ (double) — hàng T1..T10 = metadata offset 0x79E5E8, 0x7955D8, 0x761BD8, 0x792FE0, 0x796BC8, 0x791FD8, 0x794738, 0x793840, 0x797360, 0x79F008 (mỗi hàng 20 double).

**Sát thương boss — `BossDamageByTier` (float[10]) [CHẮC]:** `elx(_, tier)` = `[300, 600, 900, 3000, 6000, 9000, 12000, 15000, 18000, 21000][clamp(tier-1,0,9)]`
→ gán vào `CharacterModel.EnemyAttackPower` của boss (EnemyHealth model = 1, sau đó HP thật ghi đè = CurrentHP/MaxHP).

`TierConfigs` (AttackMultiplier/RewardMultiplier) chỉ có 1 dòng Tier 0 = 0 → không dùng. `DamageDivisor = 100`: **không thấy client đọc** (đã quét các hàm BossRush/GameController/SpawnController) → nếu có dùng là ở server.

## 4. Boss — `BossDefinitions` (10 mẫu, boss #N dùng mẫu `(N-1) % 10`) [CHẮC]

| # | Tên (`Common.BossRush.Boss.N`) | Animator | Ghi chú |
|---|---|---|---|
| 1 | Dark Lich | Lich | |
| 2 | Black Dragon | Dragon | |
| 3 | Green Dragon | Dragon | |
| 4 | Red Dragon | Dragon | |
| 5 | Crimson Lich | Lich | |
| 6 | Elder Lich | Lich | |
| 7 | Ogre King | Ogre | |
| 8 | Ogre Chieftain | Ogre2 | |
| 9 | Green Hag | Witch | |
| 10 | Purple Hag | Witch | |

Chung cả 10: ColliderRadiusMultiplier **1.5**, MassMultiplier **5**, CanMove **false**, EnemyBehaviorType **2 = RepositionAfterAttack**
(enum: DirectChase=1), RepositionDelay **5s**, RepositionSearchRadius **3** ô. Weapon/Helmet/Gloves riêng mỗi boss (guid trong BossRushConfig.json).
Player collider ×1 (`PlayerColliderRadiusMultiplier`). Asset rip có: Ogre1/Ogre2, Witch/Witch2 (Helmet/Glove/WeaponData + overrideController), Weapon_Lich_*.

## 5. Luồng client gốc [CHẮC]

1. **Tab Events → Join** (`BossRushJoinPopup`): ngoài giờ → "No events today." / "Starts in {0}"; trong giờ → "Ends in: {0}". Join → `joinBossRush` gửi snapshot đồ.
2. **`BossRushPopup`** (sảnh pool): TierText = tên league, TimerText, BossTitle `Boss #{0}: {1}`, slider + `HP / MaxHP`, danh sách 8 người
   (`BossRushPlayerElementUI`: Position, PlayerName, DamagePoints = TotalDamagePoints, Power; dòng của mình highlight), nút Fight `{0}/{1}\nFight`,
   hết vé → "No fights remaining today!" + "Next in {0}", nút Rewards (`BossRushRewardsPanel`: Guaranteed Rewards theo số boss đã giết + bảng theo hạng, "Your placement: {0}").
   Trạng thái: "Ending", "Event Ended", "Results are being prepared".
3. **Fight** → `startBossRushFight` (tiêu vé free trước, hết free mới dùng vé ad) → nhận fightToken + HP boss hiện tại →
   chọn ngẫu nhiên ≤7 người khác trong pool làm ghost → load `GameplayScene` (transition "Boss Rush").
4. **Trong trận** (`GameController` + `BossRushUI` HUD: tên boss, HP slider + slider trắng trễ, thời gian còn lại):
   - Đếm ngược 30s (`wuj -= deltaTime`), hết giờ → kết thúc.
   - Mỗi đòn đánh vào boss (`Soldier.CharacterDealtDamage` → `hif`): nếu attacker thuộc phe nhà (`IsHome`) → **team damage += dmg**;
     nếu attacker là **hero chính** (`IsMainCharacter`) → **damage của mình += dmg**.
   - Boss chết trong trận → popup "Boss Defeated", `bossNumber + 1`, HP mới = `BossHPTable[tier][bossNumber]`, attack = `BossDamageByTier[tier]`, spawn boss mới ngay.
   - Ghost: `Soldier.IsGhost = true`, mặc đồ theo snapshot (items+substats, companions, wing, cape, enchantment).
5. **Kết thúc** → `reportBossRushDamage(fightToken, damageDealt = của mình, totalDamageDealt = cả đội)` (lỗi → lưu `BossRushPendingReport` gửi lại),
   đợi 1.5s → `BossRushEndPopup` "Fight Complete!": "Team Damage: {0}" + "Damage Dealt: {0}" → Continue về lobby.
6. **Thứ 2** (sự kiện hết) → `BossRushClaimPopup` "Boss Rush Results": Placement, tier hiện tại → tier mới, phần thưởng bay về icon.

Phần **chỉ server biết** (không thể lấy từ APK): số lượng phần thưởng thật (`BossRushLeagueConfig` trong APK chỉ là placeholder 1000×Bone),
luật lên/xuống tier, cách trừ máu boss chung (khả năng cao trừ theo `totalDamageDealt` để khớp trận local), ý nghĩa `damagePoints`, ghép pool & bot.

## 6. Kế hoạch làm lại (bám StickIdle + pattern Dungeon/Events đang có)

**Phase 0 — Data**: `Scripts/BossRush/StaticBossRushData` (nhúng HP table, damage-by-tier, 10 boss def, arena, 30s, lịch tuần, tên tier)
+ `UserBossRushData` (key `key_user_boss_rush`: tier, currentPoolId, lastJoinEventKey, unclaimedPoolId, pendingReport, pendingSettlement). Vé tiếp tục ở `UserEventData`/`EventService` (đã có).

**Phase 1 — Backend trừu tượng**: `IBossRushBackend` với 6 hàm đúng tên/DTO gốc (Join, GetPool, StartFight, ReportDamage, Claim, UpdatePlayer).
Bản `LocalBossRushBackend` chạy offline: tự tạo pool 8 người (7 bot giả lập damage theo thời gian), boss chung, lịch tuần, claim → dựng & test gameplay/UI không cần mạng.

**Phase 2 — Gameplay**: `BossRushMode : BaseMode` (ChangeMode(ModeType.BossRush), map `BossRushMapPrefab` 12×12), `BossRushBossUnit` (đứng yên, đánh xong
5s đổi vị trí trong bán kính 3 ô, HP double, collider ×1.5), ghost allies từ snapshot (dùng lại HeroUnit + equipment), đếm damage mình/đội, timer 30s, chuỗi boss.

**Phase 3 — UI**: nối 5 prefab đã có ở `Prefabs/UI/BossRush/` (JoinPopup, Popup, PlayerElementUI, EndPopup, ClaimPopup) + HUD trong trận; nút Join ở `UITabEvent` thay toast "coming soon".

**Phase 4 — Firebase**: Auth (anonymous/Google) + backend thật (xem quyết định mục 7). Firestore schema đề xuất:
`bossRushPools/{poolId}` {eventKey, tier, players{uid: snapshot+totalDamage}, bossNumber, bossHP, maxBossHP, isFinalized},
`bossRushPlayers/{uid}` {tier, currentPoolId, lastJoinEventKey, unclaimedPoolId, tickets{dayKey, free, ad, adClaimed}, activeFight{token, expiresAt}}.

**Phase 5 — Chốt tuần**: finalize Thứ 2 (xếp hạng, thưởng guaranteed + placement, lên/xuống tier), `BossRushClaimPopup`, cộng thưởng vào inventory.

**Phase 6 — Bền vững**: pending report retry, fightToken hết hạn (autoLoss), throttle updatePlayer, chống gửi damage ảo (server kẹp theo power × 30s).

## 7. Điểm cần chốt

1. **Server**: (A) Cloud Functions + Firestore — giống gốc 1:1, an toàn, có job cron chốt tuần + bot lấp pool, nhưng cần gói **Blaze** và viết Node/TS;
   (B) chỉ Firestore + transaction từ client — miễn phí (Spark), không cần code server, nhưng dễ gian lận và việc chốt tuần phải làm "lazy" ở client.
2. **Vé/ngày**: 5 (theo mô tả + save mặc định gốc) hay 3 (BossRushConfig.MaxFreeEntries); ads +3.
3. **Cân bằng HP**: boss #1 tier 1 = 500M HP trong khi hero mới ~10 damage → giữ nguyên bảng gốc hay thêm hệ số scale theo damage hiện tại của project.
4. **Phần thưởng**: server gốc không lộ số liệu → cần tự thiết kế bảng guaranteed (theo số boss giết) + theo hạng 1..8.

## 8. Asset 10 boss (resolve PPtr BossRushConfig.BossDefinitions từ xapk bằng UnityPy) [CHẮC]

| # | Boss | Animator | Weapon | Helmet | Gloves |
|---|---|---|---|---|---|
| 1 | Dark Lich | LychAnimatorController | LychWeaponData | LychHelmetData | LychGloveData |
| 2 | Black Dragon | DragonAnimatorController | PurpleDragonWeaponData | PurpleDragonHelmetData | PurpleDragonGloveData |
| 3 | Green Dragon | DragonAnimatorController | GreenDragonWeaponData | GreenDragonHelmetData | GreenDragonGloveData |
| 4 | Red Dragon | DragonAnimatorController | RedDragonWeaponData | RedDragonHelmetData | RedDragonGloveData |
| 5 | Crimson Lich | LychAnimatorController | Lych2WeaponData | Lych2HelmetData | Lych2GloveData |
| 6 | Elder Lich | LychAnimatorController | Lych3WeaponData | Lych3HelmetData | Lych3GloveData |
| 7 | Ogre King | OgreAnimatorController | Ogre1WeaponData | Ogre1HelmetData | Ogre1GloveData |
| 8 | Ogre Chieftain | Ogre2AnimatorController | Ogre2WeaponData | Ogre2HelmetData | Ogre2GloveData |
| 9 | Green Hag | WitchAnimatorController | WitchWeaponData | WitchHelmetData | WitchGloveData |
| 10 | Purple Hag | WitchAnimatorController | Witch2WeaponData | Witch2HelmetData | Witch2GloveData |

Power (rm.iqm → rm.iqj) **[CHẮC]**: `Power = (Attack × (1 + Damage%)) × (HP × (1 + HP% [+ Melee%/Ranged% theo loại vũ khí]))`
= tích Attack cuối × Máu cuối của hero; gửi lên server trong joinBossRush/updateBossRushPlayer, hiển thị cột Power.

## 9. Đấu trường + hành vi boss trong trận (reverse thân hàm 2026-10-17) [CHẮC]

- **Lưới**: `GameController.hie` → `GridManager.iiy(BossRushConfig.ArenaGridSize)` = **12×12** (không phải 9×12 của MapConfig);
  `MapConfig.SpawnObstacles = 0` ⇒ không chướng ngại. Prefab `BossRushMapPrefab` gốc: `Walls`, `Corners`, `Doors` **inactive**,
  `InnerPlane` scale (170.13, 219.58, 155.31) ⇒ sàn trải rộng, không tường cản. `ArenaBoundsThickness` không thấy client đọc.
- **Đội** (`SpawnController.hvj`): ô = `GridManager.ijq(1 + số ghost)`: ô giữa (x 0, y H/2) nếu trống → hero;
  ghost bốc ngẫu nhiên không trùng trong 8 ô bao quanh (dy ngoài, dx trong). Hero collider × `PlayerColliderRadiusMultiplier` (1).
- **Camera** (`CameraController.gxt` + LateUpdate): bám hero, vị trí = người bám + `CameraFollowOffset` (0, −5, 0), giữ z;
  bám lại người ban đầu thì đặt thẳng, đổi sang người khác thì lerp `t = min(dt × 4, 1)`.
- **Boss** (`SpawnController.hvm` → `GridManager.ijt`): ô trống ngẫu nhiên khắp lưới TRỪ vùng |x| ≤ 2 và |y − H/2| < 3; quay 180°;
  `CanMove = Definition.CanMove` (false), collider × 1.5 (`huw`), Rigidbody mass × 5 (`hux`); `BossRushEnemyBehaviorType` = 2.
  Boss chết → boss kế spawn lại bằng `hvm` (ô ngẫu nhiên mới).
- **Dời chỗ (RepositionAfterAttack)**: mỗi đòn TRÚNG (`MeleeAttackState.fcc`, `RangeAttackState.fcl/fcm` → `Character.ewo`) đặt
  `BossRushPendingReposition = true`, `RepositionAtTime = now + RepositionDelay (5)` ⇒ boss đánh liên tục thì cứ bị hoãn.
  `IdleState.fbq` xét `ewp` (pending và đã tới giờ) ĐẦU TIÊN → `BossRushRepositionState`: bỏ hẹn, ô đích = `iju(ô hiện tại,
  max(1, SearchRadius = 3))` (ô trống ngẫu nhiên trong vuông bán kính 3, trừ chính nó); khác ô hiện tại → `PhysicsMovementController.htj`
  (đi bằng lực theo A*, xoay thân theo waypoint), hết path → Idle; không có ô → Idle.
- **Đòn đánh**: chỉ khác vũ khí — `WeaponType` gốc chỉ có Melee/Range. Rồng/Lich/Hag: Range, tầm 100, đạn tốc **12**
  (rồng đỏ = `Projectile_Dragon_Red` cầu lửa, rồng xanh `_Green`, rồng đen `_Purple`; Lich tím/đỏ/xanh orb; Hag orb xanh);
  Ogre: Melee tầm 3 (không đuổi vì CanMove = false). Tốc đạn lấy từ `WeaponData.ProjectileSpeed` (`Character.ProjectileSpeed`).

## 10. Bản v59 (`Ver59/Dungeon+Rush_59_APKPure.xapk`, reverse 2026-10-21) — skill boss + chỗ đứng quanh boss [CHẮC]

Nguồn: lib chỉ có **armeabi-v7a** (ARM32, metadata v31). Dump bằng Il2CppDumper, giải chuỗi cùng thuật toán v41
(`b ^ (i & 0xFF) ^ 0xAA`, blob metadata @9262920 dài 0x793BB). Config đọc thẳng `BossRushConfig` (sharedassets0 path 17662)
bằng UnityPy + DLL AssetRipper `Ver59/AssetRipper/Assemblies`. `BossHPTable` / `BossDamageByTier` **không đổi** so với v41.

### 10.1 Config chung (`BossRushConfig`)

| Trường | Giá trị |
|---|---|
| ArenaGridSize | 12 × 12 |
| CameraFollowOffset | (0, −5, 0) |
| PlayerColliderRadiusMultiplier | 1.1 |
| SlotStandoff | 1.25 |
| MeleeRangeBonus | 1.6 |
| SlotArrivalTolerance | 0.9 |
| SlotGraceSeconds | 3 |
| PlayerSpawnRingRadius | 2 |
| SkillIntervalMin / Max | 4 / 6 |
| SlamWindup / Recovery / Radius / Knockback | 0.7 / 0.4 / 4.2 / 8 |
| SlamTelegraphWarmupSeconds | 1 |
| EarthquakeWindup / WaveInterval | 0.5 / 0.55 |
| EarthquakeWaveRadii | 2.6, 4.4, 6.0 |
| EarthquakeKnockback / HopHeight | 7.5 / 1.25 |
| TeleportWindup / Recovery | 0.35 / 0.25 |
| TeleportMin / MaxDistanceCells | 7 / 10 |
| TeleportMinPlayerDistanceCells | 5 |
| FireBreathWindup / Sweep / Recovery | 0.4 / 2.5 / 0.3 |
| FireBreathRadius / ConeAngle | 3.5 / 70 |
| FireBreathMuzzleOffset / PointBlankMargin | 0.8 / 0.8 |
| FireBreathHitInterval / Knockback / FXScale | 0.5 / 4.5 / 1 |
| FireBreathShowDamageCone | false |
| AllyHitKnockbackForce | 3 |
| BossCritPunchScale / Cooldown | 0.08 / 0.35 |
| FightDuration / DamageDivisor / MaxFree / MaxAd | 30 / 100 / 3 / 3 |

`ArenaBoundsThickness` = 1 nhưng client v59 cũng không đọc.

### 10.2 10 boss (`BossDefinitions`)

| Boss | Collider × | Body scale × | Mass × | CanMove | RepositionDelay | SearchRadius | MoveSpeed × | Skill |
|---|---|---|---|---|---|---|---|---|
| Dark Lich | 1.6 | 1.2 | 5 | true | 1.6 | 3.5 | 1.5 | Slam |
| Black Dragon | 1.75 | 1.5 | 5 | true | 1.75 | 3.5 | 1.0 | FireBreath |
| Green Dragon | 1.75 | 1.5 | 5 | true | 1.6 | 3.5 | 1.0 | FireBreath |
| Red Dragon | 1.75 | 1.5 | 5 | true | 1.6 | 3.5 | 1.0 | FireBreath |
| Crimson Lich | 1.6 | 1.2 | 5 | true | 1.6 | 3.5 | 1.5 | Slam |
| Elder Lich | 1.6 | 1.2 | 5 | true | 1.6 | 3.5 | 1.5 | Slam |
| Ogre King | 1.75 | 1.5 | 5 | true | 1.6 | 3.5 | 1.0 | Earthquake |
| Ogre Chieftain | 1.75 | 1.5 | 5 | true | 1.6 | 3.5 | 1.0 | Earthquake |
| Green Hag | 1.5 | 1.1 | 5 | true | 1.6 | 3.5 | 1.5 | Teleport |
| Purple Hag | 1.5 | 1.1 | 5 | true | 1.6 | 3.5 | 1.5 | Teleport |

FX skill = prefab particle (bảng 10.6); không có prefab (chỉ Ogre) thì gốc vẽ FX mặt đất bằng code
(`BossRushGroundFX`). `SpawnController.jmj`: CanMove = định nghĩa (true ⇒ **không còn quay cố định 180°**), MoveSpeed × MoveSpeedMultiplier,
hình × BodyScaleMultiplier (`Character.fyh`), collider/mass như cũ.

### 10.3 Lịch skill + dời chỗ

- `IdleState.gfu` (trước mọi thứ): boss có skill → `NextSkillAt ≤ 0` thì hẹn `now + Random(SkillIntervalMin, Max)` (`fzt`);
  đã tới giờ (`fzu`) → `BossRushSkillState`. Sau đó mới xét dời chỗ (`fzs`: pending và tới giờ) → `BossRushRepositionState`.
- `BossRushSkillState`: vào = dừng di chuyển, boss thành **Kinematic** + vận tốc 0 (`fzz(true)`, lưu bodyType cũ), Animator `IsIdle = true`,
  tạo skill `gy.Create(type)` rồi `Begin`. Mỗi frame `Tick(dt × tốc game)` trả true là xong → Idle. Ra = `Cancel` skill, trả bodyType,
  `IsIdle = false`, hẹn skill sau `now + Random(4, 6)`.
- Hẹn dời chỗ (`fzr`, gọi khi đòn của boss trúng): **chỉ đặt khi chưa có hẹn** (v41 đặt lại mỗi đòn) ⇒ cứ ~1.6 s sau đòn trúng đầu tiên boss dời chỗ.
  `BossRushRepositionState`: ô đích = `GridManager.kcq(ô hiện tại, R = max(1, SearchRadius 3.5), r = max(1, R − 1))` —
  ô trống, đi được, khoảng cách Euclid ≤ R; ưu tiên ô có khoảng cách ≥ r, không có thì lấy bất kỳ ô ≤ R; chọn ngẫu nhiên.
- Damage skill = 1 đòn thường của boss lên từng người (`Character.fyw` — cùng công thức chí mạng/chặn như đòn thường).
  Mục tiêu = `SpawnController.HomeCharacters` còn sống. Đẩy lùi `gy.fng`: AddForce **Impulse** = hướng (người − tâm) chuẩn hoá × lực
  (trùng tâm thì hướng ngẫu nhiên); bỏ qua nếu Rigidbody kinematic.

### 10.4 4 skill

- **Slam** (Lich, class `hb`): tâm = boss. Gồng 0.7 s: telegraph (prefab hoặc vòng tròn đỏ thu từ 2×R về cỡ thân boss); hết gồng →
  mọi người cách tâm ≤ 4.2 bị 1 đòn + đẩy 8; FX nổ (prefab hoặc chớp tròn 1.3 trong 0.3 s màu (1, 0.9, 0.7, 0.9) + vành từ 0.4 → R
  trong 0.3 s màu (1, 0.35, 0.2, 0.85)), rung camera (0.3, 0.3). Xong lúc 0.7 + 0.4 = 1.1 s.
- **Earthquake** (Ogre, class `gz`): tâm cố định lúc bắt đầu. Sóng i nổ lúc `0.5 + 0.55 × i`; vành trúng = từ bán kính sóng trước
  (sóng 0 từ 0.3) tới `Radii[i]` ⇒ [0.3–2.6], [2.6–4.4], [4.4–6.0]. Trúng: 1 đòn + đẩy 7.5 + nảy hình (`fzv`: DOPunchPosition (0, 1.25) 0.45 s
  + punch scale). FX vành nâu (0.6, 0.38, 0.18, 0.95) 0.35 s, vành nhạt (0.85, 0.72, 0.5, 0.55) 0.5 s, 7 mảnh đất; rung camera (0.26, 0.2 + 0.1 i).
  Xong lúc `0.5 + 0.55 × 2 + 0.4` = 2.0 s.
- **Teleport** (Hag, class `hc`): đích = ô cách ô boss 7–10 (Euclid), đi được, trống, xa người gần nhất ≥ 5; ngẫu nhiên trong danh sách,
  không có thì ô xa người nhất. Gồng 0.35 s (chớp tím (0.75, 0.35, 1, 0.85) tại chỗ, hình thu ×0.1 sau 30% thời gian, ease InBack);
  tới giờ → đặt vị trí + rigidbody, chớp ở chỗ cũ + chỗ mới (0.3 s), quay mặt về người gần nhất, hình phóng lại 0.2 s ease OutBack.
  Xong lúc 0.6 s. **Không gây damage.**
- **FireBreath** (rồng, class `ha`): gồng 0.4 s; bỏ mục tiêu đang đánh (trả lại khi xong). Quét **360°** trong 2.5 s bắt đầu từ hướng
  mặt boss, thân boss xoay theo. Nón 70° (± 35°), tầm = 3.5 + miệng (0.8 × BodyScale); trong vòng "sát thân" (= nửa cỡ thân + 0.8)
  luôn trúng. Mỗi người tối đa 1 lần / 0.5 s: 1 đòn + đẩy 4.5. FX: prefab lửa (scale = Radius × FXScale) bám miệng, hoặc nón vẽ màu
  (1, 0.45, 0.1, 0.75). Xong lúc 0.4 + 2.5 + 0.3 = 3.2 s.

### 10.5 Chỗ đứng quanh boss (class `gq`, tạo ở `GameController.isk` với số chỗ = số người trong đội)

- Chỉ áp cho **cận chiến** đội nhà đánh boss (`GridCombatController.jbw`: IsBossRush + attacker.IsHome + target.IsBossRushBoss;
  `IdleState`/`MoveState` gọi ở nhánh melee). Bắn xa giữ cách cũ.
- Chỗ i: góc `i × 360 / N`; vị trí = tâm collider boss + hướng × (bán kính collider theo hướng đó `ClosestPoint` + **SlotStandoff 1.25**).
- Gán chỗ (`fko`): người đã có chỗ giữ nguyên; người mới lấy chỗ **còn trống** có góc gần góc (người − boss) nhất (hết chỗ trống thì xét cả chỗ có người).
- `MoveState.ggl`: ô đích của melee = ô chứa vị trí chỗ (thay cho ô boss).
- Tầm đánh boss (`jbv`): khoảng cách từ người tới **điểm gần nhất trên collider boss** ≤ `AttackDistance + MeleeRangeBonus 1.6`, VÀ
  (`fkn`) đứng cách chỗ ≤ `SlotArrivalTolerance 0.9`; lệch chỗ thì bắt đầu đếm, đủ `SlotGraceSeconds 3` vẫn được đánh.
- Đội xuất phát (`GridManager.kcm(N, PlayerSpawnRingRadius 2)`): hero ô giữa; còn lại rải đều trên vòng vuông Chebyshev bán kính 2
  quanh ô giữa (16 ô, sắp theo góc, lệch ngẫu nhiên, lấy `round(i × 16 / k)`), thiếu thì lấp 8 ô kề.
- Bị boss đánh trúng: người đội nhà bị đẩy `AllyHitKnockbackForce 3` (Impulse, hướng từ boss) (`Character.fzw`). Boss bị chí mạng:
  DOPunchScale hình × 0.08 trong 0.18 s, hồi 0.35 s (`fzx`).

### 10.6 Prefab FX skill (resolve PPtr BossRushConfig/BossDefinitions trong sharedassets0, 2026-10-22) [CHẮC]

| Boss / config | FireBreathFX | SlamTelegraphFX | SlamImpactFX |
|---|---|---|---|
| Dark Lich | — | MagicZoneGreen | ExplosionFireballGreen |
| Black Dragon | FlameThrowerParentGreen | — | — |
| Green Dragon | FlameThrowerParentYellow | — | — |
| Red Dragon | FlameThrowerParent | — | — |
| Crimson Lich | — | MagicZoneRed | ExplosionFireballRed |
| Elder Lich | — | MagicZonePurple | ExplosionFireballPurple |
| Ogre ×2, Hag ×2 | — | — | — |
| Config (dự phòng) | FlameThrowerParent | MagicZoneRed | ExplosionFireballRed |

Config còn `TeleportFlashFXPrefab` = **FlashExplosionRadial** (Hag dùng prefab này, KHÔNG phải chớp tím vẽ code), `EarthquakeWaveFXPrefab` = trống.
Cách gốc dùng (disasm hb/ha/hc + BossRushGroundFX.fks/fkt):
- `fks(prefab, pos, scale)`: Instantiate tại pos với góc xoay của prefab, localScale = one × scale, `fkt` (AudioSource con: âm lượng × SFX, tắt tiếng thì Stop).
  Không huỷ (particle stopAction None, không script).
- Slam: telegraph Instantiate tại boss, scale 2 × SlamRadius, `Simulate(SlamTelegraphWarmupSeconds 1, children, restart)` + Play; lúc nổ
  Destroy telegraph rồi `fks(impact, tâm, SlamRadius)`. Không có prefab nổ → telegraph code `fkz` (màu (1, 0.85, 0.6, 0.85) mờ 0.3s) + chớp + vành.
- FireBreath: Instantiate tại miệng, scale Radius × FXScale (3.5); mỗi frame vị trí = miệng, rotation = Euler(0,0,góc−90) × rotation prefab; hết quét Destroy.
- Teleport `fod`: `fks(TeleportFlash, pos, 1)` ở chỗ cũ lúc gồng + chỗ cũ & chỗ mới lúc dịch chuyển.
- ExplosionFireball* có AudioSource `etfx_explosion_fireball` (pitch 1.5, playOnAwake). Sorting gốc: nổ/lửa order 800, MagicZone/Flash 0.


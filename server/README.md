# Server DungeonRush — Firebase (Cloud Functions v2 + Firestore + Auth)

Server online cho Boss Rush, bám server gốc (`https://{fn}-umgnfrxyuq-uc.a.run.app/`, us-central1).
Reverse gốc: `DecodedData/BOSS_RUSH_MODEL.md`.

## Thành phần

| File | Nội dung |
|---|---|
| `functions/src/config.ts` | Hằng số GỐC: bảng máu boss 10 tier × 20 boss, sát thương boss theo tier, vé 3 free + 3 ads, nhóm 8 người, trận 30s |
| `functions/src/schedule.ts` | Lịch tuần gốc: mở Thứ 3 00:00 → CN 23:59:59 UTC, Thứ 2 chốt |
| `functions/src/bossRush.ts` | 6 endpoint: `joinbossrush`, `getbossrushpool`, `startbossrushfight`, `reportbossrushdamage`, `claimbossrushrewards`, `updatebossrushplayer` + job `finalizebossrushendedevents` (00:10 UTC Thứ 2) |
| `functions/src/rewardConfig.ts` | Thưởng + lên/xuống tier đọc từ Firestore `config/bossRush` (sửa trên console, KHÔNG cần deploy) |
| `functions/src/admin.ts` | Test/admin: `seedbossrushdummyplayers` (lấp nhóm bằng bot), `removebossrushdummyplayers`, `finalizebossrushnow` |
| `firestore.rules` | Chặn client đọc/ghi trực tiếp — mọi thứ đi qua Functions |

Firestore: `bossRushPlayers/{uid}` (tier, nhóm hiện tại, vé theo ngày, trận đang đánh) và
`bossRushPools/{poolId}` (8 người + snapshot đồ, boss chung: bossNumber/bossHP/maxBossHP, bossesKilled, claimed).

## Chạy thử local (Emulator — không cần project thật)

```bash
cd server/functions && npm install && npm test && npm run test:emulator
```
```bash
cd server && firebase emulators:start
```

Unity Editor mặc định gọi emulator (`Scripts/Network/FirebaseSettings.cs`: `USE_EMULATOR = true` trong Editor,
project `demo-dungeonrush`). Trên emulator mọi tài khoản đều là admin → gọi được seed bot.

## Đưa lên Firebase thật

1. Firebase console: tạo project, nâng gói **Blaze**, bật **Authentication → Anonymous**, tạo **Firestore** (production mode).
2. Sửa `.firebaserc` → `"default": "<project-id>"`.
3. `firebase login` (đăng nhập Google, làm 1 lần), rồi:
   ```bash
   cd server && firebase deploy --only functions,firestore
   ```
4. Unity `FirebaseSettings.cs`: `PROJECT_ID = "<project-id>"`, `WEB_API_KEY = "<Web API Key>"`.
5. (Tuỳ chọn) Import Firebase Unity SDK (App + Auth) + `google-services.json` / `GoogleService-Info.plist`,
   rồi thêm Scripting Define `FIREBASE_SDK` → đăng nhập qua SDK như StickIdle. Không có define vẫn chạy qua Auth REST.
6. Admin thật: tạo document `config/admins` = `{ "uids": ["<uid của bạn>"] }`.

## Đổi thưởng / lên-xuống tier (không cần deploy)

Document `config/bossRush`:
```json
{
  "tiers": {
    "default": {
      "guaranteedRewardsByBossesKilled": [[], [{"Type": 1, "Amount": 10}], [{"Type": 1, "Amount": 20}]],
      "placementBrackets": [
        {"minRank": 1, "maxRank": 1, "rewards": [{"Type": 1, "Amount": 100}]},
        {"minRank": 2, "maxRank": 8, "rewards": [{"Type": 0, "Amount": 500}]}
      ]
    },
    "3": { "...": "bảng riêng cho tier 3" }
  },
  "promotion": { "promoteMaxRank": 2, "demoteMinRank": 7 }
}
```
`Type` = RewardType gốc: Bone 0, Gem 1, DragonBossDungeonKey 2, ZombieHordeDungeonKey 3, Lootbox 4, Exp 5, …, Vial 11.
Chưa có document → mặc định = `BossRushLeagueConfig` gốc (hạng 1–100 nhận 1000 Bone), không lên/xuống tier.

## PvP Arena (`functions/src/pvp.ts`, `pvpConfig.ts`)

Mô hình gốc: `DecodedData/PVP_MODEL.md`. Endpoint (callable, tên viết thường như gốc): `initpvpprofile`, `openpvp`,
`findpvpopponents`, `startpvpbattle`, `reportpvpbattle`, `grantpvpadticket`, `getpvpleaderboard`; admin
`seedpvpdummyplayers` / `removepvpdummyplayers` / `removeallpvpplayers` (emulator hoặc uid trong `config/admins`).
Dữ liệu: `pvpPlayers/{uid}` (trophy Elo, vé 5 free + 4 ads/ngày UTC, roster 600s, battle token 180s, snapshot đồ).

Test: `npm test` (công thức Elo) và `npm run test:emulator:pvp` (luồng đầy đủ trên emulator; nếu emulator đã chạy sẵn thì
`node functions/scripts/pvpEmulatorTest.mjs`).

Thưởng thắng/thua theo league — **chưa có số gốc** (server gốc giấu), mặc định là placeholder. Sửa ở Firestore `config/pvp`:
```json
{ "rewardTable": [
  { "leagueIndex": 1, "winRewards": [{ "Type": "Bone", "Amount": 100 }], "loseRewards": [{ "Type": "Bone", "Amount": 30 }] }
] }
```
`Type` là TÊN `RewardType` (Bone, Gem, DragonBossDungeonKey, ZombieHordeDungeonKey, Lootbox, Exp, CloakCurrency, CultistDungeonKey, Vial…).
Deploy thật cần tạo index `pvpPlayers(countryCode, trophy)` (đã khai trong `firestore.indexes.json`).

## Data test online (1000 người chơi + 10 clan)

`functions/scripts/seedTestData.mjs` ghi thẳng vào Firestore **emulator** (không bao giờ ghi lên project thật):
1000 người chơi PvP (`seed_0001`…`seed_1000`, đồ + pet lấy từ `data/botBank.json`, trophy trải đủ 10 league),
10 clan (`c_seed_01`…`c_seed_10`, 315 người có clan, đủ 5 tier, có clan đầy 50 / đúng 30 / có đơn chờ duyệt)
và Clan War tuần hiện tại kèm điểm đóng góp các ngày đã qua. Seed cố định → chạy lại ra y hệt.

```bash
cd server/functions && npm run seed
```
```bash
cd server/functions && npm run seed -- war
```
```bash
cd server/functions && npm run seed -- clear
```

- `npm run seed`: xoá data seed cũ rồi dựng lại hết. Emulator không giữ data qua lần tắt/bật → bật lại thì chạy lại lệnh này.
  Tài khoản thật đang ở trong clan seed sẽ bị văng ra (clan được ghi đè).
- `-- war`: chỉ ghép war của tuần hiện tại + bơm điểm (emulator không chạy cron Thứ Ba). Chạy lại khi sang tuần/ngày mới;
  không đụng tới clan và giữ nguyên điểm người thật đã đóng góp.
- Cần `npm run build` trước nếu vừa sửa `src/` (script dùng `lib/`).

### Test các mode online trên data seed

```bash
cd server/functions && npm run test:seed
```

`functions/scripts/onlineModesSeedTest.mjs` — 45 ca: toàn vẹn data seed (A), PvP (B), Clan (C), Clan War trọn 1 tuần (D).
Gọi thẳng handler trong `lib/` trên Firestore emulator (không qua HTTP/Auth) nên đóng vai được leader clan seed và **giả được
đồng hồ**: Clan War chạy đủ Thứ Ba → Chủ Nhật (PvP) → Thứ Hai (nhận thưởng) → ghép tuần kế mà không phải chờ đúng ngày.
Test tự dựng lại data seed lúc đầu và lúc cuối (uid test là `test_*`, tài khoản thật không bị đụng); nếu emulator đang có
clan thật thì test dừng ngay vì phần giả lập tuần sau sẽ chốt sớm war của clan đó.

Lưu ý: `scripts/pvpEmulatorTest.mjs` gọi `removeallpvpplayers` → chạy nó trên emulator đang mở sẽ xoá sạch người chơi PvP
(cả data seed lẫn tài khoản thật); chạy xong thì `npm run seed` lại.

## Tài khoản + cloud save (`functions/src/account.ts`)

Mô hình gốc: `DecodedData/ACCOUNT_MODEL.md`. Mở game là có tài khoản khách (Auth ẩn danh); Sign Up gắn email + mật khẩu vào chính
tài khoản đó, Sign In / Forgot Password đi qua Firebase Auth (client gọi thẳng Auth REST, không qua Functions).
Endpoint: `saveuserdoc` (chỉ tài khoản đã gắn email; từ chối `VERSION_NOT_ADVANCING` khi SaveVersion không lớn hơn bản đang giữ,
`RATE_LIMITED` khi lưu cách nhau dưới 5 giây), `loaduserdoc`, `deleteuserdoc` (dọn save + hồ sơ PvP của tài khoản khách bị bỏ lại).
Dữ liệu: `userDocs/{uid}` = `{ UserJson, SaveVersion, LastSyncUtcMs, SchemaVersion, LastWriterDeviceId }`.

Test: `npm run test:emulator:account` (emulator đã chạy sẵn thì `node functions/scripts/accountEmulatorTest.mjs`).
Lên Firebase thật: bật thêm **Authentication → Email/Password**; thư đặt lại mật khẩu dùng mẫu thư mặc định của Firebase.

---
name: dungonrush-online-test-data
description: Data test online trên emulator (1000 người chơi PvP + 10 clan + Clan War) và bộ test 45 ca; cách giả đồng hồ để test logic phụ thuộc ngày
metadata:
  type: project
---
**Data seed (dựng 2026-10-12):** `server/functions/scripts/seedTestData.mjs` ghi thẳng Firestore EMULATOR bằng Admin SDK. uid `seed_0001..1000` (đồ/pet từ `data/botBank.json`, trophy đủ 10 league, 15% "người mới" quanh 1000 trophy), clan `c_seed_01..10` (315 người; đầy 50 / đúng 30 / có đơn chờ; đủ 5 tier), war tuần hiện tại + điểm đóng góp. Lệnh: `npm run seed` (dựng lại hết), `npm run seed -- war` (ghép war tuần mới, giữ điểm người thật), `npm run seed -- clear`.
**Why:** user cần đông người để test PvP/Clan trên Editor; emulator KHÔNG giữ data qua lần tắt/bật và không chạy cron ghép war Thứ Ba.
**How to apply:** user báo emulator trống / sang tuần mới → chạy lại lệnh seed, đừng viết seed mới. `npm run seed` ghi đè clan seed nên tài khoản thật đang ở clan seed bị văng. `pvpEmulatorTest.mjs` gọi `removeallpvpplayers` → xoá cả data seed.
**Bộ test:** `npm run test:seed` (`scripts/onlineModesSeedTest.mjs`, 45 ca A/B/C/D, PASS hết 2026-10-12). Gọi thẳng `handler.run({auth:{uid},data})` của `lib/*.js` + giả đồng hồ bằng `mock.timers.enable({apis:["Date"]})` của `node:test` → chạy được trọn tuần Clan War (Thứ Ba → Chủ Nhật PvP → Thứ Hai → ghép tuần kế) không cần chờ đúng ngày. Dùng lại cách này cho mọi logic server phụ thuộc ngày (vé reset, cooldown 24h, mùa Boss Rush). Test dừng nếu emulator có clan thật.
**Bật emulator trên Mac:** máy KHÔNG có java/firebase trong PATH — dùng `~/.local/tools/jdk-21.0.12.1+1/Contents/Home` (JAVA_HOME) + `~/.local/tools/node-v20.18.1-darwin-arm64/bin` (có `firebase`); trong `server/`: `npm --prefix functions run build && firebase emulators:start --only auth,functions,firestore`, rồi `npm run seed`. Port 8080 bị python chiếm nên Firestore ở 8085.
Liên quan: [[dungonrush-pvp-mode]], [[dungonrush-clan-clanwar]].

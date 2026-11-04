---
name: dungonrush-firebase-prod-deploy
description: "Server đã deploy lên Firebase thật dungeon-duck-quack-slash (us-central1, Node 22); emulator vẫn chạy project demo-dungeonrush"
metadata:
  node_type: memory
  type: project
  originSessionId: 86b0554c-4d56-489d-8f83-57d71e846de8
  modified: 2026-10-24T06:11:41.037Z
---

2026-10-24: server/ đã deploy lên project Firebase THẬT `dungeon-duck-quack-slash` (Blaze, Auth Anonymous + Email/Password bật, Firestore (default) ở us-central1, 50 function Node 22). Đã gọi thử function bằng token khách → chạy đúng.

- `.firebaserc` default = dungeon-duck-quack-slash → `firebase deploy` đẩy lên thật. Deploy: `cd server && firebase deploy --only functions,firestore` (CLI ở ~/.local/bin/firebase, đã login tongviettiepbk@gmail.com).
- Emulator vẫn tên `demo-dungeonrush`: script npm `serve`/`test:emulator*` có `--project demo-dungeonrush`; tự bật thì `firebase emulators:start --project demo-dungeonrush`.
- Unity `FirebaseSettings.cs`: PROJECT_ID thật + WEB_API_KEY (key của Web app, KHÔNG dùng key Android trong google-services.json); `EMULATOR_PROJECT_ID` cho URL emulator; `ServerId` (emulator→demo) là trường `server` gửi lên — clan lọc theo nó, data seed mang demo-dungeonrush.
- Editor để USE_EMULATOR=true (chơi/test local); build gọi server thật. 2026-10-24 đã thử false → Play qua MCP gọi server thật OK (khách, loaduserdoc, getbossrushpool), rồi user bảo quay về emulator. getpvpleaderboard trả "Failed to load leaderboard." khi user chưa có hồ sơ PvP — đúng code. Bật emulator: tab terminal export JAVA_HOME=~/.local/tools/jdk-21.0.12.1+1/Contents/Home + PATH, `cd server && npm --prefix functions run build && firebase emulators:start --project demo-dungeonrush --only auth,functions,firestore`, rồi `npm run seed` (emulator không --import nên mỗi lần bật data trống). Chưa bật define FIREBASE_SDK (nhánh SDK thiếu 4 hàm tài khoản email) dù SDK 13.17.0 đã import.
- Còn: tạo `config/admins` trên Firestore thật; Console mới không hiện Web API Key → lấy qua Add app → Web.

**Why:** chuyển từ emulator sang server thật, giữ luồng test local cũ.
**How to apply:** sửa server xong deploy lại bằng lệnh trên; đừng đổi .firebaserc về demo. Liên quan [[dungonrush-online-test-data]], [[dungonrush-account-system]].

**Deploy lại 2026-11-04** (commit c5a2f567, gồm PvP mùa, Clan War, Chat, getservertime; 53 hàm + firestore). Đã xoá 5 hàm PvP cũ, prod khớp local 53 hàm. Đã tạo RTDB mặc định us-central1 + deploy rules; `firebase database:instances:create` từ chối tạo instance mặc định và `firebase init database` đòi tương tác, nên gọi `createInstance(..., DEFAULT_DATABASE)` trong firebase-tools. Deploy gộp có `--force` từng bị kiểm tra quyền chặn: dùng `--only functions:...` rồi xoá riêng bằng `functions:delete`.

**Test server thật 2026-11-04 (Editor, phiên khách có sẵn):** tạo/tìm/giải tán clan, state Clan War trả cấu hình mới, PvP đánh trọn trận (+1 cúp đúng StarsOnWin, trừ vé), Chat Thế giới + Clan gửi nhận qua RTDB đều đạt. Chưa test được: nhận mốc/ngày 6/ngày 7 trên prod (clan tạo giữa tuần chưa được ghép; `config/admins` chưa có nên không gọi được matchclanwarsnow). Cách test: `USE_EMULATOR=false` tạm trong FirebaseSettings, sao lưu `key_user_*`, bật `UserData.isDisableSaveDataLocal`, xong tắt cờ + restore + checkout file.

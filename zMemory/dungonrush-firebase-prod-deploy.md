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
- Editor vẫn USE_EMULATOR=true; build thì gọi server thật. Chưa bật define FIREBASE_SDK (nhánh SDK thiếu 4 hàm tài khoản email) dù SDK 13.17.0 đã import.
- Còn: tạo `config/admins` trên Firestore thật; Console mới không hiện Web API Key → lấy qua Add app → Web.

**Why:** chuyển từ emulator sang server thật, giữ luồng test local cũ.
**How to apply:** sửa server xong deploy lại bằng lệnh trên; đừng đổi .firebaserc về demo. Liên quan [[dungonrush-online-test-data]], [[dungonrush-account-system]].

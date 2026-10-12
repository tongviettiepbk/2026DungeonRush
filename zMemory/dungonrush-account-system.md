---
name: dungonrush-account-system
description: Hệ tài khoản khách/email + cloud save dựng 2026-10-13 theo gốc (Sign Up = gắn email vào khách, Sign In, Forgot, Sign Out, Choose Save); đã test Play trên emulator, CHƯA commit
metadata:
  type: project
---
Dựng theo `DecodedData/ACCOUNT_MODEL.md` (reverse AccountController / FirebaseAuthController / CloudSaveController gốc). Test Play qua MCP trên emulator đạt hết 2026-10-13, CHƯA commit.

**Code:** `Scripts/Account/` (AccountController, CloudSaveController, UserDto, AccountAuthError), `Scripts/UI/Account/` (UISignIn/UISignUp/UIAccountSelect/UICloudSave/UINickname Popup), `FirebaseManager` thêm LinkWithEmail/SignInWithEmail/SendPasswordResetEmail/SignOut (Auth REST: accounts:update / signInWithPassword / sendOobCode), server `functions/src/account.ts` (saveuserdoc/loaduserdoc/deleteuserdoc, collection `userDocs`). Prefab dựng bằng menu `Tools/DungeonRush/Build Profile UI/Account Popups`; mục Account của UIProfilePopup nối bằng `.../Profile Popup - Wire Account Only` (không dựng lại prefab).

**Luồng:** Login.LoadRoutine gọi `AccountController.BootRoutine` (tối đa 7s) → chưa có phiên thì tự tạo khách. Khách chỉ lưu máy; gắn email mới sync cloud (đẩy sau lần lưu cuối 30s / app xuống nền / thoát; thêm trần 5 phút vì project lưu dày). Sign Out = máy về khách MỚI + save trắng, không hỏi xác nhận (đúng gốc). Lấy save cloud / đăng xuất → `RestartGame` (huỷ BossRush/PvP/Clan/ClanWar controller, về scene Login).

**Chưa làm / tự đặt:** nhánh `FIREBASE_SDK` chưa có 4 lời gọi email (đang REST-only); UICloudSavePopup chưa có chỗ mở (gốc: callback `ClanBattleTab.ftn`, chưa rõ điều kiện); đặt tên chưa lọc từ tục + chưa đăng ký tên phía server; `RATE_LIMITED` 5s là số tự đặt; "đóng Sign In khi mất phiên → thành khách" là suy đoán; chữ đều tiếng Anh cứng (isLocalize:false).

**Test:** `server/functions` → `node scripts/accountEmulatorTest.mjs` (emulator đang chạy). Test Play có đăng xuất/đăng nhập thì PHẢI sao lưu PlayerPrefs trước (18 key `key_user_*` + `fb_*_emu` + `cloud_*_emu`, `last_*_emu`) và xoá để chạy bằng khách tạm — đăng xuất xoá save trên máy, đăng nhập từ khách thật sẽ cho khách đó rời clan trên emulator.
Liên quan: [[dungonrush-profile-popup]], [[dungonrush-reverse-native-il2cpp]], [[dungonrush-unity-mcp-editmode-test]].

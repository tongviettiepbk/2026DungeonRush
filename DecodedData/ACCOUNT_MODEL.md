# ACCOUNT_MODEL — tài khoản khách / email + cloud save (bản gốc v41)

Reverse từ `libil2cpp.so`: `AccountController`, `FirebaseAuthController`, `CloudSaveController`, `UserDto`, 5 popup
(`SignInPopup`, `SignUpPopup`, `AccountSelectPopup`, `CloudSavePopup`, `NicknamePopup`) và phần Account của `ProfilePopup`.
Tên hàm trong ngoặc là tên obfuscate trong dump.

## 1. Mô hình

- Dùng **Firebase Auth**. `AuthProvider { Anonymous = 0, Email = 1 }` (chuỗi provider còn có `google.com`, `apple.com` nhưng không có popup nào dùng).
- Mở game: còn phiên cũ → dùng lại (`ReloadAsync`; lỗi mạng thì dùng bản cache). Chưa có phiên → `SignInAnonymouslyAsync` → **tài khoản khách**.
- Khách chỉ lưu save trên máy. **Chỉ tài khoản đã gắn email mới đồng bộ cloud** (`CloudSaveController.czq` = đã đăng nhập và không ẩn danh).
- Máy từng gắn email (`PlayerPrefs last_linked_email`) mà mất phiên → KHÔNG tự tạo khách ("Was-linked install with no session; awaiting sign-in"),
  mở `SignInPopup` điền sẵn email (`AccountController.cnb → cna`, `SignInPopup.kta`).

## 2. Các thao tác (AccountController)

| Thao tác | Hàm | Firebase | Sau khi thành công |
|---|---|---|---|
| Sign Up | `cml(email, pw, cb)` | `LinkWithCredentialAsync(EmailAuthProvider.GetCredential)` — chỉ khi đang ẩn danh, không thì "Already signed in" | Giữ nguyên uid. Lưu `last_linked_email`, `czr` (gắn trạng thái sync với uid), **đẩy save lên cloud ngay** (`czt`). Đẩy lỗi → toast `Errors.Account.CloudSyncFailed`, vẫn báo thành công |
| Sign In | `cmm` → `CaptureAnonTokenThenSignIn` | `SignInWithEmailAndPasswordAsync` | Xem mục 3 |
| Forgot Password | `cmr(email, cb)` | `SendPasswordResetEmailAsync` | Lỗi `UserNotFound` (14) cũng báo thành công |
| Sign Out | `cms(cb)` | `SignOut` rồi `SignInAnonymouslyAsync` | Xem mục 4 |
| Sync Now | `cmt(cb)` | — | `czt` (đẩy save) |

Thông báo lỗi (chuỗi cứng tiếng Anh, theo `Firebase.Auth.AuthError`):

| AuthError | Sign Up (`cnd`) | Sign In (`cne`) | Reset (`cnf`) |
|---|---|---|---|
| 1 Failure | (mặc định) | Invalid email or password. | (mặc định) |
| 5 UserDisabled | (mặc định) | This account has been disabled. | (mặc định) |
| 8 EmailAlreadyInUse | This email is already registered. Try signing in instead. | (mặc định) | (mặc định) |
| 11 InvalidEmail | Please enter a valid email address. | Invalid email or password. | Please enter a valid email address. |
| 12 WrongPassword / 14 UserNotFound | (mặc định) | Invalid email or password. | (mặc định) |
| 13 TooManyRequests | Too many attempts. Please try again later. | như trái | như trái |
| 15 ProviderAlreadyLinked | An account is already linked. | (mặc định) | (mặc định) |
| 19 NetworkRequestFailed | Network error. Check your connection and try again. | như trái | như trái |
| 23 WeakPassword | Password is too weak. Use at least 6 characters. | (mặc định) | (mặc định) |
| mặc định | message gốc, không có thì "Sign up failed. Please try again." | "Sign in failed. Please try again." | "Could not send reset email." |

## 3. Luồng Sign In

1. Đang là khách → nhớ `anonUid`, `anonHasClan`, lấy `anonToken` (ID token của khách).
2. `SignInWithEmailAndPassword`. Lỗi → trả thông báo theo bảng trên.
3. Uid mới khác `anonUid` → dọn tài khoản khách bằng **token của khách**: có clan thì gọi `leaveClan`, rồi `deleteUserDoc`
   (log "Orphan anon doc cleanup").
4. Lưu `last_linked_email`, báo thành công, rồi kéo save cloud (`cmo`): `czr`, `czs` (bật cờ `cloud_switch_pending`), `loadUserDoc`:
   - tải lỗi → toast `CloudSyncFailed`;
   - cloud trống hoặc `last_uploaded_save_version >= cloud.SaveVersion` → đẩy save trên máy lên;
   - `last_uploaded_save_version <= 0` và máy chưa chơi gì (`czx`: các level ≤ 1, chưa có tiền/đồ/pet) → lấy save cloud, nạp lại game;
   - còn lại → `AccountSelectPopup` (không đóng được).
5. `AccountSelectPopup`: **Use Cloud** → ghi đè save trên máy (`UserController.dti`), `czv`, nạp lại game (`ControllerManager.dam`).
   **Use Local** → `czw`: `last_uploaded = max(last_uploaded, cloud.SaveVersion)` rồi đẩy đè lên cloud; lỗi → toast `CloudSyncFailed`.

## 4. Luồng Sign Out

Còn thay đổi chưa đẩy → đẩy save trước (lỗi → toast `Errors.Account.SaveFailedSigningOut`, vẫn đăng xuất) → `SignOut` + đăng nhập ẩn danh mới →
`UserController.dtj` (save trắng) → xoá `last_linked_email` → nạp lại game. Không có hộp xác nhận.

## 5. Cloud save (CloudSaveController)

- Endpoint: `saveUserDoc` (thân = `UserDto`), `loadUserDoc` (`{}`), `deleteUserDoc` (`{}`), `leaveClan` (`{}`).
- `UserDto { string UserJson; int SaveVersion; long LastSyncUtcMs; int SchemaVersion; string LastWriterDeviceId }`.
- Mỗi lần đẩy: `user.SaveVersion = max(user.SaveVersion, last_uploaded) + 1`. Server trả lỗi chứa `VERSION_NOT_ADVANCING` → tải cloud về đối chiếu.
  `ProfilePopup` còn xử lý lỗi chứa `RATE_LIMITED` → "Synced recently, try again later" (số giây giới hạn nằm ở server, không rõ).
- Tự đẩy: 30 giây sau thay đổi cuối ("Auto-sync triggered (debounce elapsed)"), lúc app xuống nền, lúc thoát. Hằng số khác: 60, 300 (giãn lần thử lại), 10, 7 (giây chờ lúc mở game).
- PlayerPrefs: `last_uploaded_save_version`, `last_sync_utc_ms`, `last_upload_utc_ms`, `cloud_device_id` (Guid "N"), `cloud_unsynced_local`,
  `cloud_sync_uid`, `cloud_switch_pending`, `last_linked_email`.
- Bảng quyết định lúc mở game (chuỗi log "Boot decision"):

| Tình huống | Quyết định |
|---|---|
| đổi tài khoản chưa chốt (`cloud_switch_pending`) | hỏi (popup) |
| cloud không mới hơn | không làm gì |
| cloud mới hơn, cùng máy, máy có thay đổi chưa đẩy | giữ save trên máy, đẩy đè |
| cloud mới hơn, cùng máy, máy sạch | lấy save cloud |
| cloud mới hơn, máy khác, máy sạch | lấy save cloud |
| cloud mới hơn, máy khác, máy có thay đổi chưa đẩy — hoặc bản cloud không ghi máy | hỏi (popup) |

## 6. Popup

- **SignInPopup**: email khớp `^[^\s@]+@[^\s@]+\.[^\s@]+$` (sai → `Popup.Auth.Error.InvalidEmail`), mật khẩu rỗng → `Popup.SignIn.Error.PasswordRequired`.
  Thành công → toast "Signed in", đóng. Forgot: email không hợp lệ → "Enter your email first"; gửi xong → "Reset email sent".
  Đang chờ thì ẩn nút, hiện `LoadingImage` xoay (−360°/1s), không đóng được.
- **SignUpPopup**: cùng kiểm tra email; mật khẩu < 6 ký tự → `Popup.SignUp.Error.PasswordTooShort`. Thành công → "Account created", đóng.
- **AccountSelectPopup**: mỗi bên hiện tên (rỗng → "Guest") + 3 dòng: `Player Lv. {0}` / màn cao nhất (≤ 0 → "New adventurer") /
  thời điểm lưu (≤ 0 → "Last save: unknown"; < 60s "Saved just now"; < 1h "Saved {0} min ago"; < 24h "Saved {0}h ago"; còn lại "Saved {0}d ago").
- **CloudSavePopup** (mở từ `ClanBattleTab`): nút Save — đã gắn email → Sync Now, toast "Synced to cloud" / "Sync failed, try again later", đóng;
  còn là khách → đóng, mở Profile trang Settings rồi mở Sign Up (chưa có tên thì toast `SetNicknameFirst` + NicknamePopup trước).
- **NicknamePopup**: 3–15 ký tự, `^[a-zA-Z0-9 ]+$`, lọc từ tục; chưa có tên → đăng ký qua PvPController, có rồi → đổi qua ChatController (đều gọi server).
- **ProfilePopup / Account**: khách → nút "Sign In / Up" (chưa đặt tên thì đòi đặt tên trước); đã gắn email → nút "Sign Out",
  dòng `{email} · Last sync: {Never | just now | N min ago | N h ago | dd/MM/yyyy HH:mm}`, nút "Sync Now".

## 7. Khác biệt của bản dựng lại trong project

Xem `Scripts/Account/` + `Scripts/UI/Account/`. Auth đi qua REST (Identity Toolkit) thay cho SDK; `last_linked_email` gộp vào email lưu ở `FirebaseManager`;
save cloud = json của toàn bộ `UserData`; chưa lọc từ tục và chưa đăng ký tên phía server; số giây `RATE_LIMITED` (5s) và cách giãn thử lại là tự đặt.

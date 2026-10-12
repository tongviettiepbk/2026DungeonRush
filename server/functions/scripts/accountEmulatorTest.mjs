// Test tích hợp tài khoản + cloud save trên Firebase Emulator (chạy: npm run test:emulator:account).
// Giả lập client Unity: khách → gắn email (Sign Up) → đẩy/tải save → đăng nhập từ máy khác → quên mật khẩu → dọn tài khoản khách.
import assert from "node:assert/strict";

const PROJECT = process.env.GCLOUD_PROJECT || "demo-dungeonrush";
const AUTH = "http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts";
const TOKEN = "http://127.0.0.1:9099/securetoken.googleapis.com/v1/token?key=emulator-key";
const FN = `http://127.0.0.1:5001/${PROJECT}/us-central1`;

// Trả { ok, json, error } — error = mã lỗi Firebase (EMAIL_EXISTS...) như FirebaseManager.PostAuth đọc.
async function auth(action, body) {
  const r = await fetch(`${AUTH}:${action}?key=emulator-key`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  const j = await r.json();
  return { ok: r.ok, json: j, error: j.error?.message ?? null };
}

async function call(token, name, data = {}) {
  const r = await fetch(`${FN}/${name}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ data }),
  });
  const j = await r.json();
  if (j.error) throw new Error(name + ": " + JSON.stringify(j.error));
  return j.result;
}

const email = `player${Date.now()}@test.com`;
const save = (version, device = "deviceA") => ({ UserJson: JSON.stringify({ profile: { userName: "Alice" }, v: version }), SaveVersion: version, SchemaVersion: 1, LastWriterDeviceId: device });

// --- Khách: có tài khoản ngay, nhưng KHÔNG được lưu cloud ---
const guest = (await auth("signUp", { returnSecureToken: true })).json;
assert.ok(guest.idToken && guest.localId);
assert.deepEqual(await call(guest.idToken, "saveuserdoc", save(1)), { success: false, message: "NOT_LINKED" });
assert.equal((await call(guest.idToken, "loaduserdoc")).exists, false);

// --- Sign Up = gắn email vào tài khoản khách: uid giữ nguyên ---
const weak = await auth("update", { idToken: guest.idToken, email, password: "123", returnSecureToken: true });
assert.equal(weak.ok, false);
assert.ok(weak.error.startsWith("WEAK_PASSWORD"), weak.error);
const linked = await auth("update", { idToken: guest.idToken, email, password: "secret1", returnSecureToken: true });
assert.equal(linked.ok, true, JSON.stringify(linked.json));
assert.equal(linked.json.localId, guest.localId);
assert.ok(linked.json.idToken && linked.json.refreshToken);

// --- Cloud save: chỉ nhận phiên bản tăng dần ---
const s1 = await call(linked.json.idToken, "saveuserdoc", save(1));
assert.equal(s1.success, true);
assert.equal(s1.SaveVersion, 1);
assert.ok(s1.LastSyncUtcMs > 0);
assert.equal((await call(linked.json.idToken, "saveuserdoc", save(1))).message, "VERSION_NOT_ADVANCING");
assert.equal((await call(linked.json.idToken, "saveuserdoc", save(2))).message, "RATE_LIMITED");
assert.equal((await call(linked.json.idToken, "saveuserdoc", { SaveVersion: 3 })).message, "INVALID_PAYLOAD");

const loaded = await call(linked.json.idToken, "loaduserdoc");
assert.equal(loaded.exists, true);
assert.equal(loaded.SaveVersion, 1);
assert.equal(loaded.LastWriterDeviceId, "deviceA");
assert.equal(JSON.parse(loaded.UserJson).profile.userName, "Alice");

// --- Refresh token của tài khoản đã gắn email vẫn đổi được ID token (mở lại game) ---
const refreshed = await (await fetch(TOKEN, { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: "grant_type=refresh_token&refresh_token=" + encodeURIComponent(linked.json.refreshToken) })).json();
assert.equal(refreshed.user_id, guest.localId);
assert.equal((await call(refreshed.id_token, "loaduserdoc")).SaveVersion, 1);

// --- Sign In từ máy khác: sai mật khẩu / email lạ bị từ chối, đúng thì vào cùng uid và thấy save ---
const wrong = await auth("signInWithPassword", { email, password: "nope123", returnSecureToken: true });
assert.equal(wrong.ok, false);
assert.ok(wrong.error.startsWith("INVALID_PASSWORD") || wrong.error.startsWith("INVALID_LOGIN_CREDENTIALS"), wrong.error);
const unknown = await auth("signInWithPassword", { email: "nobody" + email, password: "secret1", returnSecureToken: true });
assert.equal(unknown.ok, false);
assert.ok(unknown.error.startsWith("EMAIL_NOT_FOUND") || unknown.error.startsWith("INVALID_LOGIN_CREDENTIALS"), unknown.error);

const guestB = (await auth("signUp", { returnSecureToken: true })).json;      // máy B mở game: khách mới
const taken = await auth("update", { idToken: guestB.idToken, email, password: "secret1", returnSecureToken: true });
assert.equal(taken.ok, false);
assert.ok(taken.error.startsWith("EMAIL_EXISTS"), taken.error);

const deviceB = await auth("signInWithPassword", { email, password: "secret1", returnSecureToken: true });
assert.equal(deviceB.ok, true);
assert.equal(deviceB.json.localId, guest.localId);
assert.equal((await call(deviceB.json.idToken, "loaduserdoc")).SaveVersion, 1);

// --- Dọn tài khoản khách bị bỏ lại: chỉ token khách mới được xoá ---
assert.equal((await call(deviceB.json.idToken, "deleteuserdoc")).message, "NOT_ANONYMOUS");
assert.equal((await call(guestB.idToken, "deleteuserdoc")).success, true);
assert.equal((await call(deviceB.json.idToken, "loaduserdoc")).exists, true);

// --- Quên mật khẩu: gửi thư đặt lại ---
const reset = await auth("sendOobCode", { requestType: "PASSWORD_RESET", email });
assert.equal(reset.ok, true, JSON.stringify(reset.json));
const badEmail = await auth("sendOobCode", { requestType: "PASSWORD_RESET", email: "not-an-email" });
assert.equal(badEmail.ok, false);

// --- Hết khoảng chờ thì máy B đẩy được bản mới hơn; máy A (bản cũ) bị từ chối ---
await new Promise((r) => setTimeout(r, 5200));
const s2 = await call(deviceB.json.idToken, "saveuserdoc", save(2, "deviceB"));
assert.equal(s2.success, true);
assert.equal((await call(linked.json.idToken, "saveuserdoc", save(2))).message, "VERSION_NOT_ADVANCING");
assert.equal((await call(linked.json.idToken, "loaduserdoc")).LastWriterDeviceId, "deviceB");

console.log("account emulator test: OK (badEmail error = " + badEmail.error + ", unknown email error = " + unknown.error + ", wrong password error = " + wrong.error + ")");

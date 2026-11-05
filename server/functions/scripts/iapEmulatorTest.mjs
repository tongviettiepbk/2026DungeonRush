// Test tích hợp verifypurchase trên Firebase Emulator (chạy: npm run test:emulator:iap).
// Google được giả bằng fetchPurchase truyền vào verifyOrder; transaction + sổ iapPurchases chạy thật trên Firestore emulator.
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { initializeApp } from "firebase-admin/app";
import { getFirestore } from "firebase-admin/firestore";

const require = createRequire(import.meta.url);
const { verifyOrder } = require("../lib/iap.js");
const { IAP_PACKAGE_NAME, purchaseDocId } = require("../lib/iapVerify.js");

const PROJECT = process.env.GCLOUD_PROJECT || "demo-dungeonrush";
const AUTH = "http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts";
const FN = `http://127.0.0.1:5001/${PROJECT}/us-central1`;

initializeApp({ projectId: PROJECT });

const order = (purchaseToken, productId = "tnt.gem1") => ({ packageName: IAP_PACKAGE_NAME, productId, purchaseToken });
const paid = (extra = {}) => async () => ({ purchaseState: 0, consumptionState: 0, acknowledgementState: 0, orderId: "GPA.1", purchaseTimeMillis: "1000", purchaseType: 0, ...extra });
const ledger = (token) => getFirestore().collection("iapPurchases").doc(purchaseDocId(token)).get();

// Lần đầu: trao, ghi sổ.
assert.deepEqual(await verifyOrder("u1", order("t1"), paid()), { status: "ok", orderId: "GPA.1" });
const row = await ledger("t1");
assert.equal(row.get("uid"), "u1");
assert.equal(row.get("productId"), "tnt.gem1");
assert.equal(row.get("purchaseType"), 0);

// Phản hồi lần trước mất, đơn chưa khép: cùng người hỏi lại vẫn được trao.
assert.equal((await verifyOrder("u1", order("t1"), paid())).status, "ok");

// Người khác dùng lại biên lai chưa khép: bác, sổ giữ chủ cũ.
assert.deepEqual(await verifyOrder("u2", order("t1"), paid()), { status: "invalid", reason: "token-other-account", orderId: "GPA.1" });
assert.equal((await ledger("t1")).get("uid"), "u1");

// Đã khép: mọi lượt sau là duplicate.
assert.equal((await verifyOrder("u1", order("t1"), paid({ consumptionState: 1 }))).status, "duplicate");
assert.equal((await verifyOrder("u1", order("t1"), paid({ acknowledgementState: 1 }))).status, "duplicate");
assert.equal((await verifyOrder("u2", order("t1"), paid({ consumptionState: 1 }))).status, "duplicate");

// Google chưa thu tiền / đã huỷ: không ghi sổ.
assert.equal((await verifyOrder("u1", order("t2"), paid({ purchaseState: 2 }))).status, "pending");
assert.equal((await verifyOrder("u1", order("t3"), paid({ purchaseState: 1 }))).status, "invalid");
assert.equal((await ledger("t2")).exists, false);
assert.equal((await ledger("t3")).exists, false);

// Biên lai giả và lỗi cấu hình: 404 token → invalid, 403 service account → error để client giữ đơn thử lại.
const playError = (status, message) => async () => { throw Object.assign(new Error(message), { response: { status, data: { error: { message } } } }); };
assert.equal((await verifyOrder("u1", order("t4"), playError(404, "The purchase token was not found."))).status, "invalid");
assert.equal((await verifyOrder("u1", order("t5"), playError(403, "permission denied"))).status, "error");
assert.equal((await verifyOrder("u1", order("t6"), async () => { throw new Error("socket hang up"); })).status, "error");

// 2 lượt song song cùng 1 biên lai: transaction giữ 1 chủ, người còn lại bị bác.
const racing = await Promise.all([verifyOrder("u1", order("t7"), paid()), verifyOrder("u2", order("t7"), paid())]);
assert.deepEqual(racing.map((r) => r.status).sort(), ["invalid", "ok"]);

// Qua HTTP thật: thiếu đăng nhập bị chặn, package lạ bị bác trước khi đụng tới Google.
const noAuth = await fetch(`${FN}/verifypurchase`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ data: order("t8") }) });
assert.equal(noAuth.status, 401);

const guest = await (await fetch(`${AUTH}:signUp?key=emulator-key`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ returnSecureToken: true }) })).json();
const badPackage = await fetch(`${FN}/verifypurchase`, {
  method: "POST",
  headers: { "Content-Type": "application/json", Authorization: `Bearer ${guest.idToken}` },
  body: JSON.stringify({ data: { ...order("t8"), packageName: "com.other.game" } }),
});
assert.deepEqual((await badPackage.json()).result, { status: "error", reason: "package-not-allowed" });

console.log("iapEmulatorTest OK");
process.exit(0);

import * as assert from "assert";
import {
  IAP_PACKAGE_NAME, classifyPlayApiError, decidePurchase, isClosed, ledgerVerdict, purchaseDocId, validateRequest,
} from "../iapVerify";

const PRODUCT = "tnt.gem1";
const request = { packageName: IAP_PACKAGE_NAME, productId: PRODUCT, purchaseToken: "token" };

// Dữ liệu client gửi lên.
assert.strictEqual(validateRequest(request), null);
assert.strictEqual(validateRequest(undefined), "missing-packageName");
assert.strictEqual(validateRequest({ ...request, productId: "" }), "missing-productId");
assert.strictEqual(validateRequest({ ...request, purchaseToken: "x".repeat(4097) }), "token-too-long");
assert.strictEqual(validateRequest({ ...request, packageName: "com.other.game" }), "package-not-allowed");

// Biên lai Google.
assert.deepStrictEqual(decidePurchase({ purchaseState: 0, productId: PRODUCT, orderId: "GPA.1" }, PRODUCT), { status: "ok", orderId: "GPA.1" });
assert.strictEqual(decidePurchase({ purchaseState: 0 }, PRODUCT).status, "ok");     // Google không trả productId
assert.strictEqual(decidePurchase({ purchaseState: 2, productId: PRODUCT }, PRODUCT).status, "pending");
assert.deepStrictEqual(decidePurchase({ purchaseState: 1 }, PRODUCT), { status: "invalid", reason: "canceled" });
assert.deepStrictEqual(decidePurchase({ purchaseState: 0, productId: "tnt.gem6" }, PRODUCT), { status: "invalid", reason: "product-mismatch" });
assert.deepStrictEqual(decidePurchase(null, PRODUCT), { status: "invalid", reason: "no-purchase" });
assert.deepStrictEqual(decidePurchase({ purchaseState: 9 }, PRODUCT), { status: "invalid", reason: "unknown-state:9" });

// Đơn đã khép.
assert.strictEqual(isClosed({ consumptionState: 0, acknowledgementState: 0 }), false);
assert.strictEqual(isClosed({ consumptionState: 1, acknowledgementState: 0 }), true);
assert.strictEqual(isClosed({ consumptionState: 0, acknowledgementState: 1 }), true);
assert.strictEqual(isClosed({}), false);

// Sổ đối chiếu.
assert.strictEqual(ledgerVerdict(null, "u1", false).status, "ok");
assert.strictEqual(ledgerVerdict("u1", "u1", false).status, "ok");          // phản hồi lần trước mất giữa đường: không được mất hàng
assert.strictEqual(ledgerVerdict("u1", "u1", true).status, "duplicate");
assert.strictEqual(ledgerVerdict(null, "u1", true).status, "duplicate");
assert.strictEqual(ledgerVerdict("u1", "u2", true).status, "duplicate");
assert.deepStrictEqual(ledgerVerdict("u1", "u2", false), { status: "invalid", reason: "token-other-account" });

// Lỗi Play API: 404 có 2 nghĩa, chỉ message phân biệt được.
assert.deepStrictEqual(classifyPlayApiError(404, "The purchase token was not found."), { status: "invalid", reason: "play-api-404" });
assert.deepStrictEqual(classifyPlayApiError(404, "No application was found for the given package name."), { status: "error", reason: "play-api-404-app-not-found" });
assert.deepStrictEqual(classifyPlayApiError(400, "Invalid Value"), { status: "invalid", reason: "play-api-400" });
assert.deepStrictEqual(classifyPlayApiError(410, ""), { status: "invalid", reason: "play-api-410" });
assert.deepStrictEqual(classifyPlayApiError(401, "denied"), { status: "error", reason: "play-api-401" });
assert.deepStrictEqual(classifyPlayApiError(403, "denied"), { status: "error", reason: "play-api-403" });
assert.deepStrictEqual(classifyPlayApiError(0, "socket hang up"), { status: "error", reason: "play-api-network" });

// ID sổ ổn định, không chứa token gốc.
assert.strictEqual(purchaseDocId("a"), purchaseDocId("a"));
assert.notStrictEqual(purchaseDocId("a"), purchaseDocId("b"));
assert.strictEqual(purchaseDocId("a").length, 64);

console.log("iapVerifyTest OK");

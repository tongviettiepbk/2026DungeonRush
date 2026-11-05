import { createHash } from "crypto";

// Phần THUẦN của verifypurchase (không mạng, không Firestore). Gọi Play Developer API + ghi sổ nằm ở iap.ts.

export const IAP_PACKAGE_NAME = "com.zone.dungeon.duck.quack.slash";
const MAX_TOKEN_LENGTH = 4096;

// purchases.products.get → ProductPurchase.purchaseState
const STATE_PURCHASED = 0;
const STATE_CANCELED = 1;
const STATE_PENDING = 2;

export type VerifyStatus = "ok" | "duplicate" | "pending" | "invalid" | "error";

export interface VerifyResult {
  status: VerifyStatus;
  reason?: string;
  orderId?: string;
}

export interface PlayPurchase {
  productId?: string;
  orderId?: string;
  purchaseState?: number;
  consumptionState?: number;
  acknowledgementState?: number;
  purchaseTimeMillis?: string;
  purchaseType?: number;
}

export interface VerifyRequest {
  packageName: string;
  productId: string;
  purchaseToken: string;
}

// Kiểm dữ liệu client gửi lên. Trả chuỗi lỗi, hoặc null nếu hợp lệ.
export function validateRequest(data: Partial<VerifyRequest> | null | undefined): string | null {
  for (const key of ["packageName", "productId", "purchaseToken"] as const) {
    const value = data?.[key];
    if (typeof value !== "string" || value.length === 0) return "missing-" + key;
  }
  if (data!.purchaseToken!.length > MAX_TOKEN_LENGTH) return "token-too-long";
  if (data!.packageName !== IAP_PACKAGE_NAME) return "package-not-allowed";
  return null;
}

// Đọc biên lai Google trả về:
//   ok      — đã thu tiền, đúng sản phẩm
//   pending — Google còn chờ thanh toán (tiền mặt, duyệt sau): chưa trao, chưa khép đơn
//   invalid — huỷ/hoàn tiền, hoặc token của sản phẩm khác: không trao
export function decidePurchase(purchase: PlayPurchase | null | undefined, expectedProductId: string): VerifyResult {
  if (!purchase) return { status: "invalid", reason: "no-purchase" };
  if (typeof purchase.productId === "string" && purchase.productId !== expectedProductId) {
    return { status: "invalid", reason: "product-mismatch" };
  }

  switch (purchase.purchaseState) {
    case STATE_PURCHASED: return { status: "ok", orderId: purchase.orderId ?? "" };
    case STATE_PENDING: return { status: "pending" };
    case STATE_CANCELED: return { status: "invalid", reason: "canceled" };
    default: return { status: "invalid", reason: "unknown-state:" + purchase.purchaseState };
  }
}

// Đơn đã khép ở Google: consumable đã consume, hoặc non-consumable đã acknowledge. Khép rồi thì Google không phát lại nữa.
export function isClosed(purchase: PlayPurchase): boolean {
  return purchase.consumptionState === 1 || purchase.acknowledgementState === 1;
}

// Đối chiếu sổ iapPurchases. ownerUid = uid đã ghi trong sổ, null = token lần đầu thấy.
//   Đơn đã khép                     → duplicate: hàng đã trao, biên lai đang bị phát lại.
//   Chưa khép, chưa có sổ / cùng uid → ok: lần trước phản hồi có thể đã mất giữa đường (server ghi sổ xong, client chưa trao).
//                                     Trả duplicate ở đây thì client khép đơn, người chơi mất tiền mà không có hàng.
//   Chưa khép, sổ của uid khác      → invalid: không trao, không khép, Google tự hoàn tiền sau 3 ngày nếu là người mua thật.
export function ledgerVerdict(ownerUid: string | null, uid: string, closed: boolean): VerifyResult {
  if (closed) return { status: "duplicate" };
  if (ownerUid === null || ownerUid === uid) return { status: "ok" };
  return { status: "invalid", reason: "token-other-account" };
}

// Play API từ chối → lỗi của BIÊN LAI (invalid) hay lỗi CẤU HÌNH/hạ tầng (error: client giữ đơn treo, thử lại sau)?
// Google dùng 404 cho cả "purchase token not found" (biên lai giả) lẫn "No application found" (app chưa có trên
// Play Console), chỉ message phân biệt được. 401/403 = service account thiếu quyền. 400 = token sai dạng.
export function classifyPlayApiError(code: number, message: string): VerifyResult {
  const lower = message.toLowerCase();
  const reason = "play-api-" + (code || "network");

  if (code === 404 && (lower.includes("application") || lower.includes("package"))) {
    return { status: "error", reason: reason + "-app-not-found" };
  }
  if (code === 400 || code === 404 || code === 410) return { status: "invalid", reason };
  return { status: "error", reason };
}

// ID document cho purchaseToken: token dài và có ký tự tuỳ ý nên băm.
export function purchaseDocId(purchaseToken: string): string {
  return createHash("sha256").update(purchaseToken, "utf8").digest("hex");
}

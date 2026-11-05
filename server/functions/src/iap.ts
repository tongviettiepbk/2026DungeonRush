import { FieldValue, getFirestore } from "firebase-admin/firestore";
import { onCall } from "firebase-functions/v2/https";
import { GoogleAuth } from "google-auth-library";
import { requireUid } from "./clan";
import {
  PlayPurchase, VerifyRequest, VerifyResult, classifyPlayApiError, decidePurchase, isClosed, ledgerVerdict, purchaseDocId,
  validateRequest,
} from "./iapVerify";

// verifypurchase: xác minh biên lai Google Play trước khi client trao hàng.
//   → { status: "ok" | "duplicate" | "pending" | "invalid" | "error", orderId?, reason? }
// ok: trao hàng rồi khép đơn. duplicate: chỉ khép đơn. Còn lại: không trao, không khép.
//
// iapPurchases/{sha256(token)} là sổ đối chiếu, KHÔNG phải quyền sở hữu: kinh tế game vẫn ở client, nên hàm này chặn
// được biên lai giả/phát lại, không chặn được app đã vá bỏ cú gọi.
//
// Service account chạy function (<số project>-compute@developer.gserviceaccount.com) phải được mời vào Play Console
// với quyền xem đơn hàng, và project phải bật Google Play Android Developer API. Xem server/README.md.
//
// Chỉ có Google Play: iOS cần App Store Server API (key .p8), khi đó thêm nhánh ở đây.

const IAP_PURCHASES = "iapPurchases";
const PLAY_API = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications";
const PLAY_API_TIMEOUT_MS = 15000;

const playAuth = new GoogleAuth({ scopes: ["https://www.googleapis.com/auth/androidpublisher"] });

export type FetchPurchase = (request: VerifyRequest) => Promise<PlayPurchase>;

async function fetchFromPlay({ packageName, productId, purchaseToken }: VerifyRequest): Promise<PlayPurchase> {
  const client = await playAuth.getClient();
  const url = `${PLAY_API}/${encodeURIComponent(packageName)}/purchases/products/` +
    `${encodeURIComponent(productId)}/tokens/${encodeURIComponent(purchaseToken)}`;
  const response = await client.request<PlayPurchase>({ url, timeout: PLAY_API_TIMEOUT_MS });
  return response.data;
}

// Tách fetchPurchase ra tham số để test chạy được transaction thật trên emulator mà không gọi Google.
export async function verifyOrder(uid: string, data: Partial<VerifyRequest>, fetchPurchase: FetchPurchase): Promise<VerifyResult> {
  const bad = validateRequest(data);
  if (bad) return { status: "error", reason: bad };

  const request = data as VerifyRequest;
  const { productId, purchaseToken } = request;

  let purchase: PlayPurchase;
  try {
    purchase = await fetchPurchase(request);
  } catch (e) {
    const failure = e as { response?: { status?: number; data?: { error?: { message?: string } } }; message?: string };
    const code = failure.response?.status ?? 0;
    const message = failure.response?.data?.error?.message ?? failure.message ?? "";
    const verdict = classifyPlayApiError(code, message);
    console.warn("verifypurchase: Play API failed", { uid, productId, code, message, status: verdict.status });
    return verdict;
  }

  const decision = decidePurchase(purchase, productId);
  if (decision.status !== "ok") {
    console.warn("verifypurchase: not granted", { uid, productId, ...decision, orderId: purchase.orderId });
    return decision;
  }

  const db = getFirestore();
  const ref = db.collection(IAP_PURCHASES).doc(purchaseDocId(purchaseToken));
  return db.runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const owner = snap.exists ? (snap.get("uid") as string) : null;
    const verdict = ledgerVerdict(owner, uid, isClosed(purchase));

    if (verdict.status === "ok" && !snap.exists) {
      tx.set(ref, {
        uid,
        productId,
        orderId: decision.orderId,
        purchaseTimeMillis: Number(purchase.purchaseTimeMillis) || 0,
        purchaseType: purchase.purchaseType ?? null,    // 0 = test
        verifiedAt: FieldValue.serverTimestamp(),
      });
    }

    if (verdict.status !== "ok") console.warn("verifypurchase: replayed", { uid, owner, productId, ...verdict, orderId: decision.orderId });
    return { ...verdict, orderId: decision.orderId };
  });
}

export const verifypurchase = onCall(async (request) => {
  const uid = requireUid(request);
  return verifyOrder(uid, (request.data ?? {}) as Partial<VerifyRequest>, fetchFromPlay);
});

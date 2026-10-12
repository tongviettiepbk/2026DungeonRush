import { getFirestore } from "firebase-admin/firestore";
import { CallableRequest, onCall } from "firebase-functions/v2/https";
import { requireUid } from "./clan";

// Cloud save của tài khoản — 3 endpoint CloudSaveController / AccountController gốc gọi (tên viết thường như các hàm khác):
//   saveuserdoc   — ghi đè save lên cloud, CHỈ khi SaveVersion mới lớn hơn bản đang có.
//   loaduserdoc   — đọc save cloud ('{}' → UserDto, chưa có thì exists = false).
//   deleteuserdoc — xoá dữ liệu của tài khoản khách bị bỏ lại sau khi đăng nhập sang tài khoản email.
// Chỉ tài khoản đã liên kết email mới lưu cloud (gốc: khách chỉ lưu trên máy). Lỗi nghiệp vụ trả {success:false, message}.

const USER_DOCS = "userDocs";
const PVP_PLAYERS = "pvpPlayers";
// Document Firestore tối đa 1 MiB.
const MAX_USER_JSON_LENGTH = 900_000;
// Client gốc xử lý lỗi RATE_LIMITED ("Synced recently") nhưng server gốc giấu số giây — tạm 5s.
const MIN_SAVE_INTERVAL_MS = 5000;

// UserDto gốc.
interface UserDto {
  UserJson: string;
  SaveVersion: number;
  LastSyncUtcMs: number;
  SchemaVersion: number;
  LastWriterDeviceId: string;
}

function fail(message: string) {
  return { success: false, message };
}

function isLinked(request: CallableRequest<unknown>): boolean {
  return request.auth?.token.firebase?.sign_in_provider !== "anonymous";
}

export const saveuserdoc = onCall(async (request) => {
  const uid = requireUid(request);
  if (!isLinked(request)) return fail("NOT_LINKED");

  const data = (request.data ?? {}) as Partial<UserDto>;
  const userJson = typeof data.UserJson === "string" ? data.UserJson : "";
  const saveVersion = Math.floor(Number(data.SaveVersion) || 0);
  if (userJson.length === 0 || userJson.length > MAX_USER_JSON_LENGTH || saveVersion <= 0) return fail("INVALID_PAYLOAD");

  const ref = getFirestore().collection(USER_DOCS).doc(uid);
  return getFirestore().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const current = snap.exists ? (snap.data() as UserDto) : null;
    const now = Date.now();
    if (current && saveVersion <= current.SaveVersion) return fail("VERSION_NOT_ADVANCING");
    if (current && now - current.LastSyncUtcMs < MIN_SAVE_INTERVAL_MS) return fail("RATE_LIMITED");

    const doc: UserDto = {
      UserJson: userJson,
      SaveVersion: saveVersion,
      LastSyncUtcMs: now,
      SchemaVersion: Math.floor(Number(data.SchemaVersion) || 0),
      LastWriterDeviceId: typeof data.LastWriterDeviceId === "string" ? data.LastWriterDeviceId.slice(0, 64) : "",
    };
    tx.set(ref, doc);
    return { success: true, message: "", SaveVersion: doc.SaveVersion, LastSyncUtcMs: doc.LastSyncUtcMs, LastWriterDeviceId: doc.LastWriterDeviceId };
  });
});

export const loaduserdoc = onCall(async (request) => {
  const uid = requireUid(request);
  const snap = await getFirestore().collection(USER_DOCS).doc(uid).get();
  if (!snap.exists) return { success: true, message: "", exists: false };
  return { success: true, message: "", exists: true, ...(snap.data() as UserDto) };
});

// Gọi bằng token của tài khoản KHÁCH vừa bị bỏ lại (gốc: "Orphan anon doc cleanup") — dọn save cloud + hồ sơ PvP của nó
// để không còn "bóng ma" trên bảng xếp hạng. Rời clan do client gọi leaveclan trước.
export const deleteuserdoc = onCall(async (request) => {
  const uid = requireUid(request);
  if (isLinked(request)) return fail("NOT_ANONYMOUS");
  const db = getFirestore();
  await Promise.all([db.collection(USER_DOCS).doc(uid).delete(), db.collection(PVP_PLAYERS).doc(uid).delete()]);
  return { success: true, message: "" };
});

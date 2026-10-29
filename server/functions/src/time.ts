import { onCall } from "firebase-functions/v2/https";

// Giờ server (UTC, mili giây) — client lấy làm mốc reset theo ngày (key Dungeon, vé Events) để chỉnh giờ máy không có tác dụng.
export const getservertime = onCall(async () => {
  return { success: true, message: "", serverTimeMs: Date.now() };
});

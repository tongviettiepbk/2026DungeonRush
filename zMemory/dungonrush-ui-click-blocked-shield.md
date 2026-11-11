---
name: dungonrush-ui-click-blocked-shield
description: Nút popup lúc được lúc không (Sell/Equip) = ShieldUI của UIManager (Overlay 1000) bật khi campaign chuyển màn; đã sửa 2026-11-11 + cách dò raycast qua MCP
metadata:
  type: project
---
Lỗi "bấm Sell/Equip không ăn / thi thoảng khó bấm" (BlueStacks + Editor, 2026-11-11): campaign chạy nền, mỗi lần qua màn gọi
`UIManager.FadeOut(target: MapFadeImage)` → `ActiveShield(true)` bật **ShieldUI** (UIManager Canvas **Screen Space Overlay order 1000**,
Image trong suốt rt=1, toàn màn) tới hết FadeIn (gồm cả lúc chờ hút mảnh exp) → nuốt MỌI click lobby/popup.
Đã sửa: fade có `target` riêng (chỉ vùng map) không bật/tắt shield; fade toàn màn (imgFade) giữ nguyên. Chưa commit lúc ghi.

**Why:** popup Resources là Screen Space Camera (layer UI, order 5+n×20); mọi Canvas Overlay (UIManager 1000) luôn thắng raycast bất kể hình hiển thị.
**How to apply:** nút "không bấm được" → dò `EventSystem.RaycastAll` tại tâm nút qua execute_code (đợi 1 frame sau khi mở popup), xem top hit;
nhớ ShieldUI/toast (PopupToastMessage BG đáy màn, chặn ~2s) thuộc Overlay 1000. Click chuột giả bằng CGEvent (osascript JXA) KHÔNG tới
được Game view (thiếu quyền Accessibility). adb BlueStacks trả "error: closed" → phải bật ADB trong cài đặt BlueStacks mới đọc log.
Liên quan: [[dungonrush-unity-mcp-editmode-test]], [[dungonrush-campaign-map-original]].

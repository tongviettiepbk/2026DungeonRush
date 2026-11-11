---
name: dungonrush-ui-click-blocked-shield
description: Nút bị nuốt click = vật ở canvas Overlay 1000 của UIManager — ShieldUI lúc chuyển màn campaign (toàn màn) + toast vô hình ở đáy (tab bar/Sell/Equip); đã sửa cả 2 ngày 2026-11-11; cách dò raycast qua MCP
metadata:
  node_type: memory
  type: project
  originSessionId: a32c4062-b2f4-4797-94a2-92bd3f253b0d
  modified: 2026-11-11T03:45:26.252Z
---

Canvas của UIManager là **Screen Space Overlay order 1000** → mọi thứ trong đó luôn thắng raycast trước popup (Screen Space Camera, layer UI, order 5+n×20) và lobby.

1. **ShieldUI (toàn màn)**: campaign qua màn gọi `UIManager.FadeOut(target: MapFadeImage)` → từng bật ShieldUI tới hết FadeIn → nuốt mọi click.
   Đã sửa (commit d9bec3d1): fade có `target` riêng không bật/tắt shield.
2. **PopupToastMessage (chỉ vùng đáy)**: BG 700×120 neo đáy, ở 1080×1920 nằm y −30..150 px = đúng tab bar + nút Sell/Equip của UINewForgePopup.
   Animation legacy `toast_message` đưa CanvasGroup alpha về 0 ở 2.08s; tự ẩn nhờ event OnAnimationDone ĐÚNG khung cuối. Lỡ event → toast đứng ở alpha 0,
   vô hình nhưng vẫn chặn (blocksRaycasts) + isAnimating kẹt nên không hiện toast nữa. Hay gặp trên BlueStacks (Boost x2 không có ads → toast "ad not ready").
   Đã sửa 2026-11-11 (chưa commit lúc ghi): PopupToastMessage.Awake tắt blocksRaycasts/raycastTarget; Show cho hiện lại nếu kẹt quá 3s.

**Why:** "chỉ vùng đáy bấm không được, không thấy gì che" = toast vô hình; "cả màn" = ShieldUI. APK trên BlueStacks build 00:03 11/11 là bản CŨ (trước cả 2 lần sửa) — phải build lại mới test được.
**How to apply:** nút "không bấm được" → dò `EventSystem.RaycastAll` qua execute_code, xem top hit. BẪY khi test:
- Graphic mới bật thì depth = −1 trong cùng frame → bật ở 1 lệnh, raycast ở lệnh sau.
- Canvas Overlay so toạ độ với `Screen.width/height`, mà trong execute_code Screen trả kích thước cửa sổ Game (vd 479×971) chứ không phải độ phân giải → điểm x/y vượt số đó bị bỏ qua, kết luận sai "không chặn". Dùng điểm nhỏ (vd 300,75) hoặc đọc `canvas.pixelRect`.
- Đổi Game view 1080×1920 (giống BlueStacks) bằng reflection `GameView.SizeSelectionCallback(index)` (nhóm Android: 5 = 1080×1920, 20 = 1080×2160).
- BlueStacks: adb có (`ANDROID_SERIAL=127.0.0.1:5555`) nhưng hay rớt ("waiting for device") → luôn bọc `timeout`. `dumpsys input` cho thấy cửa sổ game nhận chạm toàn màn (Android không che gì).
Click chuột giả bằng CGEvent (osascript JXA) KHÔNG tới được Game view (thiếu quyền Accessibility).
Liên quan: [[dungonrush-unity-mcp-editmode-test]], [[dungonrush-campaign-map-original]], [[dungonrush-android-release-build]].

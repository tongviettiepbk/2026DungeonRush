---
name: dungonrush-canvas-scaler-expand
description: Mọi canvas UI 1080x2160 (UIMainGame2 + 49 popup) đã đổi CanvasScaler sang Expand 2026-11-12; builder Editor cũng đặt Expand — đừng trả về Match Width/Height
metadata:
  node_type: memory
  type: project
  originSessionId: 1635c97d-cebc-4b50-8077-632c7ddd5e2c
  modified: 2026-11-12T10:54:30.634Z
---

2026-11-12: user báo game phải hợp nhiều tỉ lệ màn (Samsung A06 = 720x1600, 20:9). Kết quả kiểm tra:
- `UIMainGame2` (lobby) trước để Match = Height → màn dài hơn 1:2 canvas hẹp hơn 1080 (A06 ~972) → thẻ Dungeon (rộng 990)
  và Shop bị cắt mép phải.
- 49 popup ở `Resources/Prefabs/UI` trước để Match = Width → màn 16:9 / iPad canvas thấp (iPad ~1440) → popup cao
  (Battle Pass, Profile) bị cắt trên/dưới. Build iOS có iPad (targetDevice iPhoneAndiPad).
Đã đổi cả 50 prefab sang `m_ScreenMatchMode: 1` (Expand) + 12 builder Editor (`*PrefabBuilder.cs`) đặt
`scaler.screenMatchMode = Expand`. Canvas UIManager (720x1280) vốn là Expand. Đã chụp Play kiểm 16:9 / A06 / iPhone 19.5:9 /
10:24 / iPad 3:4: lobby, Dungeon, Shop, Battle Pass, map đều nằm gọn. Cũng chụp 3 dungeon (Dragon/Zombie/Cultist) + Boss Rush
(BossRushTester.StartFight qua reflection) ở 5 tỉ lệ: map vừa, hàng AUTO/pet không còn bị tấm "Dungeon in progress" che nửa
(lỗi user thấy trên A06 bản cũ), console không lỗi. Chụp mode: KHÔNG đặt Time.timeScale=0 (đứng fade chuyển cảnh), chờ
`gc.mode.type` đúng rồi mới chụp; ẩn popup thưởng offline trước.

**Why:** Expand giữ canvas ≥ 1080x2160 → màn dài giữ rộng 1080, màn thấp giữ cao 2160, không bao giờ cắt nội dung.
**How to apply:** prefab/builder UI mới có CanvasScaler thì đặt Expand. Kiểm tỉ lệ: Game view đã thêm cỡ T720x1600, T1080x2400,
T1170x2532, T1536x2048, T1080x1920 (nhóm Android, index 24–28); chụp hàng loạt bằng EditorApplication.update + ScreenCapture.
Ảnh so sánh ở `Temp/AspectShots/final/sheet_*.png`. Xem [[dungonrush-map-camera-fit]], [[dungonrush-main-ui-uimaingame2]].

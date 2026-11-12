---
name: dungonrush-map-camera-fit
description: "Camera MainGame tự căn map theo tỉ lệ màn (MapCameraFitter) — map nằm giữa TopBarUI và VerticalBottomLayout, chừa 120 canvas mỗi bên; Boss Rush giữ camera gốc"
metadata:
  node_type: memory
  type: project
  originSessionId: f9dc2817-b198-4e5e-9bc8-ec94ceb18ff7
  modified: 2026-11-12T10:22:36.571Z
---

2026-11-12: camera MainGame trước đặt cứng (ortho 16.8, pos 4.5,-1.6) → màn 20:9/22:10 thanh trên đè hàng trên map. Đã thêm
`Scripts/GamePlay/Map/MapCameraFitter.cs` (tự AddComponent lên Camera.main): đo mép dưới `UIMainLobby.rectTopBar` (TopBarUI) và
mép trên cao nhất của con đang bật trong `rectBottomLayout` (VerticalBottomLayout, = hàng AUTO/pet) theo toạ độ canvas gốc,
rồi đặt orthographicSize + vị trí cho khung map (lưới + tường 1 ô + chừa 0.25) vừa và nằm giữa; lề ngang 120 đơn vị canvas mỗi
bên để không đè cột nút offer phải / vé trái. Tự căn lại khi đổi kích thước màn hoặc vùng UI đổi.
BaseMode.CreateMap gọi `MapCameraFitter.Fit(map)`; mode tự lo camera override `FitCameraToMap => false` → `Release()` trả cỡ gốc
(BossRushMode). LỆCH GỐC có chủ ý (gốc có CameraController fit riêng, chưa reverse).

**Why:** user báo map không căn giữa trên các màn khác tỉ lệ.
**How to apply:** test Play 16:9 / 1:2 / 22:10 đã đạt (campaign); dungeon chưa chụp kiểm. Đổi UI lobby (đổi tên/cấu trúc TopBarUI,
VerticalBottomLayout) thì giữ 2 field này. Đổi Game view bằng reflection `GameView.selectedSizeIndex` (Android group: 5=1080x1920,
7=1080x2160, 23=1080x2376). Xem [[dungonrush-map-cell-snap]], [[dungonrush-main-ui-uimaingame2]].

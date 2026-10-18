---
name: dungonrush-reskin-setting
description: Reskin trang Settings của UIProfilePopup 2026-10-19 theo mockup _Concept/Setting (art giao đủ, không cần ChatGPT); slider/nút gạt đổi sprite núm; đã commit
metadata:
  type: project
---
Trang Settings trong `UIProfilePopup` reskin 2026-10-19 theo `ArtIncoming/_Concept/Setting/setting demo.png` (commit a953945).
- Mockup = bố cục Settings cũ phóng ×1.21 → `SettingsPage/VerticalLayout` localScale 1.21, đỉnh ở y 607 (hàng Sound FX tâm 659). Image 9-slice trong khối đặt `pixelsPerUnitMultiplier` 1.21 để viền giữ độ dày kit; nút xanh cao 104/1.21.
- Art giao 4 ảnh → `Art/1_UIKit/Setting/` (đổi tên, không xử lý): knob_off (xám) / knob_on (xanh) 82x94, fill_green 234x44 (viền 22 L/R), track_dark 340x52 (viền 26 L/R). Không cần ChatGPT; nút Discord/Country/Language giữ icon cũ (mockup để trống).
- Code: `UIProfilePopup.sprHandleOn/Off` — tay nắm slider đổi SPRITE thay tô màu; `ToggleButton.OnSprite/OffSprite/OnFill` (phần đầy xanh lá chỉ hiện khi bật), chữ trên núm = `Loc.L("Common.On/Off")` (trước là "ON/OFF" cứng), Padding = nửa rộng núm (núm sát mép).
- BẪY: Slider ngang ép anchor dọc tay nắm 0..1 → sizeDelta.y = cao núm − cao rãnh.
- Nhãn hàng tự co max 32, User ID/Version max 32 (trước 40). Test Play: đổi sprite/fill đúng. Ảnh so: `_Concept/Setting/compare_mockup_vs_game.png`, `play_setting.png`.

**Why:** user giao mockup Setting (BallIdle) để reskin tiếp sau Profile.
**How to apply:** KHÔNG chạy lại ProfilePrefabBuilder. Xem [[dungonrush-reskin-plan]], [[dungonrush-profile-popup]], [[dungonrush-reskin-ui-howto]].

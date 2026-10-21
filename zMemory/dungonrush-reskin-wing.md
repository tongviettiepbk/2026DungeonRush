---
name: dungonrush-reskin-wing
description: "Reskin trang Wing (PageWing trong UIMainGame2) + popup Craft/Reroll 2026-10-19 theo mockup _Concept/Wing — chỉ UI, GIỮ icon wing; kit Mastery/BattlePass, art giao chỉ 2 mũi tên; không cần ChatGPT; đã commit"
metadata:
  node_type: memory
  type: project
  originSessionId: 16ecd380-03e1-45aa-ae79-4dcd72512e2a
  modified: 2026-10-18T17:46:14.541Z
---

Reskin Wing 2026-10-19 (đã commit, chưa test Play — chỉ chụp preview). Mockup `ArtIncoming/_Concept/Wing/` (Wing demo + `_art/` 2 mũi tên xanh). Ảnh so: `_Concept/Wing/compare_mockup_vs_game.png`.

- User chốt: item wing KHÔNG vẽ lại (giữ `Art/3_Equipment/08_Wing/Wing_1..10`), chỉ reskin UI. Không cần ChatGPT — mọi khung/nút đã có kit.
- Ảnh mới: `Art/1_UIKit/Wing/arrow_left.png` + `arrow_right.png` (art giao, 80x99; trái/phải là 2 ảnh riêng vì bóng đổ cùng phía → không lật scale).
- PageWing: 3 khu nền như mockup (trên/dưới #302C3D = bật Image `Wings/Background` + `Background` chứa nút Mining; giữa #403F51 = nền trang; vạch #18161F), nút X Mastery/btn_close 118 (dời sibling xuống trước ClaimPage để không bị nền khu trên đè), panel info Mastery/mastery_panel rộng 856, nút Equip/Craft/Upgrade/Claim = Mastery/btn_blue, Mining = BattlePass/btn_orange, chữ Asap viền đen (giữ màu).
- Popup UIWingCraftPopup / UIWingRerollPopup: kit Mastery (popup_frame + Header + btn_close, box_tan ô quặng, Cancel = Gameplay/btn_red). Dòng substat `UINew/Gear/SubstatUIElementPrefab` (chỉ Reroll dùng): nền box_tan, colorUnlocked trắng / colorLocked #9C9C9C.
- Code: `UITabWing.SetButtonState(img, isOn, sprOn, sprOff)` — nút bật = Mastery/btn_blue, tắt = Mining/btn_gray (field sprButtonOn/Off trên UITabWing + 2 popup; chưa gán thì tô màu như gốc). Bỏ tô DANGER lên nút Cancel (field imgCancel đã xoá).
- Nút Boss đỏ (TopBarUI/BossButton) đè góc nút X trong ảnh chụp preview — lúc chạy luôn ẩn (Boss Gate tắt), không phải lỗi Wing.

Xem [[dungonrush-reskin-plan]], [[dungonrush-reskin-mining]], [[dungonrush-wing-cape-todo]].

**2026-10-21:** icon wing ĐÃ THAY bằng kính (slot Wing = kính trên mặt hero), xem [[dungonrush-hero-equipment-reskin]] — "giữ icon wing" ở trên chỉ đúng cho đợt reskin UI 2026-10-19.

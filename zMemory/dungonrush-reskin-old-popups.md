---
name: dungonrush-reskin-old-popups
description: "Reskin 2026-10-28 các popup còn art cũ (khung đen Button-White-Active1, nút cam, NotoSans) sang kit Mastery: Combat Summary chỉnh tay + 16 popup bằng PopupKitReskin; đã commit, CHƯA test Play"
metadata:
  node_type: memory
  type: project
  originSessionId: 3ace0cea-6075-4c1a-b327-a5acf9f92c78
  modified: 2026-10-27T18:15:50.093Z
---

Không có mockup — dùng kit Mastery như popup Dungeon (popup_frame, popup_header, btn_close, box_tan, btn_blue; Lobby/btn_green cho nút quảng cáo, btn_red, btn_gray).

**Combat Summary** (`UIOfflineEarningPopup`): đặt tay theo bố cục ×1.21 (khung 822x880 tâm 540,1100), ô /h = box_tan, Collect x2 = Lobby/btn_green. CẤM chạy lại `OfflineEarningPrefabBuilder` (Tools/DungeonRush/Build Offline Earning UI) — ghi đè reskin.

**16 popup tự áp bằng `Scripts/Art/Editor/PopupKitReskin.Apply(path, titleName)`** (giữ bố cục, chỉ đổi hình): UIItemInfoPopup (không header), UIAutoForgePopup, UICloudSavePopup, UIAccountSelectPopup, UILanguageSelectPopup, UISignInPopup, UISignUpPopup, UICapePopup, UICapeDetailPopup (tên + rarity trong header, chỉnh tay), UICapeSalvagePopup, UICapeUpgradeInfoPopup, 5 popup PvP. Quy tắc: khung đen lớn nhất → popup_frame + Header, tiêu đề dời giữa header, nút X → btn_close góc phải, nút theo màu (xanh→btn_blue, đỏ→btn_red, cam→btn_green, đen→box_tan), frame-double đen → box_tan GIỮ alpha (ô bấm trong suốt phủ hàng), chữ Asap viền đen, giữ màu chữ có sắc (link xanh, chữ đỏ).
- Code: Cape đổi SPRITE theo trạng thái thay tô màu (UICapePopup/UICapeSalvagePopup.sprButtonOn/Off, UICapeDetailPopup.sprEquip/sprUnequip, qua UITabWing.SetButtonState). AutoForge hammerOptionDefault = trắng, Highlight = #B8A080 (field prefab).
- Ô con: LanguageItemUI, CapeElementUI → font Asap (fallback Noto có sẵn cho CJK/Thai).
- Ảnh so: scratchpad (không lưu). Bỏ qua có chủ ý: Loading_Frame2 (vòng xoay loading ở popup Clan/PvP/SignIn), thanh rank PvP nền đen, LevelPopup.prefab + "UiLevelPopup .prefab" (rip không dùng — UIKey.LevelPopup = UILevelPopup), font NotoSans trong Slot*.prefab gear lobby.

Xem [[dungonrush-reskin-ui-howto]], [[dungonrush-reskin-plan]], [[dungonrush-offline-earning]].

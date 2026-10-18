---
name: dungonrush-reskin-ui-howto
description: "Quy trình reskin 1 màn UI từ mockup + bộ ảnh art (học từ lobby UIMainGame2 2026-10-18): phân nhóm A/B, pha màu, ChatGPT vẽ ảnh thiếu, gán prefab, các bẫy TMP/Slider/Game view, script ở tools/reskin/"
metadata:
  node_type: memory
  type: project
  originSessionId: 2491829d-dd83-4aeb-a0b5-0e6642497936
  modified: 2026-10-18T09:12:03.949Z
---

Cách reskin 1 màn UI (đã làm trọn lobby UIMainGame2, commit 2fe6b79 + 453853f nhánh `reskin`). Áp lại cho màn khác (Companion, Shop, popup...).

**1. Phân tích trước khi làm**
- Contact sheet bộ ảnh art: `tools/reskin/contact_sheet.py` (tên file art giao kiểu "ChatGPT Image..."/"Rounded Rectangle 1 copy" → phải nhìn mới biết là gì).
- Liệt kê MỌI Image của prefab + sprite + kích thước bằng UnityMCP `PrefabUtility.LoadPrefabContents` (parse YAML không được vì prefab lồng nested instance). Bỏ qua `SafeArea/Pages/`.
- Tra `ArtIncoming/_Reference/art_manifest.csv` cột số chỗ dùng: ảnh dùng chung nhiều màn thì ghi đè = đổi cả game.
- Canvas chuẩn 1080x2160 = đúng cỡ mockup → số đo pixel trên mockup dùng thẳng cho sizeDelta/anchoredPosition. Đo màu nền bằng quét dải màu theo cột trên mockup.

**2. Nhóm A — ghi đè tại chỗ (không sửa prefab):** ảnh mới cùng vai trò ảnh cũ → chép vào `ArtIncoming/<đường dẫn y hệt Art/>` → chạy `ArtReskinTool.Run(false)` (kiểm) rồi `Run(true)` qua reflection (menu Apply có dialog chặn MCP). Icon lệch tỉ lệ ảnh cũ → đệm trong suốt bằng `tools/reskin/pad_to_old_aspect.py` trước khi Apply, không thì méo trong ô cố định.

**3. Nhóm B — cấu trúc khác → ảnh tên mới** ở `Art/1_UIKit/<Màn>/` (btn_green, loot_bg...), import copy TextureImporterSettings từ 1 sprite UI có sẵn, đặt spriteBorder 9-slice theo đo pixel, rồi gán vào prefab bằng script execute_code (LoadPrefabContents → sửa → SaveAsPrefabAsset). Màu nền panel = ảnh White1px tô màu → chỉ đổi Image.color.

**4. Ảnh thiếu**
- Hình hình học (khung rarity, nút cùng dáng khác màu) → PHA MÀU từ ảnh art có sẵn, KHÔNG nhờ ChatGPT (lệch dáng): `recolor_frame.py` (từ khung xám), `fit_button_color.py` (fit ma trận màu từ nút xanh sang màu nút đích đo trên mockup, giữ viền tối).
- Icon minh hoạ (mũi tên Auto, tia sét Boost) → skill chatgpt-image qua Codex (`codex-batch.mjs`), prompt ở `ArtIncoming/_Concept/<Màn>/gen/` (style: flat cartoon sticker, viền đen dày; tail: nền magenta #FF00FF phẳng, không chữ) → tách nền `key_magenta_icon.py` (ngưỡng alpha ≥0.10 mới sạch viền mờ).

**5. Quyết định user đã chốt (lobby)**
- Hàng nút xám/đỏ/xanh dưới mockup = TRẠNG THÁI KHOÁ của nút ngay trên (Auto chưa tới level dùng = xám, Loot đang loot/auto = đỏ, Boost đang chạy = xanh dương) → đổi sprite theo trạng thái trong code (`ForgeButtonView.lockedTopSprite/SetLockedVisual`), không phải nút mới.
- Chữ UI = font `Asap-SemiBold` + material `Asap-SemiBold-Outline-Black` (trắng viền đen, như 2026IdleSava); chữ màu tối đổi trắng.
- Mockup có phần lobby không có (ô cash, hàng 3 nút) → bỏ, giữ bố cục.

**6. Bẫy đã dính**
- Project Linear color space: ảnh đen bán trong suốt (lớp tối art giao, vd 50%) trên UI ra NHẠT hơn Photoshop (50% → chỉ tối ~27%). Bù alpha a' = 1-(1-a)^2.2 khi xuất ảnh (gen_companion_frames.py). Tô màu Image.color (nhân) thì không bị.
- Ảnh nằm trong TMP sprite asset (vd `IsGem.asset`) đổi cỡ → sửa glyphRect/metrics tay; Apply Reskin không tự làm.
- Slider `interactable=false` + transition ColorTint → fill bị tô disabledColor (vàng thành nâu) → đặt transition None.
- Code đè màu/sprite lúc chạy: ForgeButtonView (default/pressed color), ExperienceUI (màu nút Level), ElementEquipmentUILobby (nền rarity) → đọc code trước khi chỉnh prefab.
- Viền chữ TMP kiểu Underlay dilate 1: Game view nhỏ (~400px rộng) hiện Ô XÁM quanh từng chữ — không phải lỗi; xem ở độ phân giải cố định 1080x2160. Chụp kiểm ở 1080: thêm GameViewSize tạm qua reflection rồi gỡ.
- Scene MainGame có override riêng trên instance UIMainGame2 (màu/cỡ chữ user chỉnh) → chặn thay đổi prefab; kiểm `PrefabUtility.GetPropertyModifications`, chỉ revert đúng property cần, KHÔNG lưu scene của user.
- Tách nền magenta: `key_magenta_icon.py` làm màu TÍM (gem, rương mythic) bán trong suốt → dùng `fit_icon_to_old.py` (nền = gần đúng #FF00FF, chỉ khử viền dải 3px; ghi đè icon giữ đúng W×H ảnh cũ lấy từ git HEAD) — học ở Shop [[dungonrush-reskin-shop]].
- Image của prefab RIP giữ `pixelsPerUnitMultiplier` ≠1 (1.3–3.7) → thay sprite 9-slice mới thì viền mỏng/méo; đặt lại 1.
- Chụp kiểm KHÔNG cần Play: `EditorSceneManager.NewPreviewScene` + Camera (cam.scene) + Canvas ScreenSpaceCamera → RenderTexture 1080x2160 (làm ở Rarity Table). Đóng preview có thể sinh `Singleton EventDispatcher` ở DontDestroyOnLoad → DestroyImmediate.
- Chụp kiểm: Play scene Root → tap → đóng UIOfflineEarningPopup + ẩn UIOfferPopup → `manage_camera screenshot` (không supersize — mất UI overlay), ghép cạnh mockup bằng PIL.

- Tool editor (2026-10-18): `Scripts/Art/Editor/UICaptureTool.Capture(prefab, png, setup)` chụp popup 1080x2160 trong preview scene (ép rebuild layout); `ReskinPrefabUtil` Open/Place(x,y,w,h theo pixel mockup)/Img/Header/Close/Fonts/Save — prefab popup có Canvas gốc size 0, scale 0, PIVOT (0,0) → Open tạm đặt 1080x2160 pivot giữa rồi Save trả lại. Gọi qua execute_code (codedom: viết `UnityEngine.Object`, không `Object`).
- Ảnh `manage_camera screenshot` trong Play bị MỜ TRẮNG vùng popup = kênh alpha (lớp tối 0.59) → bỏ alpha (merge RGB) trước khi xem, không phải lỗi UI. Lưu vào `Captures/` (project-relative) rồi xoá.

**Why:** user sẽ reskin tiếp các màn khác theo cùng concept; quy trình này đã được duyệt và chạy được.
**How to apply:** reskin màn mới → làm lần lượt mục 1→4, hỏi user khi mockup có phần màn hiện tại không có. Xem [[dungonrush-reskin-plan]], [[dungonrush-resource-dedupe-rule]], [[dungonrush-unity-mcp-editmode-test]].

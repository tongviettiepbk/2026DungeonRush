---
name: dungonrush-localization
description: Localization theo gốc XONG 2 đợt 2026-10-17 (CHƯA test Play) — 10 ngôn ngữ gốc, key gốc "Popup.X.Y", LocalizeText.key, Loc.L, font fallback Noto, UILanguageSelectPopup; còn sót gì
metadata:
  type: project
---
Commit aeeacae + e0012c0 (2026-10-17), compile 0 lỗi, test edit-mode tiếng Nhật ra đúng chữ; **CHƯA test Play**.

**Gốc (reverse il2cpp):** Unity Localization, 6 bảng (Items/Popup/Clan/UI/Errors/Common), key đầy đủ dạng "Popup.Level.Complete".
Code gọi `eu.L(table,key[,args])` → LocalizationController.ddh/ddi (GetLocalizedString rồi ddj đổi "\\n"→xuống dòng).
Nhãn tĩnh = component `LocalizedLabel` (Table+Key), 263 chỗ. ddt = mặc định theo ngôn ngữ máy; ddf(code) = lưu mã vào Settings,
đóng mọi popup, load lại GameplayScene. Font: TMP Settings fallback toàn cục 10 font Noto Bold. Không có thư viện RTL (Arabic hiện không nối chữ — như gốc).

**Project:**
- `LanguageID` 10 giá trị đúng thứ tự gốc + `LanguageCode.ToCode/TryParse`; JSON `Resources/Json/Localize/Languages/{id}-{Tên}.json` = strings gốc (key → chữ).
- `LocalizeManager`: lưu PlayerPrefs `LocalizeLanguageCode` (mã chuỗi), thiếu bản dịch → tiếng Anh, `ChangeLanguage` = HideAllActiveUI + LoadScene(MainGame). `HasKey`, `GetLocalizeTextFormat`.
- `Loc.L(key[,args])`, `Loc.Or(key, mặcĐịnh)`, `Loc.Rarity(r)` — dùng cho MỌI chữ code gán mới (đừng viết chuỗi tiếng Anh cứng).
- `LocalizeText.key` (+ `sourceText`): dịch lúc Awake/đổi ngôn ngữ, chỉ khi chữ còn là chữ mẫu prefab (không đè chữ code).
- Tools/DungeonRush/Localization: **Build Fallback Fonts** (NotoSansArabic/JP/KR-Bold SDF động), **Apply Original Label Keys** (dò TMP theo tên node + chữ EN với `DecodedData/localization/labels_original.json`, gắn 264 nhãn; chạy lại an toàn — prefab mới dựng thì chạy lại).
- `DecodedData/localization/code_keys.json` = mỗi hàm native gốc dùng key nào → tra khi nối chữ code của 1 popup.
- UILanguageSelectPopup (UIKey.LanguageSelectPopup) + `UINew/Profile/LanguageItemUI`; builder Tools/DungeonRush/Build Profile UI/Language Select Popup + "Profile Popup - Wire Language Only". Cờ GB/TR/SA/FR/DE/IT/JP/KR/ES/BR (thêm TR.png trích xapk).
- Đã thay 310 chuỗi code ở 76 file sang key gốc (script tự động, chỉ dòng hiển thị) + tên món/pet/cape/wing/dungeon/rarity/substat.

**Còn sót:** chữ prefab không có LocalizedLabel gốc mà project tự thêm; "Lv."/"Lv. " (gốc không có key); tên Mastery/Event/mô tả pet (Items.Mastery.*, Items.Companion.Desc.* chưa nối); chuỗi ghép "Tier x Rewards" đang dịch từng mảnh; toast "Chưa có trang bị" (ElementEquipmentUILobby) + "... coming soon" (UITabEvent) còn cứng; UIMainLobby.SlotName tiếng Việt (dead code). Script rewrite ở scratchpad (mất) — muốn chạy lại thì viết lại theo mô tả commit e0012c0.
**Bẫy key sai (sửa 2026-10-21, commit 8554cdb):** đợt gắn LocalizeText từng map nhầm key cho prefab Clan (nút Join/Request mang `Clan.ClanSettingsPopup.Save`, chữ động 48/50/Tier/giá/tên clan mang key rác) → key không có thì hiện NGUYÊN KEY đè chữ code gán. Chữ code tự điền KHÔNG gắn LocalizeText. Kiểm: quét mọi `key:` trong .prefab/.unity so với 10 file Languages/*.json — hiện đã sạch.
Xem [[dungonrush-profile-popup]], [[dungonrush-follow-original-strict]].

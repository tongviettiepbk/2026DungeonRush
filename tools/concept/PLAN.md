# Kế hoạch concept lại toàn bộ popup

Đã xong: `UISevenDayPopup`, `UIDungeonEndPopup` (commit 37505a04).
Còn: 67 popup = 66 key trong `UIKey` (trừ `UIChatTabPage` là trang) + `MateryPopupUpgrade` nằm trong UIMainGame2.
Không làm: trang tab (`StoreTabPage`, `ClanTabPage`, `UIChatTabPage`), `LevelPopup` + `UiLevelPopup ` (nguồn rip, game không mở).

## Quy trình mỗi đợt (bản tối ưu sau Đợt 1)
**A. Khảo sát (1 lượt, trước khi vẽ)**
1. Chụp hiện trạng mọi popup bằng `UICaptureTool` → `tools/concept/<đợt>/before/`.
2. Dump cây prefab (tên, anchor, pos, size, component, sprite, chữ) bằng UnityMCP `execute_code` ra scratchpad — đọc 1 lần thay vì mở từng prefab.
3. Đọc script popup + builder cũ: trường nào nối node nào, chỗ nào **tô màu đè** (đổi sang đổi sprite), chữ nào cứng / có key dịch sẵn.
4. **Đếm số dòng/ô TỐI ĐA từ data thật** (vd gear 1+3 substat, Wing 2+5, Auto Loot 10 rarity + 13 lọc) và tên dài nhất 10 ngôn ngữ → khung cao cố định theo max, rỗng thì chữ mờ.

**B. Concept**
5. `tools/concept/<đợt>/concept.html` link `../kit/kit.css`, `<base>` trỏ `Art/`, vẽ CẢ trường hợp nhiều dòng nhất lẫn ít dòng nhất + mọi trạng thái (khoá, xác nhận, cuộn đáy).
6. Tự soát trước khi gửi: nhãn/tag phải có khoảng hở với ô (không dính), không chữ đè nút X, thanh tiến độ fill thụt đều trong viền.
7. **User chốt** (sửa đến khi chốt). Ghi các chỗ lệch gốc/đề xuất để user quyết.

**C. Áp vào game**
8. Script: đổi trạng thái bằng sprite/ẩn hiện; khối lặp lại thì tách component dùng chung (vd `ItemStatsView`).
9. Tool `Scripts/Art/Editor/Batch<N>Reskin.cs` dùng `PopupStyle` (Shell/Title/CloseButton/Button/Outline/Plain/PlaceTop — toạ độ đo từ MÉP TRÊN panel tới TÂM, = concept × 2). Sửa prefab tại chỗ, chạy lại ra cùng kết quả; builder cũ gọi tool ở cuối.
10. Thoát Play mode trước khi biên dịch/chạy tool (Play đang chạy thì code mới chưa nạp).
11. Chụp kiểm từng trạng thái có dữ liệu mẫu → ghép `tools/concept/<đợt>/unity.png` cạnh concept, console 0 lỗi → commit đợt → user test Play.

**Bẫy đã gặp**
- Vùng cuộn: Mask dùng sprite `UIMask` bo mờ mép → cắt viền đen phần tử sát mép. Dùng mask không sprite + lề trong 8.
- Chụp edit mode: ContentSizeFitter lồng nhau phải `ForceRebuildLayoutImmediate` từng list rồi tới Content; TMP sprite asset gọi `UpdateLookupTables()` trước; `Loc.L` trả key → điền chữ tay.
- Ô chữ 2 cột (nhãn + số): cột số đặt theo `GetPreferredValues` của nhãn lúc chạy (nhãn đổi theo ngôn ngữ).
- Mũi tên đặt theo `textBounds` → neo = pivot của ô chữ.
- CSS: border-image dùng `/ width` + padding, đừng để `position:absolute` tính từ mép trong viền (lệch fill).

## Các đợt (thứ tự theo mức người chơi gặp sớm)

| Đợt | Nhóm | Popup | Số |
|---|---|---|---|
| 0 | Kit chung | Trang concept các thành phần dùng chung: khung, header, nút X, 5 màu nút, ô thưởng, ô chỉ số, tab, thanh tiến độ, dòng danh sách, chữ nâu/chữ viền + helper C# `PopupStyle` cho mọi tool | – |
| 1 | Trang bị & Loot | UIItemInfoPopup, UINewItemPopup, UINewForgePopup, UIAutoForgePopup, UILevelPopup, UIOfflineEarningPopup | 6 |
| 2 | Shop & ưu đãi | UIChestInfoPopup, UIChestRevealPopup, UIChestBundleRevealPopup, UIChestBundleOfferPopup, UIRewardedChestPopup, UIOfferPopup, UIStarterPackPopup, UIMissingGemsPopup, UIBattlePassPopup | 9 |
| 3 | Pet & Mastery | UIPetInfo, uiUpgradePet, UiSumonPet, UICompanionAdSummonPopup, MateryPopupUpgrade | 5 |
| 4 | Enchantment, Wing, Cape | UiEnchantmentInfoItem, UiEnchantmentMerge, UiSumonEnchantment, uiUpgradeEnchantment, UIWingCraftPopup, UIWingRerollPopup, UICapePopup, UICapeDetailPopup, UICapeSalvagePopup, UICapeUpgradeInfoPopup | 10 |
| 5 | Dungeon & Mining | UIDungeonPopup, UIMiningPopup, UIMiningRarityPopup, UIMiningOfferPopup | 4 |
| 6 | PvP & Boss Rush | UIPvPPopup, UIPvPFindOpponentPopup, UIPvPEndPopup, UIPvPRewardsPopup, UIPvPSeasonClaimPopup, UIBossRushJoinPopup, UIBossRushPopup, UIBossRushEndPopup, UIBossRushClaimPopup, UIPlayerPopup | 10 |
| 7 | Clan | UICreateClanPopup, UIClanSettingsPopup, UIClanBannerEditorPopup, UIEditAnnouncementPopup, UIClanRequestsPopup, UIClanInfoPopup, UIClanPlayerPopup, UIAdvancedSearchPopup, UILeadershipRankPopup | 9 |
| 8 | Clan War & Chat | UIClanContributionLeaderboardPopup, UIClanWarDaysAndResultsPopup, UIClanWarLeaderboardPopup, UIClanWarRewardsPopup, UIReportPopup | 5 |
| 9 | Hồ sơ & tài khoản | UIProfilePopup, UIAvatarSelectPopup, UILanguageSelectPopup, UINicknamePopup, UISignInPopup, UISignUpPopup, UIAccountSelectPopup, UICloudSavePopup, UIRateUsPopup | 9 |

## Lưu ý
- Builder cũ CẤM chạy lại (Mining, Clan UI, UIPlayerPopup) — tool reskin sửa prefab tại chỗ, không gọi builder.
- Chữ mới: key đủ 10 ngôn ngữ; chữ dài bật auto size.
- Đổi trạng thái bằng đổi sprite / ẩn hiện trong script, không tô màu đè.

## Tiến độ
**Đang ở:** Đợt 1 xong (áp + commit). **Tiếp theo:** Đợt 2 Shop & ưu đãi — bắt đầu từ bước A.

| Đợt | Concept | Chốt | Áp vào game | Commit |
|---|---|---|---|---|
| 0 | ✅ tools/concept/kit/ (kit.css dùng chung) | ✅ 2026-11-11 | ✅ Scripts/Art/Editor/PopupStyle.cs | ✅ 812200d0 |
| 1 | ✅ tools/concept/batch1/ | ✅ 2026-11-11 | ✅ Batch1Reskin (chụp edit mode: batch1/unity.png); user test Play Auto Loot → sửa viền bị mask cắt; các popup khác CHƯA test Play | ✅ 812200d0 + sửa viền |
| 2 | | | | |
| 3 | | | | |
| 4 | | | | |
| 5 | | | | |
| 6 | | | | |
| 7 | | | | |
| 8 | | | | |
| 9 | | | | |

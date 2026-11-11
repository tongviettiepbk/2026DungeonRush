# Kế hoạch concept lại toàn bộ popup

Đã xong: `UISevenDayPopup`, `UIDungeonEndPopup` (commit 37505a04).
Còn: 67 popup = 66 key trong `UIKey` (trừ `UIChatTabPage` là trang) + `MateryPopupUpgrade` nằm trong UIMainGame2.
Không làm: trang tab (`StoreTabPage`, `ClanTabPage`, `UIChatTabPage`), `LevelPopup` + `UiLevelPopup ` (nguồn rip, game không mở).

## Quy trình mỗi đợt
1. Chụp hiện trạng mọi popup trong đợt bằng `UICaptureTool` → `tools/concept/<đợt>/before/`.
2. Concept HTML bằng sprite thật → `tools/concept/<đợt>/concept.html` + `concept.png`, đủ mọi trạng thái.
3. **User chốt concept** (sửa đến khi chốt).
4. Tool `Scripts/Art/Editor/<Đợt>Reskin.cs` sửa prefab tại chỗ (giữ GUID), chạy lại ra cùng kết quả.
5. Chụp kiểm Unity cạnh concept, console 0 lỗi → commit đợt.

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
| Đợt | Concept | Chốt | Áp vào game | Commit |
|---|---|---|---|---|
| 0 | ✅ tools/concept/kit/ (kit.css dùng chung) | ✅ 2026-11-11 | ✅ Scripts/Art/Editor/PopupStyle.cs | |
| 1 | ✅ tools/concept/batch1/ | ✅ 2026-11-11 | ✅ Batch1Reskin (chụp edit mode: batch1/unity.png), CHƯA test Play | |
| 2 | | | | |
| 3 | | | | |
| 4 | | | | |
| 5 | | | | |
| 6 | | | | |
| 7 | | | | |
| 8 | | | | |
| 9 | | | | |

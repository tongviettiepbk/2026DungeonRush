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
- CSS: border-image `slice` phải < nửa cạnh ảnh (track 32 → 14, fill 24 → 10), bằng nửa thì lõi giữa rỗng → thanh không hiện.
- Chụp concept: Chrome headless phải có `--virtual-time-budget` (đã thêm vào render_concept.sh), không thì chụp trước khi font nạp → mất hết chữ.
- Ảnh mờ mềm (glow FX) nén ASTC 6x6 → vệt khối mờ quanh rìa khi phóng to; ảnh nhỏ thì đặt Uncompressed (đã làm cho 7_FX/Glow_Circle02).
- Popup có bảng ẩn/hiện dòng (Chest Info): VerticalLayoutGroup + ContentSizeFitter trên panel, Background/Header/Title/X gắn LayoutElement.ignoreLayout — bỏ code tính chiều cao tay.

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
**Đang ở:** Đợt 4 xong (áp + chụp kiểm batch4/unity.png + commit). **Tiếp theo:** Đợt 5 Dungeon & Mining — bắt đầu từ bước A. Đợt 2 + 3 + 4 CHƯA test Play.

| Đợt | Concept | Chốt | Áp vào game | Commit |
|---|---|---|---|---|
| 0 | ✅ tools/concept/kit/ (kit.css dùng chung) | ✅ 2026-11-11 | ✅ Scripts/Art/Editor/PopupStyle.cs | ✅ 812200d0 |
| 1 | ✅ tools/concept/batch1/ | ✅ 2026-11-11 | ✅ Batch1Reskin (chụp edit mode: batch1/unity.png); user test Play Auto Loot → sửa viền bị mask cắt; các popup khác CHƯA test Play | ✅ 812200d0 + sửa viền |
| 2 | ✅ tools/concept/batch2/ (khảo sát: before/); user sửa: ô mốc Battle Pass ghi tên màn "Normal 3-5" (StaticCampaignData.GetLevelText, tên Tier dịch Items.Tier.Name.TierN) thay số level | ✅ 2026-11-11 | ✅ Batch2Reskin (chụp edit mode: batch2/unity.png, 14 ảnh); CHƯA test Play | ✅ |
| 3 | ✅ tools/concept/batch3/ (khảo sát: before/; max: mô tả pet DE 126 ký tự = 4 dòng, tên mastery DE 33 ký tự → 2 dòng, summon tới 1055 ô) | ✅ 2026-11-11 (duyệt cả 4 đề xuất: Summon Rates, Unequip, key ads summon, mô tả pet/mastery dùng key dịch) | ✅ Batch3Reskin (chụp edit mode: batch3/unity.png, 7 ảnh); CHƯA test Play | ✅ |
| 4 | ✅ tools/concept/batch4/ (khảo sát: before/; max: Wing 5 substat, Cape 2 substat, relic tier 11, kho cuộn) | ✅ 2026-11-11 (duyệt: header "Relic +N", "Relic Merge", "Summon Rates" tẩu, 10 key mới) | ✅ Batch4Reskin (chụp edit mode: batch4/unity.png, 11 ảnh); CHƯA test Play | ✅ |
| 5 | | | | |
| 6 | | | | |
| 7 | | | | |
| 8 | | | | |
| 9 | | | | |

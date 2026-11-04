---
name: dungonrush-pvp-update-requests
description: "PvP mùa + nhóm league theo game thật: đã code + test Play emulator 2026-11-04, đã commit, đã deploy 2026-11-04; luật + số liệu gốc ở đây, GD ở GDD/PVP_DESIGN.md"
metadata:
  type: project
---

User chơi game thật rồi đưa thông tin update PvP; đã code xong 2026-11-04. Luật hiện hành ở phần CHỐT cuối file và GDD/PVP_DESIGN.md; các đợt 1-6 là số liệu gốc và diễn biến.

**Đợt 1 (2026-11-04) — trophy khi chọn đối thủ.** Ảnh: `ArtIncoming/_Concept/PvP/RealGame_ChooseOpponent_20261104.png`. Người chơi tieuphong, Power 1.25M, Iron League, vé 5/5:

| Đối thủ | Power | Cúp nếu thắng |
|---|---|---|
| bury zenek | 192.66M | +5 |
| Ironbrute | 647.15M | +4 |
| Babacan | 3.42M | +3 |
| kujo | 5.32M | +2 |
| Voiddroid | 115.45M | +1 |

User nhận xét: đối thủ yếu thì được ít cúp hơn. **Ảnh này chưa chứng minh được:** 115.45M chỉ +1 trong khi 3.42M được +3, và 647M thấp hơn 192M. Có thể là theo hạng trong nhóm, chưa xác minh.

**Lệch với code hiện tại:** `winDelta` Elo v41 (`pvpConfig.ts`, `StaticPvPData.GetWinDelta`) ở Iron KWin 40 cho +20 khi cùng trophy, khoảng +12..+28 với đối thủ ±150. Muốn ra +5..+1 bằng Elo thì cả 5 đối thủ phải thấp hơn mình 320-760 trophy — khó tin vì ai cũng mạnh hơn mình và số lại đúng 5,4,3,2,1.

**Giả thuyết chính (chưa reverse thân hàm):** màn này là **SpecialPvP của v59**, không phải PvP v41 project đang bám. Bằng chứng trong `Ver59/AssetRipper/Scripts/Assembly-CSharp/`:
- `SpecialPvPServerConfigModel.ChallengeStars: List<int>` + `SpecialPvPMemberModel.StarsOnWin` — số cúp do SERVER gán từng đối thủ, không tính ở client.
- `SpecialPvPChallengePopup` (Title/Description/TicketsText) + `SpecialPvPOpponentElementUI` (StarRewardText/ChallengeCostText) khớp từng dòng trong ảnh.
- `SpecialPvPMemberModel` có `Stars` + `Rank`; `GroupSize`, `RankRewardTable` theo band hạng, `SpecialPvPPromotionDTO` promote/demote, `SpecialPvPSeasonModel`, `RefillHourUtc`. Tức hệ theo mùa + nhóm league.
- Luật chọn đối thủ và phát `ChallengeStars` nằm ở server gốc nên client không có; chỉ suy ra từ dữ liệu chơi thật.

**Liên quan quyết định cũ:** [[dungonrush-reskin-pvp]] — 2026-10-28 user chốt KHÔNG làm Season/nhóm/Promotion vì coi là ngoài gốc v41. Hệ này chính là của v59. Khi làm phải hỏi user: chỉ đổi cách tính cúp, hay chuyển hẳn sang hệ mùa + nhóm.

**Đợt 2 (2026-11-04) — user trả lời:**
- Thắng: nhận đúng số cúp hiển thị lúc chọn đối thủ. [user xác nhận]
- Thua: có bị trừ cúp; về 0 cúp thì không trừ nữa. [user xác nhận]
- Mỗi lần thua trừ 1 cúp: **USER CHƯA KIỂM TRA, sẽ báo lại — nhớ hỏi trước khi chốt số này.**
- Danh sách đối thủ: ưu tiên hiện đủ các mức +1 đến +5; ít đối thủ phù hợp thì hiện ít dòng hơn (không phải lúc nào cũng đủ 5).
- Hạng của mình và đối thủ: user SUY ĐOÁN là cùng một loại rank, chưa kiểm tra.

**Đợt 3 (2026-11-04) — user trả lời:**
- Lọc đối thủ: trước hết cùng nhóm league, rồi lực chiến gần mình, có cả trên và dưới thì tốt.
- +5 là đối thủ mạnh, +1 là đối thủ yếu.
- Người bị đánh thắng có mất cúp không: user không rõ, nhờ Claude cho ý kiến.
- Ảnh đợt 1 lệch với luật này (647M +4 dưới 192M +5; 115M +1 dưới 3.42M +3; lực chiến 3.42M-647M so với mình 1.25M không gần). Game thật có thể xếp theo Rank/Stars. Project làm theo Power như user nói; đã báo user.

**Ý kiến Claude đã đưa: người bị đánh KHÔNG mất cúp** (`pvp.ts` hiện đang đổi cúp bên phòng thủ, phải bỏ khi làm). Lý do: kết quả trận v59 `SpecialPvPBattleResultResponseDTO` chỉ có `oldStars/newStars/starsGained` của mình, không có sao đối thủ như `opponentOldTrophy/NewTrophy` của v41 (bằng chứng gián tiếp, luật thật ở server gốc); không tìm thấy class nhật ký phòng thủ; người offline mất cúp mà không biết là khó chịu. User chưa chốt.

**Đợt 4 (2026-11-04) — bảng Season Rewards PvP, đọc từ ảnh game thật.** Ảnh: `ArtIncoming/_Concept/PvP/RealGame_SeasonRewards_<League>.png`. Thưởng theo league + hạng; khung hạng 1, 2, 3, 4-10, 11-25, 26-50, 51-100.
- Dãy Gem: mỗi số = số trước × 1.2 làm tròn, bắt đầu 50: `50,60,72,86,103,124,149,179,215,258,310,372,446,535,642,770,924`.
- Vị trí trong dãy = (league − 1) + bậc hạng; league Iron=1 .. Legend=10; bậc hạng: 51-100=0, 26-50=1, 11-25=2, 4-10=3, hạng 3=5, hạng 2=6, hạng 1=7 (không có bậc 4).
- Lootbox = Gem × 4, Bone = Gem × 6, Cloak = Gem × 3. Đã đối chiếu khớp đủ 280 ô của 10 league. Ví dụ Iron hạng 1: 179/716/1074/537; Legend hạng 1: 924/3696/5544/2772.
- Icon: vuông cam = Lootbox, vương miện = Cloak (suy từ ảnh Boss Rush dưới, 265 = 260 + 5).
- Thưởng chốt theo mùa + hạng, nên nhiều khả năng KHÔNG có thưởng từng trận: kết quả trận v59 không có danh sách rewards. Bảng win/lose tạm trong `pvpConfig.ts` có thể bỏ; chưa xác minh.
- Khung hạng tới 100 → nhóm có thể tới 100 người (`GroupSize`), chưa xác minh.
- Boss Rush đã có sẵn sảnh 100 + mùa + chốt thưởng + lên/xuống league (`season.ts`, `rewardConfig.ts`), có thể dùng lại khi làm PvP. KHUNG HẠNG KHÁC: Boss Rush 11-30, 31-50; PvP 11-25, 26-50.

**Mốc Boss Rush thật (không phải PvP):** `RealGame_BossRushResults_Iron_Rank90.png` — Iron hạng 90 nhận 265 Cloak, 780 Lootbox, 780 Bone, 2+2 key, 5 Gem. Khớp GDD/BOSS_RUSH_DESIGN.md mục 9: Placement 5+5, Guaranteed 260/780/780 đúng tỉ lệ 1:3:3. Theo công thức tạm 120+20×boss thì 260 = 7 boss, nhưng ảnh không có số boss nên chưa dùng để xác nhận bước +20.

**Đợt 5 (2026-11-04) — popup đầu = bảng xếp hạng nhóm.** Ảnh: `ArtIncoming/_Concept/PvP/RealGame_LeagueList_Iron.png` (Iron League, "Season ends in: 3d 15h", nút huy chương = Season Rewards, nút Challenge).
- User mô tả: popup đầu hiện 100 người cùng nhóm rank; Iron thấp nhất nên không tụt nữa; người kết quả tốt lên nhóm rank mùa sau; cùng cúp thì xếp theo sức mạnh; dòng của mình luôn nhìn thấy (dòng xanh ghim đáy list, ở đây hạng 65, 0 cúp).
- Trong ảnh: vạch xanh "PROMOTION AREA" nằm giữa hạng 40 và 41, tức Iron lên league nếu top 40/100 (mới thấy ở Iron; league khác chưa có số).
- Xác nhận gần như chắc là `SpecialPvPPopup` v59: có đủ `LeagueNameText, SeasonTimerText, RewardsButton, ZoneSeparatorPrefab, StickyPlayerRow, ChallengeButton`; `SpecialPvPZoneSeparatorUI` có `PromotionColor` + `DemotionColor` nên league trên Iron có thêm vạch xuống hạng (số chưa biết); dòng hiện `StarsText` = cúp.
- Cúp là điểm trong mùa, tách khỏi league: bạn đang 0 cúp, đầu bảng thấy 11 cúp khi còn 3d15h. Reset cúp mỗi mùa là SUY LUẬN, chưa thấy trực tiếp. Điều này gỡ lo ngại mốc league 1000 cúp.
- Đấu trong nhóm: nhóm có Power rất rộng (1.43M đến 1.25B), giải thích vì sao đối thủ ở ảnh đợt 1 không "gần lực chiến". SUY LUẬN.
- **LỆCH cần nhớ — tie-break:** nhóm 11 cúp trong ảnh xếp 1.25B, 19.50M, 80.62M, 4.46M, 16.38M, KHÔNG giảm dần theo Power (39 19.50M đứng trên 40 80.62M; 41 4.46M đứng trên 42 16.38M). Game gốc có thể có tiêu chí khác, ví dụ ai đạt cúp trước. Project làm theo Power như user nói; đã báo user.

**Đợt 6 (2026-11-04) — tài khoản Doubme 9.33T, Platinum; user báo ĐÃ GỬI ĐỦ.** Ảnh: `RealGame_LeagueList_Platinum_{Promotion,Demotion}.png`, `RealGame_SeasonComplete_Gold_Rank22.png`.
- Nhóm league cao ít người hơn 100 vì ít người đạt tới (user nói). User thấy lên/xuống theo % đầu/cuối là hợp lý (suy đoán của user).
- Platinum: PROMOTION AREA giữa hạng 7|8; DEMOTION AREA giữa hạng 23|24; mình hạng 27 (dòng ghim che đáy nên chưa chắc nhóm có 27 người). Nếu đúng 27: lên 26%, xuống 15%. Mốc khác: Iron 40/100 không xuống; Gold hạng 22 vẫn được lên.
- Popup "Season Complete!" khi mở sau mùa: Final rank #22, "Promoted to Platinum League!", 55 cúp, thưởng 124/496/744/372 = đúng bảng GOLD khung 11-25 → thưởng theo league của mùa vừa xong, không phải league mới (khớp đợt 4).
- Tie-break: Platinum hạng 20-27 đều 0 cúp mà Power lộn xộn (228T, 307T, 18T, 4T, 64T, 1.42T, 80T, 9.33T) → gốc chắc chắn KHÔNG xếp theo Power. Thêm bằng chứng cho đợt 5.
- v59 không còn popup PvP cũ (chỉ còn `SpecialPvP*Popup`); `SpecialPvPTicketState` không có trường vé ads, nhưng `RewardedType.PvPTicket = 14` vẫn còn nên chưa rõ.

**CHỐT 2026-11-04 (user trả lời, THAY các ghi chú cũ nếu trái):**
- Thua KHÔNG mất cúp (user đã kiểm tra; ghi chú đợt 2 về trừ cúp là sai).
- 5 vé mỗi ngày, KHÔNG có vé xem ads.
- Xếp hạng: cúp trước, cùng cúp thì sức mạnh.
- Tìm đối thủ theo SỐ CÚP, không theo sức mạnh. Vì vậy "mạnh" để phát +5..+1 hiểu là cúp cao: xếp theo cúp từ +5 xuống (điều này giải thích ảnh đợt 1 Power lộn xộn; Claude suy ra).

**Mặc định Claude đề xuất, user không phản đối:**
- Lịch mùa dùng chung Boss Rush (T3 00:00 → CN 23:59:59 UTC, T2 nghỉ; 5d5h hợp với mùa 6 ngày). League PvP tách riêng league Boss Rush.
- % lên/xuống Iron→Legend: lên 40,35,30,30,25,20,15,10,10,0; xuống 0,5,10,10,15,15,20,20,25,25. Làm tròn lên, chỉnh được bằng config. Chỉ Iron có số gốc chắc.
- Bù bot để nhóm có ít nhất 30 người (để luôn có đối thủ), bot không nhận thưởng, không lên/xuống.
- Người bị đánh không mất cúp. Dưới 5 đối thủ: xếp theo cúp từ +5 xuống. Người mới và data cũ: Iron, 0 cúp. Thưởng cần ≥1 trận trong mùa. Bỏ popup World/Country + bảng thắng/thua.
- Khi code: server lưu `starsOnWin` từng đối thủ vào roster, thưởng đúng số đã hiện; dùng lại `season.ts`, `rewardConfig.ts`.

**Trạng thái 2026-11-04:** server `pvp.ts` + `pvpConfig.ts` + `pvpGroup.ts` (nhóm `pvpGroups/{mùa_L{league}_{n}}`, chốt lười khi thành viên đầu tiên mở PvP sau mùa, bot = hàm thuần giống Boss Rush). Bỏ endpoint initpvpprofile, grantpvpadticket, getpvpleaderboard, seed/remove dummy; thêm claimpvpseason. Client: UIPvPPopup viết lại, UIPvPSeasonClaimPopup + ElementPvPRewardBandUI mới; xoá UIPvPLeaderboardPopup và PvPPrefabBuilder (builder cũ sẽ ghi đè reskin). Số người xuống làm tròn XUỐNG để khớp Platinum 27 người có 4 người xuống.
**Test:** pvpConfigTest, pvpEmulatorTest, test:seed 45/45 PASS. Play emulator: popup đầu, Rewards, chọn đối thủ, thắng +5 và +1, Season Complete lên/xuống + cộng đủ 4 món OK. Chưa nhìn tận mắt vạch DEMOTION đỏ; màn Defeated gọi Show trực tiếp; Thứ Hai chỉ giả state ở client.
**Lúc test thấy:** UILevelPopup (Rarity Table) tự bật đè lên popup PvP khi exp đầy và đổi màn campaign; chưa rõ có đúng ý không, chưa sửa.
**CHƯA:** deploy thật (phải xoá function cũ bằng --force), dòng người chơi bấm xem hồ sơ.

Xem [[dungonrush-pvp-mode]].

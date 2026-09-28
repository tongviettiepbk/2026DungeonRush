---
name: dungonrush-companion-todo
description: "Danh sách việc CÒN THIẾU của hệ Companion/summon pet (rà 2026-09-29) — để xử lý sau, chưa làm"
metadata:
  node_type: memory
  type: project
  originSessionId: f80032e0-65b9-4b91-8645-b397f9208ee5
  modified: 2026-09-28T18:25:58.939Z
---

Rà toàn bộ summon pet ngày 2026-09-29. Phần ĐÃ XONG: popup kết quả `UiSumonPet` (UISumonPet + ElementPetSummon, key `UIKey.SummonPet`), roll rarity khớp native, lượt thứ 53 ép Uncommon, fallback `GetRandom` (lượt 53 đã TEST 2026-09-29: 2000/2000 Uncommon, giữa batch cũng đúng) → xem [[dungonrush-companion-unlock-summon]].

**CÒN THIẾU — xử lý sau (user dặn note lại, chưa làm):**

1. **[LỖI] Nút Ads summon free vô hạn** — `CompanionUI.OnClickSummonAds` gọi thẳng `Summon(11)`: không xem ads, không giới hạn/ngày. Gốc: save `CompanionAdSummonDailyCount` + `LastResetDate` (ảnh game thật hiện "3/3" → 3 lượt/ngày) + `CompanionAdSummonBonusCount` (AdBonus trong eby, `GetAdSummonCount` đang truyền 0). `txtQuantityAds` khai báo nhưng chưa gán. Cần user chọn: tạm giới hạn/ngày trước hay chờ hệ ads.
2. **[LỖI] Tab Pet không khoá theo level** — `UIMainLobby.OnClickTab` mở tab Pet luôn; mốc `COMPANION_UNLOCK_PLAYER_LEVEL = 5` chỉ áp cho 3 ô pet lobby. Gốc khoá cả tab (TabData.UnlockPlayerLevel=5).
3. **Nút Bone lớn cố định x1** — chưa có `ChangeSummonBoneMultiplierButton` (mảng `wke[wjp]`, cost = M×200, base = M×35); `GetBoneBigCost(1)` hardcode. Chưa rõ các bậc M trong `wke` → cần đọc InitializeArray.
4. **Cờ IsNew trong kết quả summon** — gốc edi trả `CompanionSummonResult{Data, IsNew}`; code trả `List<CompanionData>`, prefab `UiSumonPet` chưa có ô New.
5. **Khung/màu rarity** — popup kết quả (ElementPetSummon), UIPetInfo (txtRarity) đều TODO vì chưa có bảng màu rarity gốc.
6. **Event sau mỗi lượt summon** — gốc bắn `co.cvd(CharacterRarity)` (Action<CharacterRarity>, có lẽ quest/analytics); chưa port.

**How to apply:** khi user quay lại hệ pet/summon, đọc list này trước; làm xong mục nào thì xoá mục đó (hết thì xoá file + dòng index).

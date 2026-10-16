---
name: dungonrush-companion-todo
description: "(1,3,4 XONG 2026-10-17, CHƯA test Play) Danh sách việc CÒN THIẾU của hệ Companion/summon pet (rà 2026-09-29) — để xử lý sau, chưa làm"
metadata:
  node_type: memory
  type: project
  originSessionId: f80032e0-65b9-4b91-8645-b397f9208ee5
  modified: 2026-09-28T18:25:58.939Z
---

Rà toàn bộ summon pet ngày 2026-09-29. Phần ĐÃ XONG: popup kết quả `UiSumonPet` (UISumonPet + ElementPetSummon, key `UIKey.SummonPet`), roll rarity khớp native, lượt thứ 53 ép Uncommon, fallback `GetRandom` (lượt 53 đã TEST 2026-09-29: 2000/2000 Uncommon, giữa batch cũng đúng) → xem [[dungonrush-companion-unlock-summon]].

**CÒN THIẾU — xử lý sau (user dặn note lại, chưa làm):**

1. ~~Ads summon vô hạn~~ XONG 2026-10-17: UICompanionAdSummonPopup (3 lượt/ngày UTC, bonus +1 mỗi lần, No Ads nhận luôn), builder Tools/DungeonRush/Build Companion UI.
2. **[LỖI] Tab Pet không khoá theo level** — `UIMainLobby.OnClickTab` mở tab Pet luôn; mốc `COMPANION_UNLOCK_PLAYER_LEVEL = 5` chỉ áp cho 3 ô pet lobby. Gốc khoá cả tab (TabData.UnlockPlayerLevel=5).
3. ~~Multiplier Bone lớn~~ XONG 2026-10-17: wke gốc = [1,2,3,5,10,20,30], nút copy từ rip vào UIMainGame2, index không lưu.
4. ~~IsNew~~ XONG 2026-10-17: CompanionSummonResult{data,isNew}, chữ NEW (copy NewTextObj) trên ô UiSumonPet.
5. **Khung/màu rarity** — popup kết quả (ElementPetSummon), UIPetInfo (txtRarity) đều TODO vì chưa có bảng màu rarity gốc.
6. **Event sau mỗi lượt summon** — gốc bắn `co.cvd(CharacterRarity)` (Action<CharacterRarity>, có lẽ quest/analytics); chưa port.

**How to apply:** khi user quay lại hệ pet/summon, đọc list này trước; làm xong mục nào thì xoá mục đó (hết thì xoá file + dòng index).

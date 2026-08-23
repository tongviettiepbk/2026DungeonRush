---
name: dungonrush-loot-forge-design
description: "Kiến trúc chốt cho hệ thống Loot — tỉ lệ rarity lấy từ ForgeData (100 level), trigger bằng tiêu item quantityLoop, KHÔNG phải giết quái"
metadata: 
  node_type: memory
  type: project
  originSessionId: ab283ab1-bd0b-430e-a508-37718fbf9cc1
  modified: 2026-08-07T04:03:59.678Z
---

User chốt (2026-08-07), 4 điểm về hệ thống Loot:

1. **Tỉ lệ rarity khi loot = ForgeData/StaticForgeData/ForgeController** (đã có sẵn ở `Assets/_Assets/Scripts/Gears/Forge/`, xem [[dungonrush-gears-data-status]]) — bảng 100 dòng (level tiến trình người chơi 0–99) × 10 cột xác suất rarity, `ForgeController.RollRarity(forgeLevel)` đã chạy được.
   - **Việc còn thiếu**: (a) chưa có field lưu "level tiến trình" 0–99 trong playerData (UserCampaignData hiện chỉ có `curStageId`/`passedStageId`, không phải thang 0–99 dùng cho forge); (b) `LootService.RollOne()` hiện KHÔNG gọi ForgeController — nó random đều trên toàn bộ pool asset (mỗi asset có rarity cố định sẵn). Cần đổi luồng: roll rarity trước bằng ForgeController rồi lọc pool gear/weapon theo đúng rarity đó mới random item, thay vì random thẳng cả pool.

2. **Loot KHÔNG trigger khi giết quái.** Trigger là người chơi tiêu 1 lượng "quantityLoop" — một loại item/tiền tệ lưu trong `UserItemData.consumables` (dict theo `ItemType`), dùng lại pattern Gold/Gem/Energy đang có (`Receive/Consume/GetQuantityHave/IsEnough`).
   - **Việc còn thiếu**: enum `ItemType` (`Common/GameEnums.cs`) mới có `NONE/GOLD/GEM/ENERGY`, chưa có giá trị cho "Loop" ticket này — cần thêm.

3. Chưa mô tả — user sẽ nói sau (đừng tự suy đoán phần này).

4. UI/hình ảnh loot (popup reward, animation) làm sau, không phải ưu tiên hiện tại — [[dungonrush-equipment-system]] phần "Cape/stats/UI hoãn" cũng cùng tinh thần này.

**Why:** đây là lần đầu user mô tả rõ cơ chế loot thật của game (khác hẳn giả định "loot khi giết quái" mà assistant tự suy đoán ở lượt trước) — tránh lặp lại nhầm lẫn đó.

**How to apply:** khi code tiếp phần loot — KHÔNG gắn trigger vào enemy-kill/battle-end. Thứ tự hợp lý: (1) thêm ItemType mới cho quantityLoop + field progression level 0–99 trong playerData, (2) nối ForgeController vào LootService.RollOne() (roll rarity trước, lọc pool sau), (3) mới tới UI tiêu thụ quantityLoop để trigger loot. Phần 3 (điểm 3 ở trên) chưa biết nội dung — hỏi lại user trước khi code phần đó.

**Quyết định cụ thể (2026-08-07):**
- Progression level 0–99 (tên field `forgeLevel`) là field RIÊNG trong `UserCampaignData`, tăng ĐỘC LẬP với `curStageId`/`passedStageId` — không suy ra từ stage. Cơ chế tăng forgeLevel cụ thể chưa định nghĩa, chờ user.
- ItemType mới cho item tiêu để loot đặt tên `LOOT_TICKET` (thêm vào enum `ItemType` ở `Common/GameEnums.cs`).

**Đã code xong (2026-08-07), điểm 1+2:**
- `ItemType.LOOT_TICKET = 4` thêm vào GameEnums.cs.
- `UserCampaignData.forgeLevel` (int, clamp 0..99 trong ValidateData) — mới có field, CHƯA có nơi nào tăng giá trị này (chờ user mô tả cơ chế).
- `ForgeController` đổi từ `MonoBehaviour` mồ côi (không gắn GameObject nào) sang `static class` để gọi được từ context static của LootService.
- `LootService.RollOne(int forgeLevel)` — đổi chữ ký (trước là không tham số), giờ roll rarity trước bằng `ForgeController.RollRarity(forgeLevel)` rồi lọc gearPool/weaponPool theo đúng rarity đó mới random item; có fallback lùi dần rarity nếu thiếu asset ở bậc đó.
- `UIMainLobby.OnClickLoot()` — check `UserItemData.IsEnough(LOOT_TICKET, 1)` trước khi roll, `Consume` sau khi roll thành công. Build qua `dotnet build Assembly-CSharp.csproj` sạch, 0 lỗi.
- Điểm 3 (mô tả sau) và điểm 4 (UI/hình ảnh) CHƯA làm, chờ user.

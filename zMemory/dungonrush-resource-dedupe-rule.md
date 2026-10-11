---
name: dungonrush-resource-dedupe-rule
description: Quy tắc ảnh trùng trong _ResourceGame — giữ TÊN GỐC game, bỏ bản đặt theo data (*Icons); trùng viền thì giữ bản có viền 9-slice
metadata:
  type: feedback
  modified: 2026-10-10
---

Khi trong `Assets/_Assets/_ResourceGame/` có 2 ảnh giống hệt điểm ảnh:
- Khác tên → giữ bản mang TÊN GỐC của game (vd `Companion/tier_06_pet_01_icon`, `Items/Necklace/tier_09_necklace_01`, `DungeonDragon/dragon_wing`, `UI/rewarded`), bỏ bản đặt theo tên data (`CompanionIcons/Companion_6_Divine_Clone`, `GearIcons/Neckle_9_1`, `GearIcons/DragonGloveData`, `Currency/ad_rewarded`). SO vẫn trỏ icon sang ảnh tên gốc.
- Trùng tên nhưng viền 9-slice khác → giữ bản CÓ viền (đặt ở thư mục dùng chung `UI/` hoặc `Items/`, không để ở `PetUI/`).

**Why:** User chọn 2026-10-10 khi dọn 43 nhóm trùng. Tool rip prefab ([[dungonrush-ui-prefabs-batch]] `tools/extract_ui_batch.py`) nối sprite theo tên gốc; ảnh Sliced trong prefab rip cần viền như game gốc.

**How to apply:** Trước khi trích/sinh icon mới vào `GearIcons/`, `CompanionIcons/`, `MasteryIcons/`, `WeaponIcons/`, `PetUI/` phải dò xem ảnh đó đã có sẵn dưới tên gốc chưa — có rồi thì trỏ guid sang, KHÔNG tạo bản sao. Dò trùng theo điểm ảnh (PIL trong `tools/.venv`), không chỉ md5.

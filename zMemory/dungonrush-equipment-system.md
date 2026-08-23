---
name: dungonrush-equipment-system
description: "Hệ mặc đồ (equip) Hero — bước 1 xong (function + visual, chưa stat); slot/visual/deferred"
metadata: 
  node_type: memory
  type: project
  originSessionId: a7fc7142-ea25-43e5-afa1-14c786e49441
  modified: 2026-08-06T15:57:17.941Z
---

Tính năng MẶC ĐỒ cho Hero (00Hero). Bước 1 hoàn tất 2026-08-05: chỉ function mặc + hiện hình, CHƯA đụng chỉ số (stats update dần sau). Build `dotnet build Assembly-CSharp.csproj` sạch.

**Đã làm (folder `Assets/_Assets/Scripts/Equipment/`):**
- Slot enum: ĐÃ GỘP `EquipSlot` vào `GearSlotType` (Scripts/Gears/GearEnums.cs) — 2026-08-06, chỉ còn 1 enum cho dễ nhớ. Dùng `GearSlotType.WEAPON/HELMET/GLOVES/BACKPACK/CAPE/WING` (+ RING/NECKLACE/NONE). Int GIỮ nguyên scheme GearSlotType (NONE=0..WING=7, nướng trong .asset + gen_gear_assets.py), `WEAPON=8` nối đuôi. Prefab CanvasMainGame.prefab `typeEquipment` đã remap sang int mới. Save key của UserEquipmentData đổi theo (dev, chưa có save thật).
- `UserEquipmentData.cs` — module save `Dictionary<string,string>` (key=(int)slot, value=id). API `Equip/Unequip/GetEquipped/IsEquipped`. Đã đăng ký 4 bước trong `UserData` (DATA_KEY_EQUIPMENT).
- `EquipVisualResolver.cs` — tra hình body từ DATA GEAR: `GetBodySprite(slot,id)` cho 5 slot sprite (Weapon/Helmet/Gloves/Backpack theo assetName, Wing theo wingId int; fallback `bodySprite ?? icon`); `GetCapeData(id)` trả `CapeData` cho slot Cape (spine).

**Visual: dùng `HeroVisual.cs` (`Scripts/Unit/`), KHÔNG còn HeroEquipmentVisual (đã XOÁ).** HeroVisual gắn SẴN trong prefab 00Hero (cùng GameObject root với HeroUnit), serialize ref các node: handLeft/handRight/weapon/helmet/backPack/wing1/wing2 (SpriteRenderer) + `cape` (SkeletonAnimation, wired vào node Cloak(Short)). API: `RefreshAll()` (đọc save áp cả 6 slot) + `WearEquipment(slot,id)`. `HeroUnit.Awake` gọi RefreshAll; `HeroUnit.RefreshEquipment()` gọi lại sau đổi đồ.

**Định danh id:** Weapon/Helmet/Gloves/Backpack = assetName; Wing = wingId string; Cape = capeId string.

**Sprite:** field `bodySprite` ở GearItemData/WeaponData/WingData (đa số CHƯA gán → tạm dùng icon).

**Cape ĐÃ LÀM (spine, 2026-08-06):** cape = Spine skin, KHÔNG phải sprite. 12 cape chia 3 skeleton theo size: short(tier1-2), mid(tier3-4), long(tier5-6) ở `_Assets/_Spine/cloak_{short,mid,long}`, mỗi skeleton 4 skin `tier_XX_cloak_YY`. Thêm field `skeletonData`(SkeletonDataAsset)+`skinName`(string) vào `CapeData` và ĐÃ ĐIỀN 12 asset (capeId n → tier=ceil(n/2), skin YY=01 nếu lẻ/02 nếu chẵn). `HeroVisual.WearCape`: đổi skeletonData→Initialize(true) khi khác nhóm, cùng nhóm thì SetSkin. LƯU Ý: 3 cloak node prefab có TRANSFORM khác nhau; swap trên 1 node Cloak(Short) → cape mid/long có thể lệch vị trí (chưa xử lý, cần offset/transform per-size nếu muốn khớp).

**Deferred:** áp stats vào combat; UI panel chọn đồ; art bodySprite thật; tách trái/phải glove/wing; căn transform cape mid/long. Liên quan [[dungonrush-gears-data-status]] [[dungonrush-item-stats-source]].

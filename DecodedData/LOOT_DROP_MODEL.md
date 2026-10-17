# Loot Drop model (quái chết rơi gì) — reverse từ libil2cpp.so (APK bản 41)

> Nguồn: disasm ARM64 `LootDropController` trong `libil2cpp.so` (đồ nghề `tools/il2cpp_reverse/`)
> + giá trị serialize của `GameResources` trong `AssetRipper/ExportedProject/Assets/_Game/_Scenes/StarterScene.unity`.
> Đây là REFERENCE bất biến, không sửa theo refactor code.

## Kết luận
- Game gốc **KHÔNG rơi Bone từ quái** trong campaign (cũng như mọi mode) — nhánh `combat_drop` Bone là code chết. **[GỐC TẮT]**
- Quái chết trong game thật **chỉ rơi mảnh EXP** (LootType 2). **[GỐC]**
- Rương (LootType 1) chỉ rơi khi màn có `CreativeDropSettings` (màn "creative" dựng bằng `GridLevelBuilder` để quay quảng cáo) — build thật không có data này → **không rơi rương**. **[GỐC TẮT]**
- → Port: KHÔNG làm Bone/rương rơi từ quái (quy tắc "gốc tắt thì không làm"). Phần mảnh EXP đã port ở `Scripts/GamePlay/LootDropController.cs`. **[ĐÃ PORT]**

## LootType
| Giá trị | Tên | Analytics khi thu |
|---|---|---|
| 0 | Bone | `bone` / `combat_drop` |
| 1 | LootBox | `loot_box` / `combat_drop` |
| 2 | ExperienceShard | — (cộng exp) |

## Hàm đã map (obfuscate → ý nghĩa)
| Hàm | VA | Ý nghĩa |
|---|---|---|
| `LootDropController.hsi(GameController)` | 0x279FCE0 | Init: lấy GameResources/UserController/AudioController, đăng ký event (trong đó `GridLevelBuilder.DropSettingsReady` → `hsj`). |
| `LootDropController.hsj(CreativeDropSettings)` | 0x279FF90 | Gán `xgz = settings`. Chỉ được gọi qua `GridLevelBuilder.DropSettingsReady`. |
| `LootDropController.hsk(Character)` | 0x27A0A38 | Character chết → `enabled = (xgz == null) ? true : xgz.EnableDrops` → `hsm(pos, enabled)`; nếu enabled → `hsl(pos)`. |
| `LootDropController.hsm(pos, enabled)` | 0x27A0B28 | Rơi **mảnh EXP**: tỉ lệ `xgz?.XPDropChance ?? GameResources.ExperienceShardDropChance`, số mảnh `Random.Range(Min, Max+1)`, chia đều exp `ExperienceController.hdn` (xem EXP_MODEL.md) → `hsn(..., type 2, amount)`. |
| `LootDropController.hsl(pos)` | 0x27A0E8C | Rơi **rương**: `if (xgz != null && xgz.EnableLootBoxDrop && Random.value <= xgz.LootBoxDropChance)` → `hsn(LootBoxDropPrefab, pos, type 1, LootBoxPerDrop)`. `xgz == null` → không rơi. |
| `LootDropController.hsn(prefab, pos, type, amount)` | 0x27A0F14 | Spawn LootDrop (LeanPool) + `LootDrop.gvd(type, amount)`. `prefab == null` → return. **Nơi DUY NHẤT gọi `LootDrop.gvd`.** |
| `LootDropController.hsy(LootDrop)` / `qi.hsh()` | 0x27A2ACC / 0x27A3050 | Thu loot: type 0 → Bone += Amount; type 1 → LootBox += Amount; type 2 → exp. |

## Bằng chứng Bone không rơi
- Caller của `hsn`: chỉ `hsm` (type 2, 2 chỗ) và `hsl` (type 1). **Không có chỗ nào truyền type 0.** **[ĐÃ QUÉT]**
- Caller của `LootDrop.gvd` (gán Type): chỉ `hsn`. Các hàm scatter/collect (`hsq`, `hsr`, `hst`, `hta`, `htb`, `hsy`) đều xuất phát từ `hsn`. **[ĐÃ QUÉT]**
- `GameResources.BoneDropPrefab = {fileID: 0}` (null) trong build. **[DATA]**
- `EnableBoneUI: 0`, `BoneUIPrefab: {fileID: 0}`. **[DATA]**
- Các field `BoneDropChance / BoneDropCountMin / BoneDropCountMax / BonePerDrop / BonePerMinute` có giá trị nhưng không hàm gameplay nào đọc. **[CODE CHẾT]**
- Không asset nào trong build chứa `CreativeDropSettings` (`EnableLootBoxDrop` / `ClearOnStart` không xuất hiện ngoài file .cs). **[DATA]**

## Giá trị GameResources (StarterScene)
| Field | Giá trị | Dùng? |
|---|---|---|
| ExperienceShardDropChance | 1 | có |
| ExperienceShardDropCountMin | 1 | có |
| ExperienceShardDropCountMax | 3 | có |
| LootBoxDropChance | 0.1 | không (chỉ creative dùng `xgz.LootBoxDropChance`) |
| LootBoxPerDrop | 1 | chỉ khi creative |
| BoneDropChance | 0.25 | không |
| BoneDropCountMin | 3 | không |
| BoneDropCountMax | 6 | không |
| BonePerDrop | 1 | không |
| BonePerMinute | 60 | không |
| DungeonBoneRewardBase | 100 | có (Zombie dungeon) |
| DungeonBoneRewardScaler | 2 | có (Zombie dungeon) |

## Mọi chỗ gốc cộng/trừ Bone (caller của `UserController.dpv` = set Bone)
| Hàm | Analytics | Chiều |
|---|---|---|
| `DungeonController.hce` / `DungeonPopup.kcp` | `dungeon_reward` | + (Zombie Outbreak: Base 100 + Scaler 2) |
| `BattlePassPopup.jvd` | reward | + |
| `ClanWarController.gao` | reward | + |
| `PurchaseController.dhk` / `ty.jtb` | `iap_purchase` (gói/deal) | + |
| `CompanionTabPage.gtk` | `companion_summon` | − (summon pet) |
| `ForgeUpgradePopup.igt` | `forge_upgrade` | − |
| `LootDropController.hsy` / `qi.hsh` | `combat_drop` | + nhưng **không bao giờ chạy** (xem trên) |
| `MobileTestPopup.kng/koa` | — | cheat test |

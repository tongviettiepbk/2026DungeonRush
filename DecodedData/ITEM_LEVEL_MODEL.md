# ITEM LEVEL MODEL — công thức Level của gear khi Forge/Summon (reverse từ game gốc)

Reverse từ `libil2cpp.so` (APK bản 41) + giá trị serialized của `GameResources`.
Trả lời câu hỏi: **"Level của gear khi summon ra được tính thế nào?"**

> Kết luận cốt lõi: **Level KHÔNG cố định = 1**. Đây là hệ **forge/nâng cấp theo món ĐANG MẶC**:
> mỗi lần forge chọn 1 slot ngẫu nhiên, so rarity roll được với rarity món đang mặc ở slot đó,
> rồi cộng dồn 1 offset ngẫu nhiên có trọng số vào level cũ.

## Enum liên quan

- `ItemType` (slot forge random 0..5): 0..5 = 6 loại trang bị forge được (vũ khí + 5 gear).
- `CharacterRarity`: 0 Common, 1 Uncommon, 2 Rare, 3 Epic, 4 Legendary, 5 Mythic,
  6 Artifact, 7 Ancient, 8 Immortal, 9 Divine, 10 Ultimate.

## Hằng số (GameResources)

| Field | Giá trị |
|---|---|
| `ForgeMinLevel` | 1 |
| `ForgeMaxLevel` | 100 |
| `ForgeLowerRarityMinLevel` | 90 |

## Bảng offset level (`ForgeRarityLevelOffsets` + `ForgeDefaultLevelOffsets`)

Mỗi entry `{Offset, Weight}`; chọn Offset theo random có trọng số (weighted pick trên tổng Weight).

Bảng riêng theo rarity (chỉ Common/Uncommon/Rare có bảng riêng):

| Rarity | Offset:Weight |
|---|---|
| 0 Common   | 3:10, 6:30, 9:40, 12:15, 15:5 |
| 1 Uncommon | 2:10, 4:30, 6:40, 8:15, 10:5 |
| 2 Rare     | 1:10, 2:30, 3:40, 4:15, 5:5 |

Rarity ≥ 3 (Epic trở lên) → **fallback `ForgeDefaultLevelOffsets`**:

| Default | Offset:Weight |
|---|---|
| (mọi rarity ≥3) | -2:5, -1:10, 0:15, 1:20, 2:25, 3:15, 4:10 |

## Luồng tính (map hàm gốc)

Entry: `ForgeController.hel()` → tail-call `qv.iap(...)`:

```
slot        = Random.Range(0, 6)                 // 1 ItemType ngẫu nhiên
cur         = UserController.dww(slot)            // món đang mặc ở slot (EquippedItemModel)
isEmpty     = cur.ItemId < 0                      // EquippedItemModel.iao() = ItemId>>31
base        = isEmpty ? 1        : cur.Level      // <-- base level
inRarity    = isEmpty ? Common(0): cur.Rarity
rolledRarity= qv.iau(...)                         // roll rarity theo ForgeRarityProbabilities
                                                  //   + EconomyController.hcq (theo tiến trình người chơi)
newLevel    = qv.iaq(base, inRarity, rolledRarity, isEmpty)
```

`qv.iaq(base, inRarity, rolledRarity, isEmpty)` — 4 nhánh:

```
if isEmpty:                       Level = ForgeMinLevel (= 1)
elif rolledRarity > inRarity:     Level = ForgeMinLevel (= 1)          // lên rarity cao hơn → reset về 1
elif rolledRarity == inRarity:    Level = clamp(base + weightedOffset(rolledRarity),
                                                ForgeMinLevel, cap)    // cùng rarity → cộng dồn offset
else (rolledRarity < inRarity):   Level = Random.Range(ForgeLowerRarityMinLevel, cap+1)  // tụt rarity → 90..cap
```

- `weightedOffset(rarity)` = `qv.ias(rarity)` → weighted-random Offset từ `qv.iat(rarity)`
  (bảng theo rarity ở trên, fallback ForgeDefault).
- `cap` = `qv.iar()` = `ForgeMaxLevel + round(MasteryController.iuh(10))`
  = **100 + bonus mastery** (base player chưa mastery ⇒ cap = 100).

Level sau đó lưu vào `PendingForgeDecision.Level` (ForgeController.hfi) rồi vào
`EquippedItemModel.Level` khi mặc.

## Đối chiếu save thực (playerprefs.xml, PlayerLevel=6, HighestLevel=49)

EquippedItems: đa số `Level 1` (vừa lên rarity/mới), 1 món Rare `Level 1`,
1 món Uncommon `Level 9` (cộng dồn qua nhiều lần forge cùng rarity: 1 + offset...). Khớp mô hình.

## Ghi chú port sang rebuild (LootService)

- `LootService.LOOT_LEVEL = 1` hiện là hardcode → thay bằng logic trên nếu muốn đúng game gốc.
- Rebuild hiện là "loot random 1 món" (không feed món đang mặc). Muốn đúng gốc phải:
  đọc Level+Rarity món ĐANG MẶC ở slot đích (UserEquipmentData) làm base/inRarity,
  roll rarity, rồi áp 4 nhánh + bảng offset ở trên.

## Đồ nghề

Script reverse (scratchpad phiên làm): `read_forge_offsets.py` (đọc GameResources đầy đủ),
`disasm_fn.py` (disasm ARM64). Giá trị đã merge vào `DecodedData/gameresources_values.json`.
Hàm gốc: `qv.iap/iaq/ias/iat/iau/iav`, `ForgeController.hel/hfi`, `EquippedItemModel.iao`.

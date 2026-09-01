---
name: dungonrush-gear-icon-extract
description: "Cách trích icon gear thiếu từ xapk — item SO nằm trong sharedassets0 (split), typetree strip nên parse raw MB lấy Icon PPtr"
metadata: 
  node_type: memory
  type: reference
  modified: 2026-09-01T04:30:31.224Z
  originSessionId: 4f72cd6e-7c63-4229-9f86-d9a65eb6dace
---

Khi gear (Gloves/Weapons/Armor…) trong project bị **thiếu icon** (`icon: {fileID: 0}`): nguyên nhân gốc là texture icon chưa import (guid mồ côi vì AssetRipper export không có .meta — xem [[assetripper-export-no-meta]]). Decoded CSV (vd `DecodedData/tables/GlovesData.csv`) CÓ ref icon nhưng guid đó không tồn tại trong project. Pipeline gán icon hiện tại chỉ khớp theo TÊN FILE `_ResourceGame/GearIcons/<assetName>.png`; SO nào không có PNG cùng tên → mất icon.

**Cách lấy icon ĐÚNG (đã chạy 2026-09-01 cho 11 Gloves enemy):**
1. Item ScriptableObject (CultistGloveData_Melee…) **KHÔNG ở `bin/Data`** mà ở `sharedassets0.assets` — file này bị chia `.split00..split80` (81 phần). UnityPy `UnityPy.load(<thư mục bin/Data đã bung>)` tự ghép split.
2. **Typetree bị strip** (chỉ còn base `m_GameObject/m_Enabled/m_Script/m_Name`), nên `read_typetree()` không đọc được field custom. Phải **parse raw bytes** của MonoBehaviour (`obj.get_raw_data()`), theo layout ItemData: PPtr m_GameObject(12B) → m_Enabled(1B+align4) → PPtr m_Script(12B) → m_Name(string, align4) → ItemId(int) → ItemName(str) → LocalizationKey(str) → Rarity(int) → **Icon PPtr(int fileID+long pathID)** → GlovesSprite PPtr. PPtr fileID=0 nghĩa cùng file; pathID = object đích.
3. Resolve PPtr → Sprite (thường trong atlas `sactx-…ASTC`), `sprite.image.save(png)`. Rồi tạo .meta (textureType:8 spriteMode:1, ref bằng `fileID: 21300000` + guid mới, spriteID=guid[:24]+"00000000") clone từ `GearIcons/LychGloveData.png.meta`, và sửa dòng `icon:` trong .asset.

**Mapping icon enemy gloves (Icon==GlovesSprite):** Cultist_Melee/Ranged→`cultist_dungeon_enemy_0X_hand`; Zombie→`zombie_hand`; Witch & Witch2→cùng `witch_01_hand`; Ogre1/2→`ogre_0X_punch`; Dragon/Green/Purple/Red→`dragon[_color]_wing` (rồng không có tay, slot glove = cánh); Lych1/2/3→`lych_0X_glove`.

**Đã làm tiếp 2026-09-01 (Helmet/Necklace/Ring/Weapon):** layout field TRƯỚC Icon giống hệt mọi ItemData con (ItemId/ItemName/LocalizationKey/Rarity/Icon) → parser dùng chung. Đã trích 26: **14 Helmet** enemy (Icon = sprite đầu/thân quái: `cultist_dungeon_enemy_0X`, `dragon_without_wings`, `dragon_green/purple/red`, `lych_0X`, `ogre_0X`, `witch_0X`, `zombie_head`), **6 Necklace** (`tier_09/10_necklace_0X`) + **6 Ring** (`tier_09/10_ring_0X`) — necklace/ring thiếu là item TIER CAO chứ không phải enemy. Helmet/Necklace/Ring icon vào `GearIcons/`; Weapon icon vào `WeaponIcons/`.
⚠️ **11 enemy Weapon (Dragon*/Lych*/Ogre*/Witch*/GreenDragon…) có `Icon: {fileID: 0}` NGAY TRONG DATA GAME GỐC** (cả export lẫn bundle) → game vốn không định nghĩa icon, KHÔNG có gì để trích, đừng bịa. Cultist/Zombie weapon thì có sẵn icon. Nếu cần placeholder có thể map tay sang texture vũ khí quái (`lych_0X_weapon`, `ogre_weapon`, `witch_0X_weapon`, `cultist_dungeon_melee/range_weapon`, `dragon_*_fire_ball`) nhưng phải hỏi user.
**Lưu ý match tên:** khi tìm MB theo raw bytes, "DragonHelmetData" là substring của "GreenDragon…" → phải parse m_Name rồi so KHỚP CHÍNH XÁC, không dùng `in`.

Script ở scratchpad (tạm): `extract_final.py` + `import_into_project.py`. UnityPy 1.25.2 cài vào Windows Store Python 3.12 (`pip install --only-binary :all: --prefer-binary UnityPy`). Xem [[dungonrush-reverse-native-il2cpp]], [[dungonrush-item-stats-source]], [[dungeonrush-config-format]].

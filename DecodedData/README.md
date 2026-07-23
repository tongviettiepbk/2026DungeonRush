# Dungeon Rush — Decoded Config Data

Bảng data cân bằng (balance/config) của game **Dungeon Rush** (`com.lavalabs.dungeonrush`),
trích từ bản decompile bằng AssetRipper.

## Quan trọng: game này KHÔNG dùng format binary "2def"

Skill `decode-unity-apk-config` nhắm tới format binary tự chế (magic number + fixed/var-row,
kèm HybridCLR). **Dungeon Rush không dùng format đó.** Đây là bước 0 của skill ("nhận diện có
đáng làm không"): game này serialize toàn bộ config bằng **Unity ScriptableObject chuẩn**, nên
AssetRipper đã export thẳng ra `.asset` dạng YAML đọc được. Không có gì phải reverse binary.

Vì vậy công việc thật ở đây là: gom ~350 file `.asset` rải rác thành **bảng data gọn, có nhãn,
enum đã dịch nghĩa**.

## Nguồn dữ liệu

- `AssetRipper/ExportedProject/Assets/MonoBehaviour/*.asset` (378 file)
- `AssetRipper/ExportedProject/Assets/Resources/**/*.asset` (67 file)
- Schema (tên class, tên field, kiểu, enum) đọc từ source C# đã decompile ở
  `Assets/Scripts/Assembly-CSharp/*.cs`.

## Cách decode (đã tự động hoá trong `extract.py`)

1. Parse toàn bộ `.cs` → lấy `class → {field: type}` và toàn bộ `enum → {int: label}`.
2. Mỗi `.asset` gom theo `m_Script` guid; nhận diện class bằng khớp field-set với source.
   (Ring/Necklace giống hệt base `ItemData` nên phân biệt bằng guid.)
3. Field kiểu enum → thêm cột `<Field>__enum` với nhãn chữ (vd `Rarity: 9 → Divine`,
   `WeaponType: 1 → Range`).
4. Mảng primitive Unity serialize thành chuỗi hex little-endian → decode lại thành list số
   (vd `UnlockGemCosts` → `[30,60,90,...]`). GUID tham chiếu asset giữ nguyên dạng `ref:<guid>`.

Chạy lại: `python extract.py` (cần `pyyaml`).

## Định dạng output

- `tables/<Class>.json` — bản đầy đủ, giữ nguyên struct lồng (list, sub-object, màu, ref...).
- `tables/<Class>.csv` — chỉ các cột scalar (số/chuỗi/ref), tiện mở Excel. Field lồng phức tạp
  chỉ có trong JSON.
- `_index.md` — danh mục toàn bộ bảng + số dòng.

## Bổ sung (nguồn ngoài ScriptableObject)

Chạy bằng `extract2_localization.py` (cần `UnityPy`):

- `localization/strings_<lang>.json` — toàn bộ text game theo key, 10 ngôn ngữ (en, ar, fr, de,
  ja, ko, pt-br, es, tr, it). `strings_all_languages.csv` gộp tất cả.
- Item tables đã được **gắn thêm cột `Name_en` / `Desc_en`** (join qua `LocalizationKey`).
  Nguồn: Unity Localization StringTables trong Addressables bundle `assets/aa/` (AssetRipper
  không export phần này; đọc bằng UnityPy).
- `tables/forge_rarity_probabilities.csv` — bảng xác suất forge lên rarity (100 dòng × 10 cột,
  mỗi dòng tổng 100%). Trích từ default Firebase Remote Config nhúng trong
  `RemoteConfigController.cs` (`forge_rarity_probabilities`).
- `remote_config_keys.json` — danh sách 14 key Firebase Remote Config game đọc (forge odds,
  experience curve, army power, weapon offer, boss gate...). Giá trị số của các key này (trừ forge)
  do server trả runtime, không có sẵn local.

## Remote Config LIVE từ server (lấy qua app-cache, KHÔNG cần MITM)

Chạy game trên LDPlayer 14 (rooted). Game tải Firebase Remote Config về và cache tại
`/data/data/com.lavalabs.dungeonrush/files/frc_*_firebase_activate.json`. Pull thẳng bằng
root adb (chỉ đọc file, không chặn traffic, không cert). Bản pull ở `appcache/drdump/`,
parse bằng `parse_remote_config.py`:

- `remote_config_live.json` — **giá trị server thật, config_version 17.0.0**. Trước đây tôi nói
  các số này "chỉ có ở server" — giờ đã lấy được.
- `tables/experience_required_per_level.csv` — XP cần mỗi level (100 level).
- `tables/army_power_segments.csv` + scalar `army_power_base=500`, `army_power_exponential_scaler=3.16227766`,
  `army_power_level_scaler=20` — công thức tính Army Power.
- `tables/weapon_offer_config.csv` — army level → weapon offer (2→Squire Crossbow, 10→Arcane Dagger,
  20→Frost Bow, 50→Void Scythe, 100→Holy Greatsword).
- `tables/forge_rarity_probabilities_live.csv` — forge odds live (trùng khớp bản default trong code).
- Scalar khác: `starting_loot_box_count=240`, `chest_store_enabled=true`, `boss_gate_enabled=false`,
  `experience_level_base/mult/scaler`, `ab_test_group=control`.

## Chỉ còn lại: player save-state (không phải balance data)

- **Player state** (tiền, inventory, tiến độ người chơi): server-side (Firebase Functions/Firestore),
  KHÔNG cache local dạng đọc được — PlayerPrefs chỉ có session + user id. Đây là save của TÀI KHOẢN,
  không phải balance data chung. Muốn xem phải MITM API lúc login (cần cert-pinning bypass) — nhưng
  nó không bổ sung gì cho bộ cân bằng game.
- **Stat quái theo wave / PvP matchmaking / reward event**: nếu có, do Cloud Functions trả runtime.
  Chưa thấy cache local. Cần MITM nếu muốn.
- **Công thức runtime** (damage scaling, drop logic) nằm trong `libil2cpp.so` (native) — method body
  trong bản decompile chỉ là stub. Muốn đọc phải Ghidra + global-metadata; ROI thấp.

## Ghi chú đọc dữ liệu

- `ref:<guid>` = tham chiếu tới asset khác (sprite, prefab, animator, audio...). Không phải giá trị.
- Chỉ số sức mạnh của Ring/Necklace **không** nằm trong data (chỉ có ItemId/Rarity/tên) — chúng
  được sinh theo rarity/substat lúc runtime hoặc từ server.
- Game này **nặng server-driven** (rất nhiều class `*DTO`/`*RequestDTO`/`*ResponseDTO`): stat quái
  theo wave, matchmaking PvP, phần thưởng event... phần lớn do server trả về, KHÔNG có trong APK.
  Những gì local hoá được đều đã nằm trong các bảng ở đây.

# Reskin DungeonRush — Kế hoạch & chia thư mục

Cập nhật: 2026-10-18 (đợt 0 xong). Concept mới: hero vịt, UI phẳng kiểu cartoon (mockup ở `ArtIncoming/_Concept/Lobby/lobby demo.png`, `ArtIncoming/_Concept/Companion/companion demo.png`).
Brief đồ hero đã có riêng ở `GDD/ART_HERO_EQUIPMENT.md`.

| Ký hiệu | Nghĩa |
|---|---|
| **[ĐO]** | Số đo từ project hiện tại |
| **[CHỐT]** | Đã thống nhất |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt |
| **[CHỜ BẠN]** | Bạn cần quyết |

---

## 1. Hiện trạng ảnh trong project (trước đợt 0)

### 1.1. Con số

| Hạng mục | Số ảnh |
|---|---|
| Ảnh trong `_Assets/` | 1477 |
| Ảnh game ĐANG DÙNG lúc chạy | 706 |
| Ảnh không dùng | 771 |
| Ảnh không dùng nằm trong `_ResourceGame/_CommingUse` | 656 |

- Đếm "đang dùng" = lần theo GUID từ 3 scene build (Root, Login, MainGame) + mọi file trong `Resources/`. **[ĐO]**
- Code KHÔNG nạp ảnh theo đường dẫn: mọi ảnh đều gắn qua prefab/ScriptableObject bằng GUID. **[ĐO]**
- Vì vậy chuyển thư mục (giữ `.meta`) hay thay nội dung PNG đều không phải sửa code. **[ĐO]**
- Không có Sprite Atlas, không có ảnh dạng sheet (Multiple) trong `_ResourceGame`. **[ĐO]**
- Animation unit/boss chủ yếu là animation xoay/dời transform → chỉ cần thay ảnh, không phải vẽ lại từng frame. **[ĐO]**
- Ngoại lệ vẽ theo frame: Zombie (idle, attack) và cung `Weapon_BasicBow` (idle, attack). **[ĐO]**

### 1.2. Ảnh đang dùng, gom theo mảng việc của art

| Mảng | Thư mục hiện tại | Ảnh |
|---|---|---|
| UI kit (khung, nút, thanh, nền tier) | `UI`, `PetUI`, `_NonDefile`, `BgRarity`, `Graphics/Ingame` | 121 |
| Icon (tiền, rương, avatar, mastery, ...) | `Currency`, `Chest`, `Avatar`, `Flags`, `MasteryIcons`, `Mastery`, `Enchantment`, `CompanionIcons`, `Forge`, `Loot`, `PvP`, `BossRush`, `Misc` | 79 |
| Ảnh riêng màn Clan | `Clan` | 43 |
| Đồ hero | `HeroEquipment`, `GearIcons`, `WeaponIcons`, `Items`, `Weapon`, `Wing` | 298 |
| Nhân vật (hero, quái, pet) | `Character`, `Enemies`, `Companion`, `_Spine/cloak_*` | 52 |
| Bản đồ | `MainMap`, `DungeonZombie`, `DungeonDragon`, `DungeonCultist`, `Mining` | 59 |
| FX | `Fx` | 23 |

- Ngoài ra 19 ảnh đang dùng lại nằm lẫn trong `_CommingUse` (10 cờ quốc gia, 8 ảnh Mining, 1 diamond). **[ĐO]**
- `Root.unity` đang trỏ tới `_ExportTemp/unit_base_all.prefab` (bản rip để thử) → cần gỡ hoặc chuyển. **[ĐO]**
- `00Hero.prefab` đang dùng 7 ảnh trong `_ArtReskin/` (thân vịt + 6 món L01-A). **[ĐO]**
- `_QuackBag/` đang dùng: `unit_all.controller` + 19 animation + ảnh bóng `unit_shadow_0`. **[ĐO]**

---

## 2. Nguyên tắc kỹ thuật

### 2.1. "Thay ruột, giữ vỏ" **[CHỐT]**

- Reskin chỉ thay hình, giữ nguyên bố cục UI hiện tại. **[CHỐT]**
- Ảnh mới được ghi ĐÈ lên file PNG cũ, giữ nguyên tên file và file `.meta`. **[CHỐT]**
- GUID không đổi → mọi prefab, SO, scene tự hiện ảnh mới, không phải gán lại tay. **[CHỐT]**
- Ảnh mới to/nhỏ hơn ảnh cũ: Pixels Per Unit và viền 9-slice tự nhân theo tỉ lệ → kích thước hiển thị như cũ. **[CHỐT]**
- Chỉ đặt tên mới + gán lại khi concept đổi cấu trúc (vd hero ghép từng bộ phận, đồ hiện trên người). **[ĐỀ XUẤT]**

### 2.2. Thư mục "gương" cho art giao hàng **[CHỐT]**

- Art giao vào `ArtIncoming/` (gốc repo) có cây thư mục và tên file Y HỆT `Art/`; thư mục gương đã dựng sẵn. **[CHỐT]**
- Unity → `Tools/DungeonRush/Reskin/1. Kiểm tra ArtIncoming (không ghi)` → xem trước. **[CHỐT]**
- Unity → `Tools/DungeonRush/Reskin/2. Apply Reskin` → ghi đè, giữ `.meta`. **[CHỐT]**
- Báo cáo ở `ArtIncoming/_Reference/reskin_report.txt`: file lệch tỉ lệ, 9-slice cần xem lại, file sai tên/không có bản cũ. **[CHỐT]**
- File đã thay cộng dồn vào `ArtIncoming/_Reference/reskin_log.csv`. **[CHỐT]**
- Thư mục/file bắt đầu bằng `_` hoặc `.` bị bỏ qua (`_Concept`, `_Reference`). **[CHỐT]**
- Chạy lại với file y hệt thì bỏ qua, không nhân PPU chồng. **[CHỐT]**
- Ảnh gốc vẫn còn trong git (tag `pre-reskin`) để so sánh hoặc quay lại. **[CHỐT]**

### 2.3. Bảng theo dõi **[CHỐT]**

- `Tools/DungeonRush/Reskin/3. Xuất bảng ảnh (CSV) + thư mục gương` → `ArtIncoming/_Reference/art_manifest.csv`. **[CHỐT]**
- Mỗi dòng 1 ảnh trong `Art/`: kích thước, PPU, 9-slice + viền, đang dùng không, số chỗ dùng, trạng thái cũ / đã thay. **[CHỐT]**
- Contact sheet từng thư mục: `tools/.venv/bin/python tools/art_contact_sheets.py` → `ArtIncoming/_Reference/sheets/`. **[CHỐT]**
- Hướng dẫn ngắn cho đội art: `ArtIncoming/README.md`. **[CHỐT]**

---

## 3. Cấu trúc thư mục **[CHỐT]**

### 3.1. Cây thư mục

```
Assets/_Assets/
└── Art/                          ← thay _ResourceGame + Graphics, CHỈ chứa ảnh game dùng
    ├── 1_UIKit/
    │   ├── Common/               khung, nút, thanh, tab, ribbon, icon hệ thống
    │   ├── PetUI/                khung riêng trang pet (trùng tên ảnh Common, viền khác)
    │   ├── Rarity/               nền ô theo tier
    │   └── HUD/                  thanh máu
    ├── 2_Icons/
    │   ├── Avatar/  Chest/  Currency/  Enchantment/  Loot/  Mastery/
    │   ├── Companion/            (+ _Icons1-5)
    │   └── Flags/                (+ Language/ = 10 cờ chọn ngôn ngữ)
    ├── 3_Equipment/
    │   ├── 00_EmptySlot … 08_Wing    icon 241 món + ô trống
    │   ├── OnBody/               ảnh gắn lên hero (6 món L01-A)
    │   ├── InGame/               vũ khí cầm tay, mũi tên
    │   ├── Projectiles/          đạn theo tier
    │   └── GearIcons/  WeaponIcons/  Items/  Wing/
    ├── 4_Characters/
    │   ├── Hero/                 vịt (Hero.png) + soldier cũ
    │   ├── Enemies/
    │   └── Pets/
    ├── 5_Maps/
    │   ├── Campaign/             (+ Materials/ lưới, Shaders/ GridShader)
    │   └── DungeonZombie/  DungeonDragon/  DungeonCultist/  Mining/
    ├── 6_Features/
    │   └── BossRush/  Clan/  Forge/  Misc/  PvP/
    └── 7_FX/

ArtIncoming/                      ← ngoài Assets: art giao hàng, cây Y HỆT Art/
    ├── _Concept/                 mockup + art mới chưa gắn (cũ là _ArtReskin)
    └── _Reference/               contact sheet, CSV, báo cáo
ArtArchive/                       ← ngoài Assets: ảnh không ai dùng (_CommingUse, HeroEquipment.zip)
```

- Tầng 1 chia theo MẢNG VIỆC của art, tầng 2 theo tính năng. **[CHỐT]**
- Mỗi thư mục tầng 1 = 1 đợt giao hàng (mục 4), số thứ tự đầu tên = thứ tự làm. **[CHỐT]**
- `ArtIncoming/` và `ArtArchive/` ngoài `Assets` → Unity không import ảnh dở dang / ảnh thừa. **[CHỐT]**

### 3.2. Đối chiếu cũ → mới

| Thư mục cũ | Thư mục mới |
|---|---|
| `_ResourceGame/UI` | `Art/1_UIKit/Common` |
| `_ResourceGame/_NonDefile/Square, White1px` | `Art/1_UIKit/Common` |
| `_ResourceGame/PetUI` | `Art/1_UIKit/PetUI` |
| `_ResourceGame/BgRarity` | `Art/1_UIKit/Rarity` |
| `Graphics/Ingame` | `Art/1_UIKit/HUD` |
| `_ResourceGame/Currency` + `_CommingUse/Currency/diamond_icon` | `Art/2_Icons/Currency` |
| `_ResourceGame/Chest` | `Art/2_Icons/Chest` |
| `_ResourceGame/Avatar` | `Art/2_Icons/Avatar` |
| `_ResourceGame/Flags` | `Art/2_Icons/Flags` |
| 10 cờ trong `_CommingUse/Flags` | `Art/2_Icons/Flags/Language` |
| `_ResourceGame/MasteryIcons` + `Mastery` | `Art/2_Icons/Mastery` |
| `_ResourceGame/Enchantment` | `Art/2_Icons/Enchantment` |
| `_ResourceGame/CompanionIcons` | `Art/2_Icons/Companion` |
| `_ResourceGame/_NonDefile/_Icons1-5` | `Art/2_Icons/Companion/_Icons1-5` |
| `_ResourceGame/Loot` | `Art/2_Icons/Loot` |
| `_ResourceGame/HeroEquipment/*` | `Art/3_Equipment/*` |
| `_ArtReskin/Items/*/…-L01-A` (6 ảnh hero đang mặc) | `Art/3_Equipment/OnBody` |
| `_ResourceGame/Weapon` | `Art/3_Equipment/InGame` |
| `_ResourceGame/_NonDefile/_Common_Uncommon…` (mũi tên) | `Art/3_Equipment/Projectiles` |
| `GearIcons`, `WeaponIcons`, `Items`, `Wing` | `Art/3_Equipment/<cùng tên>` |
| `_ResourceGame/Character` + `_ArtReskin/DemoHero/Hero.png` | `Art/4_Characters/Hero` |
| `_ResourceGame/Enemies` | `Art/4_Characters/Enemies` |
| `_ResourceGame/Companion` | `Art/4_Characters/Pets` |
| `_ResourceGame/MainMap` | `Art/5_Maps/Campaign` |
| `_ResourceGame/MapMaterials`, `Shaders` | `Art/5_Maps/Campaign/Materials`, `Shaders` |
| `_ResourceGame/Dungeon*` | `Art/5_Maps/Dungeon*` |
| `_ResourceGame/Mining` + 8 ảnh `_CommingUse/Mining` | `Art/5_Maps/Mining` |
| `_ResourceGame/Clan`, `BossRush`, `PvP`, `Forge`, `Misc` | `Art/6_Features/<cùng tên>` |
| `_ResourceGame/Fx` | `Art/7_FX` |
| `_ResourceGame/_CommingUse` (656 ảnh không dùng) + `HeroEquipment.zip` | `ArtArchive/` |
| 3 ảnh `Items/Gloves, Helmet, Necklace` không dùng | `ArtArchive/Items` |
| `_ArtReskin/*` còn lại (mockup, 54 món art mới) | `ArtIncoming/_Concept` |

- `_Spine`, `_QuackBag` (animator/anim hero), `Fonts` giữ nguyên chỗ. **[CHỐT]**
- Chuyển bằng `AssetDatabase.MoveAsset` (giữ GUID); trước/sau đều 706 ảnh đang dùng, 0 tham chiếu gãy mới. **[ĐO]**

### 3.3. Đã sửa theo khi đổi đường dẫn

- 12 builder Editor (`*PrefabBuilder.cs`) + comment `RarityBackgroundConfig.cs`: hằng `_ResourceGame/...` → `Art/...`; 48 đường dẫn cứng đều trỏ tới file có thật. **[ĐO]**
- 8 tool trong `tools/` → `Art/`. **[ĐO]**
- Tool rip prefab (`extract_ui_batch.py`, `join_levelpopup_sprites.js`) giờ chỉ dò tên sprite trong `Art/`; ảnh đã lưu trữ ở `ArtArchive/` không còn được nối. **[ĐO]**
- Code chạy game: không có chỗ nào phải sửa. **[ĐO]**
- Sau khi reskin, KHÔNG chạy lại builder prefab cũ: builder dựng lại prefab từ đầu sẽ xoá chỉnh tay. **[CHỐT]**

---

## 4. Các đợt làm

### 4.1. Bảng tổng

| Đợt | Nội dung | Ai làm | Ảnh |
|---|---|---|---|
| 0 | Dọn + chia thư mục + bảng theo dõi | Dev | 0 |
| 1 | Style guide: màu, font, nét viền, bo góc | Art | 0 |
| 2 | UI kit + font | Art + Dev | 121 |
| 3 | Icon | Art | 79 |
| 4 | Đồ hero | Art | 298 |
| 5 | Nhân vật: hero, quái, boss, pet | Art + Dev | 52 |
| 6 | Bản đồ | Art + Dev | 59 |
| 7 | FX, màn Clan, icon app, splash, ảnh store | Art | 66 |

- UI kit làm TRƯỚC: chỉ 121 ảnh nhưng gần như popup nào cũng dùng → đổi xong là cả game đổi màu. **[ĐỀ XUẤT]**
- Đồ hero là đợt lớn nhất; đã xong 60/241 món (mẫu A, 6 loại × 10 tier). **[ĐO]**

### 4.2. Đợt 0 — Dọn (dev) — XONG 2026-10-18

1. ✅ Commit việc đang dở trên `main` (c9eab7a), gắn tag `pre-reskin`, làm tiếp trên nhánh `reskin`.
2. ✅ Chuyển 19 ảnh đang dùng ra khỏi `_CommingUse`.
3. ✅ 656 + 3 ảnh không dùng → `ArtArchive/`; 100 ảnh mockup/art mới chưa gắn → `ArtIncoming/_Concept/`.
4. ✅ Xoá object `unit_base_all` (đang tắt) khỏi `Root.unity`.
5. ✅ Đổi `_ResourceGame` + `Graphics` → `Art/` theo bảng 3.2, sửa hằng builder + tool.
6. ✅ `Scripts/Art/Editor/ArtReskinTool.cs` + `tools/art_contact_sheets.py`; đã thử ảnh 2×, ảnh 9-slice, ảnh lệch tỉ lệ, ảnh sai tên.
7. ✅ 706/706 ảnh đang dùng, 0 GUID gãy mới, Play Root → Login → MainGame 0 lỗi.

### 4.3. Đợt 2 — UI kit + font

- Đổi font: tạo TMP Font Asset từ font mới, đặt làm font chính, giữ Noto làm fallback cho 10 ngôn ngữ. **[ĐỀ XUẤT]**
- 12 material viền chữ (`*-Outline-*.mat`) dựng lại cho font mới. **[ĐO]**
- Ảnh 9-slice (18 ảnh): Apply Reskin tự nhân viền theo tỉ lệ; viền vẽ khác hẳn thì art gửi số px 4 cạnh, dev sửa tay. **[ĐỀ XUẤT]**

### 4.4. Đợt 5 — Nhân vật

- Hero vịt đã gắn được vào `00Hero` (thân + 6 món L01-A). **[ĐO]**
- Quái/boss/pet: thay ảnh thân giữ pivot cũ → animation transform chạy lại được ngay. **[ĐỀ XUẤT]**
- Zombie và cung cần vẽ đủ frame như bản cũ. **[ĐO]**
- 10 prefab boss Boss Rush + 16 pet cần art riêng từng con. **[ĐO]**

### 4.5. Đợt 6 — Bản đồ

- Nền campaign là lưới vẽ bằng shader (không phải ảnh) → reskin = chỉnh màu material Inner/Outer. **[ĐO]**
- Tường, đồ trang trí 3 dungeon + Mining là ảnh thường → thay ruột. **[ĐO]**

---

## 5. Việc bạn cần chuẩn bị

### 5.1. Trước khi giao art

- [ ] Style guide 1 trang: bảng màu, độ dày nét viền, bo góc, bóng đổ.
- [ ] Font mới + giấy phép dùng thương mại; font phải có đủ dấu tiếng Việt nếu bật ngôn ngữ VN.
- [ ] Quy cách giao file: PNG nền trong suốt, ĐÚNG tên file cũ, ĐÚNG thư mục gương.
- [ ] Độ phân giải: giữ bằng ảnh cũ hay gấp đôi (gấp đôi thì Apply Reskin tự chỉnh Pixels Per Unit).
- [ ] Gửi đội art: `ArtIncoming/` (README + `_Reference/sheets` + `art_manifest.csv`).

### 5.2. Ngoài ảnh trong game

- [ ] Icon app + splash + màn loading.
- [ ] Ảnh store (screenshot, feature graphic).
- [ ] Tên game / logo mới (nếu đổi).

---

## 6. Rủi ro

- Ảnh mới khác tỉ lệ ảnh cũ → bị méo trong UI dùng kích thước cố định. Báo cáo `Apply Reskin` liệt kê để chỉnh. **[ĐỀ XUẤT]**
- Ảnh 9-slice thiếu số viền → khung bị kéo giãn xấu. **[ĐỀ XUẤT]**
- Chạy lại builder prefab cũ sau khi chỉnh tay → mất chỉnh tay. **[ĐỀ XUẤT]**
- Đổi thư mục khi đang có thay đổi chưa commit → khó lần lại. Làm đợt 0 trên nhánh sạch. **[ĐỀ XUẤT]**

---

## 7. Chưa chốt

### 7.1. Chờ bạn quyết

- Có đổi âm thanh (`_Sound`, 57 file) trong đợt này không? **[CHỜ BẠN]**
- Có đổi tên game / bundle id không? **[CHỜ BẠN]**
- Nhẫn, dây chuyền, vũ khí tầm xa, Cape 6 tier vs tẩu 10 tier (xem `ART_HERO_EQUIPMENT.md`). **[CHỜ BẠN]**

### 7.2. Tình trạng

- Đã chốt: chỉ thay hình, giữ bố cục UI; cấu trúc `Art/` + `ArtIncoming/` + `ArtArchive/`.
- Đã làm: đợt 0 (mục 4.2) trên nhánh `reskin`; brief đồ hero; 60 món đồ mới + thân vịt gắn vào `00Hero`.
- Chưa làm: đợt 1 trở đi (chờ art).

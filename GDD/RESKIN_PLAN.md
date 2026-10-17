# Reskin DungeonRush — Kế hoạch & chia thư mục

Cập nhật: 2026-10-18. Concept mới: hero vịt, UI phẳng kiểu cartoon (mockup ở `_ArtReskin/Lobby/lobby demo.png`, `_ArtReskin/Companion/companion demo.png`).
Brief đồ hero đã có riêng ở `GDD/ART_HERO_EQUIPMENT.md`.

| Ký hiệu | Nghĩa |
|---|---|
| **[ĐO]** | Số đo từ project hiện tại |
| **[CHỐT]** | Đã thống nhất |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt |
| **[CHỜ BẠN]** | Bạn cần quyết |

---

## 1. Hiện trạng ảnh trong project

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

### 2.1. "Thay ruột, giữ vỏ" **[ĐỀ XUẤT]**

- Ảnh mới được ghi ĐÈ lên file PNG cũ, giữ nguyên tên file và file `.meta`. **[ĐỀ XUẤT]**
- GUID không đổi → mọi prefab, SO, scene tự hiện ảnh mới, không phải gán lại tay. **[ĐỀ XUẤT]**
- Chỉ chỉnh lại trong `.meta` khi cần: viền 9-slice, pivot, Pixels Per Unit. **[ĐỀ XUẤT]**
- Chỉ đặt tên mới + gán lại khi concept đổi cấu trúc (vd hero ghép từng bộ phận, đồ hiện trên người). **[ĐỀ XUẤT]**

### 2.2. Thư mục "gương" cho art giao hàng **[ĐỀ XUẤT]**

- Art giao vào thư mục `ArtIncoming/` có cây thư mục và tên file Y HỆT `Art/`. **[ĐỀ XUẤT]**
- Một lệnh Editor `Tools/DungeonRush/Apply Reskin` chép từng file sang `Art/` cùng đường dẫn, giữ `.meta`. **[ĐỀ XUẤT]**
- Lệnh in báo cáo: file đã thay, file lệch tỉ lệ so với ảnh cũ, file có 9-slice cần chỉnh viền, file không có bản cũ để thay. **[ĐỀ XUẤT]**
- Ảnh gốc vẫn còn trong git (tag `pre-reskin`) để so sánh hoặc quay lại. **[ĐỀ XUẤT]**

### 2.3. Bảng theo dõi **[ĐỀ XUẤT]**

- Xuất 1 file CSV: mỗi dòng 1 ảnh đang dùng — đường dẫn, kích thước, có 9-slice không, số prefab dùng, trạng thái (cũ / đã thay). **[ĐỀ XUẤT]**
- Xuất ảnh xem trước (contact sheet) cho từng thư mục, giống `GDD/img/hero_equipment/`. **[ĐỀ XUẤT]**
- Art nhìn contact sheet để biết vẽ gì; dev nhìn CSV để biết còn thiếu gì. **[ĐỀ XUẤT]**

---

## 3. Cấu trúc thư mục mới **[ĐỀ XUẤT]**

### 3.1. Cây thư mục

```
Assets/_Assets/
├── Art/                      ← đổi tên từ _ResourceGame, CHỈ chứa ảnh đang dùng
│   ├── 1_UIKit/
│   │   ├── Common/           khung, nút, thanh, tab, ribbon, icon hệ thống
│   │   ├── Rarity/           nền ô theo tier
│   │   └── HUD/              thanh máu, chữ sát thương
│   ├── 2_Icons/
│   │   ├── Currency/  Chest/  Avatar/  Flags/  Mastery/
│   │   ├── Enchantment/  Companion/  Loot/  Misc/
│   ├── 3_Equipment/          giữ nguyên 00_EmptySlot … 08_Wing + InGame/ (vũ khí cầm tay)
│   ├── 4_Characters/
│   │   ├── Hero/  Enemies/  Bosses/  Pets/
│   ├── 5_Maps/
│   │   ├── Campaign/  DungeonZombie/  DungeonDragon/  DungeonCultist/  Mining/  BossRush/
│   ├── 6_Features/           ảnh chỉ 1 màn dùng: Clan/  PvP/  Forge/ ...
│   └── 7_FX/
└── ...

ArtIncoming/                  ← ngoài Assets (gốc repo): art giao hàng, cây Y HỆT Art/
    └── _Concept/             mockup, demo, style guide
ArtArchive/                   ← ngoài Assets: ~670 ảnh không dùng (chủ yếu _CommingUse)
```

- Tầng 1 chia theo MẢNG VIỆC của art (UI kit, icon, đồ, nhân vật, map, FX), tầng 2 mới theo tính năng. **[ĐỀ XUẤT]**
- Lý do: mỗi thư mục tầng 1 = 1 đợt giao hàng, 1 contact sheet, 1 người vẽ; không phải lục 40 thư mục tính năng. **[ĐỀ XUẤT]**
- Số thứ tự đầu tên = thứ tự làm (mục 4). **[ĐỀ XUẤT]**
- `ArtIncoming/` và `ArtArchive/` để ngoài `Assets` → Unity không import ảnh dở dang, project nhẹ hơn. **[ĐỀ XUẤT]**

### 3.2. Đối chiếu cũ → mới

| Thư mục cũ | Thư mục mới |
|---|---|
| `_ResourceGame/UI` | `Art/1_UIKit/Common` |
| `_ResourceGame/PetUI` | `Art/1_UIKit/Common` |
| `_ResourceGame/_NonDefile` | `Art/1_UIKit/Common` |
| `_ResourceGame/BgRarity` | `Art/1_UIKit/Rarity` |
| `Graphics/Ingame` | `Art/1_UIKit/HUD` |
| `_ResourceGame/Currency` | `Art/2_Icons/Currency` |
| `_ResourceGame/Chest` | `Art/2_Icons/Chest` |
| `_ResourceGame/Avatar` | `Art/2_Icons/Avatar` |
| `_ResourceGame/Flags` + 10 cờ trong `_CommingUse/Flags` | `Art/2_Icons/Flags` |
| `_ResourceGame/MasteryIcons` + `Mastery` | `Art/2_Icons/Mastery` |
| `_ResourceGame/Enchantment` | `Art/2_Icons/Enchantment` |
| `_ResourceGame/CompanionIcons` | `Art/2_Icons/Companion` |
| `_ResourceGame/HeroEquipment` | `Art/3_Equipment` |
| `GearIcons`, `WeaponIcons`, `Items`, `Weapon`, `Wing` | `Art/3_Equipment` (dò trùng trước) |
| `_ArtReskin/Items` + `DemoHero/Hero.png` | `Art/3_Equipment` + `Art/4_Characters/Hero` |
| `_ResourceGame/Character` | `Art/4_Characters/Hero` |
| `_ResourceGame/Enemies` | `Art/4_Characters/Enemies` |
| `_ResourceGame/Companion` | `Art/4_Characters/Pets` |
| `_ResourceGame/MainMap` | `Art/5_Maps/Campaign` |
| `_ResourceGame/Dungeon*` | `Art/5_Maps/Dungeon*` |
| `_ResourceGame/Mining` + 8 ảnh `_CommingUse/Mining` | `Art/5_Maps/Mining` |
| `_ResourceGame/Clan` | `Art/6_Features/Clan` |
| `_ResourceGame/Fx` | `Art/7_FX` |
| `_ResourceGame/_CommingUse` (phần không dùng) | `ArtArchive/` |
| `_ArtReskin/Lobby`, `Companion`, ảnh demo | `ArtIncoming/_Concept` |

- Thư mục không phải ảnh (`MapMaterials`, `Shaders`, `_Spine`, `_QuackBag` animator) giữ nguyên chỗ, chỉ ảnh mới chuyển. **[ĐỀ XUẤT]**
- Chuyển bằng `AssetDatabase.MoveAsset` qua UnityMCP (giữ GUID), kiểm lại bằng đếm ảnh đang dùng trước/sau phải bằng nhau. **[ĐỀ XUẤT]**

### 3.3. Thứ phải sửa theo khi đổi đường dẫn

- 12 builder Editor (`*PrefabBuilder.cs`) có hằng đường dẫn `_ResourceGame/...` → sửa hằng. **[ĐO]**
- 8 tool Python/JS trong `tools/` trỏ `_ResourceGame` → sửa đường dẫn. **[ĐO]**
- Code chạy game: không có chỗ nào phải sửa. **[ĐO]**
- Sau khi reskin, KHÔNG chạy lại builder prefab cũ: builder dựng lại prefab từ đầu sẽ xoá chỉnh tay. **[ĐỀ XUẤT]**

---

## 4. Các đợt làm **[ĐỀ XUẤT]**

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

### 4.2. Đợt 0 — Dọn (dev, làm trước khi art giao)

1. Commit các thay đổi đang dở trên `main`, tạo nhánh `reskin`, gắn tag `pre-reskin`.
2. Chuyển 19 ảnh đang dùng ra khỏi `_CommingUse`.
3. Chuyển ~670 ảnh không dùng ra `ArtArchive/`, ~100 ảnh art mới chưa gắn trong `_ArtReskin` ra `ArtIncoming/` (cả hai ngoài Assets).
4. Gỡ tham chiếu `Root.unity` → `_ExportTemp/unit_base_all.prefab`.
5. Đổi `_ResourceGame` → `Art/` theo bảng 3.2, sửa hằng builder + tool.
6. Viết lệnh `Apply Reskin` + xuất CSV theo dõi + contact sheet từng thư mục.
7. Kiểm: số ảnh đang dùng trước/sau bằng nhau, Play 3 scene không lỗi missing sprite.

### 4.3. Đợt 2 — UI kit + font

- Đổi font: tạo TMP Font Asset từ font mới, đặt làm font chính, giữ Noto làm fallback cho 10 ngôn ngữ. **[ĐỀ XUẤT]**
- 12 material viền chữ (`*-Outline-*.mat`) dựng lại cho font mới. **[ĐO]**
- Ảnh 9-slice: art gửi kèm số px viền 4 cạnh, dev nhập vào `.meta`. **[ĐỀ XUẤT]**

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
- [ ] Độ phân giải: giữ bằng ảnh cũ hay gấp đôi (gấp đôi thì dev chỉnh Pixels Per Unit).
- [ ] Mockup các màn chính ngoài Lobby và Companion: Store, Dungeon, Event, Clan, popup chi tiết đồ.

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

- Reskin chỉ thay hình, hay đổi cả bố cục UI? Mockup lobby có 5 tab dưới và 8 ô đồ ngay ở lobby, khác bố cục hiện tại (7 trang). **[CHỜ BẠN]**
- Đồng ý cấu trúc `Art/` + `ArtIncoming/` + `ArtArchive/` ở mục 3? **[CHỜ BẠN]**
- Có đổi âm thanh (`_Sound`, 57 file) trong đợt này không? **[CHỜ BẠN]**
- Có đổi tên game / bundle id không? **[CHỜ BẠN]**
- Nhẫn, dây chuyền, vũ khí tầm xa, Cape 6 tier vs tẩu 10 tier (xem `ART_HERO_EQUIPMENT.md`). **[CHỜ BẠN]**

### 7.2. Tình trạng

- Đã làm: khảo sát, đếm ảnh đang dùng, brief đồ hero, 60 món đồ mới + thân vịt gắn vào `00Hero`.
- Chưa làm: toàn bộ đợt 0 (dọn + chia thư mục + lệnh Apply Reskin).

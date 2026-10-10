# STORE (Shop) — mô hình gốc (reverse v41) + cách làm lại

Nguồn: `libil2cpp.so` + `global-metadata.dat` (xapk v41) qua Il2CppDumper + capstone; prefab rip
`AssetRipper/ExportedProject/Assets/GameObject/{StoreTabPage,Chest*,DailyDealRewardEntryUI}.prefab` (còn nguyên field
wiring → `tools/rip_fieldmap.py` → `Scripts/Shop/Editor/shop_fieldmap.json`); ScriptableObject `ChestData/ChestBundleData/ChestConfig`
đọc bằng UnityPy (`tables/ChestCatalog.names.json` — kèm TÊN sprite); `localization/strings_en.json`.
Ký hiệu: **[CHẮC]** = đọc thẳng từ code/data; **[SUY]** = không có trong APK hoặc chưa đọc, tự chọn.

---

## 1. Trang Store (`StoreTabPage`) [CHẮC]

Thứ tự mục đang bật trong prefab: Lifetime Boost → Chests → Daily Deals (3 ô) → Boosters → Resources → Gems (6 gói).
Mục TẮT sẵn trong prefab (không hiện ở v41): NoAds, StarterPack, EpicPack, DungeonDeal, DealsRibbon, ResourceDeal.

| Mục | Hàm gốc | Logic |
|---|---|---|
| Hiển thị | `ktw` | `NoAdsParent` luôn ẩn; `OffersRibbonParent` = !IsNoAdsBought; `LifetimeBoostParent` = !IsLifetimeBoostBought; mục rương `Refresh(gate)` với gate = `GameResources.ChestStoreEnabled (=1)` && đã mở Cape (`UserController.dtc`: `IsCapeFeatureEarlyUnlocked` hoặc PlayerLevel ≥ `CapeUnlockPlayerLevel` = 15) |
| Giá | `ktv` | giá store của từng product, chưa có → "N/A" |
| Drill | `ktx` | gem ≥ 45 → −45 gem, Drill +3 (analytics `store_drill`) |
| Gold Pickaxe | `kty` | gem ≥ 45 → −45 gem, GoldenPickaxe +3 |
| Boost "x1.5 Exp" | `kua`/`kuc` | đang chạy thì bỏ qua; gem ≥ 45 → −45, `X2ExpBoostEndTime = now + 1200s` (nhãn 20m) |
| Boost "x2 Exp" | `kub`/`kud` | đang chạy thì bỏ qua; gem ≥ 75 → −75, `X5ExpBoostEndTime = now + 300s` (nhãn 5m) |
| Boost UI | `kuf` | đang chạy → ẩn nút mua, hiện thời gian còn lại |
| Thiếu gem | | toast `Errors.NotEnoughGems` "Not enough gems!" |

Hằng số `zbo..zbt` = 45, 45, 45, 75, 1200f, 300f. Tên field là X2/X5 nhưng hệ số THẬT (dưới) là ×1.5 / ×2.

### Hệ số exp (`ExperienceController.hdq`) [CHẮC]
`mult = (IsLifetimeBoostBought ? 2 : 1) × (boost quảng cáo đang bật ? MasteryController.iuh(AdBoostWorth) hoặc 2 : 1)
× (X2 đang chạy ? 1.5 : 1) × (X5 đang chạy ? 2 : 1)`; `mult > 1` → exp = round(exp × mult). "Boosters stack multiplicatively".
(Boost quảng cáo — nút Boost ở lobby — chưa có trong project.)

---

## 2. Rương (`ChestData`, `UserController.eet/eeu/eev/eex/eey/eew`) [CHẮC]

| Rương | Gem | Món/lần | Rarity đỉnh | Pity | Tỉ lệ (trọng số) | Điều kiện bán |
|---|---:|---:|---|---:|---|---|
| Standard | 40 | 2 | Rare | 0 | C 87 / U 9 / R 4 | — |
| Epic | 80 | 6 | Epic | 20 | C 75 / U 20 / R 4 / E 1 | — |
| Legendary | 120 | 6 | Legendary | 20 | C 62.5 / U 30 / R 5 / E 1 / L 0.5 | — |
| Mythic ("Mythic Vault") | 200 | 6 | Mythic | 20 | C 45.92 / U 40 / R 10 / E 3 / L 0.75 / M 0.33 | đã sưu tầm ĐỦ rarity Epic |

- **Roll rarity** (`ChestData.flo`): random có trọng số (trọng số âm = 0); tổng ≤ 0 → Rarity của rương.
- **1 lần mở** (`eex`): roll `max(1, ItemCount)` rarity. Nếu KHÔNG món nào trúng rarity đỉnh, `PityCount ≥ 1` và `pity + 1 ≥ PityCount`
  → ép món CUỐI thành rarity đỉnh (cờ `hq.vev`). Sau đó: có món rarity đỉnh → pity = 0, không → pity + 1. Pity lưu theo RARITY
  (`User.ChestPityCounters` = list `{Rarity, Count}`), không theo rương.
- **Chọn món** (`eey`): gom pet (`GameResources.jho`) + mẫu cape (`jhp`) của rarity đó. Còn mẫu CHƯA sở hữu → random đều trong các mẫu
  chưa sở hữu (pet + cape gộp chung); hết → random đều trong tất cả. Pet: chưa có → thêm `CompanionModel` mới, có rồi → `CardCount + 1`.
  Cape: luôn tạo `CapeModel` mới (substat roll `jht`). Không có pet/cape nào của rarity → log lỗi, lấy mọi rarity.
- **Đủ rarity** (`eew`): sở hữu mọi pet VÀ mọi mẫu cape của rarity đó.
- **Mua** (`eet`): gem < GemCost → không làm gì; ngược lại −gem (analytics `chest_purchase`) rồi mở.
- **Kết quả** `hq` = {ChestData, kind Companion/Cape, id, tên, sprite, rarity, isNew, isPity}.

### UI rương
- `ChestStoreSection.fok/fom`: rương hợp lệ = không yêu cầu hoàn thành hoặc đã đủ. Rương Rarity ≥ Legendary dùng `FeaturedCardPrefab`
  (Chest_Single_Product_Horizontal) chèn ngay TRÊN lưới; còn lại `CardPrefab` (Chest_Single_Product_Vertical) trong `CardContainer`.
  Thứ tự các thẻ featured: sort bằng lambda chưa đọc → **[SUY]** rarity cao lên trên.
- `ChestStoreCard`: icon = MainIcon ?? Icon; tên `Items.Chest.Name.{ChestId}`; giá `<sprite=0>{GemCost}`; "Contains {0}" (list rarity tô màu
  `GameResources.RarityColors`, cách ", "); pity "Get {rarity} in {n} opens" (số tô `PityCountColor` #FFCC33), ẩn khi PityCount = 0.
- `ChestInfoPopup`: "Contains {0} cloak and companion"; mỗi `ChestOddsRow` = trọng số / tổng × 100, định dạng "0.##" + "%"; ẩn dòng rarity
  không có và hạ bảng `RarityRowHeight (50) × số dòng ẩn`.
- `ChestRevealPopup` (mua bằng gem): chạm mở rương → từng món → tổng kết; "Open all". `ChestBundleRevealPopup` (prefab ChestRewardsPopup,
  mua gói): lưới cuộn, hiện dần mỗi `RevealInterval` 0.03s, chạm để đóng.

### Gói rương IAP (`ChestBundleData`, `PurchaseController.dhk`, `UserController.eev`) [CHẮC]
| Gói (StoreId) | Gem | Rương mở sẵn |
|---|---:|---|
| lootio.chestbundle_legendary (LegendaryFeaturedBundle) | 2500 | 20 Epic + 10 Legendary |
| lootio.chestbundle_mythic (MythicFeaturedBundle) | 3000 | 20 Legendary + 10 Mythic |
| lootio.chestbundle_1 | 500 | 2 Legendary |
| lootio.chestbundle_2 | 400 | 2 Mythic |

Mua: chưa mở Cape → `IsCapeFeatureEarlyUnlocked = true` ("Unlocks the Cloak feature!"); +GemAmount gem; mở mọi rương trong gói; hiện
bảng kết quả. Popup chào bán `ChestBundleOfferPopup` do lobby (`GameplayUI.kiz/kja`) mở: Legendary bundle, hoặc Mythic bundle khi đã
đủ rarity Epic. Giá gói: **[SUY]**.

---

## 3. Daily Deals (`DailyDealController`, class `ty`) [CHẮC]

- combo = `size × 10 + type`. `DealSize` Tiny 0 / Small 1 / Medium 2 / Large 3 → product `lootio.deal_tiny|small|medium|large`.
  `DealType` DungeonKey 0 / Gem 1 / Cloak 2 / Mining 3 / Companion 4 (tên hiển thị "Dungeon/Gem/Cloak/Mining/Companion Deal").
- Seed ngày = `year×10000 + month×100 + day`. Sang ngày → xoá `DailyDealPurchasedCombos`, sinh lại.
- Sinh (`jsp`): `System.Random(seed × 31 + purchased.Count)`; gom 20 combo chưa mua; xáo Fisher-Yates (`for n = count; n > 1; n--: j = Next(n); swap(n-1, j)`);
  duyệt lần lượt, bỏ combo trùng SIZE hoặc trùng TYPE với combo đã chọn, đủ 3 thì dừng; sort (lambda chưa đọc → **[SUY]** tăng dần).
  Ra < 3 combo → xoá purchased, sinh lại.
- Mua (`jsk`): ghi `PendingDealCombo` rồi gọi mua product theo size. Xong (`jsl`): phát thưởng, thêm vào purchased, gỡ khỏi current;
  current rỗng → sinh 3 deal MỚI ("Buy all three daily deals to get three new ones!").

Thưởng theo size [Tiny, Small, Medium, Large] (`ty.jtb`, thứ tự hiển thị `DailyDealSlotUI.kam`):

| Loại | Thưởng chính |
|---|---|
| DungeonKey | Dragon bonus key [1,2,3,5] + Zombie bonus key [1,2,3,5] |
| Gem | Gem [200,500,2000,5000] |
| Cloak | Cloak [20,50,200,500] |
| Mining | Pickaxe [10,20,100,200] + Golden Pickaxe [1,2,10,20] + Drill [1,2,10,20] |
| Companion | Bone [400,1000,4000,10000] |
| mọi loại | + Loot box (Hammer) [100,300,1500,3000] |
| mọi loại TRỪ Gem | + Gem [50,150,750,1500] |

UI: > 4 dòng thưởng (deal Mining = 5) → lưới 3 cột + ô cao `ContentHeightFor5Rewards` 295.95 (thường 2 cột, 249.17).

---

## 4. Gói IAP (`PurchaseController.dhk`) [CHẮC phần thưởng]

| Product | Thưởng |
|---|---|
| lootio.gem1..gem6 | 100 / 255 / 520 / 1100 / 2700 / 5500 gem |
| lootio.lifetimeboost | `IsLifetimeBoostBought` (exp ×2 vĩnh viễn) |
| lootio.noads | `IsNoAdsBought` |
| lootio.resourcepack | 1200 loot box + 750 bone + 400 gem |
| lootio.dungeonkeys | 4 Dragon bonus key + 4 Zombie bonus key |
| lootio.starterpack | 500 gem + 300 loot box |
| lootio.epicpack | 2400 gem + 1500 loot box + 750 bone |
| lootio.battlepass | premium Battle Pass |
| lootio.offer_099..9999 | Weapon Offer (`OfferController`) |
| lootio.mining_offer(_large) | Mining Offer |

Giá tiền thật do store trả về, KHÔNG có trong APK → bảng `StaticShopData.DEFAULT_PRICES` là **[SUY]**
(gem $0.99/2.49/4.99/9.99/24.99/49.99; deal $0.99/2.49/9.99/24.99; lifetime $4.99; bundle $19.99/$29.99).

---

## 5. Làm lại trong project

| Gốc | Project |
|---|---|
| `ChestData/ChestBundleData/ChestConfig` | cùng tên, `Scripts/Shop/`; asset ở `Resources/Scriptable Objects/Shop/` |
| `User.*` (pity, deal, boost, cờ đã mua) | `UserShopData` (`key_user_shop`) |
| `UserController.eet…`, `StoreTabPage.ktx…`, `PurchaseController.dhk` | `ShopService` |
| `DailyDealController` + `ty` | `DailyDealService` |
| `PurchaseController` (Unity IAP) | `PurchaseController` BASE chưa gắn SDK: Editor = mua thành công ngay; build thật = báo "Store is not available". Có SDK thì viết class kế thừa (giống `MediationAds`) |
| `StoreTabPage`, `ChestStoreSection`, `ChestStoreCard`, `DailyDealSlotUI`, `DailyDealRewardEntryUI`, `ChestOddsRow`, `ChestRewardElementUI`, `ChestBundleOfferContentIcon` | cùng tên, `Scripts/UI/Shop/` |
| `ChestInfoPopup`, `ChestRevealPopup`, `ChestBundleRevealPopup`, `ChestBundleOfferPopup` | `UI` + tên gốc (kế thừa `BaseUI`) |
| Hammer / Drill / GoldenPickaxe / Pickaxe | `ItemType.LOOT_TICKET` / `DRILL` / `GOLDEN_PICKAXE` / `PICKAXE` |

Dựng asset + prefab: menu **Tools/DungeonRush/Build Shop UI Prefabs** (`ShopPrefabBuilder`) — chạy lại được.

**Chưa làm:** `OfferPopup` (Weapon Offer — cần `OfferController`), `RewardedChestPopup` (rương quảng cáo bay ở lobby — `RewardedChestUI`),
nút chào gói rương ở lobby, Battle Pass, Mining Offer, boost quảng cáo, hiệu ứng UIParticle (plugin không có trong project).

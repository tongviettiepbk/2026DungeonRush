---
name: dungonrush-shop-system
description: Store (tab Shop) dựng 2026-10-10 theo gốc — rương/pity, Daily Deals, boost exp, gói gem; PurchaseController là lớp BASE chưa có SDK IAP; đã test Play
metadata:
  type: project
---
**GD (thiết kế của project, khuôn giống server/BOSS_RUSH_DESIGN.md):** `Scripts/Shop/SHOP_DESIGN.md` — mục 12 liệt kê việc chờ user quyết (giá, pack tắt, nút lobby, giờ server).
**Gốc:** `DecodedData/SHOP_MODEL.md` (+ `tables/ChestCatalog.names.json`). Dựng lại bằng menu `Tools/DungeonRush/Build Shop UI Prefabs`
(`Scripts/Shop/Editor/ShopPrefabBuilder` — cùng kiểu ClanPrefabBuilder: `shop_fieldmap.json` nối field gốc, `shop_tmpsprites.json` gắn TMP sprite asset gốc).
**Code:** `Scripts/Shop/` = ChestData/ChestBundleData/ChestConfig (SO, field PascalCase như gốc), StaticShopData, UserShopData (`key_user_shop`),
ShopService (mua gem, boost, mở rương, GrantProduct), DailyDealService, PurchaseController (Singleton BASE: Editor mua thành công ngay, build thật báo
"Store is not available" → có SDK thì viết class kế thừa như MediationAds). UI `Scripts/UI/Shop/`; `UITabShop` nạp `Resources/Prefabs/UI/StoreTabPage`.
**Nối sang hệ khác:** CapeService.IsUnlocked thêm `shop.isCapeFeatureEarlyUnlocked`; CampaignMode nhân exp qua `ShopService.ApplyExpBoost`;
`DungeonService.AddBonusKeys`; ItemType thêm PICKAXE 16 / DRILL 17 / GOLDEN_PICKAXE 18 (Mining chưa có nơi tiêu).
**[SUY]:** giá tiền thật (`StaticShopData.DEFAULT_PRICES`), thứ tự sort deal + thẻ rương featured.
**CHƯA:** OfferPopup (Weapon Offer), RewardedChestPopup (rương quảng cáo ở lobby), nút lobby mở `UIChestBundleOfferPopup.Show(ShopService.GetFeaturedBundle())`,
Battle Pass, boost quảng cáo, UIParticle (builder tắt mọi ParticleSystem trong prefab UI vì thiếu plugin).
Liên quan: [[dungonrush-clan-clanwar]], [[dungonrush-wing-cape-todo]], [[dungonrush-mediation-ads]], [[dungonrush-ripped-prefab-rewire]].

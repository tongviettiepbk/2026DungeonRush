---
name: dungonrush-mining-system
description: Hệ Đào mỏ (Mining) — nguồn quặng Wing; dựng theo gốc 2026-10-16, test Play OK (cuốc/khoan/cuốc vàng/ads/lưu); doc DecodedData/MINING_MODEL.md
metadata:
  type: project
---

Dựng 2026-10-16 theo reverse il2cpp v41 + asset gốc đọc thẳng APK (UnityPy). Doc đầy đủ: `DecodedData/MINING_MODEL.md`.

- Code `Scripts/Mining/` (MiningConfig SO, UserMiningData key_user_mining, MiningService, MiningPathfinder A* 8 hướng,
  MiningGrid/Block/Character/Camera/Controller) + `Scripts/UI/Mining/` (UIMiningPopup/RarityPopup/OfferPopup).
- Mở từ nút Mining trang Wing (UITabWing.OnClickMining); chấm đỏ = chưa vào mỏ / cuốc đầy. EventID.MiningChanged.
- Cuốc/khoan/cuốc vàng + quặng nằm ở **UserItemData** (PICKAXE/DRILL/GOLDEN_PICKAXE, COAL_ORE..STONE_ORE), KHÔNG ở MiningData như gốc (Shop/Wing đã dùng item).
- Asset sinh bằng `Tools/DungeonRush/Build Mining/All` (MiningPrefabBuilder): layer "Mining"=8, Weapon_Hammer override
  (clip copy từ AssetRipper), _MiningGridMaterial, Prefabs/Mining/{MiningBlock,MiningWorld}, Resources/Scriptable Objects/Mining/MiningConfig,
  Resources/Prefabs/UI/UIMining*.
- World sinh (1000,1000) render qua RenderTexture runtime vào RawImage; nền ContentAll của UIMiningPopup KHÔNG được có Button (nuốt chạm).
- FX vỡ khối: Prefabs/Fx/Mining/<Ore>Particle (rip) mất sprite Texture Sheet Animation → hạt không hiện; menu `Build Mining/Fix Ore Particles` gán lại sprite `box-loot` (gốc, đọc APK). Đã test Play thấy hạt (2026-10-16).
- Gói Mining Offer: ShopService.GrantProduct → MiningService.GrantOffer; giá chưa có (hiện "N/A").
- Chưa làm: chấm đỏ Mining ở ô Wing lobby (ItemUI.MiningNotificationUI gốc), TMP sprite icon quặng `<sprite=0>`.

**How to apply:** ĐÃ RESKIN 2026-10-19 → KHÔNG chạy lại menu Build Mining (ghi đè prefab UI + config, mất reskin; xem [[dungonrush-reskin-mining]]); trừ Fix Ore Particles. Test Play: backup PlayerPrefs key_user_items + key_user_mining TRƯỚC mỗi lần test (user có thể đã tự chơi giữa chừng) rồi khôi phục đúng bản đó. Xem [[dungonrush-wing-cape-todo]], [[dungonrush-reverse-native-il2cpp]], [[dungonrush-il2cpp-sprite-pipeline]].

---
name: dungonrush-ui-prefabs-batch
description: Đã batch-rewire 128 prefab UI từ AssetRipper vào Prefabs/UI/<Feature>/ + tooling mac
metadata:
  type: project
---

Ngày 2026-09-20: lấy **128 prefab UI** còn lại từ `AssetRipper/ExportedProject/Assets/GameObject/` vào `2026DungeonRushUnity/Assets/_Assets/Prefabs/UI/<Feature>/` (giống cách đã làm Pet=Companion). Layout-only (đã strip script game, wire controller sau) + map sprite từ `_ResourceGame`.

**20 thư mục feature** (số prefab): Battle 8, BattlePass 4, BossRush 5, Cape 5, Chat 3, Clan 13, ClanWar 10, Common 5, Dungeon 3, Enchantment 6, Events 3, Forge 4, Gear 6, Mastery 2, Mining 3, Profile 10, PvP 8, Settings 14, Shop 12, Wing 4. (Pet 6 đã có trước.)

**Đã loại (không phải UI):** Projectile_*, Companion_1..6 (battle unit), particle/FX, EnchantmentGlow_*, map/obstacle, DebugUI* (SRP debug của Unity), Console tab.

**Tooling MỚI trên macOS** (máy cũ là Windows, [[dungonrush-il2cpp-sprite-pipeline]] path E:\ đã lỗi thời):
- venv: `tools/.venv/` (python3.9) đã cài `UnityPy` 1.25.3 + `TypeTreeGeneratorAPI` 0.0.10 (có wheel mac arm64). `brew`+`pip` có sẵn; **node KHÔNG cài được qua brew** (kẹt vendor ruby) → đã bỏ node, port join/rewire sang Python.
- 3 script gốc (`dump_/join_/rewire_levelpopup`) đã sửa ROOT tự suy từ vị trí file (bỏ hardcode Windows); `rewire_levelpopup.js` thêm argv[3]=Feature → xuất `Prefabs/UI/<Feature>/`.
- **Driver chính: `tools/extract_ui_batch.py`** — gộp dump→join→rewire, load xapk/UnityPy/typetree **1 LẦN** cho cả batch (nhanh, ~vài giây). Chạy: `tools/.venv/bin/python tools/extract_ui_batch.py [Feature ...]`. Report → `tools/_rewire/ui_batch_report.json`.
- **Cải tiến then chốt:** thay bảng SCRIPT_MAP tay bằng **auto-map TẤT CẢ class UGUI/TMP**: tính MonoScript fileID bằng thuật toán Unity (`md4("s\0\0\0"+namespace+class)`, 4 byte đầu little-endian, signed int32 — khớp 100% Image=-765806418...), rồi tra guid từ `.cs.meta` trong `Library/PackageCache/com.unity.ugui@*/Runtime` (TMP nằm chung ở `Runtime/TMP/`). Hết sạch cảnh báo "UI class chưa map" (ScrollRect/LayoutGroup/ContentSizeFitter... đều được giữ + remap). Namespace `TMPro`→assembly 67dfb1, còn lại→d3e719.
- meta prefab guid = md5(feature+"/"+target) (ổn định khi chạy lại); folder meta = md5("folder:"+feature).

**Cần soi lại trong Unity (đã ghi trong report):** các `*TabPage` (EnchantmentTabPage 114/108, ClanTabPage 94/77...) lệch số Image do chứa nested element-prefab riêng → sprite trực tiếp vẫn map, element con là prefab riêng. `MobileTestPopup` conflict=33 (debug popup, bỏ qua được). mapped=0: ChestRewardsPopup, Tab. Sprite built-in Unity còn trống: UIMask, UISprite, InputFieldBackground, RoundedCorner-5px@2x + vài icon thật chưa tách: info-64, no_adds_icon, trophy 1.

**Fill texture cho SCENE gốc (2026-09-20):** `tools/fill_scene_textures.py` — map sprite cho scene AssetRipper (khác prefab: nhiều root → match theo **đường dẫn phân cấp** xapk↔YAML, map theo GUID nên 1 path khớp là phủ hết occurrence). Xử lý cả UGUI Image lẫn SpriteRenderer(212). Đã fill `Assets/_Assets/SceneOrigin/GameplayScene.unity` (=lobby gốc: Canvas>TabBarUI 5 tab Store/Skills/PvP/Dungeon/Clan, InventoryUI, LootButton, CharacterPreview) + `StarterScene.unity`. Kết quả: GameplayScene 69 sprite distinct + StarterScene 3, **0 thiếu**, strip 193/45 script game, GameObject/RectTransform giữ nguyên. Scene lấy từ `AssetRipper/ExportedProject/Assets/_Game/_Scenes/` (game gốc CHỈ có 2 scene này; lobby dựng thẳng trong scene, KHÔNG có prefab lobby riêng). Tận dụng env đã load của extract_ui_batch (import module).

Xem pipeline chi tiết: [[dungonrush-ripped-prefab-rewire]], [[dungonrush-il2cpp-sprite-pipeline]]. Nguồn sprite: [[dungonrush-gear-icon-extract]].

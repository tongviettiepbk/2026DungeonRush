---
name: dungonrush-pending-todo
description: DANH SÁCH VIỆC CÒN DỞ (cập nhật 2026-10-09) sau đợt Enchantment/Power/Substat — đọc đầu phiên khi user nói "làm tiếp"
metadata:
  type: project
---

Việc còn dở sau phiên 2026-10-09 (Enchantment + Power + chỉ số trận + substat). Làm xong mục nào thì xoá/cập nhật ở đây.

**1. Kiểm tra trong Unity (chưa làm được vì UnityMCP ngắt)**
- Prefab/scene sửa bằng YAML: PageEnchantment (8 slot), UiEnchantmentInfoItem (3 nút mới), UiEnchantmentMerge, UiSumonEnchantment, uiUpgradeEnchantment, EnchantmentSpriteConfig.asset. Mở console xem lỗi + test: phím V (+2000 Vial) → summon → đeo → merge (kho + đang đeo) → dismantle → quick equip.
- Cape (2026-10-09): UICapePopup/UICapeDetailPopup/UICapeSalvagePopup/UICapeUpgradeInfoPopup (prefab sinh bằng YAML), CapeElementUI, RarityBackgroundConfig.asset. Test: lên lv15 → bấm ô Cape → phím K (+1000 Cloak) → summon x1/x30 → bấm cape → Equip → Upgrade (chọn cape gộp) → Info tỉ lệ → Show Cloak ON/OFF.
- Wing (2026-10-09): PageWing (UITabWing wire YAML, PageWing set inactive), UIWingCraftPopup (đã đổi script từ UISumonEnchantment), UIWingRerollPopup, WingElementUIPrefab, SubstatUIElementPrefab (+ElementSubStatRerollUI). Test: lên lv6 → bấm ô Wing → phím O (+1000 quặng) → craft → claim → upgrade → reroll (khoá dòng) → equip.
- Chỉ số trận đổi: đồ THAY nền PlayerBase, Wing/Cape vào chỉ số, CritDamage nền 1.05, HealthRegen tick, đánh đôi → kiểm tra cân bằng màn chơi.

**2. Server Boss Rush**: `server/functions` thêm `enchantmentTiers` — chưa `npm run build` + deploy (máy mac không có node).

**3. Hình ảnh (user tự làm / làm sau)**
- Hiệu ứng relic trên nhân vật (SoldierEnchantmentEffectController: material từ tier 6, particle sau từ 7, trước từ 8; asset rip EnchantmentGlow_Tier_7..11(+_Front), 24 material Enchantment_*, shader Enchant_SpineEnchant/OutlineFlash; cả PreviewCharacter).
- Nút summon: icon Vial `<sprite=0>` + màu Default/Disabled khi thiếu Vial; slot RarityPatterns (Ancient/Immortal/Divine); popup xem trước khi bấm ô kết quả Merge (EnchantmentPreviewInfoPopup); nền tier sau "+tier" ở ô lobby; animation TutorialUI "equip here"; cuộn bảng kết quả summon.
- Power: icon `<sprite=0>` (lobby + toast đang thay bằng chữ "Power"), toast kiểu WarningUI; định dạng số gốc rn.iqt (hậu tố K,M,B,T,q,Q,s,S,O,N,D,aa…; đang dùng ToLetter).
- Đánh đôi: khi có Spine anim event thật thì đổi đòn 2 theo event (gốc anim x2), hiện đặt giữa nhịp.

**4. Tính năng gốc chưa có trong project (có dính relic/Vial)**
- Wing + Cape đã có UI (chưa có nguồn quặng/Cloak) → xem [[dungonrush-wing-cape-todo]].
- BattlePass, ClanWar (thưởng Vial); PvP, Chat, Loadout upload (gửi enchantmentTiers).

**5. Nghi vấn chưa chốt**
- Substat Melee/Ranged khi CHƯA cầm vũ khí: Power gốc coi như Melee; chỉ số trận project hiện bỏ qua (Soldier[0x264] chưa xác minh).

**6. Git**: commit trên main nhưng CHƯA push (origin/main đang sau) — user tự push.

Liên quan: [[dungonrush-enchantment-system]], [[dungonrush-power-model]], [[dungonrush-substat-combat]].

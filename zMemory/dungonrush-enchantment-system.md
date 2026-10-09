---
name: dungonrush-enchantment-system
description: Hệ Enchantment (Relic) — reverse xong (tier², summon 50 lv, merge 3→1) + đã dựng data/service/UI/prefab/scene; chưa test Unity; doc ở DecodedData/ENCHANTMENT_MODEL.md
metadata:
  type: project
---

Enchantment = relic chỉ có TIER 1..11, đeo vào 8 slot trang bị (GearSlotType 1..8). Main stat món ở slot × (1 + tier²/100); slot không có đồ thì không nhân. Summon 100 Vial→5 lượt (multiplier [1,2,3,5,10,20,30], nút multiplier chỉ hiện khi Vial ≥ 2000), chỉ ra tier 1..6 theo Summon Level (max 50, cần 10+5×lv lượt/level). Merge 3→1 tier+1; Dismantle 1→3 tier-1; Quick Equip theo priority rarity đồ (+4 Cape, +1 Wing). Vial = ItemType.VIAL (dungeon Cultist). Doc đầy đủ + bảng tỉ lệ 50×6: `DecodedData/ENCHANTMENT_MODEL.md`.

Đã làm (2026-10-09): `Scripts/Enchantment/` (EnchantmentConfig, UserEnchantmentData key_user_enchantment, EnchantmentService, StaticEnchantmentData + EnchantmentSpriteConfig asset ở Resources/Scriptable Objects/Enchantment); EquipmentStatResolver nhân main stat; EventID.EnchantmentChanged (HeroUnit + ô lobby nghe). UI: UITabEnchantment (PageEnchantment trong UiMainGame, 8 slot đã wire scene), UIEnchantmentElementInfo (đã thêm 3 nút Equip/Merge/Dismantle vào prefab), UIEnchantmentMerge, UIUpgradeEnchantment (prefab copy từ pet → đã đổi script), UISumonEnchantment. Lobby: btEnchantmentInfo mở page, txtLevelEnchantment "+tier". Cheat editor: phím V +2000 Vial.

Đã ĐỐI CHIẾU lại native (2026-10-09, lần 2) & sửa: popup Merge (đặt sẵn 1, kho lọc theo tier, merge relic đang đeo qua popup), nút Merge Info bật khi tier<11, kho tier tăng dần, chữ "Lvl N"/"Lv N"/F2%, thiếu Vial→tab Dungeon, mở khoá = PlayerLevel ≥ mốc Cultist. Nhãn "+tier" lobby theo gốc: hiện khi tier ≥ 1, không xét có đồ (user chốt 2026-10-09).

Đợt 3 (2026-10-09): relic vào snapshot Boss Rush (client DTO + ghost + server TS `enchantmentTiers`, CHƯA build/deploy server vì máy mac không có node), UIGearInfo tính relic. Còn: Power chung (rm.iqm) + thông báo đổi Power, BattlePass/ClanWar/PvP chưa có trong project, phần hình ảnh (xem ENCHANTMENT_MODEL.md §8).

**Why:** compile dotnet sạch nhưng UnityMCP ngắt nên CHƯA mở Unity verify prefab/scene sửa bằng YAML.
**How to apply:** lần sau mở Unity kiểm tra console + PageEnchantment trước khi làm tiếp. Chưa làm: hiệu ứng relic trên nhân vật (SoldierEnchantmentEffectController), relic trong snapshot Boss Rush, Vial từ BattlePass/ClanWar. Xem [[dungonrush-companion-save-upgrade]], [[dungonrush-reverse-native-il2cpp]].

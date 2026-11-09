---
name: dungonrush-reskin-cultist-weapons
description: "Vũ khí quái Dark Ritual đổi sang liềm đen + cung nến (Codex concept), ghi đè giữ GUID, pivot custom theo tay cầm cũ; 2026-11-09 chưa test Play, đã commit"
metadata:
  node_type: memory
  type: project
  originSessionId: d5ab7e46-9699-43d1-bfca-e554524203b4
  modified: 2026-11-09T06:18:15.799Z
---

2026-11-09: quái mode Dark Ritual (CultistDungeonMode) bỏ dao karambit đỏ + cung đen (sót từ thời "Mafia Crew"), thay bằng:
- Cận chiến: liềm đen khắc phù văn tím + hình vịt → `Art/4_Characters/Enemies/Cultist/cultist_dungeon_melee_weapon.png` (293×468, PPU 650, pivot custom 0.547/0.466).
- Bắn xa: cung gỗ quấn vải tím, treo 2 nến + bùa ngôi sao → `cultist_dungeon_range_weapon.png` (483×290, PPU 453, pivot 0.5/0.6976).

Cách làm: Codex vẽ lưới 2×3 concept (lưu ở `ArtArchive/Concepts/cultist_weapons_concept.png`), cắt ô bằng grid_split, tách nền xám bằng flood fill
(cái liềm KHÔNG xoá đảo nền kín vì vệt sáng lưỡi màu xám), ghi đè PNG giữ .meta/GUID nên prefab không phải sửa. Pivot đặt sao cho tay cầm
cách pivot đúng như sprite cũ (sprite cũ pivot giữa, có viền tối to). Đã chụp so cũ/mới ở tư thế UnitIdle trong preview scene qua UnityMCP HTTP
(`ArtArchive/Concepts/cultist_weapons_ingame_compare.png`). Icon `CultistWeaponData_Ranged.png` (của SO) chưa đổi.

Đạn (`Projectile_Weapon_Cultist`, sprite `Art/5_Maps/DungeonCultist/cultist_dungeon_arrow.png`) đổi sang mũi tên gỗ, đầu đen khắc ngôi sao hồng
(mẫu 3 trong `ArtArchive/Concepts/cultist_arrow_concept.png`, user chọn), 230×958, PPU 340, pivot giữa, mũi hướng LÊN như cũ; đã commit.

**Why:** user muốn vũ khí hợp chủ đề nghi lễ hắc ám (nến, ngôi sao năm cánh).
**How to apply:** chưa test Play, đã commit. Xem [[dungonrush-reskin-dungeon-maps]], [[dungonrush-reskin-ui-howto]].

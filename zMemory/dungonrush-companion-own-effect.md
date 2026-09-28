---
name: dungonrush-companion-own-effect
description: Own Effect của pet (OwnAttack/OwnHealth) cộng PHẲNG vào hero cho MỌI pet SỞ HỮU (không cần equip) — reverse lu.gov; ĐÃ áp vào HeroUnit (2026-09-28)
metadata:
  type: project
---

Reverse il2cpp v41 (2026-09-28):
- `lu.gov(List<CompanionModel> owned, GameResources)` @0x2760E5C: lặp list, mỗi con cộng
  attack += OwnAttackBase(0x1BC) + OwnAttackScaler(0x1C0)×level ; health += OwnHealthBase(0x1C4) + OwnHealthScaler(0x1C8)×level.
  Trả tuple (attack, health).
- Caller `Soldier.ewc` @0x2A1C308 (tính chỉ số hero) truyền `UserController.ebp()` = `OwnedCompanions` (0x338),
  KHÔNG phải EquippedCompanions (0x340) → **mọi pet đã sở hữu đều buff hero, equip hay không không quan trọng**.
  Cộng phẳng cùng các nguồn flat khác (base + gear) của Damage/Health, TRƯỚC bước áp % substat (`Soldier.eyv`)
  → trong project = Pass 1 (flat) của HeroUnit.CalculateCurrentStats. Nhánh `IsPvPOpponent`(Character 0x15F)=true
  thì dùng list pet của đối thủ (lu.got/gou từng con) thay vì OwnedCompanions.
- Cũng dùng ở NewForgePopup.ihp / NewItemPopup.iif / rm.iqm (so sánh chỉ số/power); CompanionInfoPopup.grk hiển thị từng con.
- Hiển thị popup: "Damage X Health Y" (cắt phần lẻ) — đã làm ở UIPetInfo.

**Why:** user hỏi owned stat có phải buff hero khi đeo pet không — đáp án: buff khi SỞ HỮU.
**Đã làm (2026-09-28):** `CompanionService.BuildOwnEffectModifiers()` (2 modifier flat Attack/MaxHp, source Companion) nạp trong `HeroUnit.LoadPermanentModifiers`; event `EventID.CompanionOwnedChanged` (post ở CompanionUI sau summon/upgrade + cheat P) → HeroUnit ReloadStats. Gốc giữ TỈ LỆ máu khi tính lại (ewc a=false: hp = hp/oldMax×newMax), project chỉ clamp — chưa sửa.
**How to apply:** sửa công thức own stat thì sửa CompanionService.GetOwnAttack/GetOwnHealth (UIPetInfo dùng chung). Xem [[dungonrush-hero-base-stats]], [[dungonrush-companion-save-upgrade]].

---
name: dungonrush-companion-battle-classes
description: Pattern class chiến đấu cho companion in-battle — PetUnit companion-aware + mỗi loại 1 lớp con
metadata: 
  node_type: memory
  type: project
  originSessionId: 96619a4c-92d2-4055-9717-a167602da288
  modified: 2026-09-22T09:15:33.952Z
---

Hành vi companion TRONG TRẬN (khác hệ meta unlock/summon ở [[dungonrush-companion-unlock-summon]]).

**Pattern chốt (user duyệt qua Companion_1_Common_DPS_Single):**
- `PetUnit : BaseUnit` là LỚP NỀN companion-aware: giữ `CompanionData companionData`, `SetupCompanion(data, owner, level, pos)` quy đổi data→hành vi (nhịp = 1/cooldown → attackSpeed; tầm = max(followDistance,minDistance); moveSpeed), tính `GetAbilityDamage()`/`GetAbilityHeal()` = base + scaler*(level-1) rồi × `owner.stats.companionDamage`. Ra đòn tại `OnAttackEnd→ReleaseAttack→ReleaseAbility()` (hook virtual).
- Companion: `isDead=false`, `isTargetable=false`, `isImmuneCC=true` (enemy KHÔNG nhắm, pet không chết) — giống StickIdle BaseCompanion.
- MỖI loại companion = 1 lớp con override `ReleaseAbility()`. Đã làm: `PetCompanionDps` (Ember Fist) = bắn `BaseBullet` đạn lửa đơn mục tiêu, speed từ `projectileSpeed`. Còn Heal/Lightning + các tier sau.

**Data:** value thật ở Resources/Scriptable Objects/Companions/*.asset (đã reverse). damageBase/scaler là sát thương TUYỆT ĐỐI ("deal X Damage"), KHÔNG phải % hero.attack (khác StickIdle).

**Công thức damage GỐC (ĐÃ reverse il2cpp v41, xem DecodedData/COMPANION_MODEL.md §6):**
`damage = (DamageBase + DamageScaler × level) × (1 + CompanionDamageBonus/100)`.
- Chốt ở strategy DPS `kn$$gfk` @0x2C408D0. **level dùng THẲNG (1-based, cap 100), KHÔNG phải (level-1)** →
  đã sửa code GetAbilityDamage/GetAbilityHeal thành `* level`. DPS lv1 = 94.5 (mô tả 90 chỉ là base hiển thị).
- `(1+CompanionDamageBonus/100)` = đúng `owner.stats.companionDamage` (Stats lưu bội số 1+%).
- Mỗi CompanionType → 1 strategy obf (factory `lp$$gjt` switch): 0=kn,1=kq,2/3=kx,4=kl,5=kk,6=lh,7=kf,
  8=ko,9=kd,10=kh,12/13=kt,15=kv. Heal/Lightning (kq/kx) suy cùng dạng, chưa disasm chốt.
- Đồ nghề reverse chạy trên mac: artifact dump ở `/private/tmp/claude-501/.../321174b3.../scratchpad/il2cpp`
  (libil2cpp.so + out/dump.cs + script.json); disasm.py cần venv có capstone+lief (lief arm64 hơi flaky, retry).

**FX:** behavior gốc AssetRipper STUB RỖNG. FX gốc (ProjectilePrefab guid 6c98eedd…) nằm trong AssetBundle KHÔNG export → dùng FX đã import (RedOrb làm đạn lửa). FX đã import CHƯA prefab nào gắn `BaseFx` → impact-on-hit chờ pass wiring BaseFx (task tồn: "30 prefab missing-script"). Script ở Unit/Pet/PetCompanionDps.cs (user đã move).

**Wire test:** CampaignMode.SpawnHeroAndPets đã nối lại spawn qua SetupCompanion (gán petPrefab=companion prefab + petCount≥1 ở mode inspector). Compile-check: `dotnet build 2026DungeonRushUnity/Assembly-CSharp.csproj` (nhớ thêm file mới vào .csproj vì liệt kê thủ công).

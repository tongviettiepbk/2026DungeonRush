---
name: dungonrush-companion-battle-classes
description: Pattern class chiến đấu cho companion in-battle — PetUnit companion-aware + mỗi loại 1 lớp con
metadata: 
  node_type: memory
  type: project
  originSessionId: 96619a4c-92d2-4055-9717-a167602da288
  modified: 2026-09-22T07:03:34.874Z
---

Hành vi companion TRONG TRẬN (khác hệ meta unlock/summon ở [[dungonrush-companion-unlock-summon]]).

**Pattern chốt (user duyệt qua Companion_1_Common_DPS_Single):**
- `PetUnit : BaseUnit` là LỚP NỀN companion-aware: giữ `CompanionData companionData`, `SetupCompanion(data, owner, level, pos)` quy đổi data→hành vi (nhịp = 1/cooldown → attackSpeed; tầm = max(followDistance,minDistance); moveSpeed), tính `GetAbilityDamage()`/`GetAbilityHeal()` = base + scaler*(level-1) rồi × `owner.stats.companionDamage`. Ra đòn tại `OnAttackEnd→ReleaseAttack→ReleaseAbility()` (hook virtual).
- Companion: `isDead=false`, `isTargetable=false`, `isImmuneCC=true` (enemy KHÔNG nhắm, pet không chết) — giống StickIdle BaseCompanion.
- MỖI loại companion = 1 lớp con override `ReleaseAbility()`. Đã làm: `PetCompanionDps` (Ember Fist) = bắn `BaseBullet` đạn lửa đơn mục tiêu, speed từ `projectileSpeed`. Còn Heal/Lightning + các tier sau.

**Data:** value thật ở Resources/Scriptable Objects/Companions/*.asset (đã reverse). damageBase/scaler là sát thương TUYỆT ĐỐI ("deal X Damage"), KHÔNG phải % hero.attack (khác StickIdle).

**Đục khi thiếu:** behavior gốc trong AssetRipper bị STUB RỖNG (Companion.cs obfuscated body trống) → công thức phải reverse `libil2cpp.so`. Đường cong level (cộng dồn scaler) hiện suy từ pattern model khác, CHƯA đối chiếu native — ở level 1 = damageBase nên review được. FX gốc (ProjectilePrefab guid 6c98eedd…) nằm trong AssetBundle KHÔNG export → dùng FX đã import (RedOrb làm đạn lửa). FX đã import CHƯA prefab nào gắn `BaseFx` → impact-on-hit chờ pass wiring BaseFx (task tồn: "30 prefab missing-script").

**Wire test:** CampaignMode.SpawnHeroAndPets đã nối lại spawn qua SetupCompanion (gán petPrefab=companion prefab + petCount≥1 ở mode inspector). Compile-check: `dotnet build 2026DungeonRushUnity/Assembly-CSharp.csproj` (nhớ thêm file mới vào .csproj vì liệt kê thủ công).

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
- Mỗi CompanionType → 1 strategy obf (factory `lp$$gjt`, ĐÃ giải jump-table 2026-09-27): 0kn 1kq 2lo 3kx 4kl 5kk
  6lh 7kf 8ko 9kd 10kh 11lb 12lm 13kt 14ld 15kv (bảng cũ 2/3=kx,12/13=kt là SAI).
- Đồ nghề reverse chạy trên mac: artifact dump ở `/private/tmp/claude-501/.../321174b3.../scratchpad/il2cpp`
  (libil2cpp.so + out/dump.cs + script.json); disasm.py cần venv có capstone+lief (lief arm64 hơi flaky, retry).

**FX:** behavior gốc AssetRipper STUB RỖNG. FX gốc (ProjectilePrefab guid 6c98eedd…) nằm trong AssetBundle KHÔNG export → dùng FX đã import (RedOrb làm đạn lửa). FX đã import CHƯA prefab nào gắn `BaseFx` → impact-on-hit chờ pass wiring BaseFx (task tồn: "30 prefab missing-script"). Script ở Unit/Pet/PetCompanionDps.cs (user đã move).

**Wire test:** CampaignMode.SpawnHeroAndPets đã nối lại spawn qua SetupCompanion (gán petPrefab=companion prefab + petCount≥1 ở mode inspector). Compile-check: `dotnet build 2026DungeonRushUnity/Assembly-CSharp.csproj` (nhớ thêm file mới vào .csproj vì liệt kê thủ công).

**Cooldown/kích hoạt (2026-09-27):** PetUnit ra đòn theo cooldown CompanionData (initialDelay lần đầu), KHÔNG theo attackPerSecond (chỉ còn wind-up 0.3s). Auto/thủ công lưu `UserCompanionData.isAutoActive`; thủ công → bấm ô ElementPetUILobby → `PetUnit.RequestActivate()`. CampaignMode spawn pet theo danh sách EQUIP (bỏ petCount), UI tra pet qua `CampaignMode.GetPet(assetName)`. PetUnit tự override UpdateBehavior vì isTargetable=false làm base bỏ qua AI.

**Prefab riêng từng con (2026-09-27):** prefab companion ở `Resources/Prefabs/Units/Companions/`, TÊN FILE = assetName (đã đổi tên Bomb/Burn + Divine_1/2/3 → Clone/HealNova/Immortal, map bằng kích thước icon). Spawn qua `CompanionData.LoadPrefab()`; đã BỎ `BaseMode.petPrefab` dùng chung + 00Pet.prefab.

**Mặc định ra đòn + state (2026-09-27):** PetUnit.ReleaseAbility MẶC ĐỊNH = đạn homing (`BaseBullet.ActiveHoming`, đuổi target mỗi frame) với field `projectilePrefab`/`sfxShoot` ở PetUnit; lớp con CHỈ khi khác kiểu. `PetCompanionDps` ĐÃ XOÁ (gộp vào PetUnit, prefab DPS đổi script sang PetUnit). Tầm đánh chung `ATTACK_RANGE=20`. Đang hồi chiêu/chờ bấm → pet Move theo hero, chỉ Attack khi CanReleaseAbility. Prefab pet gravityScale đã về 0 (trước =1 làm pet trôi). Đã sửa BaseBullet.Active không truyền attackData.

**Healer — Magic Wool (2026-09-27, reverse `kq` chốt):** lớp `PetCompanionHeal : PetUnit` (Scripts/Unit/Pet/) — override
`FindNearestTarget` → target = CHỦ (tái dùng state machine hồi chiêu→Attack), `ReleaseAbility` bắn `BulletHeal`
(Scripts/Bullets/, homing, tới nơi `GetHeal`). heal = GetAbilityHeal() (đã ĐÚNG gốc: ×(1+CompanionDamageBonus/100)).
Bắn MỖI lần hồi chiêu, KỂ CẢ hero đầy máu, không cần enemy (đúng gốc). Prefab đạn `Prefabs/Projectiles/Projectile_Companion_HealOrb`
(root BulletHeal + child HealOrbProjectile FX). `FxController.fxHeal` CHƯA gán ở scene → chưa có particle hồi máu.
Gốc có `CompanionCooldownReduction` (cooldown×(1−CDR/100)) — PetUnit CHƯA áp. Dump il2cpp dựng lại bằng git clone
Il2CppDumper + build net9 (`dotnet build -c Release`) — mất sau session.

**ĐỦ 16 pet (2026-09-27, compile sạch, CHƯA chạy thử Unity):** mỗi type 1 lớp `PetCompanion*` ở Scripts/Unit/Pet/
(Heal, Slow, MultiSlow, Siphon, Bomber, AoeSlow:Bomber, Meteor:Bomber, Lightning, Beam, ChainHeal, Blaster, Guardian,
HealNova, Immortal, Clone; DPS vẫn PetUnit). Effect dùng chung ở Unit/Pet/Effects/ (CompanionUnitEffect nền →
Slow/Burn/Block/Immortal; CompanionLink tia LineRenderer; CompanionNova vòng nở; CompanionFx; CompanionCloneTracker).
`BulletHeal` → đổi thành `CompanionBullet` (callback, Homing/Arc/Point/Sine; giữ guid). PetUnit thêm TargetsOwner,
pha active (effectDuration) cộng vào lượt hồi chiêu, helper enemy xa nhất/cụm đông nhất/bán kính. BaseUnit thêm
bonusBlockChance + isImmortal + GetBaseStats/OverrideMaxHp. CampaignMode: thua khi HERO chết (clone không giữ trận).
FX/đạn gốc KHÔNG có (AssetBundle) → chọn FX import theo tên: SlowOrb/SnowBall/FireBomb/Spectral/Divine + NovaFire/NovaBlue/FrostMuzzleBlue.
ĐÃ theo gốc (2026-09-27): damage pet NonCrit (MakeAttackData); CompanionCooldownReduction = Σ% thô substat CompanionCooldown
(Soldier.eyw) → Stats.companionCooldownReduction (đơn vị %) → PetUnit cooldown×(1−CDR/100). Lệch còn: Storm Eye nối gần nhất (gốc: thứ tự list).
ĐÃ SỬA (2026-09-27): BaseUnit.CalculateCurrentStats dựng `stats = new Stats(baseStats)` (hết cộng dồn khi ReloadStats nhiều lần);
ReloadStats tự ApplyWeapon (trước đây InitModeDone/buff/CC reload làm mất tầm vũ khí) + áp `slowMultiplier` (Slow bền qua reload).
Substat BlockChance/Lifesteal đã nối (Stats.blockChance/lifesteal, đơn vị %): chặn trọn đòn khi bên đánh là enemy
(BaseUnit.IsBlock, cộng bonusBlockChance Guardian); hút máu ở HeroUnit.OnAttackDone (đòn thường). Clone: HeroUnit.SetupAsMirrorClone
(% máu áp trong CalculateCurrentStats, bỏ qua EquipmentChanged); đã bỏ BaseUnit.OverrideMaxHp.
Chi tiết từng strategy: DecodedData/COMPANION_MODEL.md §6.

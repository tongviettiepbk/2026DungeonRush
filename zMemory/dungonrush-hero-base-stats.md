---
name: dungonrush-hero-base-stats
description: Chỉ số NỀN hero lúc mới vào (chưa đồ) = Damage 10/HP 50 từ PlayerBase* (GameResources native); ĐÃ wire vào code
metadata: 
  node_type: memory
  type: reference
  originSessionId: 345477a5-d71e-4d98-8c70-25e80a9aa5ed
  modified: 2026-09-01T17:59:03.141Z
---

Chỉ số hero LÚC MỚI VÀO game (chưa mặc đồ) = **Damage 10, HP 50, AttackSpeed 1.0, MoveSpeed 2.0, AttackRange 1.5**.
Trích từ native `libil2cpp.so` v31, class **`GameResources`** (MonoBehaviour ở scene `level0`, path_id 132; MonoScript GameResources = path_id 133). Xác nhận 2026-09-02, khớp đúng "attack 10" user thấy in-game.

Game có 2 tầng base tách biệt:
- **PlayerBase\*** = chỉ số NỀN người chơi, có sẵn dù slot trống:
  WeaponDamage=6, GlovesDamage=4, RingDamage=0 (→ Damage nền = 6+4+0 = **10**);
  HelmetHealth=30, BackpackHealth=20, NecklaceHealth=0 (→ HP nền = 30+20+0 = **50**);
  AttackSpeed=1.0, MoveSpeed=2.0, AttackDistance=1.5, BaseCriticalDamagePercent=5.0.
- **ItemStatBase\*** = base main-stat mỗi MÓN đồ (= GearStatConfig): MeleeWeapon=9, RangedWeapon=7,
  GlovesDamage=6, HeadItem=45, BackItem=30, NecklaceHealth=20, RingDamage=6, TierScaler=3.1622777, LevelScaler=0.015.

Công thức: `Chỉ số cuối = PlayerBase(nền) + Σ(main stat món đang mặc)`; slot trống góp 0.

**ĐÃ IMPLEMENT (2026-09-02, compile sạch + verify runtime 10/50/1/1.5/2 trong Editor):**
- `GearStatConfigData` (Scripts/Gears): thêm 9 field `playerBase*` + method `GetPlayerBaseStats()` (trả BaseStats gộp Damage/Health). Giá trị cũng ghi vào asset `Resources/Scriptable Objects/Gears/GearStatConfig.asset` (thiếu key → Unity load 0).
- `CampaignMode.BuildHeroStats` (Scripts/GamePlay/Mode/Campaign): load config → dùng `GetPlayerBaseStats()`; đã XOÁ placeholder hero (`heroMaxHp=1000`…) khỏi `BaseMode`.
- `StatUtils.DEFAULT_MOVEMENT_SPEED` sửa 2.5 → **2.0** (gốc); nhưng CalculateMovementSpeed còn nhân ×2 kiểu StickIdle (path modifier CHƯA wire) — rà lại sau.

**CÒN LẠI (bước sau):** cộng dồn main stat đồ ĐANG MẶC lên tầng nền. Equipment (`UserEquipmentData`) hiện CHỈ lưu định danh món, chưa resolve rarity/level → chưa tính được gear main stat. Khi làm: `final = GetPlayerBaseStats() + Σ GearStatCalculator.GetGearMainStat(slot)`, post EventID.EquipmentChanged để hero reload.

Reverse pipeline: dotnet KHÔNG có sẵn máy này → cài dotnet-install.ps1 -Channel 8.0 -Runtime dotnet (user-level, no admin) + Il2CppDumper net7 (roll-forward). Giá trị serialize đọc bằng UnityPy từ APK (ghép split .assets), typetree strip → parse raw MonoBehaviour: **mỗi bool serialize chiếm 4 byte (align)**, anchor bằng chuỗi float đã biết (ItemStat 9,7,… và ArmyPowerBase=500) để định vị.
Xem [[dungonrush-reverse-native-il2cpp]], [[dungonrush-item-stats-source]], [[dungonrush-gears-data-status]].

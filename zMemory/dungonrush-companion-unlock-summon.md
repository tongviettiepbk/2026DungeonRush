---
name: dungonrush-companion-unlock-summon
description: "Reverse companion (pet) - unlock PlayerLevel 5, nguyên liệu Bone, công thức summon ad x12 / Bone 100→16 / 200→36"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 321174b3-078c-4d6a-bc7c-09b5f091f26d
  modified: 2026-09-22T08:26:24.921Z
---

Reverse từ libil2cpp v41 (Il2CppDumper + capstone) + GameplayScene.unity + save. Doc đầy đủ: DecodedData/COMPANION_MODEL.md.

**Unlock:** tab dưới gate bằng `TabData.UnlockPlayerLevel` serialize trên TabBarElementUI (GameplayScene). Companion (TabIds=5) = **PlayerLevel 5**. (Store 3, Dungeon 4, Events 15, Clan 20, Battle 0.) Cờ save `IsCompanionTabUnlocked` KHÔNG phải gate — chỉ marker popup; getter/setter `UserController.dwp/dwq` không code nào gọi trực tiếp.

**Nguyên liệu = Bone** (KHÔNG phải Gem), reward dungeon "Zombie Outbreak" (ZombieHorde). Save có field `Bone`.

**Summon (ra thẻ, cộng CardCount → lên Level; equip tối đa 3):** executor `UserController.edi(count)`. Hệ số chung = mastery `CompanionSummonCount` (SummonCapacity, type 7; default 0, unlock lvl1=60gem, Value 1..5), làm tròn banker's.
- `CompanionTabPage.guc(base) = round(SummonCapacity) + base`
- Bone nút nhỏ: 100 Bone → guc(15); nút lớn: (M×200) Bone → guc(M×35), M=wke[] multiplier (nút ChangeSummonBoneMultiplier).
- Ad (eby): `min(round(SummonCapacity)+AdBonus+11, round(SummonCapacity)+35)`, giới hạn/ngày.
- Người chơi thực (SummonCapacity=1): **ad 12, Bone 100→16, 200→36** (khớp quan sát user). Account mới (=0): 11/15/35.

**ĐÃ ĐƯA VÀO GAME DATA (2026-09-22, compile sạch):** `Companions/CompanionSummonConfig.cs` (nhúng hằng số + công thức guc/eby, kiểu StaticExperienceData), nối qua `StaticCompanionData.summonConfig`; thêm `ItemType.BONE=5` (GameEnums). CHƯA có: Bone currency trong UserData, mastery system, tab-unlock system (companion mới ở mức data+battle). Config chỉ mới là data/formula, chưa wiring runtime.

Xem [[dungonrush-reverse-native-il2cpp]] (pipeline), [[dungonrush-genre-rpg-action]], [[dungonrush-mainmap-gameplay-spec]]. Đồ nghề reverse: tools/il2cpp_reverse/ (disasm.py); xapk ở repo root.

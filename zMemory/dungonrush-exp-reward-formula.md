---
name: dungonrush-exp-reward-formula
description: Công thức exp reward mỗi màn/mỗi quái reverse từ il2cpp game gốc
metadata: 
  node_type: memory
  type: reference
  originSessionId: 93d5ec13-cae4-47da-8bc3-ea944554097d
  modified: 2026-09-06T19:29:13.197Z
---

Reverse từ `libil2cpp.so` (APK 41): **exp thắng 1 màn = `round(ExperienceLevelBase + ExperienceLevelScaler × level)` = round(49 + 1×level)** (`ExperienceController.hdm`). Mỗi quái rơi 1 phần chia đều tổng đó (`hdn(level, enemyCount)`) → clear sạch = nhận đúng tổng, độc lập số quái; **thua không cộng**.

Validate KHỚP TUYỆT ĐỐI 4 điểm liên tiếp (5-2=91, 5-3=92, 5-4=93, 5-5=94) với level=(world-1)×10+stage ⟹ **10 màn/world** (đúng như STAGES_PER_CHAPTER=10 rebuild đang để, xem [[dungonrush-map-mode-structure]]). exp phụ thuộc LEVEL màn, KHÔNG phụ thuộc số quái → khi win chỉ cộng round(49+level), ko cần gắn exp vào enemy prefab. (Điểm cũ "2-5=60" bị lệch, bỏ.)

`hdk(level)` = XP để lên level = `experience_required_per_level[clamp(level-1,0,99)]` (bảng 100 dòng, KHÔNG cộng dồn: lv1=200...+1800/level cuối). `hdl()` = curExp/hdk = % thanh exp.

**playerLevel = level popup "Rarity Table"** (vương miện Level N = forge_rarity_probabilities row N-1; xác nhận UI: Lv9=row8, Lv10=row9). Thắng campaign +exp → đầy → playerLevel++ → rarity tốt lên + thưởng (gem). CHỈ 1 con level điều khiển bảng rarity, KHÔNG phải User.ForgeLevel (đó là auto-forge/hammer khác). Rebuild ĐÃ BỎ UserCampaignData.forgeLevel; loot dùng playerLevel-1 làm index. Xem [[dungonrush-loot-forge-design]].

Doc đầy đủ: `DecodedData/EXP_MODEL.md`. Đồ nghề: [[dungonrush-reverse-native-il2cpp]] (dump cũ còn ở scratchpad 77eac08e).

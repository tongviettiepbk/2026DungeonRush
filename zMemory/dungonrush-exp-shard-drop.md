---
name: dungonrush-exp-shard-drop
description: FX mảnh EXP campaign — quái chết rơi mảnh, thắng thì mảnh bay về HERO; reverse LootDropController gốc; dựng + test Play 2026-10-16
metadata:
  type: project
---

Port phần ExperienceShard của `LootDropController` gốc (reverse il2cpp hsm/hsn/hsq/hsv/hsx/hsy, 2026-10-16):
- Quái chết → rơi Random[1..3] mảnh (ExperienceShardDropChance=1); đang boost exp → round(min(boost,5)×count). Nảy ra cung Bezier quanh xác: bán kính [R/2,R] R=1, cao 1.6–2, 0.3s OutQuad + scale OutBack 0.12s; kẹp biên map.
- Thắng → mảnh bay về **hero** (KHÔNG về UI): delay 0.8+[0..0.7], bay [0.2..0.5]s, lerp t^4; chạm hero phát `Special & Powerup (15)` (AudioConfig.LootCollectSound). Thua → mảnh bị dọn khi dựng lại màn.
- Gốc cộng exp theo TỪNG mảnh; bản rebuild giữ cộng 1 lần ở CampaignMode.CalculateResult (mảnh chỉ là FX). Reload màn chờ thêm tới khi thu xong.
- Code: Scripts/GamePlay/LootDropController.cs; prefab Prefabs/Fx/LootExpPrefab.prefab (sortingOrder -9). CampaignMode.lootDrop gắn ở CẢ scene MainGame (bản CampaignMode riêng, không phải instance prefab) LẪN Resources/Prefabs/Game Modes/CampaignMode.prefab.
- DOTween trong project KHÔNG có SetLink → dùng SetTarget + DOTween.Kill.
- Test Play OK, chưa commit. Liên quan [[dungonrush-exp-reward-formula]], [[dungonrush-campaign-flow]].

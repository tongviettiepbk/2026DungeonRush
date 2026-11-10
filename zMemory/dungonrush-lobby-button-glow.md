---
name: dungonrush-lobby-button-glow
description: "FX glow nút trái/phải lobby = Coffee UIParticle v4 + PowerupGlow1_1/MagicDust, material glow1_ADD 1 giữ guid gốc; dựng 2026-11-09"
metadata:
  node_type: memory
  type: project
  originSessionId: 63fdb03e-88c2-4136-a0c4-669f6d469788
  modified: 2026-11-09T09:26:02.427Z
---

Nút BattlePass (trái) + 6 nút trong OffersVerticalLayout (phải) của UIMainGame2 có FX glow theo gốc:
node `UIParticle` (Coffee UIParticle, Scale3D 60, AutoScalingMode=Transform) → `PowerupGlow1_1` → `MagicDust`.
- Package `com.coffee.ui-particle` v4.14.2 (git, path=Packages/src), user cho phép thêm 2026-11-09; gốc cũng dùng Coffee v4 (DLL trong rip).
- Material `Prefabs/Fx/Materials/glow1_ADD 1.mat` (UI/Default + glow1) tạo với ĐÚNG guid gốc `8e17ab246b9fe88488458a3ac850a842` → các prefab rip tự nhận.
- Glow SevenDay LUÔN bật như các nút offer (user yêu cầu 2026-11-09): đã bỏ field objReady ở SevenDayButtonView + dòng gán trong RetentionUIBuilder.
- RewardedChestUI (rương bay): đã gắn UIParticle Scale3D 20 + material `sparkle_ADD 1` (guid gốc 21ed1748…) dùng shader `UI/Additive` VIẾT LẠI ở Prefabs/Fx/Shaders (bản rip là dummy); test Play OK.
- ĐÃ SỬA 2026-11-09: `SafeArea/GameUI` trước tắt sẵn (gốc bật) nên rương bay không bao giờ hiện → bật GameUI, TẮT sẵn BossGateBossHpUI + LevelProgressParent (gốc do GameplayUI điều khiển, bản mình không script nào dùng, Boss Gate tắt). Test Play: rương tự bay qua map.

Test Play 2026-11-09: glow hiện ở vé, rương Legendary, kiếm OfferButton, rương bay. Đã commit d8bdad9e.
Liên quan: [[dungonrush-main-ui-uimaingame2]], [[assetripper-export-no-meta]]

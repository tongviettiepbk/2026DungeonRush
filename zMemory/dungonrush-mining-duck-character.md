---
name: dungonrush-mining-duck-character
description: "Nhân vật Mining = vịt NHÌN NGANG như Hero campaign (LỆCH GỐC có chủ ý, user chọn 2026-11-09) — không xoay, chỉ lật trái/phải; rig chép 00Hero + unit_all.controller"
metadata:
  node_type: memory
  type: project
  originSessionId: 9f09fe0f-3a5f-46ac-ac84-eb439e34c362
  modified: 2026-11-09T07:45:28.148Z
---

2026-11-09 user chọn "vịt nhìn ngang" thay cho đầu tròn nhìn từ trên của gốc (body.png/hand.png). Gốc xoay cả người theo hướng đi. ĐỪNG trả lại rig cũ.

- `MiningCharacter.cs` viết lại: field visualRoot/animator/helmet/glass(Wing)/pipe(Cape, ẩn khi tắt Show Cloak)/backpack/clothes(Gloves)/mineTool. Lật bằng `visualRoot.localScale.x` khi hướng có thành phần ngang. Đi lên/xuống giữ hướng cũ. Đứng UnitIdle, đi UnitRun, đập strike_atk tua ×1.333 cho vừa nhịp gốc 0.5s/nhát, hit ở 0.3125s (khớp AE_Attack). Hit/complete chạy bằng hẹn giờ, KHÔNG dùng animation event.
- `SetTool(Sprite, MiningToolType)`: cuốc xoay −45° như vũ khí cận chiến hero, khoan +90° (mũi chĩa ra trước). Đã xoá `MiningConfig.GetToolAnimator`. Các field *Animator trong config còn đó nhưng không dùng nữa.
- Rig ở prefab MiningWorld: MiningCharacter > Visual (scale 0.65) > Soldier (Animator unit_all + UnitAnimationEventRelay) > shadow + main/body (toạ độ y hệt 00Hero) + main/weapon/weapon_slot (scale 0.4). Sorting order = order của hero + 10 để luôn đè khối quặng.
- Menu riêng `Tools/DungeonRush/Build Mining/Duck Character` (MiningPrefabBuilder.RebuildCharacterInWorld) chỉ thay node nhân vật, KHÔNG đụng phần reskin, nên chạy lại được. Vẫn CẤM chạy Build Mining/All.
- Test Play 2026-11-09 OK: mặc đủ đồ, quay trái, vung cuốc, cầm khoan. Save đã khôi phục sau test. Đã commit 5df35392.

Xem [[dungonrush-mining-system]], [[dungonrush-reskin-mining]], [[dungonrush-hero-anim-unit-all]].

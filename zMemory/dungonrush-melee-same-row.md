---
name: dungonrush-melee-same-row
description: "Cận chiến chỉ đánh NGANG HÀNG ở Campaign/Zombie/Cultist — lệch gốc CÓ CHỦ Ý theo yêu cầu user, đừng \"sửa về gốc\""
metadata:
  node_type: memory
  type: project
  originSessionId: f7626801-1609-4bde-90f5-caf121d1e4f4
  modified: 2026-10-21T17:55:52.557Z
---

2026-10-22 (commit 573b004): hero/quái cầm vũ khí cận chiến phải đứng cùng hàng, sát trái/phải mục tiêu mới được đánh; đi tới ô trái/phải thay vì ô mục tiêu. Cả 2 bên là tường → đánh theo tầm thường. Pet, đánh xa, Boss Rush, PvP, Dragon giữ nguyên.

Code: `BaseMode.MeleeSameRowOnly` (+ TryGetMoveGoalCell/TryCheckMeleeRange mặc định), override true ở CampaignMode, ZombieDungeonMode, CultistDungeonMode. CHƯA test Play.

**Why:** user yêu cầu trực tiếp (thấy quái đứng dưới vẫn chém được) — KHÔNG có trong gốc, là ngoại lệ của [[dungonrush-follow-original-strict]].

**How to apply:** khi đối chiếu gốc phần tầm đánh/di chuyển ([[dungonrush-movement-original]]), giữ luật này; không gỡ trừ khi user bảo.

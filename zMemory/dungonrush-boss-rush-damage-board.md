---
name: dungonrush-boss-rush-damage-board
description: Bảng damage từng người trong trận Boss Rush (thay "Battle in progress") — dựng + test Play 2026-10-21, đã commit
metadata:
  type: project
  modified: 2026-10-21
---

Trên tấm phủ EventBlockerUI (UIMainGame2 → SafeArea/EventBlockerUI/DamageBoard), thay vòng xoay + chữ "Battle in progress" bằng 8 dòng: hạng, avatar, tên, thanh đỏ, damage. Dòng của mình tô xanh. Bảng tự sắp theo damage giảm dần, dòng trượt tới chỗ mới (DOAnchorPosY 0.25s), làm mới tối đa 5 lần/giây.
- Thanh đỏ = damage / TỔNG damage đội. Số này khớp ảnh bản gốc mới hơn mà user gửi; bản v41 (xapk) KHÔNG có bảng này. EventBlockerUI v41 chỉ có Rotate/ProgressText/GiveUp.
- Code: `UIBossRushDamageBoard` + `ElementBossRushDamageUI` (Scripts/UI/BossRush). BossRushMode.damageEntries map unit → BossRushDamageEntry: hero lấy profile.userName/avatarId, ghost lấy ally.PlayerName/AvatarId. Board.Hide() trả lại vòng xoay cho PvP (PvP dùng chung tấm phủ).
- Scene MainGame giờ LÀ prefab instance của UIMainGame2 (từ commit 5313056), nên sửa prefab là đủ. Dòng memory cũ trong [[dungonrush-boss-rush-model]] ghi "không phải instance" là đã cũ.
- Damage hiện bằng ToLetter (A=1e3, B=1e6), không phải K/M như ảnh gốc.

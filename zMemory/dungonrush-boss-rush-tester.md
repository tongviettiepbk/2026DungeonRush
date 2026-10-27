---
name: dungonrush-boss-rush-tester
description: Công cụ test từng boss Boss Rush — BossRushTester (IMGUI Editor-only, tự tạo khi Play), nút "Boss" ở MainGame, không cần server
metadata:
  type: project
---

2026-10-26: `Scripts/_Test/BossRushTester.cs` — tự tạo bằng RuntimeInitializeOnLoadMethod (KHÔNG đặt trong scene), chỉ hiện ở MainGame, nút "Boss" cạnh nút "Đồ" của [[dungonrush-equipment-tester]]. Chọn boss 1..10 (tên + skill), tier 1..10 (HP/dmg bảng gốc), 0..7 đồng đội lấy ngẫu nhiên từ `server/functions/data/botBank.json` ở mốc 0..6, bật/tắt giới hạn 30s (bật "không giới hạn" = ghi defaultBattleTime 99999 mỗi frame). Đánh = BossRushFightSession.Begin(fightToken null) + ChangeMode(BossRush); hết giờ ReportDamage chỉ log lỗi vì token null → không sinh pending report giả. Thoát = ExitMidFight. Compile sạch, CHƯA test Play (UnityMCP rớt lúc làm).

**Why:** user cần test lần lượt từng boss ([[dungonrush-boss-rush-model]]).
**How to apply:** thêm tính năng test boss thì sửa file này, đừng dựng tool mới.

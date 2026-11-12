---
name: dungonrush-bgm-ai-music
description: "Nhạc nền DungeonRush thay bằng 2 bản AI (ACE-Step 1.5 trong máy): Playful đang gán, Mystic có sẵn; cách tạo lại"
metadata:
  node_type: memory
  type: project
  originSessionId: 4cec804d-8603-4bce-96fa-74eb90edbb6a
  modified: 2026-11-12T04:46:16.574Z
---

2026-11-12: thay nhạc nền (AudioConfig.Music, 1 bài lặp cho mọi mode như gốc) bằng nhạc AI tự tạo.
- AI nhạc trong máy = ACE-Step 1.5 ở /Volumes/Work/_AISound (API 127.0.0.1:8001, bản low-mem cho M1 16GB, không LM, ~4-8 phút/bài).
- Thư mục làm việc: /Volumes/Work/_AISound/music_dungeonrush (gen.py = prompt, run.sh tự bật server, loopify.py cắt vòng lặp, level.py limiter mềm nâng RMS về -17.2 dB = bằng nhạc cũ).
- Trong Unity: Assets/_Assets/_Sound/Music/DR_BGM_Mystic.ogg (thần bí, ĐANG GÁN — user chọn 2026-11-12); Playful đã xoá khỏi project (bản gốc còn ở music_dungeonrush); meta chép setting của "Shire Evening Echoes (Edit)" (file cũ vẫn giữ).

**Why:** user muốn nhạc nhẹ nhàng hiện đại, 1 bản hơi vui nhộn, 1 bản thần bí.
**How to apply:** tạo lại/thêm bài thì sửa JOBS trong gen.py rồi chạy run.sh; nhớ pkill server sau khi xong để trả RAM. Liên quan [[dungonrush-audio-system]].

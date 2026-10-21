---
name: dungonrush-audio-system
description: "Hệ âm thanh theo gốc (AudioController/AudioConfig) dựng 2026-10-21 — AudioManager 2 source, AudioConfig.asset (UI + vũ khí + pet theo assetName), PressButtonUI (click + lún) trên 266 nút theo gốc"
metadata:
  node_type: memory
  type: project
  originSessionId: 06dda39e-823e-4024-b6d9-be3c61d898f0
  modified: 2026-10-21T12:06:01.023Z
---

Dựng + test Play 2026-10-21. Trước đó game KHÔNG kêu gì: AudioManager tạo bằng `new GameObject()` nên `sfxSource` null.

- Doc reverse: `DecodedData/AUDIO_MODEL.md` (ai gọi gì, số liệu, điều kiện).
- `AudioManager` = port AudioController: source gameplay PlayOneShot + source nhạc lặp; chặn cùng clip trong 1 frame (AudioReplayInterval=0); volume nhạc = 0.5 × slider (bám save trong Update vì nhạc có thể bắt đầu trước khi save nạp).
- Mọi clip/volume ở `Resources/Scriptable Objects/Audio/AudioConfig.asset`; âm vũ khí/pet tra theo `assetName` (WeaponData/CompanionData giữ thuần stat). Đã bỏ các field clip rải rác (ExperienceUI, UILevelPopup, ForgeUIController, LootDropController, CampaignMode, BaseMode.bgm, BaseUnit.sfxAttack, PetUnit.sfxShoot).
- Tiếng đánh/bắn chỉ hero chính (`IsMainCharacter`), tắt trong BossRushMode — đúng gốc. Pet mọi phe đều kêu.
- Click + lún nút: `UI/Common/PressButtonUI` (port đủ, trừ giữ nút — gốc đều tắt) gắn theo `UI/Common/Editor/press_buttons.json` (khớp đường dẫn nút project ↔ rip v41, kèm PressYValue gốc). Chỉ prefab runtime (Resources/Prefabs/UI, Prefabs/UINew, UIMainGame2); prefab rip nguồn Prefabs/UI/<Feature>/ KHÔNG gắn. Builder dựng lại prefab → chạy menu `Tools/DungeonRush/Apply Press Buttons`. Lún XONG + test Play 2026-10-21.
- Chưa làm: clip etfx_explosion_fireball (chỉ v59).

Xem [[dungonrush-follow-original-strict]], [[dungonrush-reverse-native-il2cpp]].

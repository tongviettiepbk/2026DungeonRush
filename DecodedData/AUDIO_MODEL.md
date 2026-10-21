# Âm thanh game gốc (AudioController / AudioConfig)

Nguồn: reverse il2cpp v59 (ARMv7) + đọc thẳng apk bằng UnityPy. Số liệu AudioConfig v41 = v59. 58 clip gốc (v41 có 57, thiếu `etfx_explosion_fireball` — chỉ dùng trong AudioSource của FX nổ v59).

## AudioController (scene GameplayScene, ControllerManager/AudioController)

- 1 AudioSource gameplay `GameplayAudioSource` phát mọi tiếng bằng `PlayOneShot(clip, volume)`; `source.volume` = slider Sound FX — `diz`
- 1 AudioSource nhạc `MusicAudioSource`: `AudioConfig.Music` lặp, volume = `MusicVolume × slider Music` — `diy` / `djb`
- Chống trùng: clip vừa phát (theo InstanceID) bị bỏ nếu phát lại trong `AudioReplayInterval` giây; scene để **0** → chỉ chặn trùng trong cùng 1 frame — `djd/djh/dji/Update`
- `djc` (fade tắt nhạc, DOFade 1s) và `dja` (cờ tắt tiếng gameplay) không ai gọi
- Nhạc bắt đầu khi game khởi tạo (`GameController.ire → GameplayUI.ntm → djb`), 1 bài cho mọi màn
- Bug gốc: `djb` đặt volume = MusicVolume (bỏ qua slider) — project nhân slider

## AudioConfig

| Field | Clip | Volume |
|---|---|---|
| ButtonClick | ui_menu_button_click_16 | 1 |
| ItemEquip | (trống) | 1 |
| ItemSell | Special & Powerup (15) | 1 |
| BoxPunch | footstep_wood_wobble_03 | 1 |
| BoxOpen | wood_tree_branch_break_03 | 1 |
| LevelUp | levelup | 2 |
| DoorOpen | rock_door_slide_block_move_drag_03 | 0.151 |
| LootCollect | Special & Powerup (15) | 1 |
| UnarmedHitAudios | punch_grit_wet_impact_01..03 | 0.239 |
| Music | Shire Evening Echoes (Edit) | 0.5 |

## Ai phát gì

- ButtonClick: `PressButtonUI.OnPointerDown` (nút còn bấm được), Loot (`ForgeUI.jwj`), Auto (`AutoForgeButton.OnPointerUp`), Boost (`BoostButton.OnPointerUp`), thanh exp (`ExperienceUI.OnLevelButtonClicked`)
- `PressButtonUI` chỉ gắn trên ~276/513 nút prefab gốc (nút hành động/đóng); ô danh sách, checkbox, mũi tên, tab, nền tối thì im
- ItemEquip / ItemSell: `NewForgePopup.kaf` (Equip) / `kai` (Sell hoặc xác nhận Yes)
- BoxPunch / BoxOpen: `ForgeUI.jxb/jxc` (hộp loot rung / vỡ)
- LevelUp: `LevelPopup.nyf`; DoorOpen: `LevelTransitionController`; LootCollect: `LootDropController.jjk`
- Mining: `MineOreConfig.HitSounds` ngẫu nhiên × HitSoundVolume (`MiningController.kvx`)

## Tiếng trận

- Chỉ hero của người chơi (`Soldier.IsMainCharacter`) và **không** trong Boss Rush (`GameController.bavy`, bật ở `isk`)
- Cận chiến trúng: `WeaponData.HitAudios` (nếu có) không thì `UnarmedHitAudios` — `MeleeAttackState.ggh`
- Bắn xa: nhả đạn kêu `ShootAudios`; đạn mang `ProjectileHitAudios` (ProjectileContext.HitAudios) kêu lúc trúng — `RangeAttackState.ggt`, `Projectile*.idt/iee/ien/ifa`
- Quái, ghost, đối thủ PvP: im

## Tiếng pet (CompanionData)

- ShootSound × ShootVolume: lúc tung chiêu — `Companion.hra → hqd`
- ProjectileHitSound: đạn pet tới nơi — `CompanionProjectile.hwt/Update`
- ProjectileImpactSound: bom/thiên thạch nổ (Bomber, AoeSlow, Meteor) — `Companion.hrj`
- EffectActiveSound: AudioSource riêng của pet, volume = EffectActiveSoundVolume × slider, lặp nếu `LoopEffectActiveSound` — `hri` bắt đầu, `hrk` tắt (hết hiệu ứng / pet despawn)
- Hiệu ứng có EffectActive: Lightning, Beam, ChainHeal, Guardian, Immortal, HealNova, Blaster
- Kéo slider Sound FX cập nhật volume tiếng lặp của mọi pet — `Companion.hrl`
- Pet mọi phe đều kêu (không kiểm IsMainCharacter)

## Project

- `Scripts/Audio/AudioManager.cs` (port AudioController), `AudioConfig.cs` + `Resources/Scriptable Objects/Audio/AudioConfig.asset` (gồm bảng âm vũ khí/pet theo assetName)
- `ButtonClickSfx` = phần âm của PressButtonUI; danh sách nút ở `Scripts/Audio/Editor/button_click_sfx.json`, gắn bằng menu `Tools/DungeonRush/Apply Button Click Sfx`

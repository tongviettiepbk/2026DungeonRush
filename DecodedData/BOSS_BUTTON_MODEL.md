# Nút Boss (BossButtonUI) + Boss Gate — reverse il2cpp v41

Nguồn: `BossButtonUI` (Awake/OnDestroy/Show/jwq..jwu), `GameplayUI.kjm`, `fc.ejv/ejw/ejx/ejy`, `BossGateController.eju`,
`User` (field 0xD8/0xE4/0xE8), `RemoteConfigController` + giá trị serialized trong `GameplayScene.unity` (TopBarUI/BossButton).

## Nút Boss thuộc tính năng Boss Gate
- Boss Gate: level campaign chia hết 10 (`fc.ejv`: `level ≥ 1 && level % 10 == 0`) là cổng boss (`BossGateConfig`, 1 bảng boss).
- `fc.ejw(level)`: là cổng boss cần đánh = `ejv(level)` && Boss Gate bật && `HighestBossGateCleared < level`.
- Thua cổng boss → lưu `LastFailedBossGateLevel`, người chơi bị lùi về level thấp hơn (LevelController / LevelTransitionController).
- Nút Boss hiện để người chơi chủ động quay lại đánh cổng boss đã thua.
- **Live v41 remote `boss_gate_enabled = false`** → `ejx` luôn false → nút KHÔNG BAO GIỜ hiện, step 22 không bao giờ chạy.

## Khi nào nút hiện (GameplayUI.kjm, gọi mỗi lần GameplayUI.OnStateChanged)
- `fc.ejy()` true → `BossButtonUI.Show()`, ngược lại `jwr()` (Hide).
- `fc.ejx()` = Boss Gate bật && `LastFailedBossGateLevel > 0` && `LastFailedBossGateLevel > HighestBossGateCleared`.
- `fc.ejy()` = `ejx()` && không ở chế độ đặc biệt (4 cờ bool của GameController 0xF8/0x100/0x101/0x140 đều false) && `Level < LastFailedBossGateLevel`.
- Boss Gate bật = `RemoteConfigController` bool 0x30 (key `boss_gate_enabled`).
- Field save: `User.Level` (0xD8), `User.HighestBossGateCleared` (0xE4), `User.LastFailedBossGateLevel` (0xE8).

## BossButtonUI
| Field | Node gốc | Giá trị |
|---|---|---|
| _canvasGroup | BossButton (ngoài) | |
| _button | BossButton (trong) | |
| _buttonRect | BossButton (trong) | |
| _glowRect | Glow | |
| _tutorialUI | BossButton/TutorialUI | |
| _buttonPulseScale | | 1.1 |
| _buttonPulseDuration | | 0.5 |
| _glowPulseScale | | 2.1581 |
| _glowPulseDuration | | 1.5 |

- Node BossButton luôn active; ẩn/hiện bằng CanvasGroup.
- Awake: `_button.onClick += jwu` → `jws()` (ẩn ngay).
- OnDestroy: `jwt()` (kill tween).
- `Show()`: đang hiện thì bỏ qua → `yll = true` → `jwt()` → CanvasGroup alpha 1, interactable, blocksRaycasts → `jwq()`.
- Show: `_buttonRect.localScale = one` → `DOScale(_buttonPulseScale, _buttonPulseDuration).SetEase(InOutSine).SetLoops(-1, Yoyo)`.
- Show: `_glowRect` tương tự với `_glowPulseScale`, `_glowPulseDuration`.
- `jwr()` (Hide public): chỉ khi `yll` → `jws()`.
- `jws()`: `yll = false` → `jwt()` → alpha 0, khoá bấm, chặn raycast tắt → 2 rect về scale one → `_tutorialUI.Hide()`.
- `jwt()`: Kill tween nút (ylj) + tween glow (ylk).
- `jwq()` (tay chỉ): `efw(22)` → `_tutorialUI.Hide()`, ngược lại `_tutorialUI.Show(1)`.

## Bấm nút (jwu)
- `!efw(22)` → `efx(22)` + analytics `co.csm(22, "boss_button_clicked")`.
- `_tutorialUI.Hide()`.
- `GameController.BossGateController.eju()`.
- `eju`: `LastFailedBossGateLevel ≥ 1` mới chạy → `GameController.StopAllCoroutines()` → `LevelTransitionController.hrm()`.
- `eju` tiếp: `GameplayUI.kjp()` → `GameplayUI.ytm = false` → `BossGateButtonUI.jwr()` (ẩn nút) → `LevelTransitionController.hro(LastFailedBossGateLevel)` (chuyển thẳng về level cổng boss).

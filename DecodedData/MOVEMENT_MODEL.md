# Di chuyển & AI hero / quái / pet — thân hàm GỐC (reverse libil2cpp v41, 2026-10-17)

> Nguồn: `StateController` + các state (`IdleState`, `MoveState`, `PatrolState`, `MeleeAttackState`, `RangeAttackState`),
> `GridCombatController`, `PhysicsMovementController`, `PathfindingManager`, `GridManager`, `Companion`.
> Giá trị serialized đọc từ `GameplayScene.unity`, `SoldierPrefab.prefab`, `Companion_*.prefab`, `GameResources`.

## 1. Khung state (`StateController`)
- vtable state: `ewu` = Enter, `ewv` = Exit, `fbq` = Update mỗi frame (gọi từ `StateController.Update`).
- Chuyển state bằng tên chuỗi (`fby("MoveState")`): Exit state cũ → Enter state mới.

## 2. Chọn mục tiêu
| Hàm | Ai dùng | Luật |
|---|---|---|
| `hor` | phe nhà (hero, `IsHome`), hoặc cờ `GameController.wtr` | đối thủ còn sống gần nhất **toàn map** (khoảng cách² theo vị trí Rigidbody) |
| `hos` | phe địch | tầm = max(`DetectionRange` > 0 ? nó : `GameResources.EnemyDetectionRange` = **4**, `AttackDistance`); gần nhất trong tầm; không có thì **giữ mục tiêu cũ nếu còn sống** |

Tầm đánh: `hot` (cận chiến) = khoảng cách tâm ≤ `AttackDistance`; `hou` (bắn xa) = trong tầm **và** `how` thấy mục tiêu
(`hoz`: Physics2D.Raycast tới mục tiêu, chỉ layer "Obstacle"; gần < 0.01 coi như thấy). Boss Rush boss: đo tới `Collider.ClosestPoint`.

## 3. IdleState.fbq
1. Chọn mục tiêu (`hor`/`hos`). Không có → nếu phe địch và `CanMove && CanPatrol` → **PatrolState**.
2. `AttackCooldown > 0` → đứng chờ.
3. Cận chiến: trong tầm → `MeleeAttackState`; không → `MoveState` nếu `CanMove`.
4. Bắn xa: trong tầm + thấy → `RangeAttackState`; trong tầm mà bị che → `MoveState`; ngoài tầm → `MoveState` nếu `CanMove`.
- Attack state chạy hết 1 nhịp (1/AttackSpeed) rồi **luôn về Idle** để xét lại.

## 4. MoveState
- Enter: lưu vị trí, đặt timer, gọi `fcf` (tìm đường).
- Update: mục tiêu chết → dừng (`htr`) → Idle. Vào vị trí đánh (`fcg` = hot / hou) → dừng, vận tốc 0 → Attack.
  - Đang có path: mục tiêu đổi ô so với ô đích → `fcf` ngay. Cứ **0.15s** xét lại mục tiêu gần nhất, đổi thì `fcf`.
    Cứ **1s** so vị trí: đi < **0.05** → kẹt → `fcf`.
  - Không có path: cứ **0.2s** `fcf`.
- `fcf`: chọn lại mục tiêu (không có → Idle); đích = ô mục tiêu (cận chiến) hoặc **ô bắn** `fch` (bắn xa);
  `htj` thất bại → thử 8 ô quanh đích (đi được, chưa có ai đứng) → vẫn không → Idle.
- `fch`: duyệt vuông bán kính max(1, floor(AttackDistance) − 1) quanh mục tiêu (cột ngoài, hàng trong), ô đi được,
  cách mục tiêu ≥ AttackDistance/2, thấy mục tiêu → chọn ô gần mục tiêu nhất; không có → ô mục tiêu.

## 5. PatrolState (quái không có mục tiêu)
- SoldierPrefab gốc `CanMove = CanPatrol = 1` ⇒ quái campaign đều đi tuần. Dungeon theme `EnemyCanMove = 0` (rồng) ⇒ `CanMove = false`.
- Enter: ô nhà = ô hiện tại, **MoveSpeed × 0.5** (Exit trả lại), chọn ô tuần `fcj`.
- `fcj`: xáo 8 ô quanh **ô nhà** (UnityEngine.Random), đi tới ô đầu tiên khác ô đang đứng và đi được; không có → đứng chờ.
- Update: thấy mục tiêu (`hos`) → MoveState. Đang chờ: hết 0.5–1.5s (Random) → `fcj`. Đang đi: frame này đi < 0.05 dồn ≥ **0.8s**
  → dừng + chờ. Hết path → chờ.

## 6. Lái theo path — `PhysicsMovementController` (scene: Force 10, Stopping 0.1, Waypoint 0.5, MaxForce 6, Rate 3, Stuck 0.05)
- Rigidbody2D Soldier: Dynamic, mass 1, linear drag 2, gravity 0, khoá xoay, nội suy, va chạm liên tục; CircleCollider r = 0.43.
- `htj(char, goal)`: start = ô hiện tại (kẹp lưới); start == goal → false; path A*; waypoint đầu bỏ nếu là ô đứng.
- `htk(dt × SpeedBoost)` mỗi frame khi GameState = Playing → `htl` từng unit:
  nhảy tới waypoint gần nhất phía trước; cách waypoint ≤ 0.5 → sang ô kế (hệ số lực 1, hết path → dừng, vận tốc 0);
  không thì: đi < 0.05 × dt → kẹt: hệ số += 3 × dt (≤ 6); `AddForce(hệ số × (10 × (hướng × MoveSpeed − vận tốc) + hướng × Random(1, 10)))`.
- `htr`: bỏ path, vận tốc 0, hệ số 1. `htm`: xoay thân (BodyTransform) về hướng đi / mục tiêu 600°/s.

## 7. A* — `PathfindingManager.ikb`
- Lân cận `GridManager.ijk(cell, true)`: 4 thẳng (0,1)(1,0)(0,−1)(−1,0) rồi 4 chéo (1,1)(1,−1)(−1,−1)(−1,1); chéo chỉ khi CẢ 2 ô kề vuông góc đi được (`ijj`).
- Chi phí: thẳng 1, chéo **1.414**; chéo đi từ ô có ô kề vuông góc là chướng ngại (`ike`) **× 3**.
- Heuristic octile `1.414 × min(Δx, Δy) + |Δx − Δy|`. Open/closed là `Dictionary`, lấy nút f nhỏ nhất (gặp trước thắng).
- Đích phải đi được. Tối đa 1000 lượt mở. Trả path gồm cả start.

## 8. Pet — `Companion`
- Data: `MoveSpeed` (2), `FollowDistance` (3, Beam = 2), `MinDistance` (1.2).
- Update: đang khựng sau ra chiêu (0.3s, `gis`) → vận tốc 0, bỏ qua. Không thì `gip` (đẩy nhau) rồi `giu` (đi theo).
- `giu`: cách chủ > FollowDistance → bật theo; cứ **0.3s** (hoặc khi chưa có path) `giv` = A* từ ô pet tới ô chủ;
  lái như mục 6 nhưng waypoint **0.6**, hệ số tối đa **3**, tăng **0.5**/s. Về ≤ FollowDistance → thôi theo (bỏ path).
- `gip`: với mọi companion khác gần hơn **1.2** (d² < 1.44, > 0.001): `AddForce(15 × (1 − d/1.2) × hướng ra xa)`.
- Pet **không đi tới quái** — kỹ năng nhắm mục tiêu riêng (gjm) và bắn từ chỗ đứng.

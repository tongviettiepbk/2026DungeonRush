---
name: dungonrush-baseunit-rig-mismatch
description: BaseUnit (port StickIdle) đòi rig FlipPoints/health-bar khác prefab thật (rig Soldier); ĐÃ xong HP bar+show damage+FlipPoints (hướng A runtime) & bake health-bar vào prefab Hero/Enemy 2026-09-01; còn Spine anim/rig thật + Pet
metadata: 
  node_type: memory
  type: project
  originSessionId: 073ec130-1fed-44b6-9da0-6248fa8f9fe1
  modified: 2026-09-01T11:02:55.993Z
---

**Blocker chặn mọi verify combat/mode LIVE của DungeonRush** (phát hiện 2026-07-30 khi test [[dungonrush-rebuild-progress]] CampaignMode ở scene `_BattlePreview`).

`BaseUnit.Awake` (port từ StickIdle, `Scripts/Unit/BaseUnit.cs` ~dòng 89–105) đòi rig:
- root: `Rigidbody2D` + `AudioSource` + `AnimationController`
- child TRỰC TIẾP tên `Body` có `Collider2D`
- node `FlipPoints` chứa `CenterBody` / `FirePoint` / `health-bar`(component `HealthBar`)

User đã đặt prefab combat ở `Assets/_Assets/Prefabs/Units/` — `Hero/00Hero.prefab`, `Enemies/00Enemy.prefab`, `Pet/00Pet.prefab` (+ `Hero/PreviewCharacter.prefab`), và đã kéo vào slot heroPrefab/petPrefab/enemyPrefab của CampaignMode (2026-07-30). Nhưng CẢ 3 vẫn là rig "Soldier"/Spine (SkeletonAnimation guid d247ba06…) — vẫn KHÔNG có FlipPoints/health-bar → blocker vẫn còn, kéo prefab chỉ hết cảnh báo "chưa gán".

Prefab nhân vật THẬT (rig "Soldier" rip từ APK) cấu trúc KHÁC HẲN:
`root(chỉ Transform) → Soldier[Animator,SortingGroup,Rigidbody2D,CircleCollider2D] → Body[chỉ SpriteRenderer + slot gear]`.
KHÔNG có `FlipPoints`/`CenterBody`/`FirePoint`/`health-bar`/`AudioSource`; collider ở `Soldier` không ở `Body`; `AnimationController` (script stub DungeonRush) không gắn trên prefab.

Grep cả project: **0 prefab có `FlipPoints` hoặc `health-bar`** → không tồn tại prefab combat-ready nào. `AddComponent<HeroUnit/EnemyUnit/PetUnit>` lên prefab này → NRE ngay ở `BaseUnit.Awake` (`transform.Find("Body")`=null) và `ActiveHpAndCollider`/`hpBar`.

**Why:** combat core được port nguyên từ StickIdle (kiến trúc [[stickidle-architecture]]) nhưng art/rig của DungeonRush là bản rip gốc, layout khác → BaseUnit không tương thích art thật.

**How to apply (2 hướng, CHỜ user chọn — user 2026-07-30 tạm hoãn "chỉ báo cáo"):**
- **A. Adapt BaseUnit sang rig thật**: sửa `BaseUnit.Awake` + phần phụ thuộc (`ActiveHpAndCollider`, `hpBar`, `firePoint`, `centerBodyPoint`, `AnimationController`) để dùng collider trên `Soldier`, bỏ hoặc tự tạo runtime các node FlipPoints/CenterBody/FirePoint/health-bar. Đụng combat core nhưng dùng được art hiện có.
- **B. Dựng prefab combat-ready**: thêm `FlipPoints`>`CenterBody`/`FirePoint`/`health-bar` + đưa `Collider2D` xuống child `Body` + `AudioSource` + `AnimationController` vào prefab cho khớp contract BaseUnit (giữ core nguyên, cần prefab HealthBar).

CampaignMode + BaseMode bản thân CHẠY ĐÚNG (dựng map/obstacle OK); chỉ chặn ở bước spawn unit. `CombatDirector` cũ (đã xóa) cũng chưa từng verify vì cùng gap này.

**ĐÃ CHỌN HƯỚNG A + code xong (2026-07-30):** user muốn "chạy cơ bản, chưa cần anim/Spine, chỉ cần action chuẩn". Đã sửa `BaseUnit.Awake` → gọi `SetupRig()` (mới) tự resolve/tạo rig runtime: thêm Rigidbody2D Kinematic ở ROOT (rig rip để Rigidbody2D+CircleCollider2D r=0.43 ở con "Soldier", gravityScale 0) + tắt `simulated` mọi Rigidbody2D con (để art theo root khi MovePosition); tự AddComponent AnimationController (stub no-op) + AudioSource; `ResolveBodyCollider()` lấy collider con "Soldier"; `ResolveRigNode()` tạo FlipPoints/CenterBody/FirePoint nếu thiếu; health-bar vẫn optional. AnimationController & HealthBar vốn đã là stub no-op nên không NRE. **dotnet build Assembly-CSharp.csproj = 0 error.** Tags TeamA/TeamB ĐÃ có trong TagManager.asset.

**CHƯA verify Play-mode**: Unity Editor MCP bridge KHÔNG kết nối (refresh_unity/instances = "Unable to connect"). Cần user mở Unity (bật MCP bridge) để tôi vào Play, hoặc user tự Play scene `_BattlePreview` rồi báo console. Khi port Spine thật thì thay SetupRig bằng rig/anim/health-bar thật (các TODO(follow-stick) đã cắm trong code).

---

**ĐÃ GIẢI QUYẾT phần HP bar + show damage + FlipPoints (2026-09-01):** user xác nhận "chạy rồi".
- **HealthBar.cs**: thay stub rỗng bằng bản thật StickIdle (2 lớp `hp`+`visual` trượt, màu theo team, ẩn khi knock/hex). CombatText.cs vốn đã port đủ.
- **Asset copy từ StickIdle (giữ guid)**: 3 sprite HP (`_Assets/Graphics/Ingame/hp-unit-*`), `health-bar.prefab` + `combat-text.prefab` đặt ở `_Assets/Resources/Prefabs/`; 3 anim clip CombatText, 2 TMP sprite-asset (ic-crit/ic-ignore-def), font Playground SDF. Rewrite guid script HealthBar trong prefab sang guid DR (a851cc7f...).
- **Sorting layers**: thêm 7 layer (Ground/Unit/Projectile/Fx/GameOverlay/UI/PopupOverlay) vào `TagManager.asset` (trước chỉ có Default) để `"UI"` resolve.
- **Nối code**: `FxController.Awake` → `Resources.Load<CombatText>("Prefabs/combat-text")` khi prefabTextDamage null (Singleton runtime, không có scene gán). `BaseUnit.SetupRig` → nếu `flipPoints.Find("health-bar")==null` thì `Resources.Load<HealthBar>("Prefabs/health-bar")` + Instantiate dưới FlipPoints (offset `HP_BAR_LOCAL_OFFSET` y=1.1, worldPositionStays:false, scale reset 1).
- **BAKE vào prefab (2026-09-01, UnityMCP đã kết nối lại)**: user muốn health-bar nằm sẵn trong prefab Hero/Enemy. Đã dùng `manage_prefabs modify_contents` thêm node `FlipPoints` (con trực tiếp root) → `health-bar` (nested prefab instance của health-bar.prefab, guid cebf8839...) tại y=1.1 vào `00Hero.prefab` + `00Enemy.prefab`. Cấu trúc: `root → FlipPoints → health-bar(HealthBar) → background/visual/hp`. SetupRig giờ TÌM THẤY sẵn → KHÔNG instantiate runtime cho Hero/Enemy; fallback runtime vẫn giữ cho Pet (chưa bake). CenterBody/FirePoint vẫn tạo runtime (chưa bake, user chưa yêu cầu). Console 0 error. **Vẫn chưa Play-mode verify lần bake (user tự test).**
- Lưu ý env: máy KHÔNG có dotnet/MSBuild/Unity trên PATH → không compile-check bằng CLI được trong phiên; dựa vào Unity tự recompile.

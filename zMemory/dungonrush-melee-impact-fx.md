---
name: dungonrush-melee-impact-fx
description: FX trúng đòn cận chiến = hero_impact_101 (hero default Pom của StickIdle), nạp Resources ở FxController.fxImpactMelee, spawn tại pivot (giữa thân) nạn nhân
metadata:
  type: project
---
2026-10-28 (user yêu cầu, lấy từ StickIdle): FX impact của hero mặc định StickIdle (Pom = hero-01 → `hero_impact_101`) gắn vào MỌI đòn đánh gần.
- Prefab: `Resources/Prefabs/Fx/hero_impact_101.prefab` (guid giữ nguyên StickIdle); mat+texture ở `Prefabs/Fx/Combat/HeroImpact101/`. Renderer đã chuyển layer Fx → Default order 41–46 (theo [[dungonrush-world-sorting-under-ui]]). Shader built-in legacy chạy được trên URP.
- `FxController.fxImpactMelee` tự Resources.Load trong Awake (FxController tạo runtime, không có trong scene).
- `BaseUnit.ReleaseAttack` nhánh cận chiến: `impactAttack ?? fxImpactMelee` → `TakeAttack` → `ShowImpactHit` tại **Pivot** (transform.position) của target — pivot mọi unit = tâm body_slot (lệch ≤0.1), còn CenterBody cao hơn 0.5 (lệch lên đầu, user báo sai 2026-10-28). KHÔNG gán vào `impactAttack` prefab vì BaseBullet cũng dùng field đó (đòn xa sẽ hiện theo).
- Pet ability / skill boss Boss Rush không đổi. Test Play OK (hiển thị đúng), đã commit 50c0c78.
- **Đánh xa (2026-10-28, user yêu cầu bản nhẹ hơn):** `Resources/Prefabs/Fx/hero_impact_ranged.prefab` = bản sao hero_impact_101 chỉnh nhỏ + **xanh dương nhạt** (startColor 0.6,0.82,1; α glow 0.6 / star 0.7 / circle 0.3 / spark 0.7; gradient trắng→trắng, tia lửa →xanh 0.35,0.62,1 thay cam; size glow 1.0, 1 star 0.7–0.9, spark1 4–6 tia chậm 2–6, circle 3.6, lifeTime 1). Material DÙNG CHUNG với 101 → chỉ chỉnh màu hạt, không sửa material. `FxController.fxImpactRanged`; `BaseBullet.OnTargetTakeDamage` gọi `target.ShowImpactHit(impactAttack ?? fxImpactRanged, Pivot)`. CompanionBullet (pet) không đổi. Test Play: đạn thật trúng đúng pivot.

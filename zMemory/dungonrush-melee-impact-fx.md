---
name: dungonrush-melee-impact-fx
description: FX trúng đòn cận chiến = hero_impact_101 (hero default Pom của StickIdle), nạp Resources ở FxController.fxImpactMelee, spawn tại centerBodyPoint nạn nhân
metadata:
  type: project
---
2026-10-28 (user yêu cầu, lấy từ StickIdle): FX impact của hero mặc định StickIdle (Pom = hero-01 → `hero_impact_101`) gắn vào MỌI đòn đánh gần.
- Prefab: `Resources/Prefabs/Fx/hero_impact_101.prefab` (guid giữ nguyên StickIdle); mat+texture ở `Prefabs/Fx/Combat/HeroImpact101/`. Renderer đã chuyển layer Fx → Default order 41–46 (theo [[dungonrush-world-sorting-under-ui]]). Shader built-in legacy chạy được trên URP.
- `FxController.fxImpactMelee` tự Resources.Load trong Awake (FxController tạo runtime, không có trong scene).
- `BaseUnit.ReleaseAttack` nhánh cận chiến: `impactAttack ?? fxImpactMelee` → `TakeAttack` → `ShowImpactHit` tại `centerBodyPoint` của target. KHÔNG gán vào `impactAttack` prefab vì BaseBullet cũng dùng field đó (đòn xa sẽ hiện theo).
- Pet ability / skill boss Boss Rush không đổi. Test Play OK (hiển thị đúng), đã commit 50c0c78.

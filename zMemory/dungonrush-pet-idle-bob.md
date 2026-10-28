---
name: dungonrush-pet-idle-bob
description: Pet luôn nhún idle như hero — PetAnimationController nhún node Icon bằng code theo đường cong UnitIdle; đã commit ce03204 (2026-10-28)
metadata:
  type: project
---

Pet không có Animator (chỉ 1 sprite node `Icon`) → `Scripts/Unit/PetAnimationController.cs` nhún bằng code theo clip hero `Animations/HeroesNew/UnitIdle.anim` (scale Y 0.9→1.1@0.517s→0.9@1s, X giữ), bù localPosition để chân (mép dưới sprite) đứng yên. Chạy LIÊN TỤC mọi state; Pause/Freeze thì dừng. `PetUnit.Awake` tự AddComponent trước base.Awake (không gắn trên prefab).

3 prefab Heal_Chain/Blaster/Meteor trước để SpriteRenderer trên root → đã tách ra con `Icon` (collider/particle không bị co giãn). Mọi prefab pet giờ đều có con `Icon`.

**Why:** user muốn pet "nhún nhún như hero" (2026-10-28). Test Play OK, đã commit ce03204.
**How to apply:** prefab pet mới phải có con tên `Icon` chứa sprite. Các pet nhún cùng nhịp (chưa lệch pha) — chỉ thêm nếu user muốn. Liên quan [[dungonrush-hero-anim-unit-all]].

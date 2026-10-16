---
name: dungonrush-main-ui-uimaingame2
description: UI lobby chính = UIMainGame2.prefab (bản gốc rip) đã gắn UIMainLobby; UiMainGame (bản user tự dựng) đã bỏ khỏi scene MainGame
metadata:
  node_type: memory
  type: project
  originSessionId: a7a13290-8879-4f3d-a10b-9bf6dc65113c
  modified: 2026-10-15T10:06:58.491Z
---

Từ 2026-10-15, UI lobby chính là `Prefabs/UI/UIMainGame2.prefab` (layout gốc rip). User chủ động bỏ UiMainGame tự dựng.
- 7 page (Shop/Companion/Dungeon/Event/Clan/Enchantment/Wing) chuyển nguyên sang `SafeArea/Pages`.
- UIMainLobby gắn ở root; ô pet = `CompanionUI/Parent/Skill1..3` (ElementPetUILobby); ô đồ = `InventoryUI/Item_*`
  (ElementEquipmentUILobby, imgEquipment = PlaceholderImage, Item_Cloak = CAPE); tab = 5 `*TabBarElementUI`
  (Store=Shop, Skills=Pet, PvP=Event), objSelect = CloseButton (đã tắt Button riêng để bấm đi lên tab).
- Loot/AutoForge/Boost gốc mất Button khi rip → đã gắn Button mới ở root mỗi nút.
- Thanh XP + máu boss gốc là Slider → code đã đổi: `UIMainLobby.sliderProcessPlayer`, `UIBossRushHud.sliderHp/sliderWhite`.
- Đã ẩn node chưa có logic: GameUI, Battlepass/Offline/Leaderboard, OffersVerticalLayout, BossButton, mọi TutorialUI*/NotificationUI*.
- UiMainGame.prefab ĐÃ XOÁ (2026-10-15); Test.unity + _Recovery/0 (3).unity đã gỡ instance → GameController.uiLobby ở 2 scene đó = null.
- TopBarUI/LevelText (txtLevelMap) theo gốc 2026-10-16: campaign = `StaticCampaignData.GetLevelText(level)` "<color>Tier</color> a-b" (8 Tier/100 level, GameplayUI.kii); dungeon = "<sprite=0><color=KeyColor>Tên</color>\nDifficulty a-b" (kig, icon IsBox/IsBone/IsVial ở `dungeonRewardSpriteAssets`). BossRush title/tên tab chưa làm.

**Why:** user muốn dùng UI gốc thay bản tự dựng.
**How to apply:** sửa UI lobby thì sửa UIMainGame2, đừng dựng lại Loot/Footer tự chế. Xem [[dungonrush-uinew-prefab-folder]].

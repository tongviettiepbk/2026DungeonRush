---
name: dungonrush-uinew-prefab-folder
description: Quy ước thư mục prefab UI (user đặt 2026-10-11) — Prefabs/UINew/<TínhNăng>/ = prefab dùng bằng tham chiếu trực tiếp, KHÔNG load qua Resources
metadata:
  type: project
---
Ba nơi chứa prefab UI, mỗi nơi một vai trò:
- `Assets/_Assets/Resources/Prefabs/UI/` — popup/trang nạp bằng `UIManager.LoadUI` / `Resources.Load`.
- `Assets/_Assets/Prefabs/UINew/<TínhNăng>/` — prefab ĐANG DÙNG bằng tham chiếu trực tiếp (scene MainGame/Root, hoặc prefab trong Resources trỏ tới): element, slot, tab menu, UIManager. Mỗi tính năng 1 thư mục có tên (Cape, Clan, Enchentment [user gõ vậy, giữ nguyên], Gear, Mastery, Pet, Shop, UIMainGame/{ElementGear,ElementPet,TabMenu}, UIManager, Wing).
- `Assets/_Assets/Prefabs/UI/<TínhNăng>/` — prefab rip layout-only làm NGUỒN cho builder (không dùng lúc chạy).

**Why:** user đang dọn lại thư mục, tự chuyển UIMainGame/Cape/Enchentment/Wing rồi nhờ kiểm phần còn sót; 2026-10-11 đã chuyển nốt 9 prefab (Substat, MasteryCard, CompanionPageElement, 5 thẻ Shop, UIManager) → kiểm lại còn 0.

**How to apply:** prefab UI mới mà được kéo thả/tham chiếu trực tiếp thì tạo ở `UINew/<TínhNăng>/`, không để ở `Prefabs/UI`. Builder sinh prefab được tham chiếu thật phải xuất vào UINew (ShopPrefabBuilder.BUILT đã đổi sang `Prefabs/UINew/Shop`). Chuyển prefab bằng `AssetDatabase.MoveAsset` qua UnityMCP để giữ guid. Cách kiểm: dựng đồ thị guid từ 3 scene trong build + mọi asset dưới Resources, liệt kê prefab reachable nằm ngoài Resources và ngoài UINew (`UiMainGame.prefab`/`UIMainGame2.prefab` ở Prefabs/UI không ai dùng).

**Clan (2026-10-11):** 12 thẻ Clan/ClanWar + TabSelectorItemUI đã chuyển sang `UINew/Clan` và popup/ClanTabPage TRỎ THẲNG prefab thẻ (trước đó builder nhúng bản sao làm template ẩn). ClanPrefabBuilder.BUILT = `Prefabs/UINew/Clan`; đã bỏ 14 dòng `XxxPrefab.gameObject.SetActive(false)` trong UI/Clan + UI/ClanWar — ĐỪNG thêm lại (sẽ tắt luôn prefab asset). `Prefabs/UI/Clan/Built` và `Prefabs/UI/Shop/Built` đã xoá.

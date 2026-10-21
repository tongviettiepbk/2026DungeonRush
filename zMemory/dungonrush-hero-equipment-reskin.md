---
name: dungonrush-hero-equipment-reskin
description: Reskin đồ hero (concept vịt) — ảnh gốc 8 slot đã gom về Art/3_Equipment/; brief art ở GDD/ART_HERO_EQUIPMENT.md; mapping slot cũ→đồ mới
metadata:
  node_type: memory
  type: project
  originSessionId: 80f48afc-66cc-4da6-b804-ba63ea63437d
  modified: 2026-10-15T04:20:08.320Z
---

2026-10-15: gom ảnh icon của 241 món (8 slot) + 8 icon ô trống về `_Assets/Art/3_Equipment/` (00_EmptySlot, 01_Weapon/Melee|Ranged, 02_Helmet … 08_Wing) bằng `git mv` kèm .meta → GUID giữ nguyên, SO vẫn trỏ đúng. Thư mục cũ `BGGearEmpty/`, `Items/Ring/` đã xoá; `GearIcons/` còn 22 ảnh không SO nào dùng, `WeaponIcons/` còn 1.

Brief cho đội art: `GDD/ART_HERO_EQUIPMENT.md` + `.html` (ảnh nhúng base64, gửi 1 file), ảnh bảng xem trước ở `GDD/img/hero_equipment/`.

Mapping slot cũ → đồ mới (suy từ tên node prefab 00Hero, user chưa xác nhận bằng lời): Weapon→dao, Helmet→mũ, Gloves→áo (node Hand_Clothes), Backpack→balo, Wing→kính (Wing_Glass), Cape→tẩu (Cap_Pipes); Ring/Necklace chưa có concept. Art mới ở `ArtIncoming/_Concept/Items/<loại>/<loại>-LXX-A.png (L01-A đang dùng ở Art/3_Equipment/OnBody/)` (6 loại × 10 tier, mới mẫu A).

**Why:** user chuẩn bị gửi đội art vẽ lại toàn bộ đồ hero.

**How to apply:** tool cũ `tools/gen_gear_assets.py`, `gen_cape_wing_assets.py`, `attach_weapon_icons.py` vẫn trỏ ICON_DIR cũ (GearIcons/WeaponIcons) — chạy lại phải sửa đường dẫn. Khi art giao: gán vào field `bodySprite` (ảnh gắn người) / `icon` của SO, xem [[dungonrush-equipment-system]]. Chưa chốt: vũ khí tầm xa, nhẫn/dây chuyền, Cape 6 tier vs tẩu 10 tier.

**2026-10-21 Necklace XONG, commit fe2ed55:** art giao ở `/Volumes/Work/_AssetReskin/DungeonRush/Necklace/` (30 ảnh tên "Layer 7xx"/"exec-…", 3 dáng × 10 màu). Phân tier theo MÀU bảng Rarity Table (0 Common xám, 1 Uncommon be, 2 Rare xanh lá non, 3 Epic xanh lơ, 4 Legendary vàng, 5 Mythic tím, 6 Artifact xanh ngọc, 7 Ancient xanh dương đậm, 8 Immortal đỏ, 9 Divine cam). File `Neckle_X_Y` = rarity X-1 (SO `rarity` field xác nhận), tier 9/10 tên `tier_0X_necklace_0Y`. Dáng: tim=_1, trứng xoáy=_2, nanh trăng=_3 (Claude tự chọn, user chưa chốt). Canvas đồng nhất 103×110 (tỉ lệ 134×143), không phóng ảnh, PPU 76.87 cho cả 30 → ArtIncoming → `ArtReskinTool.Run(true)` (GUID giữ). Slot khác làm y vậy: đếm màu, map theo bảng rarity, canvas chung.

**2026-10-21 Ring XONG, đã commit "Reskin Ring":** art ở `/Volumes/Work/_AssetReskin/DungeonRush/Ring/`, 3 dáng: nhẫn đá=_1, hoa văn=_2, trơn=_3; Common = `ring-concept-03…` (đá xám) + 2 file `exec-…` xám. Bộ Ring KHÔNG có xanh ngọc đậm → Artifact dùng xanh MINT (hue ~170, khác Epic xanh lơ ~185). Canvas vuông 111×111, PPU 88.8 cả 30. Script ghép ở scratchpad (bảng tên→tier viết tay) — slot sau viết lại theo mẫu này.

**2026-10-21 Melee Weapon XONG, đã commit "Reskin Melee": art ở `/Volumes/Work/_AssetReskin/DungeonRush/Melee Weapon/`, 3 dáng: dao ngang=_1, chuỳ gai=_2, rìu=_3 (tên data cũ Pitchfork/Sickle… KHÔNG đổi). Vũ khí KHÔNG có `bodySprite` → icon chính là hình CẦM TRÊN TAY hero (`HeroVisual.weapon` = node weapon_slot, mặc định `OnBody/weapons-L01-A` dao ngang 83px@PPU100). Canvas vuông 103×103 (tâm giữ nguyên), PPU 124 để dao mới cầm bằng cỡ dao mặc định. Chuỳ/rìu đứng dọc, pivot giữa → đầu chuỳ nằm ngang bụng; dời pivot xuống cán: user nói SẼ TỰ TINH CHỈNH sau, đừng tự làm.

**2026-10-21 Ranged Weapon XONG, đã commit "Reskin Ranged":** art ở `/Volumes/Work/_AssetReskin/DungeonRush/Range Weapon/`; vũ khí xa chỉ 29 món (Uncommon có 2: `Weapon_r_2_1/2_2`, KHÔNG có 2_3). Dáng: nỏ ("Layer 6xx copy")=_1, gậy phép=_2, giáo=_3. Art thiếu giáo CAM (Divine) → `Weapon_r_10_3` = giáo đỏ (Layer 686) xoay hue sang cam ×1.3 bão hoà; giáo be (Layer 693) dư, không dùng. Canvas vuông 150×150, PPU 124 (cùng tỉ lệ pixel bộ Melee). Món dọc cầm ở giữa như Melee — user tự tinh chỉnh.

**2026-10-21 Wing → Kính XONG, đã commit "Reskin Wing":** art ở `/Volumes/Work/_AssetReskin/DungeonRush/Glasses/` (10 kính, 1 dáng × 10 màu); `Wing_N` = rarity N-1 (SO Wings xác nhận). Không có `bodySprite` → icon là kính đeo trên mặt (node `Wing_Glass`, mặc định `OnBody/glasses-L01-A` 52px@PPU100). Canvas 120×44, PPU 230.77 để kính mới đeo vừa đúng chỗ kính mặc định (đã chụp kiểm, khít). Mọi ô UI icon (lobby slot, WingElementUI, Enchantment) preserveAspect=true → không méo.

**2026-10-21 Cape → Tẩu XONG, đã commit "Reskin Cape":** art ở `/Volumes/Work/_AssetReskin/DungeonRush/Pipe/` (12 tẩu = 6 màu × 2 dáng). Cape chỉ 6 tier (rarity 0..5, Common→Mythic) × 2 món: rarity r → `(2r+1)-Cape` dáng chữ J, `(2r+2)-Cape` dáng cong (chốt câu hỏi "Cape 6 tier vs tẩu 10 tier": tẩu cũng 6 tier). Không có `bodySprite` → icon là tẩu ngậm trên mỏ (node `Cape_Pipes`, mặc định `OnBody/pipes-L01-A` 40px@PPU100). Canvas 123×91, PPU 290 (tẩu mới cỡ bằng tẩu mặc định; đã chụp kiểm — vị trí hơi thấp, để user tự tinh chỉnh như vũ khí).

**2026-10-21 Gloves → Áo XONG, đã commit "Reskin Gloves" (kèm đổi tên thư mục):** user đổi tên thư mục `03_Gloves` → `03_Gloves_clothes` (GUID giữ). Art ở `/Volumes/Work/_AssetReskin/DungeonRush/Áo/`, tên có sẵn `<dáng>-Lxx` với Lxx = tier (màu đúng thứ tự bảng Rarity). `Gloves_<tier>_<n>`: ba lỗ (tank-top)=_1, hoodie=_2, vest=_3, áo giáp (army)=_4. Không có `bodySprite` → icon thay sprite tay/áo `Hand_Clothes` (mặc định `OnBody/shirts-L01-A` 68px@PPU100, cởi thì trả lại). Canvas 120×96, PPU 176.5; đã chụp kiểm, mặc vừa thân.

**Lỗi chờ user tự xử lý (2026-10-21):** màn Đào mỏ `MiningCharacter.ApplyEquipment` gắn sprite Gloves lên CẢ 2 bàn tay (glovesRendererLeft/Right) → sau reskin Gloves thành áo, tay nhân vật đào mỏ hiện 2 cái áo. User nói "để sau, tôi xử lý" — đừng tự sửa. Mũ/ba lô ở màn đó cũng lấy icon theo cách này → reskin Helmet/Backpack phải xem lại.

**2026-10-21 Helmet → Mũ XONG, đã commit "Reskin Helmet":** art ở `/Volumes/Work/_AssetReskin/DungeonRush/Helmet/`, tên `Hat01..10` + `<dáng>-L01..L10` = tier (màu đúng bảng Rarity). `Helmet_<tier>_<n>`: lưỡi trai (Hat)=_1, mũ cánh (winged-cap)=_2, mũ giáp (combat-helmet)=_3, phù thuỷ (wizard-revised)=_4. Art giao CÙNG khung 150×208 đã căn chung → cắt 1 cửa sổ chung 164×100 tâm = tâm mũ lưỡi trai (giữ vị trí tương đối các dáng). Không có `bodySprite` → icon đội trên đầu (node `HelmetRenderer`, mặc định `OnBody/hats-L01-A` 62px@PPU100). PPU 167.7; đã chụp kiểm, 4 dáng đội vừa đầu.

**2026-10-21 Backpack → Balo XONG, đã commit "Reskin Backpack":** art ở `/Volumes/Work/_AssetReskin/BackPack/` (KHÔNG nằm trong thư mục DungeonRush), `rabbit|rocket-backpack-Lxx` = tier. `Backpack_<tier>_<n>`: thỏ=_1, tên lửa=_2. Cắt sát hình, đặt giữa khung 94×120, PPU 184.6 (cao bằng balo mặc định `OnBody/backpacks-L01-A` 65px@PPU100). Balo mới hẹp hơn mặc định → pivot giữa bị thân che → đặt **pivot Custom** để mép trái balo mới trùng mép trái balo mặc định (thỏ x=0.696, tên lửa x=0.589, y=0.5); pivot không ảnh hưởng icon UI. Đã chụp kiểm.

**HOÀN TẤT reskin 9 slot đồ hero 2026-10-21.** Bộ đồ nghề chung: script ghép ở scratchpad theo mẫu (bảng tên→tier, khung chung, đo màu hue), Apply bằng `ArtReskinTool.Run` qua reflection, đặt PPU theo sprite mặc định `OnBody/*-L01-A` trên node HeroVisual tương ứng, chụp kiểm bằng preview scene + 00Hero.

**2026-10-21 BẬT hiển thị đồ lên hero:** bỏ cờ tạm `HeroVisual.skipLoadVisual` + bật lại listener `EquipmentChanged` trong HeroVisual (đổi đồ là đổi hình ngay). User đã bọc mỗi node đồ trong 1 node cha (SR nằm ở con: `HelmetRenderer/Helmet`, `Wing_Glass/Wing_Glass`, `Cape_Pipes/Cape_Pipes (1)`, `BackpackRenderer/BackpackRenderer (1)`, `Hand_Clothes/Hand_Clothes (1)`, `body_slot/body_slot`) và đặt sprite mặc định prefab = món tier 1; ref HeroVisual đã trỏ đúng SR con; anim unit_all chỉ bind `main`, `main/body`, `main/weapon/*` nên không vỡ. Đã test Play: hero lấy đúng đồ từ save, slot trống ẩn (áo trống = sprite mặc định prefab `Gloves_1_1`).

**2026-10-22 đổi luật hiển thị khi TRỐNG slot (user chốt, KHÁC gốc có chủ ý):** gốc trống găng = sprite `hand` (bàn tay trần) và không có vũ khí mặc định. Giờ: (1) áo/tay (`Hand_Clothes (1)`) trống thì **TẮT** (ApplySlot như slot thường, đã bỏ cache defaultHand; prefab 00Hero + 00Enemy để node này inactive); (2) vũ khí trống thì cầm **`Art/3_Equipment/ZDefaultWeapon.png`** (dao, PPU 124) — field `HeroVisual.defaultWeapon` + `EnemyUnit.defaultWeaponSprite` (quái campaign tay không L1-4, qua ShowWeapon). Chỉ là HÌNH, combat vẫn tay không. Biến thể dungeon (Zombie/Cultist/Dragon) không qua ShowWeapon → ô vũ khí prefab để trống như cũ.

**Quái chỉ hiện đồ có trong data (2026-10-22):** 00Enemy copy rig hero nên mang sẵn mũ/kính/tẩu/balo. `EnemyUnit.HideUnwornGear` (gọi trong SpawnEnemy): kính/tẩu/balo LUÔN tắt; campaign (overrideWeapon) tắt thêm mũ + áo (gốc huy → fbb(weapon,null,null,...)); dungeon giữ mũ/găng theme trên prefab variant. Node áo 00Enemy để bật (variant cần). **LỖI CHƯA SỬA:** override sprite đầu/tay zombie, đồ cultist, cánh rồng trong 4 variant đã MỒ CÔI từ bản commit (đích fileID không còn trong 00Enemy — nghi do đợt bọc node đồ vào node cha) → variant đang hiện Helmet_1_1/Gloves_1_1.

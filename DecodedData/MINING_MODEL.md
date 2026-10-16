# Mining (Đào mỏ) — nguồn quặng cho Wing (reverse il2cpp v41)

Nguồn: `MiningController`, `MiningGrid`, `MiningCharacter`, `MiningCamera`, `MiningBlock`, `st` (A*), `MiningPopup`,
`MiningRarityPopup`, `MiningConfig` (izn..jab), `MiningData` (jct/jcu), `UserController.ecz`, `WingCraftTabPage.kzj`
trong libil2cpp.so + giá trị asset `MiningConfig` / `MiningWorld` đọc thẳng từ APK (UnityPy).

## Mở
- Nút **Mining** ở trang Wing (`WingCraftTabPage.MiningButton`) → `MiningPopup`. Trang Wing mở khoá PlayerLevel 6.
- Chấm đỏ nút (kzj): chưa vào mỏ lần nào (`MiningData.Seed == 0`) hoặc số cuốc (tính cả phần hồi) ≥ trần.

## Config gốc
| Field | Giá trị |
|---|---|
| GridWidth / VisibleRows / BaselineRow | 9 / 9 / 8 |
| CellSize / BlockScale / EmptinessThreshold | 1 / 1.09 / 0.4 |
| PickaxeRegenIntervalMinutes / MaxPickaxeCount / StartingPickaxeCount | 5 / 30 / 30 |
| DrillDepth / GoldenPickaxeRows | 8 / 5 |
| Pickaxe / Drill / GoldenPickaxe AdRewardCount | 10 / 1 / 1 |
| Daily*AdLimit (cả 3) | 6 |
| CharacterMoveSpeed / PowerUpMoveSpeed / CameraScrollSpeed | 4 / 8 / 3 |
| DrillGemCost / GoldenPickaxeGemCost | 50 / 100 (client v41 KHÔNG dùng) |
- DepthTiers: lv1–20 = 50 m/level, lv21–30 = 100, lv31–40 = 150, lv41+ = 200. Level tối đa = 60 (MinMineLevel lớn nhất).
  ⇒ mốc: lv11 = 500 m, lv21 = 1000 m, lv31 = 2000 m, lv41 = 3500 m, lv51 = 5500 m.
- RarityLevels: 60 bảng trọng số (MinMineLevel 1..60), thứ tự Dirt, Stone, Coal, Iron, Ruby, Emerald, Gold, Diamond
  (đủ số ở `tables/MiningConfig.json`). VD lv1 Dirt 79.21 / Stone 19.8 / Coal 0.99; lv60 Dirt 38.72 … Diamond 6.45.
- OreConfigs: mọi quặng HitsRequired = 1; Dirt **không** nhặt được, còn lại nhặt được. Sprite khối: dirt, coal_mine,
  emerald_mine, gold_mine, diamond_mine, iron_mine, ruby_mine, stone_obstacle. Hạt `<Ore>Particle`.
  Tiếng: Dirt = footstep_dirt_walk_run_06..08, quặng = pick_axe_stone_small_hit_mine_impact_01..03.
- Dụng cụ cầm tay: pickaxe_character / drill_character / gold_pickaxe_character; cả 3 dùng `Weapon_Hammer_AnimatorController`.
- MiningOfferTiers: `lootio.mining_offer` (Drill 17, Golden 17, Pickaxe 55, Gem 600, +10 trần cuốc),
  `lootio.mining_offer_large` (100 / 100 / 330 / 3200, +20). Gói kế = gói thứ `MiningOfferPurchasedCount`; mua hết thì ẩn nút.

## Save (`MiningData`)
Seed, CurrentDepth, MineLevel, CharacterGridX/Y, MinedPositions, DamagedBlocks, Pickaxe/Drill/GoldenPickaxe,
LastPickaxeRegenTime, Pickaxe/Drill/GoldenPickaxeAdDailyCount, LastMiningAdResetTime, 8 bộ đếm quặng.

## Sinh map (MiningGrid.jdd/jde) — không lưu map, chỉ lưu ô đã đào
- Ô (x cột, y hàng; y tăng = xuống). Hàng 0 luôn trống.
- Rỗng nếu `EmptinessThreshold > new System.Random(x·0x127409F ^ y·0x4F9FFB7 ^ seed·0x466F45D).NextDouble()`.
- Không rỗng: `r = new System.Random(x·0x1A5D215 ^ y·0x2752387 ^ seed·0x359AF13).NextDouble() × tổng trọng số`;
  quặng = phần tử đầu tiên có tổng dồn > r (bảng theo level mỏ hiện tại).
- Sinh hàng depth .. depth+VisibleRows+2; hàng trôi lên trên bị quên (không quay lại được).
- Lần đầu vào (Seed = 0): Seed = Random.Range(1, int.MaxValue), mốc hồi cuốc = bây giờ, +StartingPickaxeCount cuốc.

## Đi lại (st)
- A* 8 hướng, chéo phải trống cả 2 ô cạnh, chi phí chéo 1.414, heuristic (dx+dy) + (1.414−2)·min(dx,dy), ≤ ~500 vòng.
- Đích = 1 trong 8 ô quanh khối được bấm (chọn đường ngắn nhất). Ô đi được = trong vùng nhìn thấy
  (hàng depth .. depth+8) và đã đào/vốn rỗng.

## Thao tác (MiningController.jbm)
- Bận (State ≠ Idle) → bỏ qua. Bấm hàng ≥ depth + VisibleRows − 1 → toast `Errors.Mining.TooDeep`.
- **Cuốc**: có khối → hết cuốc → `NoPickaxes`; không có đường → `NoPath`; trừ 1 cuốc → đi theo đường (4 ô/s, mỗi ô
  quay mặt) → quay về khối (tween 0.15s OutQuad) → vung (animation event hoặc dự phòng 0.5s hit + 0.3s xong) → đập 1 nhát.
- **Khoan**: như trên (trừ 1 khoan), rồi khoan thẳng xuống 8 ô bắt đầu từ khối bấm: phá + nhặt từng ô, nhân vật chạy
  vào ô (8 ô/s), cập nhật độ sâu sau mỗi ô.
- **Cuốc vàng**: trừ 1, rồi 5 hàng bắt đầu từ hàng khối bấm: hàng còn khối thì quay về khối (ưu tiên cột bấm) và vung
  1 nhát phá SẠCH cả hàng; rồi chạy xuống ô (cột bấm, hàng đó).
- Vỡ khối nhặt được → +1 quặng (`ecz`, ClanWar ghi `mining`), sự kiện OreMined.

## Độ sâu (jbw/jbx)
- Phá ở hàng `row ≥ depth + BaselineRow − 1` → `depth = row − BaselineRow + 2`.
- Rồi lặp (≤100): hàng `depth + BaselineRow − 1` không còn khối → depth+1.
- Level mỏ = level theo độ sâu (chặn 60). Camera trượt tới độ sâu mới (|dy|/tốc độ, 0.1..1s), xong thì dọn hàng trên.
- Nhân vật đứng ô không còn hợp lệ → dịch tới ô trống cùng cột gần nhất (không có thì quét cả vùng).

## Cuốc & ads
- Hồi 1 cuốc / 5 phút tới trần; đầy thì dời mốc về hiện tại. Trần = 30 + MaxPickaxeIncrease các gói offer đã mua
  + round(Mastery `MiningMaxPickaxe`) nếu đã mở.
- Hết dụng cụ → nút chuyển sang chế độ ads: "x<thưởng>" + "<icon ads> còn/6". Xem xong +10 cuốc / +1 khoan / +1 cuốc vàng.
  Đã mua No Ads → nhận luôn. Reset bộ đếm khi sang ngày mới.
- Popup: Pickaxe "có/trần", "Next in: <thời gian>" (cập nhật mỗi giây), thanh level "độ sâu / mốc level kế" ("Max" ở lv60),
  7 ô quặng (Stone, Coal, Iron, Ruby, Emerald, Gold, Diamond), nút bảng tỉ lệ (level hiện tại / kế, % trên tổng không tính Dirt).

## Hiển thị
- `MiningWorld` sinh ở (1000, 1000), layer "Mining", camera ortho size = VisibleRows/2 render vào RenderTexture
  hiện trên RawImage của popup; camera chính bỏ layer Mining. BlockParent đặt y = depth (hàng depth luôn ở y = 0).
- Nhân vật nhìn từ trên xuống: rig Body (0.5) + 2 tay + dụng cụ + mũ/ba lô/găng theo đồ Hero; xoay toàn thân
  góc atan2(dy_world, dx) − 90°.
- Hạt vỡ khối `<Ore>Particle`: burst 8 hạt, life ~1s, material `box_loot_AB` + Texture Sheet Animation (Sprites) = sprite `box-loot`
  (mảnh vuông), màu theo quặng (VD Coal #7F7399, Diamond #A1FFF8, Ruby #EB4242, Stone #9E9E9E, Dirt #96553D→#9E9E9E).
- Nền: 2 tấm Square scale 20 (_MiningGridMaterial: nền #A66B4C, line #925A41, ô 0.3), cuộn vô hạn theo camera.

## Project (2026-10-16)
`Scripts/Mining/{MiningConfig, UserMiningData, MiningService, MiningPathfinder, MiningGrid, MiningBlock, MiningBlockHpUI,
MiningCharacter, MiningCamera, MiningController, Editor/MiningPrefabBuilder}`, `Scripts/UI/Mining/{UIMiningPopup,
UIMiningRarityPopup, UIMiningOfferPopup}`. Cuốc/khoan/cuốc vàng + quặng nằm ở `UserItemData`
(ItemType.PICKAXE/DRILL/GOLDEN_PICKAXE, COAL_ORE..STONE_ORE). Builder: Tools/DungeonRush/Build Mining/All.

---
name: dungonrush-map-coord-convention
description: "Quy ước toạ độ ma trận map DungeonRush — GỐC (col-major) vs MỚI (row-major grid[row,col], gốc DƯỚI-trái tại PointStart, row đi LÊN); chốt 2026-07-30"
metadata: 
  node_type: memory
  type: reference
  originSessionId: b97a7480-03cb-47b1-986c-b9024a0601de
  modified: 2026-07-30T17:31:23.881Z
---

Bảng đối chiếu quy ước toạ độ lưới map, tách 2 cột để sau này còn truy được **data/hành vi GỐC** khi cần so với **cấu trúc MỚI**. Đổi tại session 2026-07-30 theo thói quen mảng 2 chiều của user (chọn "grid[row, col] — hàng trước"). Liên quan [[dungonrush-map-mode-structure]], [[dungonrush-map-spawn-procedural]].

**Chốt quan trọng (user sửa 2026-07-30):** row tăng → đi **LÊN (+Y)**, nên **PointStart = (0,0) = góc DƯỚI-trái**. Tức MỚI giữ NGUYÊN hướng trục như GỐC (gốc đáy, y-lên); chỉ khác GỐC ở **thứ tự index row-major** + **neo tại PointStart**.

| Khía cạnh | GỐC (game gốc LevelGenerator / impl cũ trước 2026-07-30) | MỚI (chốt 2026-07-30) |
|---|---|---|
| Kiểu ô | `Vector2Int(x=COL, y=ROW)` | `Vector2Int(x=ROW, y=COL)` |
| Lưu grid | `int[cols, rows]`, `grid[x,y]` (col-major) | `int[rows, cols]`, `grid[row,col]` (row-major) |
| `GetLength(0)` | = cols | = rows |
| Gốc (0,0) | góc DƯỚI‑trái | góc **DƯỚI‑trái**, neo tại `MapController.pointStart` |
| Hướng tăng | col→phải, row→LÊN (y up) | col→phải, **row→LÊN (y up)** — GIỐNG gốc |
| World map | `worldX=col, worldY=+row` | `worldX=col, worldY=+row` — GIỐNG gốc |
| Cửa / cổng enemy | hàng TRÊN = `row = rows-1` (y lớn) | hàng TRÊN = `row = rows-1` |
| Player spawn | hàng DƯỚI = `row = 0` | hàng DƯỚI = `row = 0` |
| Anchor world | `origin = BaseMode.transform.position`, canh giữa ngang | `pointStart.position` (góc dưới-trái, tắt canh giữa); null→fallback về hành vi gốc |

**Setup prefab:** `MapController` là component TRÊN root `MainMapPrefab` (guid 3cfed0fe…), `pointStart` wire tới child PointStart NGAY TRONG prefab. MapController có `Awake(){instance=this;}` → khi Instantiate tự nhận Singleton.

**Điểm khác biệt DUY NHẤT GỐC↔MỚI:** thứ tự index (`grid[col,row]`→`grid[row,col]`, storage `[cols,rows]`→`[rows,cols]`, `GetLength(0)` cols→rows) + anchor pointStart. **Hướng trục Y và vị trí cửa/spawn GIỐNG hệt gốc.** Khi đọc code CŨ gặp `grid[x,y]` x=cột → chỉ cần HOÁN thứ tự index sang `[row,col]`, KHÔNG lật trục Y. Doc `DecodedData/MAP_MODE_STRUCTURE.md` KHÔNG sửa theo (reference bất biến — [[dungonrush-decodeddata-is-reference]]).

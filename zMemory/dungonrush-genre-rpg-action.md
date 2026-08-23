---
name: dungonrush-genre-rpg-action
description: "DungeonRush là game RPG ACTION, KHÔNG phải game rắn/snake — sửa hiểu nhầm thể loại"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: c4c0092e-6043-46aa-b9ff-9702faa9e263
  modified: 2026-07-27T03:15:12.926Z
---

DungeonRush là **game RPG action**, KHÔNG phải game rắn (snake). 

**Why:** Ở phần battle mechanic (2026-07-26) tôi đã hiểu sai — dựng quân người chơi thành "rắn" (SnakeController: đầu di chuyển + thân bám trail) vì thấy class `SnakeController`/`CreativeSnakeData` trong code gốc. User đã **tự revert** toàn bộ implement battle sai đó và chỉ rõ đây là RPG action. Class `Snake*` trong bản gốc KHÔNG có nghĩa toàn game là snake — chỉ là 1 thành phần/chế độ.

**How to apply:** Khi làm gameplay/combat cho DungeonRush, mô hình là **RPG action** (nhân vật/quân đánh nhau kiểu action-RPG), KHÔNG dựng cơ chế rắn. Đợi user mô tả rõ cơ chế combat mong muốn trước khi code — đừng tự suy diễn thể loại từ tên class rip được. Phần MapGenerator/EnemySpawnGenerator/grid+spawn (procedural map, box, spawn enemy) vẫn đúng và giữ lại; chỉ battle loop kiểu snake là sai.

Liên quan: [[dungonrush-rebuild-progress]], [[dungonrush-map-spawn-procedural]].

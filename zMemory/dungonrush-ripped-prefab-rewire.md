---
name: dungonrush-ripped-prefab-rewire
description: Cách làm prefab UI ripped từ AssetRipper HIỆN ảnh — rewire m_Sprite qua UnityPy + tree-alignment
metadata: 
  node_type: memory
  type: project
  originSessionId: ae5694f3-da2c-4a70-8ef8-612a7de65309
  modified: 2026-09-04T06:18:23.598Z
---

Bản AssetRipper export KHÔNG có `.meta` ([[assetripper-export-no-meta]]) → guid `type:2` trong prefab là nhãn nội bộ AssetRipper, **không neo lại được** (không map với bundle name / path_id_map / project). Đừng cố khôi phục guid gốc.

⚠️ MẤU CHỐT khiến prefab KHÔNG hiện ảnh: component UGUI/TMP trong prefab ripped là **MISSING SCRIPT** — `m_Script` trỏ tới MonoScript assembly game đã strip (vd `d3e719b5...` gộp CẢ Image/Button/ScrollRect/Mask; `67dfb1fd`=TMP). Component missing thì KHÔNG vẽ. Phải remap `m_Script` sang script Unity thật. Chỉ sửa sprite guid là CHƯA đủ.

Cách đã dùng (pet UI, 2026-09-04) để làm 6 prefab Companion (=Pet) hiện ảnh:
0. **Remap m_Script** (bắt buộc): xác định class TỪNG component qua UnityPy alignment (đọc raw MB offset 16 = m_Script PPtr → MonoScript raw: m_Name/exec(4)/hash(16)/m_ClassName/m_Namespace), rồi thay guid strip → guid Unity thật theo fileID block (nhiều component chung 1 guid strip nên KHÔNG replace theo guid được). Guid thật lấy từ `Library/PackageCache/com.unity.ugui@*/Runtime/**/*.cs.meta` + TMP: Image=fe87c0e1cc204ed48ad3b37840f39efc, TMP UGUI=f4688fdb7df04437aeb418b961361dc5, Button=4e29b1a8..., Mask=31a19414..., ScrollRect=1aa08ab6..., Text=5f7201a1..., các LayoutGroup/ContentSizeFitter/GraphicRaycaster... Script logic riêng game (LocalizedLabel, PressButtonUI, *TabPage, *Popup, NotificationUI...) để MISSING — không vẽ. Đã remap 286 comp (Image×148, TMP×86, Button×36...). Verify Unity: 12/12 Image có sprite.
1. Giải nén `com.lavalabs.dungeonrush.apk` → `assets/bin/Data`, `UnityPy.load` cả thư mục.
2. Với mỗi prefab: tìm root GameObject theo tên; đọc cây engine (GameObject/RectTransform parse được), lấy `m_Sprite` của Image bằng **parse thô** MonoBehaviour (il2cpp không typetree): quét mọi PPtr 12-byte (int32 fileID + int64 pathID), cái nào resolve ra `Sprite` là m_Sprite.
3. Parse YAML prefab AssetRipper thành cây, **đối chiếu theo THỨ TỰ con** (structural align) với cây UnityPy → map `oldGuid(AssetRipper) → sprite thật`. Với pet: 0 lệch số con / 0 lệch tên = tin cậy cao.
4. Export sprite: non-atlas crop từ `m_RD.texture`+`textureRect`; atlas thì tra `sprite.m_RenderDataKey` trong `SpriteAtlas.m_RenderDataMap`. Nhớ **lật trục Y** (Unity gốc dưới-trái → PIL trên-trái): box=(x, H-(y+h), x+w, H-y); xoay theo `settingsRaw>>2 & 0xF`.
5. Ghi PNG + `.meta` (guid mới md5) dựa template `_ResourceGame/Avatar/angel.png.meta`, set spritePivot/spriteBorder(9-slice)/PPU. Rewrite prefab: `guid:<old>, type: 2` → `guid:<new>, type: 3`.

Kết quả pet (2026-09-04): **21 sprite** → `_Assets/_ResourceGame/PetUI/` (19 Image + `Button-White-Pressed1` cho m_PressedSprite + `rewarded` cho AdSprite; 2 field script này phân giải bằng cách lấy TẤT CẢ sprite trong MB của node rồi loại sprite đã map). **86 `m_fontAsset` + 86 `m_sharedMaterial`** → trỏ hết về `_Assets/Fonts/NotoSans/NotoSans-SemiBold SDF` (font `{fileID:11400000, guid:c72fd0b1e013ab24aa65be3fd6e6a194}`, material `{fileID:5133364889018529741, cùng guid}`) — game xài Noto, khớp convention project, KHÔNG dựng lại TMP font ripped. Scripts scratchpad: build_map.py, apply2.py, extra_map.py, apply3.py.

CÒN THIẾU (không phải ảnh, để nguyên): TMP `m_spriteAsset`/`GemSpriteAsset` (icon inline gem/emoji trong text, ~12 ref) → cần dựng TMP_SpriteAsset; ref nested prefab/SO (`CompanionElementPrefab`, `CardPrefab`, `_itemPrefab`, `MasteryConfig`). Built-in `0000...f00` (Background/UIMask) giữ nguyên — Unity có sẵn.

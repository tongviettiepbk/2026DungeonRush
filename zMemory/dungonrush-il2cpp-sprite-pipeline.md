---
name: dungonrush-il2cpp-sprite-pipeline
description: Pipeline đọc CHÍNH XÁC sprite/field của prefab ripped từ xapk (vượt rào il2cpp) + Python đã cài
metadata: 
  node_type: memory
  type: reference
  originSessionId: c27d6dfc-d776-4c4b-9edf-54a2a46d0379
  modified: 2026-09-06T10:56:02.482Z
---

Máy đã cài **Python 3.12** user-scope: `C:\Users\StarGear\AppData\Local\Programs\Python\Python312\python.exe` (KHÔNG trên PATH — gọi full path). Đã cài `UnityPy` 1.25.3 + `TypeTreeGeneratorAPI` 0.0.10.

**Vấn đề gốc:** AssetRipper export (`AssetRipper/ExportedProject`) KHÔNG có `.meta` → guid sprite trong prefab không resolve được (0 match trong project, không có trong `path_id_map.json`). Xem [[assetripper-export-no-meta]].

**Pipeline đọc field MonoBehaviour il2cpp (UnityPy đọc thẳng xapk):**
- `UnityPy.load(<apk>/assets/bin/Data)` sau khi giải nén xapk→apk.
- il2cpp: `read_typetree()` FAIL cho hầu hết MonoBehaviour (chỉ native như Sprite/Texture2D/GameObject/RectTransform đọc được). Vượt rào: `TypeTreeGenerator("2022.3.62f2").load_local_dll_folder(AssetRipper/AuxiliaryFiles/GameAssemblies)` (DummyDll) → `get_nodes("UnityEngine.UI.dll","UnityEngine.UI.Image")`.
- **BẪY version:** TTGAPI trả node kiểu riêng, `read_typetree` cần **list dict** `{m_Level,m_Type,m_Name,m_MetaFlag}` → phải convert (`to_dicts`).
- **Class detect:** đọc `m_Script` từ RAW bytes (offset cố định: m_GameObject 12B, m_Enabled+pad→16, m_Script fid@16 pid@20), resolve MonoScript (thường ở external file `globalgamemanagers.assets`, fid=1) → `m_ClassName`.
- **path_id TRÙNG giữa các .assets file** → phải index theo `(assets_file.name, path_id)` và theo `externals[fid-1]` cho ref ngoài.

**Rewire prefab ripped → project (dùng được):** RUNBOOK đầy đủ ở `tools/PREFAB_REWIRE_GUIDE.md`. 3 tool ở `tools/` đã GENERIC (nhận tên prefab làm argv, mặc định LevelPopup):
- `dump_levelpopup_sprites.py <Ten>` → dump Image→sprite + ghi `tools/_rewire/<Ten>.images.txt`
- `join_levelpopup_sprites.js <Ten>` → tự tra name→guid từ `_ResourceGame`, ghi `<Ten>.spritemap.json` (join theo thứ tự DFS, phải 0 xung đột)
- `rewire_levelpopup.js <Ten>` → thay guid UGUI/TMP+font, map sprite, **auto-strip mọi MonoBehaviour script game** (giữ engine đã remap), guid meta cố định, in cảnh báo nếu UI class chưa map.

Quy tắc strip generic: giữ MB nếu guid ∈ {6 target engine guid} ∪ {UGUI d3e719, TMP 67dfb1}; còn lại = script game → strip (layout-only). Kết quả LevelPopup: [[dungonrush-levelpopup-prefab]].

Class→guid chuẩn UGUI (lấy từ `Library/PackageCache/com.unity.ugui@*`): Image `fe87c0e1`, Button `4e29b1a8`, Slider `67db9e8f`, Mask `31a19414`, VerticalLayoutGroup `59f81469`, TMP text `f4688fdb`. Export assembly-guid: UGUI=`d3e719...`, TMP=`67dfb1...`. Xem [[dungonrush-ripped-prefab-rewire]].

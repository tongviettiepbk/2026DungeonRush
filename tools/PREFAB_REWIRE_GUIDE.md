# Hướng dẫn: lấy 1 prefab GỐC (layout + hình ảnh) từ game ripped vào project

Mục tiêu: đưa 1 prefab UI **gốc** (từ bản rip AssetRipper / xapk) vào `2026DungeonRushUnity` sao cho **mở Unity là hiện đúng layout + đúng sprite** — không phải tự dựng, không đoán ảnh. Kết quả là **layout-only** (đã strip script logic game; wire controller sau).

Vì sao cần pipeline này: bản `AssetRipper/ExportedProject` **không có `.meta`** → guid sprite trong prefab không resolve được. Cách chính xác là đọc thẳng **xapk** bằng UnityPy + sinh typetree từ il2cpp dump (`GameAssemblies`), rồi join theo thứ tự DFS.

## Chuẩn bị (đã cài sẵn 1 lần)
- Python: `C:\Users\StarGear\AppData\Local\Programs\Python\Python312\python.exe` (KHÔNG trên PATH — gọi full path). Đã có `UnityPy`, `TypeTreeGeneratorAPI`.
- Dữ liệu: `Dungeon+Rush_41_APKPure.xapk`, `AssetRipper/ExportedProject`, `AssetRipper/AuxiliaryFiles/GameAssemblies`.
- Node.js (có sẵn).

## Bước 0 — Tìm đúng prefab gốc
```bash
# liệt kê prefab, lọc theo tên tính năng
ls AssetRipper/ExportedProject/Assets/GameObject/ | grep -iE 'popup|panel|<tu-khoa>'
# xem cấu trúc / text để chắc đúng cái cần
grep -oE 'm_Name: .*|m_text: .*' AssetRipper/ExportedProject/Assets/GameObject/<Ten>.prefab | sort -u | head
```
Ghi lại **`<Ten>`** (đúng tên file, không .prefab). Ví dụ đã làm: `LevelPopup` (popup Rarity Table).

## Bước 1 — Dump sprite thật từ xapk
```bash
"C:/Users/StarGear/AppData/Local/Programs/Python/Python312/python.exe" tools/dump_levelpopup_sprites.py <Ten>
```
In cây `Image -> sprite` và ghi `tools/_rewire/<Ten>.images.txt` (thứ tự DFS).

## Bước 2 — Join + tự tra guid `_ResourceGame`
```bash
node tools/join_levelpopup_sprites.js <Ten>
```
Ghi `tools/_rewire/<Ten>.spritemap.json` (export-guid → target-guid). Phải thấy **conflicts: 0**. Nếu báo `SPRITE THIEU`: sprite đó chưa được tách vào `_ResourceGame` → thêm alias trong join (`ALIAS`) hoặc tách asset rồi chạy lại.

## Bước 3 — Rewire vào project
```bash
node tools/rewire_levelpopup.js <Ten>
```
Ra `2026DungeonRushUnity/Assets/_Assets/Resources/Prefabs/UI/<Ten>.prefab`. Việc tự động:
- thay component **UGUI/TMP** → guid chuẩn (bảng dưới); **font** → NotoSans-SemiBold SDF;
- map **sprite** theo json;
- **strip mọi MonoBehaviour script game** (giữ engine đã remap) → layout-only;
- guid `.meta` cố định (chạy lại không đổi).

Kết thúc phải in **`OK: khong con guid export sot.`**

## Xử lý cảnh báo
- `!! UI class CHUA MAP: <fileID>@<guid6>`: prefab dùng 1 class UGUI/TMP chưa có trong bảng. Soi field component đó để biết class (vd có `m_FillRect`=Slider, `m_Padding`=LayoutGroup…), lấy guid chuẩn từ `2026DungeonRushUnity/Library/PackageCache/com.unity.ugui@*` (hoặc TMP), rồi thêm 1 dòng vào `scriptMap` trong `rewire_levelpopup.js`.
- `!! sprite export CHUA MAP`: guid sprite chưa vào json → xem lại bước 2.

## Sau khi có prefab
- Mở trong Unity kiểm tra căn chỉnh (pipeline không mở được Editor).
- Wire logic sau: gắn controller `<Ten> : BaseUI`, đổ dữ liệu (vd % rarity từ ForgeData, XP từ `experience_required_per_level.csv`). Text hiện là placeholder gốc.

## Bảng guid engine đã map (trong rewire_levelpopup.js)
| Class | export (assembly, fileID) | target guid |
|---|---|---|
| Image | UGUI d3e719, -765806418 | fe87c0e1cc204ed48ad3b37840f39efc |
| Button | UGUI, 1392445389 | 4e29b1a8efbd4b44bb3f3716e73f07ff |
| Slider | UGUI, -113659843 | 67db9e8f0e2ae9c40bc1e2b64352a6b4 |
| Mask | UGUI, -1200242548 | 31a19414c41e5ae4aae2af33fee712f6 |
| VerticalLayoutGroup | UGUI, 1297475563 | 59f8146938fff824cb5fd77236b75775 |
| TextMeshProUGUI | TMP 67dfb1, 1453722849 | f4688fdb7df04437aeb418b961361dc5 |

> Chi tiết kỹ thuật vượt rào il2cpp (typetree, raw m_Script, path_id trùng file) xem memory `dungonrush-il2cpp-sprite-pipeline`.

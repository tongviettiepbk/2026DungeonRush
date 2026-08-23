---
name: dungonrush-reverse-native-il2cpp
description: Quy trình reverse THÂN HÀM native (logic/công thức) từ libil2cpp v41 khi AssetRipper chỉ ra stub rỗng; pipeline Il2CppDumper→capstone đã chạy được
metadata: 
  node_type: memory
  type: reference
  originSessionId: 1e37f0d7-d04a-49a9-b37c-9c5835d318d2
  modified: 2026-08-05T05:44:20.585Z
---

Khi cần logic/công thức THẬT của 1 hàm game (không phải chỉ field/tên): bản AssetRipper (`AssetRipper/ExportedProject/.../Assembly-CSharp/*.cs`) **thân hàm il2cpp bị stub rỗng** (`return null`/`return 0`) → phải đọc native `libil2cpp.so`.

**Pipeline (đã verify chạy 2026-08-05, dùng cho substat roll `GameResources.jhh/jhi`):**
1. Nguồn binary: `E:\00Work\00Project\2026DungOnRush\Dungeon+Rush_41_APKPure.xapk`. Bên trong: `libil2cpp.so` (96MB, lib/arm64-v8a) ở `config.arm64_v8a.apk`; `global-metadata.dat` (15MB, v31) ở `com.lavalabs.dungeonrush.apk` (assets/bin/Data/Managed/Metadata/). Bung bằng python `zipfile` (xapk = zip lồng apk).
2. Máy đã có: **capstone 5.0.7**, **lief**, **dotnet 9** (Python 3.13 ở `C:\Users\admin\AppData\Local\Programs\Python\Python313`). THIẾU Il2CppDumper → tải release chính thức `Perfare/Il2CppDumper` (bản `net7`, ~408KB) — HỎI user trước vì là tải file.
3. Dump: `DOTNET_ROLL_FORWARD=LatestMajor dotnet Il2CppDumper.dll libil2cpp.so global-metadata.dat <out>`. Ra `script.json` (ScriptMethod: Name↔Address VA), `dump.cs` (field offset + tên hàm obfuscated 1 chữ, VD `GameResources$$jhh`), `il2cpp.h`, DummyDll. (Lỗi "Press any key" cuối = vô hại; "This file may be protected" cũng bỏ qua được.)
4. Disasm: lief đọc LOAD segments để map VA→file offset, capstone `CS_ARCH_ARM64` disasm N byte của hàm (size = VA hàm kế − VA hàm này). Đọc float const qua adrp+ldr offset.

**Mẹo dịch ARM64 float:** `bl` tới hàm libm không tên → nhận diện qua pattern (sqrt+log+cos = Box-Muller; `0x42c80000`=100.0f; `frintm/frintp/fcvtzs+tst#1`=làm tròn banker's/floor/ceil). Field instance đọc qua `ldr sN,[x0,#off]` — tra `off` trong dump.cs.

Artefact dump nằm ở scratchpad TẠM (mất sau session) → cần thì bung + dump lại (~1 phút). Xem [[dungonrush-item-stats-source]], [[dungeonrush-config-format]].

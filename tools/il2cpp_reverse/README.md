# il2cpp reverse — pipeline lấy công thức/chỉ số enemy từ game gốc

Bộ script đã dùng để reverse công thức chỉ số enemy DungeonRush từ `libil2cpp.so`
(APK bản 41). Kết quả cuối: `DecodedData/ENEMY_STATS_MODEL.md`,
`DecodedData/gameresources_values.json`, `DecodedData/enemy_calc.py`.

> LƯU Ý: các script này có ĐƯỜNG DẪN scratchpad hard-code (thư mục tạm của phiên làm việc).
> Đây là artifact tham chiếu — muốn chạy lại phải sửa biến path ở đầu file cho trỏ tới nơi
> đặt `libil2cpp.so` / `global-metadata.dat` / output.

## Yêu cầu
- Python 3.12: `C:\Users\StarGear\AppData\Local\Programs\Python\Python312\python.exe`
- pip: `capstone`, `lief`, `UnityPy`, `TypeTreeGeneratorAPI` (đã cài user-scope)
- Il2CppDumper (bản self-contained `Il2CppDumper-win`, chạy `.exe` không cần .NET SDK)
- Nguồn: `Dungeon+Rush_41_APKPure.xapk`

## Các bước
1. `extract_bin.py` — bung `libil2cpp.so` (config.arm64_v8a.apk) + `global-metadata.dat`
   (com.lavalabs.dungeonrush.apk) từ xapk.
2. Chạy Il2CppDumper: `Il2CppDumper.exe libil2cpp.so global-metadata.dat <out>`
   → `script.json` (VA↔tên hàm), `dump.cs` (field/offset + tên hàm obfuscate), `DummyDll/`.
   (Lỗi "Press any key"/"file may be protected" cuối = vô hại.)
3. `disasm.py <VA...>` — disasm ARM64 thân hàm (capstone + lief map VA→offset), tự đọc
   float const qua adrp+ldr. Dùng để đọc công thức: jgm, hcj, hck, hcm, hpv, jgx, jhe...
4. `read_gameresources.py` — đọc GIÁ TRỊ field serialized của MonoBehaviour `GameResources`
   (UnityPy + TypeTreeGenerator từ DummyDll) → `gameresources_values.json`.
5. `compile_check.py` — compile-check Assembly-CSharp bằng csc bundled của Unity 6.3
   (`Editor/Data/DotNetSdkRoslyn/csc.dll` chạy bằng `Editor/Data/NetCoreRuntime/dotnet.exe`
   vì máy KHÔNG có .NET SDK); gom Compile + HintPath + ProjectReference từ .csproj, chạy cwd=project.

## Hàm gốc đã map (obfuscate → ý nghĩa)
- `GameResources.jgm` : combatLevel = base + (level-1)*3  (dungeon)
- `GameResources.jgx` / `jgn` / `jgo` / `jhe` : chọn preset theo level
- `EconomyController.hcj` : totalArmyPower = 500 * 10^hck
- `EconomyController.hck` : số mũ theo ArmyPowerSegments
- `EconomyController.hcm` : chia Lancaster + damage/health mỗi unit
- `LevelController.hpv` : entry — phân nhánh CAMPAIGN vs DUNGEON
- `GameResources.jgw` : HealthToDamageRatio theo role (3/2/10)
- `GameResources.jgu` : item stat = base * tierScaler^tier * (1 + levelScaler*level)

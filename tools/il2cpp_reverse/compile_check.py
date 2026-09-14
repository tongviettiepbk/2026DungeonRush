# -*- coding: utf-8 -*-
import re, os, subprocess, sys

PROJ = r"E:\Project\2026DungeonRush\2026DungeonRushUnity"
CSPROJ = os.path.join(PROJ, "Assembly-CSharp.csproj")
CSC = r"C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\DotNetSdkRoslyn\csc.dll"
DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
SP = os.path.dirname(os.path.abspath(__file__))

txt = open(CSPROJ, encoding="utf-8").read()
sources = re.findall(r'<Compile Include="([^"]+)"', txt)
refs = re.findall(r'<HintPath>([^<]+)</HintPath>', txt)
# ProjectReference (Spine...) -> DLL đã build trong Library/ScriptAssemblies
sa = os.path.join(PROJ, "Library", "ScriptAssemblies")
for pr in re.findall(r'<ProjectReference Include="(?:[^"]*[\\/])?([^\\/"]+)\.csproj"', txt):
    dll = os.path.join(sa, pr + ".dll")
    if os.path.exists(dll) and "Assembly-CSharp" not in pr:
        refs.append(dll)
defines = re.search(r'<DefineConstants>([^<]+)</DefineConstants>', txt).group(1)

def full(p):
    return p if os.path.isabs(p) else os.path.join(PROJ, p)

rsp = os.path.join(SP, "cc.rsp")
with open(rsp, "w", encoding="utf-8") as f:
    f.write("-target:library\n")
    f.write("-nostdlib+\n")
    f.write("-noconfig\n")
    f.write("-langversion:9.0\n")
    f.write("-unsafe+\n")
    f.write(f"-out:{os.path.join(SP,'out.dll')}\n")
    f.write("-define:" + defines.strip() + "\n")
    for r in refs:
        f.write(f'-reference:"{r.strip()}"\n')
    for s in sources:
        f.write(f'"{full(s)}"\n')

print(f"sources={len(sources)} refs={len(refs)}")
DOTNET = r"C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\NetCoreRuntime\dotnet.exe"
r = subprocess.run([DOTNET, CSC, f"@{rsp}"], capture_output=True, text=True, cwd=PROJ)
out = (r.stdout or "") + (r.stderr or "")
errs = [l for l in out.splitlines() if ": error " in l]
warns = [l for l in out.splitlines() if ": warning " in l]
print(f"exit={r.returncode} errors={len(errs)} warnings={len(warns)}")
# chỉ in lỗi liên quan file mình sửa + tổng hợp lỗi khác
mine = [l for l in errs if "EnemySpawnGenerator" in l or "EnemyUnit" in l]
print("\n=== ERRORS in edited files ===")
for l in mine: print(l)
print("\n=== OTHER errors (đầu 25) ===")
for l in errs[:25]:
    if l not in mine: print(l)

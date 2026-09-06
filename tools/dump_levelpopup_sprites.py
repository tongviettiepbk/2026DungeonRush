# -*- coding: utf-8 -*-
# Dump chinh xac Image->sprite cua 1 prefab ripped, doc THANG tu xapk (vuot rao il2cpp).
# Dung:  python tools/dump_levelpopup_sprites.py <TenPrefab>   (mac dinh LevelPopup)
# Xuat: in cay Image->sprite + ghi tools/_rewire/<Ten>.images.txt (DFS, moi dong 1 sprite name).
import os, sys, zipfile, tempfile, glob, struct
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
ROOT=r"E:\Project\2026DungeonRush"
XAPK=os.path.join(ROOT,"Dungeon+Rush_41_APKPure.xapk")
GA=os.path.join(ROOT,"AssetRipper","AuxiliaryFiles","GameAssemblies")
TARGET=sys.argv[1] if len(sys.argv)>1 else "LevelPopup"
CACHE=os.path.join(ROOT,"tools","_rewire"); os.makedirs(CACHE,exist_ok=True)

tmp=tempfile.mkdtemp(prefix="dr_v3_")
with zipfile.ZipFile(XAPK) as z: z.extractall(tmp)
apks=glob.glob(os.path.join(tmp,"**","*.apk"),recursive=True) or [XAPK]
dirs=[]
for a in apks:
    d=a+"_x"
    try:
        with zipfile.ZipFile(a) as z: z.extractall(d)
        b=os.path.join(d,"assets","bin","Data"); dirs.append(b if os.path.isdir(b) else d)
    except: pass
env=UnityPy.load(*dirs)
gen=TypeTreeGenerator("2022.3.62f2"); gen.load_local_dll_folder(GA)
def to_dicts(nodes):
    return [{"m_Level":n.m_Level,"m_Type":n.m_Type,"m_Name":n.m_Name,"m_MetaFlag":n.m_MetaFlag} for n in nodes]
IMG=to_dicts(gen.get_nodes("UnityEngine.UI.dll","UnityEngine.UI.Image"))

byfp={}
for o in env.objects: byfp[(o.assets_file.name,o.path_id)]=o
def resolve(fileo, fid, pid):
    if not pid: return None
    if fid==0: return byfp.get((fileo.assets_file.name,pid))
    exts=fileo.assets_file.externals
    if fid-1 < len(exts):
        base=os.path.basename(exts[fid-1].name)
        for (fn2,p2),oo in byfp.items():
            if p2==pid and fn2.endswith(base): return oo
    return None

def tt(o,nodes=None):
    try: return o.read_typetree(nodes) if nodes else o.read_typetree()
    except Exception: return None

# raw parse m_Script PPtr (offset: m_GameObject 12B, m_Enabled 1 + pad ->16, m_Script fid@16 pid@20)
def mb_script(o):
    raw=o.get_raw_data()
    if len(raw)<28: return (0,0)
    fid=struct.unpack_from("<i",raw,16)[0]
    pid=struct.unpack_from("<q",raw,20)[0]
    return (fid,pid)
def mb_classname(o):
    fid,pid=mb_script(o)
    ms=resolve(o,fid,pid)
    if not ms: return None
    d=tt(ms)
    return d.get("m_ClassName") if d else None

# tim root file
root=None
for o in env.objects:
    if o.type.name=="GameObject":
        d=tt(o)
        if d and d.get("m_Name")==TARGET: root=o; break
RF=root.assets_file.name
by={}
for o in env.objects:
    if o.assets_file.name==RF: by[o.path_id]=o

def go_components(gd): return [c.get("component",c).get("m_PathID",0) for c in gd.get("m_Component",[])]
def go_rect(gd):
    for pid in go_components(gd):
        co=by.get(pid)
        if co and co.type.name in ("RectTransform","Transform"): return co
    return None
def image_info(gd):
    for pid in go_components(gd):
        co=by.get(pid)
        if not co or co.type.name!="MonoBehaviour": continue
        if mb_classname(co)=="Image":
            d=tt(co,IMG)
            if d and "m_Sprite" in d:
                sp=d["m_Sprite"]; col=d.get("m_Color",{})
                so=resolve(co,sp.get("m_FileID",0),sp.get("m_PathID",0))
                sname=(tt(so).get("m_Name") if so and tt(so) else "(missing)")
                return sname,(col.get("r",1),col.get("g",1),col.get("b",1),col.get("a",1))
    return None

out=[]
def walk(gpid,depth):
    go=by.get(gpid)
    if not go: return
    gd=tt(go)
    if not gd: return
    nm=gd.get("m_Name","?")
    info=image_info(gd)
    if info: out.append((depth,nm,info[0],info[1]))
    rt=go_rect(gd); rd=tt(rt) if rt else None
    if rd:
        for ch in rd.get("m_Children",[]):
            crt=by.get(ch.get("m_PathID",0))
            if crt:
                crd=tt(crt)
                if crd: walk(crd.get("m_GameObject",{}).get("m_PathID",0),depth+1)
walk(root.path_id,0)
print("=== %d Image trong %s ==="%(len(out),TARGET))
for depth,nm,sp,col in out:
    print("%s%-22s -> %-30s (%.2f,%.2f,%.2f,%.2f)"%("  "*depth,nm,sp,*col))
# ghi DFS sprite names (cho buoc join)
with open(os.path.join(CACHE,TARGET+".images.txt"),"w",encoding="utf-8") as f:
    for depth,nm,sp,col in out: f.write(sp+"\n")
print("-> wrote", os.path.join(CACHE,TARGET+".images.txt"))

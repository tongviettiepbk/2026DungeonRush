# -*- coding: utf-8 -*-
# Fill texture that vao scene AssetRipper (SceneOrigin/): map sprite theo DUONG DAN phan cap
# (xapk il2cpp <-> AssetRipper YAML), remap UGUI/TMP de render, strip script game (khong can script).
# Dung:  tools/.venv/bin/python tools/fill_scene_textures.py [SceneName ...]  (mac dinh ca 2)
# Tan dung env + helper da load san trong extract_ui_batch.
import os, re, sys
import extract_ui_batch as B

env=B.env; tt=B.tt; resolve=B.resolve; mb_classname=B.mb_classname; IMG=B.IMG
NAME2GUID=B.NAME2GUID; UGUI_MAP=B.UGUI_MAP; TMP_MAP=B.TMP_MAP
UGUI=B.UGUI; TMP=B.TMP; FONT=B.FONT; FONT_MAT=B.FONT_MAT; KEEP=B.KEEP; IMAGE_FILEID=B.IMAGE_FILEID
ROOT=B.ROOT
SCENE_DIR=os.path.join(ROOT,"2026DungeonRushUnity","Assets","_Assets","SceneOrigin")

# ---------- xapk: index theo file, path -> sprite name ----------
files={}
for o in env.objects:
    files.setdefault(o.assets_file.name, []).append(o)

def xapk_go_names(objs):
    s=set()
    for o in objs:
        if o.type.name=="GameObject":
            d=tt(o)
            if d and d.get("m_Name"): s.add(d["m_Name"])
    return s

def build_xapk_pathmap(objs):
    by={o.path_id:o for o in objs}
    # transform-of-GO + name cache
    def go_of_tr(trd): return by.get(trd.get("m_GameObject",{}).get("m_PathID",0))
    def tr_of_go(gd):
        for c in gd.get("m_Component",[]):
            pid=c.get("component",c).get("m_PathID",0); co=by.get(pid)
            if co and co.type.name in ("RectTransform","Transform"): return co
        return None
    name_cache={}
    def go_name(go):
        if go.path_id in name_cache: return name_cache[go.path_id]
        d=tt(go); nm=d.get("m_Name","?") if d else "?"; name_cache[go.path_id]=nm; return nm
    def path_of_go(go):
        parts=[]; cur=go; guard=0
        while cur is not None and guard<64:
            guard+=1
            gd=tt(cur)
            if not gd: break
            parts.append(go_name(cur))
            tr=tr_of_go(gd); trd=tt(tr) if tr else None
            if not trd: break
            f=trd.get("m_Father",{}).get("m_PathID",0)
            if not f: break
            ftr=by.get(f);
            if not ftr: break
            cur=go_of_tr(tt(ftr) or {})
        return "/".join(reversed(parts))
    pm={}
    def add(go_pid, sname):
        go=by.get(go_pid)
        if not go: return
        pm.setdefault(path_of_go(go),[]).append(sname)
    for o in objs:
        if o.type.name=="MonoBehaviour":
            if mb_classname(o)!="Image": continue
            d=tt(o,IMG)
            if not d or "m_Sprite" not in d: continue
            sp=d["m_Sprite"]; so=resolve(o,sp.get("m_FileID",0),sp.get("m_PathID",0))
            add(d.get("m_GameObject",{}).get("m_PathID",0),
                (tt(so).get("m_Name") if so and tt(so) else None))
        elif o.type.name=="SpriteRenderer":
            d=tt(o)
            if not d or "m_Sprite" not in d: continue
            sp=d["m_Sprite"]; so=resolve(o,sp.get("m_FileID",0),sp.get("m_PathID",0))
            add(d.get("m_GameObject",{}).get("m_PathID",0),
                (tt(so).get("m_Name") if so and tt(so) else None))
    return pm

# ---------- AssetRipper YAML: path -> export sprite guid ----------
def build_export_pathmap(scene_path):
    raw=open(scene_path,encoding="utf-8").read().replace("\r\n","\n")
    docs=re.split(r"\n(?=--- !u!)",raw)
    obj={}
    for d in docs:
        m=re.match(r"^--- !u!(\d+) &(\d+)",d)
        if m: obj[m.group(2)]={"cls":m.group(1),"body":d}
    goName={}; goComps={}; trGO={}; trFather={}
    for i,o in obj.items():
        b=o["body"]
        if o["cls"]=="1":
            mm=re.search(r"m_Name: (.*)",b); goName[i]=(mm.group(1).strip() if mm else "?")
            goComps[i]=re.findall(r"- component: \{fileID: (\d+)\}",b)
        elif o["cls"] in ("224","4"):
            mm=re.search(r"m_GameObject: \{fileID: (\d+)\}",b); trGO[i]=(mm.group(1) if mm else None)
            fm=re.search(r"m_Father: \{fileID: (\d+)\}",b); trFather[i]=(fm.group(1) if fm else "0")
    go_tr={}
    for i in obj:
        if obj[i]["cls"]=="1":
            for c in goComps.get(i,[]):
                if obj.get(c) and obj[c]["cls"] in ("224","4"): go_tr[i]=c; break
    def path_of_go(gid):
        parts=[]; cur=gid; guard=0
        while cur and guard<64:
            guard+=1
            parts.append(goName.get(cur,"?"))
            tr=go_tr.get(cur)
            if not tr: break
            f=trFather.get(tr,"0")
            if f=="0" or f not in trGO: break
            cur=trGO.get(f)
        return "/".join(reversed(parts))
    pm={}  # path -> export guid
    for i,o in obj.items():
        # UGUI Image (114) hoac SpriteRenderer (212)
        is_img = o["cls"]=="114" and re.search(r"m_Script: \{fileID: %s, guid: %s"%(re.escape(IMAGE_FILEID),UGUI),o["body"])
        is_sr  = o["cls"]=="212"
        if not (is_img or is_sr): continue
        gm=re.search(r"m_Sprite: \{fileID: \d+, guid: ([0-9a-f]{32}), type: 2\}",o["body"])
        if not gm: continue
        gom=re.search(r"m_GameObject: \{fileID: (\d+)\}",o["body"])
        if not gom: continue
        pm.setdefault(path_of_go(gom.group(1)),[]).append(gm.group(1))
    return raw,pm

def rewrite_scene(raw, spritemap, dest):
    def repl_script(m):
        fid=int(m.group(1)); asm=m.group(2)
        tgt=(UGUI_MAP if asm==UGUI else TMP_MAP).get(fid)
        return "m_Script: {fileID: 11500000, guid: %s, type: 3}"%tgt if tgt else m.group(0)
    raw=re.sub(r"m_Script: \{fileID: (-?\d+), guid: (%s|%s), type: 3\}"%(UGUI,TMP),repl_script,raw)
    raw=re.sub(r"m_fontAsset: \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}",
               "m_fontAsset: {fileID: 11400000, guid: %s, type: 2}"%FONT,raw)
    raw=re.sub(r"m_sharedMaterial: \{fileID: \d+, guid: [0-9a-f]{32}, type: 2\}",
               "m_sharedMaterial: {fileID: %s, guid: %s, type: 2}"%(FONT_MAT,FONT),raw)
    for g,tgt in spritemap.items():
        pat=r"\{fileID: 21300000, guid: %s, type: 2\}"%g
        raw=re.sub(pat,("{fileID: 21300000, guid: %s, type: 3}"%tgt) if tgt else "{fileID: 0}",raw)
    # strip MonoBehaviour script game (giu UGUI/TMP da remap)
    docs=re.split(r"\n(?=--- !u!)",raw); removed=set(); kept=[]
    for d in docs:
        am=re.match(r"^--- !u!\d+ &(\d+)",d); anchor=am.group(1) if am else None
        sm=re.search(r"m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32})",d); sg=sm.group(1) if sm else None
        if re.match(r"^--- !u!114 ",d) and sg and sg not in KEEP:
            if anchor: removed.add(anchor)
            continue
        kept.append(d)
    out="\n".join(kept)
    for i in removed:
        out=re.sub(r"\s*- component: \{fileID: %s\}"%i,"",out)
        out=re.sub(r"\s*- \{fileID: %s\}"%i,"",out)  # go khoi m_Component list dang khac (scene)
    if not out.endswith("\n"): out+="\n"
    open(dest,"w",encoding="utf-8").write(out)
    return len(removed)

def process(scene):
    src=os.path.join(SCENE_DIR,scene+".unity")
    raw,exp_pm=build_export_pathmap(src)
    # tim level file xapk khop ten
    ar_names=set()
    for p in exp_pm: ar_names.update(p.split("/"))
    # dung TAT CA go name trong scene YAML de so khop
    ar_all=set(re.findall(r"m_Name: (.*)",raw))
    best=None; bestscore=-1
    for fn,objs in files.items():
        sc=len(xapk_go_names(objs) & ar_all)
        if sc>bestscore: bestscore=sc; best=(fn,objs)
    fn,objs=best
    xpm=build_xapk_pathmap(objs)
    # join theo path
    g2n={}; ambiguous=0
    for path,guids in exp_pm.items():
        names=xpm.get(path)
        if not names: continue
        uniq=set(n for n in names if n)
        if len(uniq)!=1:
            ambiguous+=1; continue
        nm=next(iter(uniq))
        for g in guids: g2n[g]=nm
    spritemap={}; missing=set()
    for g,nm in g2n.items():
        tg=NAME2GUID.get(nm)
        spritemap[g]=tg
        if not tg: missing.add(nm)
    n_removed=rewrite_scene(raw,spritemap,src)
    total_guids=len(set(g for gs in exp_pm.values() for g in gs))
    print("== %s == level file: %s (overlap %d)"%(scene,fn,bestscore))
    print("   Image path (export): %d | matched path: %d | sprite mapped: %d | missing: %d | ambiguous path: %d"%(
        len(exp_pm), len(g2n), sum(1 for v in spritemap.values() if v), len(missing), ambiguous))
    print("   distinct export sprite guid: %d | stripped script comp: %d"%(total_guids,n_removed))
    if missing: print("   sprite thieu _ResourceGame:", ", ".join(sorted(missing)))

if __name__=="__main__":
    scenes=sys.argv[1:] or ["GameplayScene","StarterScene"]
    for s in scenes: process(s)

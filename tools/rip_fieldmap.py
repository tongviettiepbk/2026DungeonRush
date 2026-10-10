# -*- coding: utf-8 -*-
# Doc prefab rip (AssetRipper/ExportedProject/Assets/GameObject/<Ten>.prefab) -> bang wiring field GOC
# cua moi MonoBehaviour game: field -> duong dan GameObject (tinh tu root prefab) + kieu component.
# Class game nhan dien bang chu ky ten field so voi dump.cs (Il2CppDumper) vi rip khong co .meta.
# Dung:  python3 tools/rip_fieldmap.py <dump.cs> <out.json> <Prefab1> [Prefab2 ...]
import os, re, sys, json

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GOBJ = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "GameObject")

# fileID MonoScript UGUI/TMP trong rip (thuat toan md4, xem extract_ui_batch.mono_fileid)
UGUI_FID = {
    "-765806418": "Image", "1392445389": "Button", "-113659843": "Slider", "-1200242548": "Mask",
    "1367256648": "ScrollRect", "1679637790": "ContentSizeFitter", "1297475563": "VerticalLayoutGroup",
    "-405508275": "HorizontalLayoutGroup", "-2095666955": "GridLayoutGroup", "1741964061": "LayoutElement",
    "-146154839": "RectMask2D", "-1862395651": "Toggle", "-98529514": "RawImage", "1573420865": "Outline",
    "-900027084": "Shadow", "-1184210157": "CanvasScaler", "1301386320": "GraphicRaycaster",
    "11500000": "?",
}


def parse_dump(path):
    """class -> (base, [public instance fields theo thu tu])."""
    classes = {}
    cur = None
    serialized = False
    for line in open(path, encoding="utf-8", errors="ignore"):
        m = re.match(r"^(?:public|internal|private)?\s*(?:sealed |abstract |static )*(?:class|struct) ([\w.<>`]+)(?: : ([\w.<>`, ]+))? // TypeDefIndex", line)
        if m:
            name = m.group(1)
            base = (m.group(2) or "").split(",")[0].strip()
            cur = name
            classes[cur] = {"base": base, "fields": []}
            continue
        if cur is None:
            continue
        fm = re.match(r"^\t(public|private|protected) (?!static|const)([\w.<>\[\], `]+?) (\w+); // 0x", line)
        if fm and (fm.group(1) == "public" or serialized):
            classes[cur]["fields"].append(fm.group(3))
        serialized = bool(re.match(r"^\t\[SerializeField\]", line))
    return classes


def all_fields(classes, name):
    out = []
    seen = set()
    while name and name in classes and name not in seen:
        seen.add(name)
        out = classes[name]["fields"] + out
        name = classes[name]["base"]
    return out


def parse_prefab(target):
    raw = open(os.path.join(GOBJ, target + ".prefab"), encoding="utf-8").read().replace("\r\n", "\n")
    docs = re.split(r"\n(?=--- !u!)", raw)
    obj = {}
    for d in docs:
        m = re.match(r"^--- !u!(\d+) &(-?\d+)", d)
        if m:
            obj[m.group(2)] = {"cls": m.group(1), "body": d}
    return obj


def mb_fields(body):
    """Cac dong field sau m_EditorClassIdentifier (giu nguyen text gia tri, ca mang nhieu dong)."""
    idx = body.find("m_EditorClassIdentifier:")
    if idx < 0:
        return []
    lines = body[idx:].split("\n")[1:]
    fields = []
    for ln in lines:
        m = re.match(r"^  (\w+):\s?(.*)$", ln)
        if m:
            fields.append([m.group(1), m.group(2)])
        elif fields and ln.startswith("  "):
            fields[-1][1] += "\n" + ln
    return fields


def build(target, classes):
    obj = parse_prefab(target)
    go_name, go_comps, rt_go, rt_father, go_rt = {}, {}, {}, {}, {}
    for i, o in obj.items():
        b = o["body"]
        if o["cls"] == "1":
            go_name[i] = re.search(r"m_Name: (.*)", b).group(1).strip()
            go_comps[i] = re.findall(r"- component: \{fileID: (-?\d+)\}", b)
        elif o["cls"] in ("224", "4"):
            rt_go[i] = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", b).group(1)
            fm = re.search(r"m_Father: \{fileID: (-?\d+)\}", b)
            rt_father[i] = fm.group(1) if fm else "0"
    for i in go_comps:
        for c in go_comps[i]:
            if c in obj and obj[c]["cls"] in ("224", "4"):
                go_rt[i] = c

    def go_path(go):
        parts = []
        rt = go_rt.get(go)
        while rt and rt != "0" and rt in rt_go:
            parts.append(go_name[rt_go[rt]])
            rt = rt_father.get(rt, "0")
        parts.reverse()
        return "/".join(parts[1:])  # bo ten root

    def comp_type(cid):
        o = obj.get(cid)
        if not o:
            return None
        if o["cls"] == "224":
            return "RectTransform"
        if o["cls"] == "4":
            return "Transform"
        if o["cls"] == "222":
            return "CanvasRenderer"
        if o["cls"] == "223":
            return "Canvas"
        if o["cls"] == "225":
            return "CanvasGroup"
        if o["cls"] == "114":
            sm = re.search(r"m_Script: \{fileID: (-?\d+), guid: (\w+)", o["body"])
            fid = sm.group(1) if sm else "?"
            if fid in UGUI_FID and fid != "11500000":
                return UGUI_FID[fid]
            keys = [k for k, _ in mb_fields(o["body"])]
            if keys and keys[0].startswith("m_"):
                if "m_text" in keys:
                    return "TMP_Text"
                if "m_TextComponent" in keys or "m_TextViewport" in keys:
                    return "TMP_InputField"
                return "UGUI:" + fid
            return "Game:" + (match_class(keys) or "?")
        return "cls" + o["cls"]

    def match_class(keys):
        if not keys:
            return None
        best, score = None, -1
        ks = set(keys)
        for cname, c in classes.items():
            fl = all_fields(classes, cname)
            if not fl:
                continue
            fs = set(fl)
            if ks <= fs:
                s = len(ks & fs) - len(fs - ks) * 0.01
                if s > score:
                    best, score = cname, s
        return best

    def resolve(val):
        m = re.match(r"\{fileID: (-?\d+)(?:, guid: (\w+), type: (\d+))?\}", val.strip())
        if not m:
            return None
        fid, guid = m.group(1), m.group(2)
        if guid:
            return {"external": guid, "fileID": fid}
        if fid == "0":
            return {"null": True}
        o = obj.get(fid)
        if not o:
            return {"missing": fid}
        if o["cls"] == "1":
            return {"path": go_path(fid), "type": "GameObject"}
        go = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", o["body"]).group(1)
        return {"path": go_path(go), "type": comp_type(fid)}

    result = []
    for cid, o in obj.items():
        if o["cls"] != "114":
            continue
        t = comp_type(cid)
        if not t or not t.startswith("Game:"):
            continue
        go = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", o["body"]).group(1)
        entry = {"class": t[5:], "path": go_path(go), "fields": {}}
        for k, v in mb_fields(o["body"]):
            if v.strip().startswith("{fileID"):
                entry["fields"][k] = resolve(v)
            elif "\n" in v and v.lstrip().startswith("- "):
                items = re.findall(r"- (\{fileID[^}]*\})", v)
                if items:
                    entry["fields"][k] = [resolve(x) for x in items]
                else:
                    entry["fields"][k] = v.strip()
            else:
                entry["fields"][k] = v.strip()
        result.append(entry)
    result.sort(key=lambda e: e["path"].count("/"))
    return result


def main():
    dump, out = sys.argv[1], sys.argv[2]
    classes = parse_dump(dump)
    data = {}
    for t in sys.argv[3:]:
        data[t] = build(t, classes)
    json.dump(data, open(out, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    for t, lst in data.items():
        print(t, [e["class"] + "@" + (e["path"] or "<root>") for e in lst])


if __name__ == "__main__":
    main()

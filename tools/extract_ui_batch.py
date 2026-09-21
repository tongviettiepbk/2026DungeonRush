# -*- coding: utf-8 -*-
# Batch: lay prefab UI GOC (layout + anh) tu AssetRipper/xapk -> Prefabs/UI/<Feature>/.
# Gop 3 buoc dump(xapk il2cpp) -> join(_ResourceGame) -> rewire, load xapk/typetree 1 LAN.
# Dung:  tools/.venv/bin/python tools/extract_ui_batch.py [Feature ...]   (khong tham so = tat ca)
# Port trung thuc tu dump_levelpopup_sprites.py / join_levelpopup_sprites.js / rewire_levelpopup.js.
import os, sys, zipfile, tempfile, glob, struct, json, re, hashlib
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
XAPK = os.path.join(ROOT, "Dungeon+Rush_41_APKPure.xapk")
GA   = os.path.join(ROOT, "AssetRipper", "AuxiliaryFiles", "GameAssemblies")
GOBJ = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "GameObject")
RES  = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets", "_ResourceGame")
UIDIR= os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets", "Prefabs", "UI")
CACHE= os.path.join(ROOT, "tools", "_rewire"); os.makedirs(CACHE, exist_ok=True)

# ---- ban do feature -> prefab ----
FEATURES = {
  "BattlePass": ["BattlePassFreeRewardUI","BattlePassLevelElementUI","BattlePassPopup","BattlePassPremiumRewardUI"],
  "BossRush":   ["BossRushClaimPopup","BossRushEndPopup","BossRushJoinPopup","BossRushPlayerElementUIPrefab","BossRushPopup"],
  "Cape":       ["CapeDetailPopup","CapePageElementUI","CapePopup","CapeSalvagePopup","CapeUpgradeInfoPopup"],
  "Wing":       ["WingCraftPopup","WingCraftTabPage","WingElementUIPrefab","WingRerollPopup"],
  "Enchantment":["EnchantmentElementUI","EnchantmentInfoPopup","EnchantmentMergePopup","EnchantmentPreviewInfoPopup","EnchantmentTabPage","EnchantmentUpgradeInfoPopup"],
  "Chat":       ["ChatItemDropItemUI","ChatMessageItemUI","ChatTabPage"],
  "Clan":       ["ClanBannerEditorPopup","ClanBannerOptionItem","ClanContributionLeaderboardPopup","ClanInfoPopup","ClanMemberCard","ClanPlayerPopup","ClanRequestCard","ClanRequestsPopup","ClanSearchCard","ClanSettingsPopup","ClanTabPage","CreateClanPopup","EditAnnouncementPopup"],
  "ClanWar":    ["ClanWarActionCard","ClanWarClanRowCard","ClanWarContributionCard","ClanWarDayCard","ClanWarDaysAndResultPopup","ClanWarEnemyCard","ClanWarLeaderboardPopup","ClanWarMilestoneCard","ClanWarRewardCell","ClanWarRewardsPopup"],
  "PvP":        ["PvPEndPopup","PvPFindOpponentPopup","PvPLeaderboardElement","PvPLeaderboardPopup","PvPLeaderboardSeperator","PvPPlayerElementUIPrfab","PvPPopup","PvPRewardPopup"],
  "Shop":       ["ChestContentUI","ChestInfoPopup","ChestRevealPopup","ChestRewardElementUI","ChestRewardsPopup","Chest_Bundle_PopupOffer","Chest_Single_Product_Horizontal","Chest_Single_Product_Vertical","DailyDealRewardEntryUI","OfferPopup","RewardedChestPopup","StoreTabPage"],
  "Forge":      ["AutoForgePopup","ForgeUpgradePopup","NewForgePopup","AdvancedSearchPopup"],
  "Gear":       ["AvatarItemUI","Item","ItemElementUI","ItemInfoPopup","NewItemPopup","SubstatUIElementPrefab"],
  "Mastery":    ["MasteryCardElementUIPrefab","MasteryPopup"],
  "Mining":     ["MiningPopup","MiningRarityPopup","Mining_Bundle_PopupOffer"],
  "Dungeon":    ["DungeonEndPopup","DungeonPopup","DungeonTabPage"],
  "Events":     ["EventRewardElementUI","EventRewardTextPrefab","EventsTabPage"],
  "Profile":    ["AccountSelectPopup","AvatarSelectPopup","CharacterPopup","CloudSavePopup","LeadershipRankPopup","NicknamePopup","PlayerPopup","ProfilePopup","SignInPopup","SignUpPopup"],
  "Settings":   ["AboutTab","CountryItemUI","CountrySelectPopup","ForceUpdatePopup","GameSpeedPopup","LanguageItemUI","LanguageSelectPopup","MobileTestPopup","NoConnectionPopup","OfflineEarningPopup","RateUsPopup1","RedeemCodePopup","ReportBugPopup","ReportPopup"],
  "Common":     ["BattleEndPopup","Category","LeaderboardTabSelectorItemUI Variant","Tab","TabSelectorItemUI"],
  "Battle":     ["BlockHPUIPrefab","CharacterNameUI","CreativeEnemyHPElementUI","DamageUI","EffectLabelTextIUIPrefab","HPUI","InfoBlock","WarningUI"],
}

# ---- hang so rewire ----
UGUI = "d3e719b59ab71ba3f6b398058c866280"; TMP = "67dfb1fdfb2b407222eda8e23ac8b724"
UGUI_PKG = os.path.join(ROOT, "2026DungeonRushUnity", "Library", "PackageCache")
FONT = "c72fd0b1e013ab24aa65be3fd6e6a194"; FONT_MAT = "5133364889018529741"
IMAGE_FILEID = "-765806418"

def stable_guid(seed): return hashlib.md5(seed.encode()).hexdigest()

def mono_fileid(cls, ns=""):
    """FileID cua MonoScript trong assembly precompiled (thuat toan Unity: md4 cua s\\0\\0\\0+ns+class)."""
    s = "s\x00\x00\x00" + ns + cls
    h = hashlib.new("md4", s.encode("utf-8")).digest()
    r = 0
    for i in range(3, -1, -1): r = (r << 8) | h[i]
    return r - 2**32 if r >= 2**31 else r

def build_ugui_tmp_map():
    """Quet package UGUI (chua ca TMP): className->fileID->guid, tach theo assembly export (UGUI/TMP)."""
    ugui, tmp = {}, {}
    pkgs = glob.glob(os.path.join(UGUI_PKG, "com.unity.ugui@*", "Runtime"))
    for base in pkgs:
        for dp, _, fns in os.walk(base):
            for fn in fns:
                if not fn.endswith(".cs.meta"): continue
                cs = os.path.join(dp, fn[:-5])
                if not os.path.exists(cs): continue
                cls = fn[:-8]
                txt = open(cs, encoding="utf-8", errors="ignore").read()
                g = re.search(r"guid: ([0-9a-f]{32})", open(os.path.join(dp, fn), encoding="utf-8").read())
                if not g: continue
                nsm = re.search(r"^\s*namespace\s+([\w.]+)", txt, re.M)
                ns = nsm.group(1) if nsm else ""
                fid = mono_fileid(cls, ns)
                (tmp if ns.startswith("TMPro") else ugui)[fid] = g.group(1)
    return ugui, tmp

# ======================= SETUP MOI TRUONG (1 LAN) =======================
print("[setup] giai nen xapk + load UnityPy ...", flush=True)
tmp = tempfile.mkdtemp(prefix="dr_uibatch_")
with zipfile.ZipFile(XAPK) as z: z.extractall(tmp)
apks = glob.glob(os.path.join(tmp, "**", "*.apk"), recursive=True) or [XAPK]
dirs = []
for a in apks:
    d = a + "_x"
    try:
        with zipfile.ZipFile(a) as z: z.extractall(d)
        b = os.path.join(d, "assets", "bin", "Data"); dirs.append(b if os.path.isdir(b) else d)
    except Exception: pass
env = UnityPy.load(*dirs)
gen = TypeTreeGenerator("2022.3.62f2"); gen.load_local_dll_folder(GA)
def to_dicts(nodes):
    return [{"m_Level":n.m_Level,"m_Type":n.m_Type,"m_Name":n.m_Name,"m_MetaFlag":n.m_MetaFlag} for n in nodes]
IMG = to_dicts(gen.get_nodes("UnityEngine.UI.dll", "UnityEngine.UI.Image"))
UGUI_MAP, TMP_MAP = build_ugui_tmp_map()
KEEP = set(UGUI_MAP.values()) | set(TMP_MAP.values()) | {UGUI, TMP}
print("[setup] UGUI class map:", len(UGUI_MAP), "TMP class map:", len(TMP_MAP), flush=True)

byfp = {}
for o in env.objects: byfp[(o.assets_file.name, o.path_id)] = o
def resolve(fileo, fid, pid):
    if not pid: return None
    if fid == 0: return byfp.get((fileo.assets_file.name, pid))
    exts = fileo.assets_file.externals
    if fid-1 < len(exts):
        base = os.path.basename(exts[fid-1].name)
        for (fn2, p2), oo in byfp.items():
            if p2 == pid and fn2.endswith(base): return oo
    return None
def tt(o, nodes=None):
    try: return o.read_typetree(nodes) if nodes else o.read_typetree()
    except Exception: return None
def mb_script(o):
    raw = o.get_raw_data()
    if len(raw) < 28: return (0, 0)
    return (struct.unpack_from("<i", raw, 16)[0], struct.unpack_from("<q", raw, 20)[0])
def mb_classname(o):
    fid, pid = mb_script(o); ms = resolve(o, fid, pid)
    if not ms: return None
    d = tt(ms); return d.get("m_ClassName") if d else None

# index GameObject theo ten + tat ca objects theo file
name2go = {}
for o in env.objects:
    if o.type.name == "GameObject":
        d = tt(o)
        if d: name2go.setdefault(d.get("m_Name"), []).append(o)
print("[setup] xong. GameObjects:", sum(len(v) for v in name2go.values()), flush=True)

# ---- NAME2GUID tu _ResourceGame (*.png.meta) ----
NAME2GUID = {}
for dp, _, fns in os.walk(RES):
    for fn in fns:
        if fn.endswith(".png.meta"):
            nm = fn[:-9]
            if nm in NAME2GUID: continue
            m = re.search(r"guid: ([0-9a-f]{32})", open(os.path.join(dp, fn), encoding="utf-8").read())
            if m: NAME2GUID[nm] = m.group(1)
ALIAS = {"gem_icon": "gem"}
print("[setup] sprite _ResourceGame:", len(NAME2GUID), flush=True)

# ======================= DUMP: doc sprite name theo DFS tu xapk =======================
def dump_names(target):
    """Tra ve list ten sprite (DFS) cho tung candidate root cung ten -> chon o buoc ngoai."""
    results = []
    for root in name2go.get(target, []):
        RF = root.assets_file.name
        by = {o.path_id: o for o in env.objects if o.assets_file.name == RF}
        def comps(gd): return [c.get("component", c).get("m_PathID", 0) for c in gd.get("m_Component", [])]
        def rect_of(gd):
            for pid in comps(gd):
                co = by.get(pid)
                if co and co.type.name in ("RectTransform", "Transform"): return co
            return None
        def img_name(gd):
            for pid in comps(gd):
                co = by.get(pid)
                if not co or co.type.name != "MonoBehaviour": continue
                if mb_classname(co) == "Image":
                    d = tt(co, IMG)
                    if d and "m_Sprite" in d:
                        sp = d["m_Sprite"]
                        so = resolve(co, sp.get("m_FileID", 0), sp.get("m_PathID", 0))
                        return (tt(so).get("m_Name") if so and tt(so) else "(missing)")
            return None
        # xac dinh la root prefab: RectTransform khong co cha
        rt = rect_of(tt(root) or {}); rtd = tt(rt) if rt else None
        is_root = bool(rtd) and (rtd.get("m_Father", {}).get("m_PathID", 0) == 0)
        seq = []
        def walk(gpid):
            go = by.get(gpid)
            if not go: return
            gd = tt(go)
            if not gd: return
            nm = img_name(gd)
            if nm is not None: seq.append(nm)
            rt2 = rect_of(gd); rd = tt(rt2) if rt2 else None
            if rd:
                for ch in rd.get("m_Children", []):
                    crt = by.get(ch.get("m_PathID", 0))
                    if crt:
                        crd = tt(crt)
                        if crd: walk(crd.get("m_GameObject", {}).get("m_PathID", 0))
        walk(root.path_id)
        results.append((is_root, seq))
    return results

# ======================= JOIN: doc export-guid theo DFS tu AssetRipper YAML =======================
def parse_export(target):
    src = os.path.join(GOBJ, target + ".prefab")
    raw = open(src, encoding="utf-8").read().replace("\r\n", "\n")
    docs = re.split(r"\n(?=--- !u!)", raw)
    obj = {}
    for d in docs:
        m = re.match(r"^--- !u!(\d+) &(\d+)", d)
        if m: obj[m.group(2)] = {"cls": m.group(1), "body": d}
    goName, goComps, rtGO, rtChildren, goRect = {}, {}, {}, {}, {}
    for i, o in obj.items():
        b = o["body"]
        if o["cls"] == "1":
            mm = re.search(r"m_Name: (.*)", b); goName[i] = (mm.group(1).strip() if mm else "?")
            goComps[i] = re.findall(r"- component: \{fileID: (\d+)\}", b)
        elif o["cls"] == "224":
            mm = re.search(r"m_GameObject: \{fileID: (\d+)\}", b); rtGO[i] = (mm.group(1) if mm else None)
            seg = b[b.find("m_Children:"): b.find("m_Father:")]
            rtChildren[i] = re.findall(r"- \{fileID: (\d+)\}", seg)
    for i in obj:
        if obj[i]["cls"] == "1":
            for c in goComps.get(i, []):
                if obj.get(c) and obj[c]["cls"] == "224": goRect[i] = c
    def image_guid_of(go_id):
        for c in goComps.get(go_id, []):
            o = obj.get(c)
            if not o or o["cls"] != "114": continue
            if not re.search(r"m_Script: \{fileID: %s," % re.escape(IMAGE_FILEID), o["body"]): continue
            m = re.search(r"m_Sprite: \{fileID: \d+, guid: ([0-9a-f]{32})", o["body"])
            return m.group(1) if m else "(none)"
        return None
    seq = []
    def dfs(go_id):
        g = image_guid_of(go_id)
        if g is not None: seq.append(g)
        for ch in rtChildren.get(goRect.get(go_id), []) or []:
            cgo = rtGO.get(ch)
            if cgo: dfs(cgo)
    root_id = next((i for i in goName if goName[i] == target), None)
    if root_id: dfs(root_id)
    return raw, seq

# ======================= REWIRE =======================
def rewire(target, feature, spritemap):
    raw, _ = parse_export(target)
    def repl_script(m):
        fid = int(m.group(1)); asm = m.group(2)
        tgt = (UGUI_MAP if asm == UGUI else TMP_MAP).get(fid)
        return "m_Script: {fileID: 11500000, guid: %s, type: 3}" % tgt if tgt else m.group(0)
    raw = re.sub(r"m_Script: \{fileID: (-?\d+), guid: (%s|%s), type: 3\}" % (UGUI, TMP), repl_script, raw)
    raw = re.sub(r"m_fontAsset: \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}",
                 "m_fontAsset: {fileID: 11400000, guid: %s, type: 2}" % FONT, raw)
    raw = re.sub(r"m_sharedMaterial: \{fileID: \d+, guid: [0-9a-f]{32}, type: 2\}",
                 "m_sharedMaterial: {fileID: %s, guid: %s, type: 2}" % (FONT_MAT, FONT), raw)
    for g, tgt in spritemap.items():
        pat = r"\{fileID: 21300000, guid: %s, type: 2\}" % g
        raw = re.sub(pat, ("{fileID: 21300000, guid: %s, type: 3}" % tgt) if tgt else "{fileID: 0}", raw)
    # strip moi MonoBehaviour script GAME
    docs = re.split(r"\n(?=--- !u!)", raw)
    removed, kept, stripped_cls = set(), [], set()
    for d in docs:
        am = re.match(r"^--- !u!\d+ &(\d+)", d); anchor = am.group(1) if am else None
        sm = re.search(r"m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32})", d); sg = sm.group(1) if sm else None
        is_mb = bool(re.match(r"^--- !u!114 ", d))
        if is_mb and sg and sg not in KEEP:
            if anchor: removed.add(anchor)
            stripped_cls.add(sg); continue
        kept.append(d)
    out = "\n".join(kept)
    for i in removed:
        out = re.sub(r"\s*- component: \{fileID: %s\}" % i, "", out)
    dest_dir = os.path.join(UIDIR, feature); os.makedirs(dest_dir, exist_ok=True)
    dest = os.path.join(dest_dir, target + ".prefab")
    if not out.endswith("\n"): out += "\n"
    open(dest, "w", encoding="utf-8").write(out)
    guid = stable_guid(feature + "/" + target)
    open(dest + ".meta", "w", encoding="utf-8").write(
        "fileFormatVersion: 2\nguid: %s\nPrefabImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n" % guid)
    # validate
    unmapped = set(re.findall(r"m_Script: \{fileID: (-?\d+), guid: (?:%s|%s)" % (UGUI, TMP), out))
    left = set(re.findall(r"m_Sprite: \{fileID: 21300000, guid: ([0-9a-f]{32}), type: 2\}", out))
    return {"stripped": len(removed), "unmapped_ui": sorted(unmapped), "sprite_left": sorted(left)}

# ======================= DRIVER =======================
def folder_meta(feature):
    p = os.path.join(UIDIR, feature); os.makedirs(p, exist_ok=True)
    mp = p + ".meta"
    if not os.path.exists(mp):
        open(mp, "w", encoding="utf-8").write(
            "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % stable_guid("folder:" + feature))

def process(target, feature):
    # 1) export DFS (dang tin cay - tu YAML co .prefab)
    _, export_seq = parse_export(target)
    export_imgs = [g for g in export_seq if g != "(none)"]
    # 2) dump candidates, chon root khop so Image
    cands = dump_names(target)
    if not cands:
        return {"target": target, "ok": False, "err": "khong thay GameObject trong xapk"}
    roots = [c for c in cands if c[0]] or cands
    # chon candidate co len(seq) gan len(export_seq) nhat
    best = min(roots, key=lambda c: abs(len(c[1]) - len(export_seq)))
    dump_seq = best[1]
    # 3) join
    g2n = {}; conflict = 0
    for i in range(min(len(export_seq), len(dump_seq))):
        g = export_seq[i]
        if g == "(none)": continue
        nm = dump_seq[i]
        if g in g2n and g2n[g] != nm: conflict += 1
        g2n[g] = nm
    spritemap = {}; missing = set()
    for g, nm in g2n.items():
        tg = NAME2GUID.get(nm) or NAME2GUID.get(ALIAS.get(nm, ""), None)
        spritemap[g] = tg
        if not tg: missing.add(nm)
    # 4) rewire
    r = rewire(target, feature, spritemap)
    return {"target": target, "ok": True, "feature": feature,
            "export_imgs": len(export_imgs), "dump_names": len(dump_seq),
            "mapped": sum(1 for v in spritemap.values() if v), "missing": sorted(missing),
            "conflict": conflict, **r}

if __name__ == "__main__":
    sel = sys.argv[1:]
    feats = sel if sel else list(FEATURES.keys())
    report = []
    for feat in feats:
        if feat not in FEATURES:
            print("!! khong co feature:", feat); continue
        folder_meta(feat)
        print("\n==== %s (%d prefab) ====" % (feat, len(FEATURES[feat])), flush=True)
        for tgt in FEATURES[feat]:
            r = process(tgt, feat)
            report.append(r)
            if not r["ok"]:
                print("  [X] %-34s %s" % (tgt, r["err"])); continue
            flag = ""
            if r["missing"]: flag += " missSprite=%d" % len(r["missing"])
            if r["unmapped_ui"]: flag += " UIclass!"
            if r["sprite_left"]: flag += " sprLeft=%d" % len(r["sprite_left"])
            if r["conflict"]: flag += " conflict=%d" % r["conflict"]
            print("  [OK] %-34s img=%d/%d mapped=%d strip=%d%s" % (
                tgt, r["dump_names"], r["export_imgs"], r["mapped"], r["stripped"], flag))
    json.dump(report, open(os.path.join(CACHE, "ui_batch_report.json"), "w"), indent=1, ensure_ascii=False)
    ok = sum(1 for r in report if r["ok"])
    print("\n=== TONG: %d/%d prefab, report -> tools/_rewire/ui_batch_report.json ===" % (ok, len(report)))

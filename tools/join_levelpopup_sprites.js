// Join export-sprite-guid <-> ten sprite that (dump tu xapk) theo thu tu DFS, roi tu tra guid _ResourceGame.
// Dung:  node tools/join_levelpopup_sprites.js <TenPrefab>   (mac dinh LevelPopup)
// Vao:  tools/_rewire/<Ten>.images.txt  (do dump sinh)  +  AssetRipper export prefab
// Ra:   tools/_rewire/<Ten>.spritemap.json  (export-guid -> target-guid) + bao sprite thieu.
const fs = require("fs"), path = require("path");
const ROOT = "E:/Project/2026DungeonRush";
const TARGET = process.argv[2] || "LevelPopup";
const SRC = `${ROOT}/AssetRipper/ExportedProject/Assets/GameObject/${TARGET}.prefab`;
const IMAGES = `${ROOT}/tools/_rewire/${TARGET}.images.txt`;
const RES = `${ROOT}/2026DungeonRushUnity/Assets/_Assets/_ResourceGame`;
const OUT = `${ROOT}/tools/_rewire/${TARGET}.spritemap.json`;

// 1) index ten sprite -> guid tu _ResourceGame (glob *.png.meta)
function walkMeta(dir, acc) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walkMeta(p, acc);
    else if (e.name.endsWith(".png.meta")) {
      const name = e.name.slice(0, -9); // bo .png.meta
      const g = (fs.readFileSync(p, "utf8").match(/guid: ([0-9a-f]{32})/) || [])[1];
      if (g && !(name in acc)) acc[name] = g;
    }
  }
}
const NAME2GUID = {};
walkMeta(RES, NAME2GUID);
// fallback ten hay thieu:
const ALIAS = { gem_icon: "gem" };

// 2) dump names theo DFS
const dumpNames = fs.readFileSync(IMAGES, "utf8").split(/\r?\n/).filter(Boolean);

// 3) parse export prefab, DFS y het dump, thu export sprite guid moi Image
const raw = fs.readFileSync(SRC, "utf8").replace(/\r\n/g, "\n");
const docs = raw.split(/\n(?=--- !u!)/), obj = {};
for (const d of docs) { const m = d.match(/^--- !u!(\d+) &(\d+)/); if (m) obj[m[2]] = { cls: m[1], body: d }; }
const goName = {}, goComps = {}, rtChildren = {}, rtGO = {}, goRect = {};
for (const id in obj) {
  const o = obj[id], b = o.body;
  if (o.cls === "1") {
    goName[id] = (b.match(/m_Name: (.*)/) || [])[1]?.trim() || "?";
    goComps[id] = [...b.matchAll(/- component: \{fileID: (\d+)\}/g)].map(x => x[1]);
  } else if (o.cls === "224") {
    rtGO[id] = (b.match(/m_GameObject: \{fileID: (\d+)\}/) || [])[1];
    const seg = b.slice(b.indexOf("m_Children:"), b.indexOf("m_Father:"));
    rtChildren[id] = [...seg.matchAll(/- \{fileID: (\d+)\}/g)].map(x => x[1]);
  }
}
for (const id in obj) if (obj[id].cls === "1")
  for (const c of goComps[id] || []) if (obj[c] && obj[c].cls === "224") goRect[id] = c;

// Image = UGUI script fileID -765806418 (assembly d3e719). Neu game khac, doi o day.
const IMAGE_FILEID = "-765806418";
function imageGuidOfGO(goId) {
  for (const c of goComps[goId] || []) {
    const o = obj[c];
    if (!o || o.cls !== "114") continue;
    if (!new RegExp(`m_Script: \\{fileID: ${IMAGE_FILEID},`).test(o.body)) continue;
    const m = o.body.match(/m_Sprite: \{fileID: \d+, guid: ([0-9a-f]{32})/);
    return m ? m[1] : "(none)";
  }
  return null;
}
const seq = [];
function dfs(goId) {
  const g = imageGuidOfGO(goId);
  if (g !== null) seq.push(g);
  for (const ch of rtChildren[goRect[goId]] || []) { const cgo = rtGO[ch]; if (cgo) dfs(cgo); }
}
dfs(Object.keys(goName).find(id => goName[id] === TARGET));

// 4) zip + tra guid
console.log(`export images: ${seq.length}  dump names: ${dumpNames.length}`);
const g2n = {}; let conflict = 0;
for (let i = 0; i < Math.min(seq.length, dumpNames.length); i++) {
  const g = seq[i], name = dumpNames[i];
  if (g === "(none)") continue;
  if (g2n[g] && g2n[g] !== name) { conflict++; console.log("CONFLICT", g, g2n[g], "vs", name); }
  g2n[g] = name;
}
const map = {}, missing = new Set();
for (const [g, name] of Object.entries(g2n)) {
  let tg = NAME2GUID[name] || NAME2GUID[ALIAS[name]] || null;
  if (!tg) { missing.add(name); }
  map[g] = tg; // null neu thieu -> rewire se de trong
}
fs.writeFileSync(OUT, JSON.stringify(map, null, 2));
console.log(`distinct guids: ${Object.keys(g2n).length}  conflicts: ${conflict}`);
if (missing.size) console.log("SPRITE THIEU trong _ResourceGame (de trong):", [...missing].join(", "));
console.log("-> wrote", OUT);

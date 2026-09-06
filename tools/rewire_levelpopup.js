// Rewire 1 prefab ripped (AssetRipper, meta-less) -> chay trong 2026DungeonRushUnity (LAYOUT + ANH).
// Dung:  node tools/rewire_levelpopup.js <TenPrefab>   (mac dinh LevelPopup)
// Vao:  AssetRipper export prefab + tools/_rewire/<Ten>.spritemap.json (do join sinh)
// Lam: (1) thay guid script UGUI/TMP + font -> guid chuan project; (2) map sprite; (3) strip MOI script game.
const fs = require("fs"), crypto = require("crypto");
const ROOT = "E:/Project/2026DungeonRush";
const TARGET = process.argv[2] || "LevelPopup";
const SRC = `${ROOT}/AssetRipper/ExportedProject/Assets/GameObject/${TARGET}.prefab`;
const DEST = `${ROOT}/2026DungeonRushUnity/Assets/_Assets/Resources/Prefabs/UI/${TARGET}.prefab`;
const SPRITEMAP = `${ROOT}/tools/_rewire/${TARGET}.spritemap.json`;

let raw = fs.readFileSync(SRC, "utf8").replace(/\r\n/g, "\n");

// ---- 1) script engine (assembly-guid, fileID) -> (target-guid, 11500000) ----
const UGUI = "d3e719b59ab71ba3f6b398058c866280", TMP = "67dfb1fdfb2b407222eda8e23ac8b724";
// Bang class engine da biet (them dong moi khi gap canh bao "UI class chua map").
const scriptMap = [
  [-765806418, UGUI, "fe87c0e1cc204ed48ad3b37840f39efc"], // Image
  [1392445389, UGUI, "4e29b1a8efbd4b44bb3f3716e73f07ff"], // Button
  [1297475563, UGUI, "59f8146938fff824cb5fd77236b75775"], // VerticalLayoutGroup
  [-1200242548, UGUI, "31a19414c41e5ae4aae2af33fee712f6"], // Mask
  [-113659843, UGUI, "67db9e8f0e2ae9c40bc1e2b64352a6b4"], // Slider
  [1453722849, TMP, "f4688fdb7df04437aeb418b961361dc5"], // TextMeshProUGUI
];
for (const [fid, g, tgt] of scriptMap)
  raw = raw.replace(new RegExp(`m_Script: \\{fileID: ${fid}, guid: ${g}, type: 3\\}`, "g"),
    `m_Script: {fileID: 11500000, guid: ${tgt}, type: 3}`);

// ---- 2) font: MOI m_fontAsset/m_sharedMaterial -> NotoSans-SemiBold SDF ----
const FONT = "c72fd0b1e013ab24aa65be3fd6e6a194", FONT_MAT = "5133364889018529741";
raw = raw.replace(/m_fontAsset: \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}/g,
  `m_fontAsset: {fileID: 11400000, guid: ${FONT}, type: 2}`);
raw = raw.replace(/m_sharedMaterial: \{fileID: \d+, guid: [0-9a-f]{32}, type: 2\}/g,
  `m_sharedMaterial: {fileID: ${FONT_MAT}, guid: ${FONT}, type: 2}`);

// ---- 3) sprite: theo spritemap.json (type 2 -> 3). null = de trong ----
const spriteMap = JSON.parse(fs.readFileSync(SPRITEMAP, "utf8"));
for (const [g, tgt] of Object.entries(spriteMap))
  raw = raw.replace(new RegExp(`\\{fileID: 21300000, guid: ${g}, type: 2\\}`, "g"),
    tgt ? `{fileID: 21300000, guid: ${tgt}, type: 3}` : `{fileID: 0}`);

// ---- 4) strip MOI MonoBehaviour script GAME. Giu: engine da remap + assembly UGUI/TMP chua map ----
const KEEP = new Set([...scriptMap.map(s => s[2]), UGUI, TMP]); // target guids + assembly guids
const docs = raw.split(/\n(?=--- !u!)/), removed = new Set(), kept = [];
let strippedClasses = new Set();
for (const d of docs) {
  const anchor = (d.match(/^--- !u!\d+ &(\d+)/) || [])[1];
  const sg = (d.match(/m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32})/) || [])[1];
  const isMB = /^--- !u!114 /.test(d);
  if (isMB && sg && !KEEP.has(sg)) { if (anchor) removed.add(anchor); strippedClasses.add(sg); continue; }
  kept.push(d);
}
let out = kept.join("\n");
for (const id of removed) out = out.replace(new RegExp(`\\s*- component: \\{fileID: ${id}\\}`, "g"), "");

// ---- 5) ghi + meta guid on dinh ----
fs.writeFileSync(DEST, out.endsWith("\n") ? out : out + "\n", "utf8");
let guid; try { guid = (fs.readFileSync(DEST + ".meta", "utf8").match(/guid: ([0-9a-f]{32})/) || [])[1]; } catch {}
if (!guid) guid = crypto.randomBytes(16).toString("hex");
fs.writeFileSync(DEST + ".meta",
  `fileFormatVersion: 2\nguid: ${guid}\nPrefabImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n`, "utf8");

// ---- 6) validate ----
const unmappedUI = [...out.matchAll(new RegExp(`m_Script: \\{fileID: (-?\\d+), guid: (${UGUI}|${TMP})`, "g"))];
const leftSprite = [...out.matchAll(/m_Sprite: \{fileID: 21300000, guid: ([0-9a-f]{32}), type: 2\}/g)];
console.log(`[${TARGET}] stripped ${removed.size} game-script comp (${strippedClasses.size} class); guid ${guid}`);
console.log("wrote:", DEST);
if (unmappedUI.length) console.log("!! UI class CHUA MAP (them vao scriptMap):", [...new Set(unmappedUI.map(m => m[1] + "@" + m[2].slice(0, 6)))].join(", "));
if (leftSprite.length) console.log("!! sprite export CHUA MAP:", [...new Set(leftSprite.map(m => m[1]))].join(", "));
if (!unmappedUI.length && !leftSprite.length) console.log("OK: khong con guid export sot.");

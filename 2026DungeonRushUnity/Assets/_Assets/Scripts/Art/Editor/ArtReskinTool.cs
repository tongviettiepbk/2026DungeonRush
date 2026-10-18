using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Reskin kiểu "thay ruột, giữ vỏ" (GDD/RESKIN_PLAN.md mục 2):
//  - Art giao PNG vào <repo>/ArtIncoming/ theo cây thư mục + tên file Y HỆT Assets/_Assets/Art/.
//  - Apply Reskin ghi đè byte PNG cũ, GIỮ .meta → GUID không đổi, prefab/SO/scene tự hiện ảnh mới.
//  - Ảnh mới khác kích thước: nhân Pixels Per Unit + viền 9-slice theo tỉ lệ → kích thước hiển thị như cũ.
//  - Thư mục/file bắt đầu bằng "_" hoặc "." trong ArtIncoming bị bỏ qua (_Concept, _Reference).
// Kết quả mỗi lần chạy ghi ở ArtIncoming/_Reference/reskin_report.txt; file đã thay cộng dồn vào reskin_log.csv.
public static class ArtReskinTool
{
    private const string ART_ROOT = "Assets/_Assets/Art/";
    private const string MENU = "Tools/DungeonRush/Reskin/";
    // Lệch tỉ lệ rộng/cao quá ngưỡng này thì báo (ảnh sẽ bị méo nếu ô UI cố định kích thước).
    private const float ASPECT_TOLERANCE = 0.03f;

    private static string IncomingDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../ArtIncoming"));
    private static string ReferenceDir => Path.Combine(IncomingDir, "_Reference");
    private static string LogPath => Path.Combine(ReferenceDir, "reskin_log.csv");

    [MenuItem(MENU + "1. Kiểm tra ArtIncoming (không ghi)")]
    private static void CheckOnly() => Run(false);

    [MenuItem(MENU + "2. Apply Reskin")]
    private static void Apply()
    {
        if (EditorUtility.DisplayDialog("Apply Reskin", "Ghi đè ảnh trong Art/ bằng ảnh cùng đường dẫn trong ArtIncoming/?", "Ghi đè", "Huỷ"))
            Run(true);
    }

    [MenuItem(MENU + "3. Xuất bảng ảnh (CSV) + thư mục gương")]
    private static void ExportManifest()
    {
        Directory.CreateDirectory(ReferenceDir);
        int folders = BuildMirrorFolders();
        int rows = WriteManifest(Path.Combine(ReferenceDir, "art_manifest.csv"));
        Debug.Log($"[Reskin] art_manifest.csv: {rows} ảnh; thư mục gương: {folders} → {IncomingDir}");
        EditorUtility.RevealInFinder(ReferenceDir);
    }

    // ---------------- Apply ----------------

    private class Entry
    {
        public string rel;          // đường dẫn tương đối dưới Art/, dấu "/"
        public string src;          // file trong ArtIncoming
        public string dst;          // asset path trong Art/
        public int oldW, oldH, newW, newH;
        public float oldPpu, newPpu;
        public Vector4 oldBorder, newBorder;
        public List<string> notes = new List<string>();
    }

    private static void Run(bool write)
    {
        if (!Directory.Exists(IncomingDir))
        {
            Debug.LogError("[Reskin] Không thấy thư mục " + IncomingDir);
            return;
        }

        var matched = new List<Entry>();
        var unmatched = new List<string>();
        var skipped = new List<string>();
        var same = new List<string>();

        foreach (string file in Directory.GetFiles(IncomingDir, "*", SearchOption.AllDirectories))
        {
            string rel = file.Substring(IncomingDir.Length + 1).Replace('\\', '/');
            if (rel.Split('/').Any(seg => seg.StartsWith("_") || seg.StartsWith("."))) continue;

            if (Path.GetExtension(rel).ToLowerInvariant() != ".png")
            {
                skipped.Add(rel + "  (không phải PNG)");
                continue;
            }

            string dst = ART_ROOT + rel;
            var importer = AssetImporter.GetAtPath(dst) as TextureImporter;
            if (importer == null)
            {
                unmatched.Add(rel);
                continue;
            }
            if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                skipped.Add(rel + "  (ảnh sheet nhiều sprite — phải cắt lại tay)");
                continue;
            }

            byte[] newBytes = File.ReadAllBytes(file);
            if (newBytes.SequenceEqual(File.ReadAllBytes(dst)))
            {
                same.Add(rel);
                continue;
            }

            var e = new Entry { rel = rel, src = file, dst = dst };
            importer.GetSourceTextureWidthAndHeight(out e.oldW, out e.oldH);
            if (!TryReadPngSize(newBytes, out e.newW, out e.newH))
            {
                skipped.Add(rel + "  (PNG hỏng)");
                continue;
            }

            // Giữ kích thước hiển thị: PPU và viền 9-slice nhân theo tỉ lệ ảnh mới / ảnh cũ.
            float sx = (float)e.newW / e.oldW;
            float sy = (float)e.newH / e.oldH;
            e.oldPpu = importer.spritePixelsPerUnit;
            e.newPpu = e.oldPpu * sx;
            e.oldBorder = importer.spriteBorder;
            e.newBorder = new Vector4(
                Mathf.Round(e.oldBorder.x * sx), Mathf.Round(e.oldBorder.y * sy),
                Mathf.Round(e.oldBorder.z * sx), Mathf.Round(e.oldBorder.w * sy));

            if (Mathf.Abs(sx / sy - 1f) > ASPECT_TOLERANCE)
                e.notes.Add($"lệch tỉ lệ {e.oldW}x{e.oldH} → {e.newW}x{e.newH}");
            if (e.oldBorder != Vector4.zero && (e.newW != e.oldW || e.newH != e.oldH))
                e.notes.Add($"9-slice: viền tự nhân thành {e.newBorder}, cần xem lại");
            if (Mathf.Max(e.newW, e.newH) > importer.maxTextureSize)
                e.notes.Add($"vượt Max Size {importer.maxTextureSize} → nâng lên");
            matched.Add(e);
        }

        if (write && matched.Count > 0)
            WriteEntries(matched);

        string report = BuildReport(write, matched, unmatched, skipped, same);
        Directory.CreateDirectory(ReferenceDir);
        File.WriteAllText(Path.Combine(ReferenceDir, "reskin_report.txt"), report, new UTF8Encoding(true));
        Debug.Log(report);
    }

    private static void WriteEntries(List<Entry> entries)
    {
        foreach (var e in entries)
            File.Copy(e.src, Path.GetFullPath(e.dst), true);

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var e in entries)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(e.dst);
                importer.spritePixelsPerUnit = e.newPpu;
                importer.spriteBorder = e.newBorder;
                int maxSide = Mathf.Max(e.newW, e.newH);
                if (maxSide > importer.maxTextureSize)
                    importer.maxTextureSize = Mathf.Min(16384, Mathf.NextPowerOfTwo(maxSide));
                importer.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Directory.CreateDirectory(ReferenceDir);
        bool newLog = !File.Exists(LogPath);
        var sb = new StringBuilder();
        if (newLog) sb.AppendLine("ngay,duong_dan,cu_rong,cu_cao,moi_rong,moi_cao,ppu_cu,ppu_moi,ghi_chu");
        string today = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        foreach (var e in entries)
            sb.AppendLine(string.Join(",", today, Csv(e.rel), e.oldW, e.oldH, e.newW, e.newH,
                e.oldPpu.ToString("0.##"), e.newPpu.ToString("0.##"), Csv(string.Join("; ", e.notes))));
        if (newLog) File.WriteAllText(LogPath, sb.ToString(), new UTF8Encoding(true));
        else File.AppendAllText(LogPath, sb.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    private static string BuildReport(bool write, List<Entry> matched, List<string> unmatched, List<string> skipped, List<string> same)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[Reskin] {(write ? "ĐÃ GHI" : "KIỂM TRA")} {DateTime.Now:yyyy-MM-dd HH:mm} — {IncomingDir}");
        sb.AppendLine($"  {(write ? "Đã thay" : "Sẽ thay")}: {matched.Count} | Giống hệt (bỏ qua): {same.Count} | Không có ảnh cũ: {unmatched.Count} | Bỏ qua: {skipped.Count}");

        var flagged = matched.Where(e => e.notes.Count > 0).ToList();
        if (flagged.Count > 0)
        {
            sb.AppendLine("\n== CẦN XEM LẠI");
            foreach (var e in flagged) sb.AppendLine($"  {e.rel}: {string.Join("; ", e.notes)}");
        }
        if (unmatched.Count > 0)
        {
            sb.AppendLine("\n== KHÔNG CÓ ẢNH CŨ CÙNG ĐƯỜNG DẪN (sai tên/thư mục, hoặc ảnh mới cần gán tay)");
            foreach (string r in unmatched) sb.AppendLine("  " + r);
        }
        if (skipped.Count > 0)
        {
            sb.AppendLine("\n== BỎ QUA");
            foreach (string r in skipped) sb.AppendLine("  " + r);
        }
        sb.AppendLine($"\n== {(write ? "ĐÃ THAY" : "SẼ THAY")}");
        foreach (var e in matched)
            sb.AppendLine($"  {e.rel}  {e.oldW}x{e.oldH} → {e.newW}x{e.newH}  ppu {e.oldPpu:0.##} → {e.newPpu:0.##}");
        return sb.ToString();
    }

    // PNG: 8 byte chữ ký, chunk IHDR bắt đầu ở byte 8; rộng/cao big-endian ở byte 16..23.
    private static bool TryReadPngSize(byte[] b, out int w, out int h)
    {
        w = h = 0;
        if (b.Length < 24 || b[0] != 0x89 || b[1] != 0x50 || b[2] != 0x4E || b[3] != 0x47) return false;
        w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        return w > 0 && h > 0;
    }

    // ---------------- Manifest + thư mục gương ----------------

    // Tạo cây thư mục trong ArtIncoming giống Art/ (kèm .gitkeep) để art biết thả file vào đâu.
    private static int BuildMirrorFolders()
    {
        int count = 0;
        string artFull = Path.GetFullPath(ART_ROOT);
        foreach (string dir in Directory.GetDirectories(artFull, "*", SearchOption.AllDirectories))
        {
            string rel = dir.Substring(artFull.Length).TrimStart('/', '\\');
            string mirror = Path.Combine(IncomingDir, rel);
            Directory.CreateDirectory(mirror);
            string keep = Path.Combine(mirror, ".gitkeep");
            if (!File.Exists(keep)) File.WriteAllText(keep, "");
            count++;
        }
        return count;
    }

    private static int WriteManifest(string csvPath)
    {
        // Ảnh "đang dùng" = phụ thuộc (đệ quy) của scene build + mọi asset trong thư mục Resources.
        var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        string[] resourceDirs = Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories)
            .Select(d => d.Replace('\\', '/'))
            .Where(d => !d.Contains("/Editor/"))
            .ToArray();
        roots.AddRange(AssetDatabase.FindAssets("", resourceDirs)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !AssetDatabase.IsValidFolder(p)));
        string[] reachable = AssetDatabase.GetDependencies(roots.Distinct().ToArray(), true);

        var userCount = new Dictionary<string, int>();
        foreach (string p in reachable)
        {
            if (p.StartsWith(ART_ROOT)) continue;
            foreach (string d in AssetDatabase.GetDependencies(p, false))
            {
                if (!d.StartsWith(ART_ROOT)) continue;
                userCount.TryGetValue(d, out int n);
                userCount[d] = n + 1;
            }
        }
        var used = new HashSet<string>(reachable);
        var replaced = ReadReplacedLog();

        var sb = new StringBuilder();
        sb.AppendLine("duong_dan,thu_muc,rong,cao,ppu,9_slice,vien_LBRT,dang_dung,so_cho_dung,trang_thai");
        int rows = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ART_ROOT.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            string rel = path.Substring(ART_ROOT.Length);
            Vector4 b = importer.spriteBorder;
            userCount.TryGetValue(path, out int users);
            replaced.TryGetValue(rel, out string when);
            sb.AppendLine(string.Join(",",
                Csv(rel), Csv(Path.GetDirectoryName(rel).Replace('\\', '/')), w, h,
                importer.spritePixelsPerUnit.ToString("0.##"),
                b == Vector4.zero ? "không" : "có",
                b == Vector4.zero ? "" : $"{b.x} {b.y} {b.z} {b.w}",
                used.Contains(path) ? "có" : "không",
                users,
                when == null ? "cũ" : "đã thay " + when));
            rows++;
        }
        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
        return rows;
    }

    // rel → lần thay gần nhất, đọc từ reskin_log.csv.
    private static Dictionary<string, string> ReadReplacedLog()
    {
        var result = new Dictionary<string, string>();
        if (!File.Exists(LogPath)) return result;
        foreach (string line in File.ReadAllLines(LogPath).Skip(1))
        {
            string[] cols = line.Split(',');
            if (cols.Length > 1) result[cols[1].Trim('"')] = cols[0];
        }
        return result;
    }

    private static string Csv(string s)
    {
        return s.IndexOfAny(new[] { ',', '"' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }
}

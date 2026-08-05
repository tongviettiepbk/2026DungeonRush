#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// Sinh/regenerate asset ForgeData từ data gốc JSON (Resources/Json/forge_rarity_probabilities.txt).
// Menu: Tools > DungOnRush > Forge > Rebuild ForgeData Asset.
public static class ForgeDataImporter
{
    private const string JsonResourcePath = "Json/forge_rarity_probabilities";
    private const string AssetPath = "Assets/_Assets/Resources/Scriptable Objects/Forge/ForgeData.asset";

    [MenuItem("Tools/DungOnRush/Forge/Rebuild ForgeData Asset")]
    public static void Rebuild()
    {
        TextAsset json = Resources.Load<TextAsset>(JsonResourcePath);
        if (json == null)
        {
            Debug.LogError("[ForgeDataImporter] Không tìm thấy JSON gốc: Resources/" + JsonResourcePath);
            return;
        }

        List<List<float>> matrix = JsonConvert.DeserializeObject<List<List<float>>>(json.text);

        ForgeData asset = AssetDatabase.LoadAssetAtPath<ForgeData>(AssetPath);
        bool isNew = asset == null;
        if (isNew)
        {
            asset = ScriptableObject.CreateInstance<ForgeData>();
        }

        asset.rows = new List<ForgeData.Row>(matrix.Count);
        for (int i = 0; i < matrix.Count; i++)
        {
            asset.rows.Add(new ForgeData.Row { probabilities = matrix[i].ToArray() });
        }

        string dir = Path.GetDirectoryName(AssetPath);
        if (!AssetDatabase.IsValidFolder(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        if (isNew)
        {
            AssetDatabase.CreateAsset(asset, AssetPath);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ForgeDataImporter] Rebuilt ForgeData: " + matrix.Count + " rows -> " + AssetPath);
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

// SO chứa bảng xác suất Forge (100 dòng × 10 cột, mỗi dòng tổng = 100%).
// Nguồn gốc: Resources/Json/forge_rarity_probabilities.txt (giữ lại làm data gốc).
// Regenerate asset qua menu: Tools > DungOnRush > Forge > Rebuild ForgeData Asset.
// Đặt asset ở Resources/Scriptable Objects/Forge để StaticForgeData load được.
[CreateAssetMenu(fileName = "ForgeData", menuName = "DungOnRush/Forge Data")]
public class ForgeData : ScriptableObject
{
    // Mỗi Row = 1 Forge Level; probabilities có COLUMN_COUNT cột (Rarity 0..9).
    [System.Serializable]
    public class Row
    {
        public float[] probabilities;
    }

    public List<Row> rows = new List<Row>();

    public int MaxForgeLevel => rows.Count - 1;
}

using UnityEngine;

// Config 1 trang bị dạng CATALOG (C2 Mũ, C3 Găng, C4 Nhẫn, C5 Dây chuyền, C6 Ba lô).
// 5 loại này trong data gốc có cấu trúc y hệt nhau (chỉ ItemId/Tên/Rarity/Icon), stat sinh
// runtime theo rarity+level → gộp chung 1 class, phân biệt bằng `slot`.
// Tạo asset: Create > DungOnRush > Gear Item Data. Đặt trong Resources/Scriptable Objects/Gears/<Loại>.
[CreateAssetMenu(fileName = "Gear-", menuName = "DungOnRush/Gear Item Data")]
public class GearItemData : ScriptableObject
{
    public GearSlotType slot;
    public string assetName;        // Khoá duy nhất trong loại, VD "Helmet_1_1".
    public int itemId;
    public string displayName;      // Tên EN mặc định.
    public Rarity rarity;
    public string localizationKey;  // Lưu tường minh (gốc: Necklace dùng key khác tên asset "Neckle").
    public Sprite icon;
}

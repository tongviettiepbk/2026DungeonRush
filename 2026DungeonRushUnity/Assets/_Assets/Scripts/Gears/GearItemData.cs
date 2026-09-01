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

    // Ảnh gắn LÊN NGƯỜI khi mặc (khác icon inventory). Chưa gán → hệ mặc tạm dùng icon (xem
    // EquipVisualResolver). Gán art thật dần ở bước sau.
    public Sprite bodySprite;

    // true = trang bị của quái/boss (VD DragonHelmetData), KHÔNG rơi cho người chơi. Đồ enemy
    // đã tách sang Resources/Scriptable Objects/GearsEnemy nên vốn không được nạp vào pool loot;
    // cờ này giữ lại làm lưới an toàn (nếu lỡ đặt 1 asset enemy dưới Gears thì vẫn bị loại loot).
    public bool isMonsterGear;
}

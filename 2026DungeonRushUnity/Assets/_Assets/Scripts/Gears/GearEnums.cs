// Enum cho hệ trang bị (C2..C8). Weapon (C1) là module riêng nên không nằm ở đây.
// Giá trị chỉ để phân loại nội bộ; localization lưu tường minh trong asset.
public enum GearSlotType
{
    NONE = 0,
    HELMET = 1,    // C2 Mũ
    GLOVES = 2,    // C3 Găng tay
    RING = 3,      // C4 Nhẫn
    NECKLACE = 4,  // C5 Dây chuyền
    BACKPACK = 5,  // C6 Ba lô
    CAPE = 6,      // C7 Áo choàng
    WING = 7,      // C8 Cánh
}

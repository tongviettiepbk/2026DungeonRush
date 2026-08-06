// Enum THỐNG NHẤT cho slot trang bị — dùng chung cho cả 2 vai trò:
//   1. Phân loại catalog gear (C2..C8) — GearItemData.slot, tool gen_gear_assets.py.
//   2. Trục MẶC ĐỒ (equip) người chơi gán lên nhân vật — thay cho EquipSlot cũ.
// Giá trị int NONE..WING (0..7) là data nướng sẵn trong .asset (slot 1..5) → ĐỪNG đổi.
// WEAPON nối đuôi (=8) vì Vũ khí (C1) là module riêng, thêm sau khi gộp EquipSlot.
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
    WEAPON = 8,    // C1 Vũ khí (module riêng)
}

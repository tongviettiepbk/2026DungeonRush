// Hệ MẶC ĐỒ (equip) — trục thống nhất cho các slot NGƯỜI CHƠI mặc lên nhân vật.
// Khác GearSlotType (phân loại catalog C2..C8): ở đây gom cả Vũ khí (module riêng) và
// bỏ Ring/Necklace vì 2 slot đó KHÔNG hiển thị trên người. 6 slot dưới đây đúng bằng
// 6 bộ phận có hình trên rig Hero (Weapon/Helmet/Gloves/Backpack/Cape/Wing).
//
// Giá trị int cố định — dùng làm khoá lưu save (xem UserEquipmentData). ĐỪNG đổi số.
public enum EquipSlot
{
    Weapon = 0,   // Vũ khí (C1)
    Helmet = 1,   // Mũ (C2)
    Gloves = 2,   // Găng tay (C3)
    Backpack = 3, // Ba lô (C6)
    Cape = 4,     // Áo choàng (C7) — visual làm ở bước sau (Spine skin)
    Wing = 5,     // Cánh (C8)
}

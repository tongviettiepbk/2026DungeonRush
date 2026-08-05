using System.Collections.Generic;

// Save "người chơi đang mặc món gì ở mỗi slot". Bước 1: CHỈ lưu định danh món mặc, CHƯA
// tính/áp chỉ số (stats làm ở bước sau).
//
// Key dict = (int)EquipSlot dạng string để serialize JSON ổn định (như UserItemData).
// Value = định danh món:
//   - Weapon/Helmet/Gloves/Backpack: assetName (VD "Helmet_1_1")
//   - Wing: wingId dạng string (WingData tra theo int)
//   - Cape: capeId dạng string (làm ở bước sau)
public class UserEquipmentData : BaseUserData
{
    public Dictionary<string, string> equipped { get; set; } = new Dictionary<string, string>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_EQUIPMENT;
    }

    // Mặc 1 món vào slot (ghi đè món cũ nếu có). id rỗng coi như cởi.
    public void Equip(EquipSlot slot, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Unequip(slot);
            return;
        }

        equipped[Key(slot)] = id;
        isDataChanged = true;

        // TODO(stats): khi làm bước chỉ số, post EventID.EquipmentChanged để Hero reload stat + UI cập nhật.
    }

    // Cởi món ở slot.
    public void Unequip(EquipSlot slot)
    {
        if (equipped.Remove(Key(slot)))
        {
            isDataChanged = true;
        }
    }

    // Định danh món đang mặc ở slot (null nếu trống).
    public string GetEquipped(EquipSlot slot)
    {
        return equipped.TryGetValue(Key(slot), out string id) ? id : null;
    }

    public bool IsEquipped(EquipSlot slot)
    {
        return string.IsNullOrEmpty(GetEquipped(slot)) == false;
    }

    private static string Key(EquipSlot slot)
    {
        return ((int)slot).ToString();
    }
}

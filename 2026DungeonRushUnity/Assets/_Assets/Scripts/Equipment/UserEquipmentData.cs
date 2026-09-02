using System.Collections.Generic;

// Save "người chơi đang mặc món gì ở mỗi slot" — mirror format game gốc EquippedItems[]:
// mỗi món lưu {equipId(ItemId), rarity, level, subStats[]}.
//   - Main stat (số to) KHÔNG lưu → tính runtime từ (slot/weaponType, rarity, level) bằng GearStatCalculator.
//   - SubStats (+X% đã roll) LƯU SẴN vì không tái tạo lại được (roll random truncated-normal).
//
// Key dict = (int)GearSlotType dạng string để serialize JSON ổn định.
public class UserEquipmentData : BaseUserData
{
    public Dictionary<string, EquippedItemData> equipped { get; set; } = new Dictionary<string, EquippedItemData>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_EQUIPMENT;
    }

    // Mặc 1 món vào slot (ghi đè món cũ nếu có). id rỗng coi như cởi. Lưu kèm rarity/level/subStats
    // (từ LootResult) để Hero tính lại đủ chỉ số: main runtime + substat từ save.
    public void Equip(GearSlotType slot, string id, Rarity rarity, int level, List<GearSubStat> subStats)
    {
        if (string.IsNullOrEmpty(id))
        {
            Unequip(slot);
            return;
        }

        equipped[Key(slot)] = new EquippedItemData
        {
            equipId = id,
            rarity = rarity,
            level = level,
            subStats = subStats != null ? new List<GearSubStat>(subStats) : new List<GearSubStat>(),
        };
        isDataChanged = true;

        // Chỉ số: nơi gọi Equip (VD UIMainLobby) post EventID.EquipmentChanged → HeroUnit tính lại
        // stat (main + substat) + HeroVisual cập nhật hình.
    }

    // Cởi món ở slot.
    public void Unequip(GearSlotType slot)
    {
        if (equipped.Remove(Key(slot)))
        {
            isDataChanged = true;
        }
    }

    // Bản ghi đầy đủ món đang mặc ở slot (null nếu trống) — dùng cho tính chỉ số (rarity/level/subStats).
    public EquippedItemData GetRecord(GearSlotType slot)
    {
        return equipped.TryGetValue(Key(slot), out EquippedItemData rec) ? rec : null;
    }

    // Định danh món đang mặc ở slot (null nếu trống) — dùng cho visual + tra catalog.
    public string GetEquipped(GearSlotType slot)
    {
        EquippedItemData rec = GetRecord(slot);
        return rec != null ? rec.equipId : null;
    }

    public bool IsEquipped(GearSlotType slot)
    {
        return string.IsNullOrEmpty(GetEquipped(slot)) == false;
    }

    private static string Key(GearSlotType slot)
    {
        return ((int)slot).ToString();
    }
}

// 1 món đang mặc (mirror EquippedItems[] game gốc). Main stat tính runtime nên KHÔNG lưu ở đây.
[System.Serializable]
public class EquippedItemData
{
    public string equipId;                                   // = ItemId (assetName / wingId / capeId)
    public Rarity rarity;
    public int level;
    public List<GearSubStat> subStats = new List<GearSubStat>();
}

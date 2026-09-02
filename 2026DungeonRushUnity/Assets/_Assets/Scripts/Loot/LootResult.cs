using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// Phân loại item loot được: gear (C2..C6) hay vũ khí (C1).
public enum LootItemKind
{
    Gear = 0,
    Weapon = 1,
}

// DTO thuần chứa dữ liệu 1 item vừa loot ra. LootService tính sẵn mọi thứ vào đây,
// UI chỉ đọc để hiển thị (không truy vấn lại data/config). Không phụ thuộc UI.
public class LootResult
{
    public LootItemKind kind;
    public string displayName;
    public Rarity rarity;
    public int level;

    // Icon inventory (lấy từ GearItemData.icon / WeaponData.icon) để UI gắn thẳng vào slot.
    // [JsonIgnore]: Sprite là UnityEngine.Object — Newtonsoft bò vào bounds/Vector3.normalized gây
    // self-reference loop (vỡ khi JsonConvert.SerializeObject cả LootResult, VD log debug).
    [JsonIgnore] public Sprite icon;

    // Định danh món để MẶC (lưu vào UserEquipmentData + tra art gắn lên người). Gear/vũ khí đều
    // là assetName. Loot không ra Wing/Cape nên không cần dạng id số.
    public string equipId;

    // Chỉ dùng khi kind == Gear.
    public GearSlotType gearSlot;

    // Chỉ dùng khi kind == Weapon.
    public WeaponType weaponType;

    // Slot MẶC ĐỒ tương ứng để UI tìm đúng ElementEquipmentUILobby: vũ khí (C1) về slot WEAPON,
    // còn lại lấy theo gearSlot. Tránh việc weapon có gearSlot = NONE nên không khớp element nào.
    public GearSlotType EquipSlot
    {
        get { return kind == LootItemKind.Weapon ? GearSlotType.WEAPON : gearSlot; }
    }

    // Chỉ số chính (vũ khí luôn là Damage).
    public GearMainStatKind mainStatKind;
    public double mainStat;

    // Chỉ số phụ đã roll sẵn.
    public List<GearSubStat> subStats;
}

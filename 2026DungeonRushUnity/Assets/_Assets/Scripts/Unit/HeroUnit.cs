using UnityEngine;

// Hero (quân người chơi, TeamA). Hành vi CHỦ ĐỘNG đúng spec:
//   tự tìm enemy GẦN NHẤT trong map → đi tới vùng đánh → attack.
// Toàn bộ state machine (Idle→Move→Attack) đã có ở BaseUnit; Hero chỉ chỉnh cách chọn mục
// tiêu để không giới hạn tầm (luôn tìm khắp map) và không lọc theo biên camera.
//
// Hình ảnh trang bị (mặc đồ) do HeroVisual xử lý — ref các node đã gán sẵn trong prefab.
public class HeroUnit : BaseUnit
{
    [SerializeField] private HeroVisual heroVisual;

    protected override void Awake()
    {
        base.Awake();

        if (heroVisual == null)
        {
            heroVisual = GetComponentInChildren<HeroVisual>();
        }
        heroVisual?.RefreshAll();
    }

    // Dựng lại hình trang bị từ save — gọi sau khi người chơi đổi đồ ở menu.
    public void RefreshEquipment()
    {
        heroVisual?.RefreshAll();
    }

    protected override void FindNearestTarget()
    {
        target = FindNearestEnemyAmong(GetAliveEnemies());
    }

    // Combat theo vũ khí ĐANG MẶC (từ save). Vũ khí quyết định tầm đánh & có đạn hay không —
    // Hero không tự quyết tầm đánh. Damage vẫn tính chung từ stats (server-driven).
    protected override WeaponData GetCombatWeapon()
    {
        return ResolveEquippedWeapon();
    }

    // Vũ khí ở slot WEAPON (từ save) → WeaponData tĩnh. Null nếu chưa mặc / không tra được.
    private WeaponData ResolveEquippedWeapon()
    {
        UserEquipmentData equip = GameData.userData != null ? GameData.userData.equipment : null;
        if (equip == null || GameData.staticData == null || GameData.staticData.weapons == null)
        {
            return null;
        }

        string id = equip.GetEquipped(GearSlotType.WEAPON);
        return string.IsNullOrEmpty(id) ? null : GameData.staticData.weapons.GetData(id);
    }
}

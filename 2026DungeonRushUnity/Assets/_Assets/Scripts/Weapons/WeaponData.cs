using UnityEngine;

// Config 1 vũ khí (C1) — tạo asset qua menu: Create > DungOnRush > Weapon Data.
// Đặt asset trong Resources/Scriptable Objects/Weapons để StaticWeaponData load được.
//
// LƯU Ý (theo GDD): config KHÔNG chứa sát thương. Damage sinh runtime theo rarity + level
// (server-driven). Client chỉ giữ kiểu tấn công, tầm đánh, đạn và thông tin hiển thị.
[CreateAssetMenu(fileName = "Weapon-", menuName = "DungOnRush/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Định danh")]
    public string assetName;    // Khoá duy nhất, VD "Weapon_m_1_1" (itemId có thể trùng giữa các asset).
    public int itemId;          // Id item gốc, dùng cho localization: Items.Weapon.Name.{itemId}.
    public string displayName;  // Tên hiển thị mặc định (EN), VD "Pitchfork".
    public Rarity rarity;
    public Sprite icon;

    // Ảnh gắn LÊN NGƯỜI khi cầm vũ khí (khác icon inventory). Chưa gán → hệ mặc tạm dùng icon.
    public Sprite bodySprite;

    [Header("Tấn công")]
    public WeaponType weaponType;
    public float attackSpeed = 1f;      // Nhịp đánh (config gốc đều = 1).
    public float attackDistance = 1.5f; // Tầm đánh (ô): melee 1.5, ranged 3.5, boss ~100.
    public float projectileSpeed;       // Tốc độ đạn; 0 nếu không có projectile.
    public bool hasProjectile;

    [Header("Nguồn")]
    public bool isMonsterWeapon;        // true = vũ khí quái/boss, không rơi cho người chơi.

    public string GetLocalizeKey()
    {
        return "Items.Weapon.Name." + itemId;
    }
}

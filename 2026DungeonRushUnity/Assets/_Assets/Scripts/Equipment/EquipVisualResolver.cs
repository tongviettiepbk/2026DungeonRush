using UnityEngine;

// Tra ẢNH GẮN LÊN NGƯỜI cho 1 món đang mặc. Mỗi slot dùng config store khác nhau
// (Weapon/Gears/Wing) nên gom việc dispatch về đây cho HeroEquipmentVisual gọi gọn.
//
// Bước 1: art body thật (bodySprite) đa số CHƯA gán → fallback về icon inventory để thấy
// hình ngay khi test. Khi gán bodySprite dần thì tự dùng art đúng, không phải sửa code.
public static class EquipVisualResolver
{
    // Sprite gắn lên người cho (slot, id). Trả null nếu không tra được (slot chưa hỗ trợ, id sai...).
    public static Sprite GetBodySprite(EquipSlot slot, string id)
    {
        if (string.IsNullOrEmpty(id) || GameData.staticData == null)
        {
            return null;
        }

        switch (slot)
        {
            case EquipSlot.Weapon:
            {
                if (GameData.staticData.weapons == null) return null;
                WeaponData w = GameData.staticData.weapons.GetData(id);
                return w != null ? (w.bodySprite != null ? w.bodySprite : w.icon) : null;
            }

            case EquipSlot.Helmet:
            case EquipSlot.Gloves:
            case EquipSlot.Backpack:
            {
                if (GameData.staticData.gears == null) return null;
                GearItemData g = GameData.staticData.gears.GetData(id);
                return g != null ? (g.bodySprite != null ? g.bodySprite : g.icon) : null;
            }

            case EquipSlot.Wing:
            {
                if (GameData.staticData.wings != null && int.TryParse(id, out int wingId))
                {
                    WingData wing = GameData.staticData.wings.GetData(wingId);
                    return wing != null ? (wing.bodySprite != null ? wing.bodySprite : wing.icon) : null;
                }
                return null;
            }

            // Cape KHÔNG dùng sprite — là Spine skin, xem GetCapeData / HeroVisual.WearCape.
            default:
                return null;
        }
    }

    // Cape mặc lên người bằng Spine (skeletonData + skin), không phải sprite. Trả CapeData
    // để HeroVisual gán trực tiếp. id = capeId dạng string.
    public static CapeData GetCapeData(string id)
    {
        if (string.IsNullOrEmpty(id) || GameData.staticData == null || GameData.staticData.capes == null)
        {
            return null;
        }

        return int.TryParse(id, out int capeId) ? GameData.staticData.capes.GetData(capeId) : null;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Danh sách avatar (AvatarSelectPopup.AvatarSprites gốc, 22 hình đúng thứ tự gốc), index = AvatarId.
// Asset: Resources/Scriptable Objects/UI/AvatarConfig. Đổi số hình thì sửa AVATAR_COUNT ở server (bots.ts).
[CreateAssetMenu(fileName = "AvatarConfig", menuName = "DungOnRush/Avatar Config")]
public class AvatarConfig : ScriptableObject
{
    public List<Sprite> sprites = new List<Sprite>();

    private static AvatarConfig instance;

    public static List<Sprite> Sprites
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<AvatarConfig>("Scriptable Objects/UI/AvatarConfig");
            }
            return instance.sprites;
        }
    }

    // Id ngoài danh sách (dữ liệu cũ / server lạ) → avatar mặc định 0.
    public static Sprite Get(int avatarId)
    {
        List<Sprite> list = Sprites;
        return list[avatarId >= 0 && avatarId < list.Count ? avatarId : 0];
    }
}

using System.Collections.Generic;
using UnityEngine;

// Gắn/gỡ HÌNH trang bị lên rig nhân vật bằng cách set sprite của các SpriteRenderer có sẵn
// trên prefab (HelmetRenderer, BackpackRenderer, WingRenderer, LeftHand/RightHand, Weapon...).
//
// Bước 1: chỉ 5 slot dùng SpriteRenderer (Weapon/Helmet/Gloves/Backpack/Wing). Cape là Spine
// skin (node Cloak(Mid)) → xử lý ở bước sau, không đụng ở đây.
//
// Component tự resolve node THEO TÊN lúc Bind (giống BaseUnit.SetupRig) nên KHÔNG cần gán
// tham chiếu trong prefab. Nếu tên node trên prefab đổi thì sửa bảng bindings dưới đây.
public class HeroEquipmentVisual : MonoBehaviour
{
    // Mỗi slot ↔ danh sách tên node SpriteRenderer nhận hình. Slot có nhiều bộ phận (2 cánh,
    // 2 tay) thì set cùng 1 sprite cho tất cả — tách trái/phải khi làm art thật ở bước sau.
    private static readonly Dictionary<EquipSlot, string[]> bindings = new Dictionary<EquipSlot, string[]>
    {
        { EquipSlot.Weapon,   new[] { "Weapon" } },
        { EquipSlot.Helmet,   new[] { "HelmetRenderer" } },
        { EquipSlot.Gloves,   new[] { "LeftHand", "RightHand" } },
        { EquipSlot.Backpack, new[] { "BackpackRenderer" } },
        { EquipSlot.Wing,     new[] { "WingRenderer", "WingRenderer_1" } },
    };

    // Cache SpriteRenderer đã resolve theo slot (tránh tìm lại mỗi lần refresh).
    private readonly Dictionary<EquipSlot, List<SpriteRenderer>> renderers = new Dictionary<EquipSlot, List<SpriteRenderer>>();
    private bool isBound;

    // Resolve các node theo tên dưới 'root' (mặc định là transform của chính component).
    public void Bind(Transform root = null)
    {
        if (root == null)
        {
            root = transform;
        }

        renderers.Clear();

        foreach (KeyValuePair<EquipSlot, string[]> pair in bindings)
        {
            List<SpriteRenderer> list = new List<SpriteRenderer>();
            for (int i = 0; i < pair.Value.Length; i++)
            {
                Transform node = FindDeep(root, pair.Value[i]);
                if (node != null)
                {
                    SpriteRenderer sr = node.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        list.Add(sr);
                    }
                }
            }
            renderers[pair.Key] = list;
        }

        isBound = true;
    }

    // Dựng lại toàn bộ hình từ save. Gọi khi Hero khởi tạo / sau khi người chơi đổi đồ.
    public void RefreshAll()
    {
        if (isBound == false)
        {
            Bind();
        }

        UserEquipmentData data = GameData.userData != null ? GameData.userData.equipment : null;

        foreach (EquipSlot slot in bindings.Keys)
        {
            string id = data != null ? data.GetEquipped(slot) : null;
            Sprite sprite = string.IsNullOrEmpty(id) ? null : EquipVisualResolver.GetBodySprite(slot, id);
            ApplySlot(slot, sprite);
        }
    }

    // Set sprite (null = ẩn) cho mọi renderer của slot.
    public void ApplySlot(EquipSlot slot, Sprite sprite)
    {
        if (renderers.TryGetValue(slot, out List<SpriteRenderer> list) == false)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            list[i].sprite = sprite;
            list[i].enabled = sprite != null;
        }
    }

    // Tìm con theo tên ở MỌI độ sâu (BFS). transform.Find chỉ 1 cấp nên không đủ.
    private static Transform FindDeep(Transform root, string nodeName)
    {
        if (root.name == nodeName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), nodeName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

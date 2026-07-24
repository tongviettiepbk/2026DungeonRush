using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Mẫu StaticXxxData load config từ ScriptableObject trong Resources.
public class StaticItemData : List<ItemData>
{
    public StaticItemData()
    {
        List<ItemData> data = Resources.LoadAll<ItemData>("Scriptable Objects/Items").ToList();
        data = data.OrderBy(x => x.type).ToList();
        AddRange(data);
    }

    public ItemData GetData(ItemType type)
    {
        for (int i = 0; i < this.Count; i++)
        {
            if (this[i].type == type)
            {
                return this[i];
            }
        }

        DebugCustom.Log("[StaticItemData] Not found=" + type);
        return null;
    }
}

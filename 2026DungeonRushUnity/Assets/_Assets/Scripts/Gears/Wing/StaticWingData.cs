using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// C8 Cánh — load 10 WingData từ Resources (mỗi rarity 1 mẫu).
public class StaticWingData
{
    public List<WingData> wings;

    public StaticWingData()
    {
        wings = Resources.LoadAll<WingData>("Scriptable Objects/Gears/Wings")
            .OrderBy(x => x.wingId).ToList();
    }

    public WingData GetData(int wingId)
    {
        for (int i = 0; i < wings.Count; i++)
        {
            if (wings[i].wingId == wingId)
            {
                return wings[i];
            }
        }

        DebugCustom.Log("[StaticWingData] Not found wingId=" + wingId);
        return null;
    }

    public WingData GetByRarity(Rarity rarity)
    {
        for (int i = 0; i < wings.Count; i++)
        {
            if (wings[i].rarity == rarity)
            {
                return wings[i];
            }
        }

        return null;
    }
}

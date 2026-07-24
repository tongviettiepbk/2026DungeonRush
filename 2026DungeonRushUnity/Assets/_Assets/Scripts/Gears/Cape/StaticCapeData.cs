using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// C7 Áo choàng — load 12 CapeData + 1 CapeConfig từ Resources.
public class StaticCapeData
{
    public List<CapeData> capes;
    public CapeConfigData config;

    public StaticCapeData()
    {
        capes = Resources.LoadAll<CapeData>("Scriptable Objects/Gears/Capes")
            .OrderBy(x => x.capeId).ToList();
        config = Resources.Load<CapeConfigData>("Scriptable Objects/Gears/Capes/CapeConfig");
    }

    public CapeData GetData(int capeId)
    {
        for (int i = 0; i < capes.Count; i++)
        {
            if (capes[i].capeId == capeId)
            {
                return capes[i];
            }
        }

        DebugCustom.Log("[StaticCapeData] Not found capeId=" + capeId);
        return null;
    }
}

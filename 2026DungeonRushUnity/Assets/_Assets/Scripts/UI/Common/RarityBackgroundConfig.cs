using System.Collections.Generic;
using UnityEngine;

// Nền ô item theo rarity (GameResources.jgr / RarityBackgroundSprites gốc), index = Rarity.
// Asset: Resources/Scriptable Objects/UI/RarityBackgroundConfig (sprite _ResourceGame/BgRarity/tier_XX_background).
[CreateAssetMenu(fileName = "RarityBackgroundConfig", menuName = "DungOnRush/Rarity Background Config")]
public class RarityBackgroundConfig : ScriptableObject
{
    public List<Sprite> sprites = new List<Sprite>();

    private static RarityBackgroundConfig instance;

    public static Sprite Get(Rarity rarity)
    {
        if (instance == null)
        {
            instance = Resources.Load<RarityBackgroundConfig>("Scriptable Objects/UI/RarityBackgroundConfig");
        }

        int index = (int)rarity;
        return instance != null && index >= 0 && index < instance.sprites.Count ? instance.sprites[index] : null;
    }
}

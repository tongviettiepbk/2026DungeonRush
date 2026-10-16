using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Dụng cụ đào (MiningToolType gốc).
public enum MiningToolType
{
    Pickaxe = 0,
    Drill = 1,
    GoldenPickaxe = 2,
}

// Trạng thái nhân vật đào (MiningState gốc).
public enum MiningState
{
    Idle = 0,
    Moving = 1,
    Mining = 2,
    UsingTool = 3,
}

[Serializable]
public class MiningOfferTier
{
    public string StoreId;
    public string TitleKey;
    public int Drill;
    public int GoldenPickaxe;
    public int Pickaxe;
    public int Gem;
    public int MaxPickaxeIncrease;
}

// Bảng tỉ lệ quặng áp dụng từ MinMineLevel trở lên.
[Serializable]
public class MineRarityLevel
{
    public int MinMineLevel;
    public List<MineOreWeight> OreWeights = new List<MineOreWeight>();
}

// Mỗi level mỏ cần DepthPerLevel mét, áp dụng tới MaxMineLevel (0 = vô hạn).
[Serializable]
public class MineLevelDepthTier
{
    public int MaxMineLevel;
    public int DepthPerLevel;
}

[Serializable]
public class MineOreWeight
{
    public MineOreType OreType;
    public float Weight;
}

[Serializable]
public class MineOreConfig
{
    public MineOreType OreType;
    public Sprite Sprite;
    public int HitsRequired = 1;
    public bool IsCollectible;
    public GameObject ParticlePrefab;
    public List<AudioClip> HitSounds = new List<AudioClip>();
    [Range(0f, 1f)] public float HitSoundVolume = 1f;
}

[Serializable]
public class MineOreSprite
{
    public MineOreType OreType;
    public Sprite Sprite;
}

[Serializable]
public class MineOreSpriteAsset
{
    public MineOreType OreType;
    public TMP_SpriteAsset SpriteAsset;
    public int SpriteIndex;
}

// Config hệ Đào mỏ (MiningConfig gốc, reverse il2cpp v41 — giá trị lấy từ asset gốc).
// Asset: Resources/Scriptable Objects/Mining/MiningConfig. Doc: DecodedData/MINING_MODEL.md.
[CreateAssetMenu(fileName = "MiningConfig", menuName = "DungOnRush/Mining Config")]
public class MiningConfig : ScriptableObject
{
    public int GridWidth = 9;
    public int VisibleRows = 9;
    public int BaselineRow = 8;
    public float CellSize = 1f;
    [Range(0.1f, 3f)] public float BlockScale = 1.09f;
    [Range(0f, 1f)] public float EmptinessThreshold = 0.4f;
    public List<MineLevelDepthTier> DepthTiers = new List<MineLevelDepthTier>();
    public List<MineRarityLevel> RarityLevels = new List<MineRarityLevel>();
    public int PickaxeRegenIntervalMinutes = 5;
    public int MaxPickaxeCount = 30;
    public int StartingPickaxeCount = 30;
    public List<MiningOfferTier> MiningOfferTiers = new List<MiningOfferTier>();
    public int DrillDepth = 8;
    public int GoldenPickaxeRows = 5;
    public float GoldenPickaxeRowDelay = 0.15f;
    public int DrillGemCost = 50;
    public int GoldenPickaxeGemCost = 100;
    public int PickaxeAdRewardCount = 10;
    public int DrillAdRewardCount = 1;
    public int GoldenPickaxeAdRewardCount = 1;
    public int DailyPickaxeAdLimit = 6;
    public int DailyDrillAdLimit = 6;
    public int DailyGoldenPickaxeAdLimit = 6;
    public float CharacterMoveSpeed = 4f;
    public float PowerUpMoveSpeed = 8f;
    public Sprite PickaxeSprite;
    public AnimatorOverrideController PickaxeAnimator;
    public Sprite DrillSprite;
    public AnimatorOverrideController DrillAnimator;
    public Sprite GoldenPickaxeSprite;
    public AnimatorOverrideController GoldenPickaxeAnimator;
    public float CameraScrollSpeed = 3f;
    public GameObject BlockPrefab;
    public GameObject BlockHpUIPrefab;
    public GameObject MiningWorldPrefab;
    public List<MineOreConfig> OreConfigs = new List<MineOreConfig>();
    public List<MineOreSprite> OreIcons = new List<MineOreSprite>();
    public List<MineOreSpriteAsset> OreSpriteAssets = new List<MineOreSpriteAsset>();

    // izl
    public Sprite GetToolSprite(MiningToolType tool)
    {
        switch (tool)
        {
            case MiningToolType.Drill: return DrillSprite;
            case MiningToolType.GoldenPickaxe: return GoldenPickaxeSprite;
            default: return PickaxeSprite;
        }
    }

    // izm
    public AnimatorOverrideController GetToolAnimator(MiningToolType tool)
    {
        switch (tool)
        {
            case MiningToolType.Drill: return DrillAnimator;
            case MiningToolType.GoldenPickaxe: return GoldenPickaxeAnimator;
            default: return PickaxeAnimator;
        }
    }

    // izo: gói offer kế tiếp (theo số gói đã mua), -1 = mua hết.
    public int GetOfferTierIndex(int purchasedCount)
    {
        return purchasedCount < MiningOfferTiers.Count ? purchasedCount : -1;
    }

    // izp
    public MiningOfferTier GetOfferTier(int index)
    {
        return index >= 0 && index < MiningOfferTiers.Count ? MiningOfferTiers[index] : null;
    }

    // izq
    public MiningOfferTier GetOfferTierByStoreId(string storeId)
    {
        for (int i = 0; i < MiningOfferTiers.Count; i++)
        {
            if (MiningOfferTiers[i].StoreId == storeId)
            {
                return MiningOfferTiers[i];
            }
        }
        return null;
    }

    public MineOreConfig GetOreConfig(MineOreType ore)
    {
        for (int i = 0; i < OreConfigs.Count; i++)
        {
            if (OreConfigs[i].OreType == ore)
            {
                return OreConfigs[i];
            }
        }
        return null;
    }

    // izr: 1 tiếng đập ngẫu nhiên của loại quặng.
    public AudioClip GetHitSound(MineOreType ore, out float volume)
    {
        MineOreConfig cfg = GetOreConfig(ore);
        volume = cfg != null ? cfg.HitSoundVolume : 1f;
        if (cfg == null || cfg.HitSounds == null || cfg.HitSounds.Count == 0)
        {
            return null;
        }
        return cfg.HitSounds[UnityEngine.Random.Range(0, cfg.HitSounds.Count)];
    }

    // izs
    public Sprite GetOreSprite(MineOreType ore)
    {
        MineOreConfig cfg = GetOreConfig(ore);
        return cfg != null ? cfg.Sprite : null;
    }

    // izt
    public Sprite GetOreIcon(MineOreType ore)
    {
        for (int i = 0; i < OreIcons.Count; i++)
        {
            if (OreIcons[i].OreType == ore)
            {
                return OreIcons[i].Sprite;
            }
        }
        return null;
    }

    // izu
    public GameObject GetOreParticle(MineOreType ore)
    {
        MineOreConfig cfg = GetOreConfig(ore);
        return cfg != null ? cfg.ParticlePrefab : null;
    }

    // izv
    public int GetHitsRequired(MineOreType ore)
    {
        MineOreConfig cfg = GetOreConfig(ore);
        return cfg != null ? cfg.HitsRequired : 1;
    }

    // izw
    public bool IsCollectible(MineOreType ore)
    {
        MineOreConfig cfg = GetOreConfig(ore);
        return cfg != null && cfg.IsCollectible;
    }

    // jad
    public TMP_SpriteAsset GetOreSpriteAsset(MineOreType ore)
    {
        for (int i = 0; i < OreSpriteAssets.Count; i++)
        {
            if (OreSpriteAssets[i].OreType == ore)
            {
                return OreSpriteAssets[i].SpriteAsset;
            }
        }
        return null;
    }

    // izx: level mỏ cao nhất = MinMineLevel lớn nhất trong RarityLevels (60).
    public int GetMaxMineLevel()
    {
        int max = 1;
        for (int i = 0; i < RarityLevels.Count; i++)
        {
            if (RarityLevels[i].MinMineLevel > max)
            {
                max = RarityLevels[i].MinMineLevel;
            }
        }
        return max;
    }

    // izy: số mét cần cho 1 level ở level `mineLevel` (tier đầu tiên có MaxMineLevel >= level; 0 = vô hạn).
    public int GetDepthPerLevel(int mineLevel)
    {
        if (DepthTiers.Count == 0)
        {
            return 0;
        }

        for (int i = 0; i < DepthTiers.Count; i++)
        {
            if (DepthTiers[i].MaxMineLevel < 1 || DepthTiers[i].MaxMineLevel >= mineLevel)
            {
                return DepthTiers[i].DepthPerLevel;
            }
        }
        return DepthTiers[DepthTiers.Count - 1].DepthPerLevel;
    }

    // izz: độ sâu bắt đầu của level `mineLevel` (tổng mét của các level 1..mineLevel-1).
    public int GetDepthForLevel(int mineLevel)
    {
        if (mineLevel < 2 || DepthTiers.Count == 0)
        {
            return 0;
        }

        int target = mineLevel - 1;
        int prevMax = 0;
        int total = 0;
        for (int i = 0; i < DepthTiers.Count; i++)
        {
            int perLevel = Mathf.Max(0, DepthTiers[i].DepthPerLevel);
            int tierMax = DepthTiers[i].MaxMineLevel >= 1 ? DepthTiers[i].MaxMineLevel : int.MaxValue;
            int levels = Mathf.Min(tierMax, target) - prevMax;
            if (levels > 0)
            {
                total += levels * perLevel;
            }
            if (tierMax >= target)
            {
                break;
            }
            prevMax = tierMax;
        }
        return total;
    }

    // jaa: level mỏ ứng với độ sâu.
    public int GetMineLevelFromDepth(int depth)
    {
        int level = 1;
        if (depth < 1)
        {
            return level;
        }

        for (int i = 0; i < DepthTiers.Count; i++)
        {
            int perLevel = DepthTiers[i].DepthPerLevel;
            if (perLevel < 1)
            {
                continue;
            }

            int tierMax = DepthTiers[i].MaxMineLevel >= 1 ? DepthTiers[i].MaxMineLevel : int.MaxValue;
            if (tierMax == int.MaxValue)
            {
                return level + depth / perLevel;
            }

            long tierDepth = (long)(tierMax - level + 1) * perLevel;
            if (tierDepth > depth)
            {
                return level + depth / perLevel;
            }

            depth -= (int)tierDepth;
            level = tierMax + 1;
        }
        return level;
    }

    // jab: bảng tỉ lệ quặng của level mỏ (bảng cuối có MinMineLevel <= level; không có thì bảng đầu).
    public MineRarityLevel GetRarityLevel(int mineLevel)
    {
        MineRarityLevel result = null;
        for (int i = 0; i < RarityLevels.Count; i++)
        {
            if (RarityLevels[i].MinMineLevel <= mineLevel)
            {
                result = RarityLevels[i];
            }
        }

        if (result == null && RarityLevels.Count > 0)
        {
            result = RarityLevels[0];
        }
        return result;
    }
}

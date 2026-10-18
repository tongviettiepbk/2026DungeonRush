using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

// Dựng toàn bộ asset hệ Đào mỏ theo bản gốc (Tools/DungeonRush/Build Mining/All):
//   - layer "Mining" (camera mỏ chỉ render layer này, camera chính bỏ layer này)
//   - Animations/Weapons/Weapon_Hammer_AnimatorController (gốc dùng chung cho cả 3 dụng cụ)
//   - Art/5_Maps/Campaign/Materials/_MiningGridMaterial (nền lưới nâu, Custom/GridShader)
//   - Prefabs/Mining/MiningBlock + MiningWorld (camera, BlockParent, nhân vật rig Soldier, 2 tấm nền)
//   - Resources/Scriptable Objects/Mining/MiningConfig (số liệu DecodedData/tables/MiningConfig.json)
//   - Resources/Prefabs/UI/UIMiningPopup, UIMiningRarityPopup, UIMiningOfferPopup (bọc bản rip Prefabs/UI/Mining)
// Chạy lại là GHI ĐÈ — đã chỉnh tay prefab nào thì chạy từng mục riêng, bỏ mục đó.
public static class MiningPrefabBuilder
{
    private const string LAYER = "Mining";
    private const string RIP = "Assets/_Assets/Prefabs/UI/Mining/";
    private const string UI_OUT = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string WORLD_DIR = "Assets/_Assets/Prefabs/Mining/";
    private const string BLOCK_PATH = WORLD_DIR + "MiningBlock.prefab";
    private const string WORLD_PATH = WORLD_DIR + "MiningWorld.prefab";
    private const string CONFIG_DIR = "Assets/_Assets/Resources/Scriptable Objects/Mining/";
    private const string CONFIG_PATH = CONFIG_DIR + "MiningConfig.asset";
    private const string MATERIAL_PATH = "Assets/_Assets/Art/5_Maps/Campaign/Materials/_MiningGridMaterial.mat";
    private const string HAMMER_PATH = "Assets/_Assets/Animations/Weapons/Weapon_Hammer_AnimatorController.overrideController";
    private const string CHARACTER_CONTROLLER = "Assets/_Assets/Animations/Heroes/CharacterAnimatorController.controller";
    private const string PUNCH_CONTROLLER = "Assets/_Assets/Animations/Heroes/PunchAnimatorController.overrideController";
    private const string RESOURCE = "Assets/_Assets/Art/";
    private const string FX = "Assets/_Assets/Prefabs/Fx/Mining/";

    [MenuItem("Tools/DungeonRush/Build Mining/All")]
    public static string BuildAll()
    {
        string log = EnsureLayer() + "\n";
        log += FixOreParticles() + "\n";
        log += BuildHammerController() + "\n";
        log += BuildGridMaterial() + "\n";
        log += BuildBlockPrefab() + "\n";
        log += BuildWorldPrefab() + "\n";
        log += BuildConfig() + "\n";
        log += BuildMiningPopup() + "\n";
        log += BuildRarityPopup() + "\n";
        log += BuildOfferPopup();
        AssetDatabase.SaveAssets();
        Debug.Log("[MiningPrefabBuilder]\n" + log);
        return log;
    }

    // ----- Layer -----

    [MenuItem("Tools/DungeonRush/Build Mining/Layer")]
    public static string EnsureLayer()
    {
        if (LayerMask.NameToLayer(LAYER) >= 0)
        {
            return "layer Mining có sẵn";
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty p = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(p.stringValue))
            {
                p.stringValue = LAYER;
                tagManager.ApplyModifiedProperties();
                return "thêm layer Mining = " + i;
            }
        }
        return "KHÔNG còn layer trống cho Mining";
    }

    // ----- FX vỡ khối -----

    // Hạt vỡ gốc (<Ore>Particle): material box_loot_AB + Texture Sheet Animation chế độ Sprites = sprite "box-loot"
    // (mảnh vuông, màu theo từng quặng). Bản rip mất ref sprite (guid AssetRipper) → hạt không hiện; gán lại.
    [MenuItem("Tools/DungeonRush/Build Mining/Fix Ore Particles")]
    public static string FixOreParticles()
    {
        Sprite chunk = AssetDatabase.LoadAssetAtPath<Sprite>(RESOURCE + "2_Icons/Loot/box-loot.png");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Assets/Prefabs/Fx/Materials/box_loot_AB.mat");
        if (chunk == null || mat == null)
        {
            return "Ore particles FAILED (thiếu sprite box-loot / material box_loot_AB)";
        }

        string[] names = { "Dirt", "Coal", "Emerald", "Gold", "Diamond", "Iron", "Ruby", "Stone" };
        int count = 0;
        foreach (string n in names)
        {
            string path = FX + n + "Particle.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                continue;
            }

            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.TextureSheetAnimationModule tsa = ps.textureSheetAnimation;
                if (tsa.enabled && tsa.mode == ParticleSystemAnimationMode.Sprites)
                {
                    while (tsa.spriteCount > 0)
                    {
                        tsa.RemoveSprite(0);
                    }
                    tsa.AddSprite(chunk);
                }
                ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
                if (r != null && r.sharedMaterial == null)
                {
                    r.sharedMaterial = mat;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            count++;
        }
        return "ore particles fixed: " + count;
    }

    // ----- Animation -----

    [MenuItem("Tools/DungeonRush/Build Mining/Hammer Animator")]
    public static string BuildHammerController()
    {
        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(CHARACTER_CONTROLLER);
        AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Assets/Animations/Weapons/Weapon_Hammer_Attack.anim");
        AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Assets/Animations/Weapons/Weapon_Hammer_Idle.anim");
        if (baseController == null || attack == null || idle == null)
        {
            return "Hammer animator FAILED (thiếu controller/clip)";
        }

        AnimatorOverrideController aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(HAMMER_PATH);
        if (aoc == null)
        {
            aoc = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(aoc, HAMMER_PATH);
        }
        aoc.runtimeAnimatorController = baseController;

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);
        aoc.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            string n = overrides[i].Key.name;
            AnimationClip clip = n.Contains("Attack") ? attack : n.Contains("Idle") ? idle : null;
            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);
        }
        aoc.ApplyOverrides(overrides);
        EditorUtility.SetDirty(aoc);
        return "Weapon_Hammer_AnimatorController saved";
    }

    // ----- Material nền (_MiningGridMaterial gốc) -----

    [MenuItem("Tools/DungeonRush/Build Mining/Grid Material")]
    public static string BuildGridMaterial()
    {
        Shader shader = Shader.Find("Custom/GridShader");
        if (shader == null)
        {
            return "Grid material FAILED (không thấy Custom/GridShader)";
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MATERIAL_PATH);
        }
        mat.shader = shader;
        mat.SetColor("_Color", Color.white);
        mat.SetColor("_BackgroundColor", new Color(0.6509804f, 0.41960788f, 0.29803923f, 1f));
        mat.SetColor("_LineColor", new Color(0.57254905f, 0.3529412f, 0.25490198f, 1f));
        mat.SetVector("_GridSize", new Vector4(0.3f, 0.3f, 0f, 0f));
        mat.SetVector("_Offset", new Vector4(0.5f, 0.5f, 0f, 0f));
        mat.SetFloat("_LineWidth", 0.1f);
        mat.SetFloat("_Padding", 0.018f);
        mat.SetFloat("_Roundness", 0f);
        mat.SetFloat("_NoiseScale", 0f);
        mat.SetFloat("_NoiseStrength", 0f);
        EditorUtility.SetDirty(mat);
        return "_MiningGridMaterial saved";
    }

    // ----- Khối (BlockPrefab gốc) -----

    [MenuItem("Tools/DungeonRush/Build Mining/Block Prefab")]
    public static string BuildBlockPrefab()
    {
        Directory.CreateDirectory(WORLD_DIR);
        GameObject root = new GameObject("MiningBlock");
        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = FindSprite("coal_mine");
        sr.sortingOrder = 0;
        root.transform.localScale = Vector3.one * 0.7146f;

        SpriteRenderer damage = NewSprite("DamageOverlay", root.transform, FindSprite("damage_overlay"), 1, Vector3.zero, 1.6535047f);
        damage.color = new Color(1f, 1f, 1f, 0.75686276f);
        damage.enabled = false;
        SpriteRenderer click = NewSprite("ClickOverlay", root.transform, FindSprite("click_overlay"), 2, Vector3.zero, 1.6585613f);
        click.color = new Color(0.85466355f, 0.9622642f, 0.077162705f, 1f);
        click.enabled = false;

        MiningBlock block = root.AddComponent<MiningBlock>();
        block.spriteRenderer = sr;
        block.damageOverlay = damage;
        block.clickOverlay = click;
        return SavePrefab(root, BLOCK_PATH);
    }

    // ----- World (MiningWorld gốc) -----

    [MenuItem("Tools/DungeonRush/Build Mining/World Prefab")]
    public static string BuildWorldPrefab()
    {
        Directory.CreateDirectory(WORLD_DIR);
        GameObject root = new GameObject("MiningWorld");
        MiningController controller = root.AddComponent<MiningController>();

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(RESOURCE + "1_UIKit/Common/Square.png");
        Transform plane1 = NewPlane("Plane1", root.transform, square, mat, Vector3.zero);
        Transform plane2 = NewPlane("Plane2", root.transform, square, mat, new Vector3(0f, -10f, 0f));

        GameObject blockParent = new GameObject("BlockParent");
        blockParent.transform.SetParent(root.transform, false);

        GameObject camGo = new GameObject("MiningCamera");
        camGo.transform.SetParent(root.transform, false);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.depth = -10f;
        int layer = LayerMask.NameToLayer(LAYER);
        if (layer >= 0)
        {
            cam.cullingMask = 1 << layer;
        }
        MiningCamera miningCamera = camGo.AddComponent<MiningCamera>();
        miningCamera.miningCam = cam;

        MiningCharacter character = BuildCharacter(root.transform);

        GameObject gridGo = new GameObject("MiningGrid");
        gridGo.transform.SetParent(root.transform, false);
        MiningGrid grid = gridGo.AddComponent<MiningGrid>();

        controller.grid = grid;
        controller.character = character;
        controller.miningCamera = miningCamera;
        controller.blockParent = blockParent.transform;
        controller.backgroundPlane1 = plane1;
        controller.backgroundPlane2 = plane2;

        if (layer >= 0)
        {
            MiningGrid.SetLayerRecursive(root, layer);
        }
        return SavePrefab(root, WORLD_PATH);
    }

    // Rig nhân vật như gốc: Body (0.5) + 2 tay + dụng cụ + mũ + ba lô; Animator theo CharacterAnimatorController.
    private static MiningCharacter BuildCharacter(Transform parent)
    {
        GameObject go = new GameObject("MiningCharacter");
        go.transform.SetParent(parent, false);
        Animator animator = go.AddComponent<Animator>();
        RuntimeAnimatorController baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PUNCH_CONTROLLER);
        if (baseController == null)
        {
            baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CHARACTER_CONTROLLER);
        }
        animator.runtimeAnimatorController = baseController;

        SpriteRenderer body = NewSprite("Body", go.transform, FindSprite("body", "Character"), 4, Vector3.zero, 0.5f);
        Sprite hand = FindSprite("hand", "Character");
        SpriteRenderer left = NewSprite("LeftHand", body.transform, hand, 2, new Vector3(-0.45f, 0.354f, 0f), 1f);
        SpriteRenderer right = NewSprite("RightHand", body.transform, hand, 2, new Vector3(0.45f, 0.354f, 0f), 1f);
        SpriteRenderer weapon = NewSprite("Weapon", body.transform, FindSprite("pickaxe_character"), 1, new Vector3(0f, 0.87f, 0f), 1f);
        SpriteRenderer helmet = NewSprite("HelmetRenderer", body.transform, null, 5, new Vector3(0f, -0.19f, 0f), 1f);
        SpriteRenderer backpack = NewSprite("BackpackRenderer", body.transform, null, 3, Vector3.zero, 1f);

        MiningCharacter character = go.AddComponent<MiningCharacter>();
        character.animator = animator;
        character.bodyTransform = body.transform;
        character.helmetRenderer = helmet;
        character.backpackRenderer = backpack;
        character.glovesRendererLeft = left;
        character.glovesRendererRight = right;
        character.mineToolRenderer = weapon;
        return character;
    }

    private static Transform NewPlane(string name, Transform parent, Sprite sprite, Material mat, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(20f, 20f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -19;
        if (mat != null)
        {
            sr.sharedMaterial = mat;
        }
        return go.transform;
    }

    private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int order, Vector3 pos, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // ----- Config -----

    [MenuItem("Tools/DungeonRush/Build Mining/Config")]
    public static string BuildConfig()
    {
        string jsonPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../DecodedData/tables/MiningConfig.json"));
        if (File.Exists(jsonPath) == false)
        {
            return "Config FAILED: không thấy " + jsonPath;
        }

        JObject src = (JObject)JArray.Parse(File.ReadAllText(jsonPath))[0];
        Directory.CreateDirectory(CONFIG_DIR);
        MiningConfig cfg = AssetDatabase.LoadAssetAtPath<MiningConfig>(CONFIG_PATH);
        bool isNew = cfg == null;
        if (isNew)
        {
            cfg = ScriptableObject.CreateInstance<MiningConfig>();
        }

        cfg.GridWidth = (int)src["GridWidth"];
        cfg.VisibleRows = (int)src["VisibleRows"];
        cfg.BaselineRow = (int)src["BaselineRow"];
        cfg.CellSize = (float)src["CellSize"];
        cfg.BlockScale = (float)src["BlockScale"];
        cfg.EmptinessThreshold = (float)src["EmptinessThreshold"];
        cfg.PickaxeRegenIntervalMinutes = (int)src["PickaxeRegenIntervalMinutes"];
        cfg.MaxPickaxeCount = (int)src["MaxPickaxeCount"];
        cfg.StartingPickaxeCount = (int)src["StartingPickaxeCount"];
        cfg.DrillDepth = (int)src["DrillDepth"];
        cfg.GoldenPickaxeRows = (int)src["GoldenPickaxeRows"];
        cfg.GoldenPickaxeRowDelay = (float)src["GoldenPickaxeRowDelay"];
        cfg.DrillGemCost = (int)src["DrillGemCost"];
        cfg.GoldenPickaxeGemCost = (int)src["GoldenPickaxeGemCost"];
        cfg.PickaxeAdRewardCount = (int)src["PickaxeAdRewardCount"];
        cfg.DrillAdRewardCount = (int)src["DrillAdRewardCount"];
        cfg.GoldenPickaxeAdRewardCount = (int)src["GoldenPickaxeAdRewardCount"];
        cfg.DailyPickaxeAdLimit = (int)src["DailyPickaxeAdLimit"];
        cfg.DailyDrillAdLimit = (int)src["DailyDrillAdLimit"];
        cfg.DailyGoldenPickaxeAdLimit = (int)src["DailyGoldenPickaxeAdLimit"];
        cfg.CharacterMoveSpeed = (float)src["CharacterMoveSpeed"];
        cfg.PowerUpMoveSpeed = (float)src["PowerUpMoveSpeed"];
        cfg.CameraScrollSpeed = (float)src["CameraScrollSpeed"];

        cfg.DepthTiers = new List<MineLevelDepthTier>();
        foreach (JToken t in src["DepthTiers"])
        {
            cfg.DepthTiers.Add(new MineLevelDepthTier { MaxMineLevel = (int)t["MaxMineLevel"], DepthPerLevel = (int)t["DepthPerLevel"] });
        }

        cfg.RarityLevels = new List<MineRarityLevel>();
        foreach (JToken t in src["RarityLevels"])
        {
            MineRarityLevel level = new MineRarityLevel { MinMineLevel = (int)t["MinMineLevel"] };
            foreach (JToken w in t["OreWeights"])
            {
                level.OreWeights.Add(new MineOreWeight { OreType = (MineOreType)(int)w["OreType"], Weight = (float)w["Weight"] });
            }
            cfg.RarityLevels.Add(level);
        }

        cfg.MiningOfferTiers = new List<MiningOfferTier>();
        foreach (JToken t in src["MiningOfferTiers"])
        {
            cfg.MiningOfferTiers.Add(new MiningOfferTier
            {
                StoreId = (string)t["StoreId"],
                TitleKey = (string)t["TitleKey"],
                Drill = (int)t["Drill"],
                GoldenPickaxe = (int)t["GoldenPickaxe"],
                Pickaxe = (int)t["Pickaxe"],
                Gem = (int)t["Gem"],
                MaxPickaxeIncrease = (int)t["MaxPickaxeIncrease"],
            });
        }

        // Asset gốc (đọc thẳng từ APK): dụng cụ cầm tay *_character, cả 3 dùng Weapon_Hammer_AnimatorController.
        AnimatorOverrideController hammer = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(HAMMER_PATH);
        cfg.PickaxeSprite = FindSprite("pickaxe_character");
        cfg.DrillSprite = FindSprite("drill_character");
        cfg.GoldenPickaxeSprite = FindSprite("gold_pickaxe_character");
        cfg.PickaxeAnimator = hammer;
        cfg.DrillAnimator = hammer;
        cfg.GoldenPickaxeAnimator = hammer;
        cfg.BlockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BLOCK_PATH);
        cfg.BlockHpUIPrefab = null;   // mọi quặng gốc 1 nhát → không cần thanh máu
        cfg.MiningWorldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WORLD_PATH);

        // OreConfigs theo thứ tự gốc: Dirt, Coal, Emerald, Gold, Diamond, Iron, Ruby, Stone.
        AudioClip[] dirtSounds =
        {
            FindAudio("footstep_dirt_walk_run_06"), FindAudio("footstep_dirt_walk_run_07"), FindAudio("footstep_dirt_walk_run_08"),
        };
        AudioClip[] oreSounds =
        {
            FindAudio("pick_axe_stone_small_hit_mine_impact_01"), FindAudio("pick_axe_stone_small_hit_mine_impact_02"),
            FindAudio("pick_axe_stone_small_hit_mine_impact_03"),
        };
        cfg.OreConfigs = new List<MineOreConfig>
        {
            Ore(MineOreType.Dirt, "dirt", "DirtParticle", false, dirtSounds),
            Ore(MineOreType.CoalMine, "coal_mine", "CoalParticle", true, oreSounds),
            Ore(MineOreType.EmeraldMine, "emerald_mine", "EmeraldParticle", true, oreSounds),
            Ore(MineOreType.GoldMine, "gold_mine", "GoldParticle", true, oreSounds),
            Ore(MineOreType.DiamondMine, "diamond_mine", "DiamondParticle", true, oreSounds),
            Ore(MineOreType.IronMine, "iron_mine", "IronParticle", true, oreSounds),
            Ore(MineOreType.RubyMine, "ruby_mine", "RubyParticle", true, oreSounds),
            Ore(MineOreType.Stone, "stone_obstacle", "StoneParticle", true, oreSounds),
        };

        cfg.OreIcons = new List<MineOreSprite>
        {
            new MineOreSprite { OreType = MineOreType.CoalMine, Sprite = FindSprite("coal_icon") },
            new MineOreSprite { OreType = MineOreType.DiamondMine, Sprite = FindSprite("diamond_icon") },
            new MineOreSprite { OreType = MineOreType.EmeraldMine, Sprite = FindSprite("emerald_icon") },
            new MineOreSprite { OreType = MineOreType.GoldMine, Sprite = FindSprite("gold_icon") },
            new MineOreSprite { OreType = MineOreType.IronMine, Sprite = FindSprite("iron_icon") },
            new MineOreSprite { OreType = MineOreType.RubyMine, Sprite = FindSprite("ruby_icon") },
            new MineOreSprite { OreType = MineOreType.Stone, Sprite = FindSprite("stone_icon") },
        };
        cfg.OreSpriteAssets = new List<MineOreSpriteAsset>();

        if (isNew)
        {
            AssetDatabase.CreateAsset(cfg, CONFIG_PATH);
        }
        EditorUtility.SetDirty(cfg);

        string missing = "";
        foreach (MineOreConfig o in cfg.OreConfigs)
        {
            if (o.Sprite == null) missing += " sprite:" + o.OreType;
            if (o.ParticlePrefab == null) missing += " fx:" + o.OreType;
        }
        if (cfg.PickaxeAnimator == null) missing += " hammer";
        return "MiningConfig saved" + (missing.Length > 0 ? " — THIẾU" + missing : "");
    }

    private static MineOreConfig Ore(MineOreType type, string sprite, string particle, bool collectible, AudioClip[] sounds)
    {
        MineOreConfig cfg = new MineOreConfig
        {
            OreType = type,
            Sprite = FindSprite(sprite),
            HitsRequired = 1,
            IsCollectible = collectible,
            ParticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FX + particle + ".prefab"),
            HitSoundVolume = 1f,
        };
        foreach (AudioClip clip in sounds)
        {
            if (clip != null) cfg.HitSounds.Add(clip);
        }
        return cfg;
    }

    // ----- UI -----

    [MenuItem("Tools/DungeonRush/Build Mining/Mining Popup")]
    public static string BuildMiningPopup()
    {
        GameObject root = NewRoot("UIMiningPopup", RIP + "MiningPopup.prefab", out Transform popup, out Button dark);
        // Nền mờ KHÔNG làm nút: nếu có Button ở ContentAll thì chạm RawImage bị nó nuốt, không tới UIMiningPopup.
        Object.DestroyImmediate(dark);
        UIMiningPopup ui = root.AddComponent<UIMiningPopup>();
        ui.isPopup = true;
        const string C = "Content/";
        ui.rawMining = Find(popup, C + "MineWorldFrame/Mask/RawImage").GetComponent<RawImage>();
        ui.prefabMiningWorld = AssetDatabase.LoadAssetAtPath<GameObject>(WORLD_PATH);

        ui.btPickaxe = Find(popup, C + "PickaxeButton").GetComponent<Button>();
        ui.btDrill = Find(popup, C + "DrillButton").GetComponent<Button>();
        ui.btGoldenPickaxe = Find(popup, C + "GoldenPickaxeButton").GetComponent<Button>();
        ui.imgPickaxeButton = ui.btPickaxe.GetComponent<Image>();
        ui.imgDrillButton = ui.btDrill.GetComponent<Image>();
        ui.imgGoldenPickaxeButton = ui.btGoldenPickaxe.GetComponent<Image>();
        ui.objPickaxeDefault = Find(popup, C + "PickaxeButton/Content/PickaxeDefaultParent").gameObject;
        ui.objPickaxeAd = Find(popup, C + "PickaxeButton/Content/PickaxeAdParent").gameObject;
        ui.objDrillDefault = Find(popup, C + "DrillButton/Content/DrillDefaultParent").gameObject;
        ui.objDrillAd = Find(popup, C + "DrillButton/Content/DrillAdParent").gameObject;
        ui.objGoldenPickaxeDefault = Find(popup, C + "GoldenPickaxeButton/Content/GoldPickaxeDefaultParent").gameObject;
        ui.objGoldenPickaxeAd = Find(popup, C + "GoldenPickaxeButton/Content/GoldPickaxeAdParent").gameObject;
        ui.txtPickaxeCount = Text(popup, C + "PickaxeButton/Content/PickaxeDefaultParent/PickaxeCounText");
        ui.txtDrillCount = Text(popup, C + "DrillButton/Content/DrillDefaultParent/DrillCountText");
        ui.txtGoldenPickaxeCount = Text(popup, C + "GoldenPickaxeButton/Content/GoldPickaxeDefaultParent/GoldPickaxeCountText");
        ui.txtPickaxeAdCount = Text(popup, C + "PickaxeButton/Content/PickaxeAdParent/PickaxeAdCountText");
        ui.txtDrillAdCount = Text(popup, C + "DrillButton/Content/DrillAdParent/DrillAdCountText");
        ui.txtGoldenPickaxeAdCount = Text(popup, C + "GoldenPickaxeButton/Content/GoldPickaxeAdParent/GoldPickaxeAdCountText");
        ui.txtPickaxeDailyLimit = Text(popup, C + "PickaxeButton/Content/PickaxeAdParent/PickaxeAdLimitText");
        ui.txtDrillDailyLimit = Text(popup, C + "DrillButton/Content/DrillAdParent/DrillAdLimitText");
        ui.txtGoldenPickaxeDailyLimit = Text(popup, C + "GoldenPickaxeButton/Content/GoldPickaxeAdParent/GoldPickaxeLimitCountText");

        // Icon quảng cáo "<sprite=0>" trong chữ giới hạn ads/ngày.
        TMP_SpriteAsset adIcon = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(RESOURCE + "2_Icons/Currency/IsAd.asset");
        ui.txtPickaxeDailyLimit.spriteAsset = adIcon;
        ui.txtDrillDailyLimit.spriteAsset = adIcon;
        ui.txtGoldenPickaxeDailyLimit.spriteAsset = adIcon;

        const string R = C + "ResourcesGridLayout/";
        ui.txtStone = Text(popup, R + "Item/StoneCountText");
        ui.txtCoal = Text(popup, R + "Item_1/CoalCountText");
        ui.txtIron = Text(popup, R + "Item_2/IronCountText");
        ui.txtRuby = Text(popup, R + "Item_3/RubyCountText");
        ui.txtEmerald = Text(popup, R + "Item_4/EmeraldCountText");
        ui.txtGold = Text(popup, R + "Item_5/GoldCountText");
        ui.txtDiamond = Text(popup, R + "Item_6/DiamondCountText");

        ui.sliderDepth = Find(popup, C + "DepthSlider").GetComponent<Slider>();
        ui.txtDepthSlider = Text(popup, C + "DepthSlider/CurrentLevelText");
        ui.txtMineLevel = Text(popup, C + "DepthSlider/MineLevelText");
        ui.txtPickaxeTimer = Text(popup, C + "NextPickaxeTimeText");
        ui.txtDepth = Text(popup, C + "MineWorldFrame/Mask/Image/Image/DepthText");

        ui.btRarity = Find(popup, C + "RarityButton").GetComponent<Button>();
        ui.btClose = Find(popup, C + "CloseButton").GetComponent<Button>();
        ui.btOffer = Find(popup, C + "MiningOfferButton").GetComponent<Button>();

        // Bản rip: chữ trên khung (DepthText, title) có Button — bỏ để không chặn chạm vào lưới.
        RemoveButton(Find(popup, C + "MineWorldFrame/Mask/Image/Image/DepthText"));
        RemoveButton(Find(popup, C + "NicknameText"));
        // Lớp phủ Dark/Image trong Mask không nhận chạm (để điểm chạm rơi xuống RawImage → popup).
        foreach (Graphic g in Find(popup, C + "MineWorldFrame").GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = g == ui.rawMining;
        }
        return SavePrefab(root, UI_OUT + "UIMiningPopup.prefab");
    }

    [MenuItem("Tools/DungeonRush/Build Mining/Rarity Popup")]
    public static string BuildRarityPopup()
    {
        GameObject root = NewRoot("UIMiningRarityPopup", RIP + "MiningRarityPopup.prefab", out Transform popup, out Button dark);
        UIMiningRarityPopup ui = root.AddComponent<UIMiningRarityPopup>();
        ui.isPopup = true;
        const string V = "ContentTransform/NewItem/VerticalLayout/";
        ui.btClose = Find(popup, "ContentTransform/NewItem/CloseButton").GetComponent<Button>();
        ui.txtCurrentLevel = Text(popup, V + "Header/CurrentLevelText");
        ui.txtNextLevel = Text(popup, V + "Header/NextLevelText");
        ui.listCurrentRate = new List<TMP_Text>();
        ui.listNextRate = new List<TMP_Text>();
        // Thứ tự dòng = OreWeights[1..]: Stone, Coal, Iron, Ruby, Emerald, Gold, Diamond.
        string[] rows = { "Element1", "Element1_1", "Element1_4", "Element1_2", "Element1_3", "Element1_5", "Element1_6" };
        foreach (string row in rows)
        {
            ui.listCurrentRate.Add(Text(popup, V + row + "/CommonCurrentRate"));
            ui.listNextRate.Add(Text(popup, V + row + "/CommonNextRate"));
        }
        dark.onClick = new Button.ButtonClickedEvent();
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(dark.onClick, ui.Close);
        return SavePrefab(root, UI_OUT + "UIMiningRarityPopup.prefab");
    }

    [MenuItem("Tools/DungeonRush/Build Mining/Offer Popup")]
    public static string BuildOfferPopup()
    {
        GameObject root = NewRoot("UIMiningOfferPopup", RIP + "Mining_Bundle_PopupOffer.prefab", out Transform popup, out Button dark);
        UIMiningOfferPopup ui = root.AddComponent<UIMiningOfferPopup>();
        ui.isPopup = true;
        const string C = "ContentTransform/";
        ui.txtTitle = Text(popup, C + "Ribbon/Title");
        ui.txtDrill = Text(popup, C + "Content/Drill/DrillAmount");
        ui.txtGoldenPickaxe = Text(popup, C + "Content/Golden/GoldenPickaxeAmount");
        ui.txtPickaxe = Text(popup, C + "Content/Pickaxe/PickaxeAmount");
        ui.objGem = Find(popup, C + "Content/Gem").gameObject;
        ui.txtGem = Text(popup, C + "Content/Gem/GemAmount");
        ui.objMaxPickaxe = Find(popup, C + "Max+").gameObject;
        ui.txtMaxPickaxe = Text(popup, C + "IncreaseMaxPickaxeText");
        ui.btBuy = Find(popup, C + "ChestBundleBuyButton").GetComponent<Button>();
        ui.txtPrice = Text(popup, C + "ChestBundleBuyButton/Content/ChestBundlePriceText");
        ui.imgLoading = Find(popup, C + "LoadingImage").GetComponent<Image>();
        ui.btBackground = dark;
        // Nhãn "-70%" của bản rip là giảm giá gói rương, gói Mining gốc không có số này.
        Find(popup, C + "ChestBundleBuyButton/DiscountLabel").gameObject.SetActive(false);
        return SavePrefab(root, UI_OUT + "UIMiningOfferPopup.prefab");
    }

    // Root Canvas (1080×2160) → ContentAll (nền mờ, có Button để bấm ra ngoài) → bản rip đã unpack.
    private static GameObject NewRoot(string name, string ripPath, out Transform popup, out Button dark)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.matchWidthOrHeight = 0f;

        GameObject contentAll = new GameObject("ContentAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        contentAll.layer = root.layer;
        RectTransform rt = contentAll.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        contentAll.GetComponent<Image>().color = new Color(0.149f, 0.149f, 0.149f, 0.588f);
        dark = contentAll.GetComponent<Button>();
        dark.transition = Selectable.Transition.None;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ripPath);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contentAll.transform);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        popup = go.transform;
        return root;
    }

    private static void RemoveButton(Transform t)
    {
        Button b = t.GetComponent<Button>();
        if (b != null)
        {
            Object.DestroyImmediate(b);
        }
        Graphic g = t.GetComponent<Graphic>();
        if (g != null)
        {
            g.raycastTarget = false;
        }
    }

    private static string SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        return Path.GetFileName(path) + (ok ? " saved" : " FAILED");
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[MiningPrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();

    // Sprite theo tên đúng trong Art/ (ưu tiên thư mục con `folderHint` nếu có).
    private static Sprite FindSprite(string name, string folderHint = null)
    {
        Sprite fallback = null;
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets/_Assets/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != name)
            {
                continue;
            }
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null)
            {
                continue;
            }
            if (folderHint == null || path.Contains("/" + folderHint + "/"))
            {
                return s;
            }
            if (fallback == null) fallback = s;
        }
        if (fallback == null) Debug.LogWarning("[MiningPrefabBuilder] Không thấy sprite " + name);
        return fallback;
    }

    private static AudioClip FindAudio(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AudioClip", new[] { "Assets/_Assets/_Sound" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name)
            {
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
        Debug.LogWarning("[MiningPrefabBuilder] Không thấy audio " + name);
        return null;
    }
}

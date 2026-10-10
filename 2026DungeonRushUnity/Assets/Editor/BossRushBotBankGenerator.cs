using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// Sinh "ngân hàng bộ đồ" cho bot Boss Rush (server/BOSS_RUSH_DESIGN.md mục 11.3) → server/functions/data/botBank.json.
// Mỗi chuỗi = một nhân vật ảo "chơi" qua 7 mốc (đầu mùa + 6 ngày) bằng ĐÚNG cơ chế của game: tỉ lệ ra đồ theo level
// (ForgeController.RollRarity), level món đồ (RollForgeLevel), substat (GearStatCalculator), chỉ mặc món làm Power
// tăng, và Power tính bằng công thức thật (PlayerPower.Calculate). Nhờ vậy bộ đồ của bot luôn khớp power hiển thị,
// và sức mạnh tăng theo bước nhảy như người thật. Server gán mỗi bot một chuỗi (bots.ts).
// Chạy lại sau khi đổi cân bằng đồ/Power, rồi deploy lại server.
public static class BossRushBotBankGenerator
{
    private const int CHAIN_COUNT = 480;
    // Hai đầu dải sức mạnh cần thêm bộ đồ riêng thì một sảnh 59 bot mới đủ người quanh mức của người chơi.
    private const int LOW_END_COUNT = 40;      // level 15–17, ít chơi
    private const int HIGH_END_COUNT = 60;     // level 88–100, cày nặng
    private const int STEP_COUNT = 7;
    private const int SEED = 20261010;
    private const int MIN_LEVEL = 15;      // Boss Rush mở ở PlayerLevel 15
    private const int MAX_LEVEL = 100;     // StaticExperienceData.MaxLevel
    private const int ENCHANT_UNLOCK_LEVEL = 20;   // GameResources.CultistDungeonUnlockPlayerLevel
    private const string OUTPUT_PATH = "../../server/functions/data/botBank.json";

    private class BankStep
    {
        public double power;
        public List<BossRushItemModel> items;
        public List<BossRushCompanionModel> companions;
        public List<int> enchantmentTiers;
        public bool showCloak;
    }

    private class BankChain
    {
        public string id;
        public List<BankStep> steps = new List<BankStep>();
    }

    private class Character
    {
        public int level;
        public bool showCloak = true;
        public int capeSummons;
        public UserEquipmentData equipment = new UserEquipmentData();
        public UserEnchantmentData enchantments = new UserEnchantmentData();
        public UserCompanionData companions = new UserCompanionData();
    }

    private static GearStatConfigData statConfig;
    private static List<GearItemData> gearPool;
    private static List<WeaponData> weaponPool;

    [MenuItem("DungeonRush/Boss Rush/Generate Bot Bank")]
    public static void GenerateFromMenu()
    {
        Debug.Log(Generate(CHAIN_COUNT, SEED));
    }

    public static string Generate(int chainCount, int seed)
    {
        if (LoadStatic() == false)
        {
            return "[BotBank] Thiếu data tĩnh (gear/weapon/GearStatConfig).";
        }

        Random.State saved = Random.state;
        List<BankChain> chains = new List<BankChain>();
        for (int i = 0; i < chainCount; i++)
        {
            Random.InitState(seed + i * 7919);
            // Rải đều level 15..100 để mọi mức sức mạnh đều có đủ bộ đồ cho một sảnh 59 bot.
            chains.Add(Simulate(MIN_LEVEL + Mathf.FloorToInt((MAX_LEVEL - MIN_LEVEL + 1) * (i + Random.value) / chainCount), 0.15f, 1.5f));
        }
        for (int i = 0; i < LOW_END_COUNT; i++)
        {
            Random.InitState(seed + (chainCount + i) * 7919);
            chains.Add(Simulate(Random.Range(MIN_LEVEL, MIN_LEVEL + 3), 0.08f, 0.35f));
        }
        for (int i = 0; i < HIGH_END_COUNT; i++)
        {
            Random.InitState(seed + (chainCount + LOW_END_COUNT + i) * 7919);
            chains.Add(Simulate(Random.Range(88, MAX_LEVEL + 1), 1.3f, 2f));
        }
        Random.state = saved;

        chains.Sort((a, b) => a.steps[0].power.CompareTo(b.steps[0].power));
        for (int i = 0; i < chains.Count; i++)
        {
            chains[i].id = "c" + (i + 1).ToString("0000");
        }

        string path = Path.GetFullPath(Path.Combine(Application.dataPath, OUTPUT_PATH));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonConvert.SerializeObject(chains, Formatting.None));

        return "[BotBank] " + chains.Count + " chuỗi × " + STEP_COUNT + " mốc → " + path
            + " (" + (new FileInfo(path).Length / 1024) + " KB) | power đầu mùa: thấp nhất " + chains[0].steps[0].power.ToLetter()
            + ", giữa " + chains[chains.Count / 2].steps[0].power.ToLetter()
            + ", cao nhất " + chains[chains.Count - 1].steps[0].power.ToLetter();
    }

    // ===== Mô phỏng một nhân vật =====

    private static BankChain Simulate(int level, float effortMin, float effortMax)
    {
        Character c = new Character();
        c.level = Mathf.Clamp(level, MIN_LEVEL, MAX_LEVEL);
        c.showCloak = Random.value < 0.9f;
        // Mức chăm: cùng level nhưng người ít chơi có đồ và pet kém hơn hẳn người cày.
        float effort = Random.Range(effortMin, effortMax);

        SummonCompanions(c, Mathf.RoundToInt(c.level * Random.Range(6f, 28f) * effort));
        Forge(c, Mathf.RoundToInt((150 + c.level * 12) * effort));
        SetupWing(c);
        SetupCape(c);
        SetupEnchantments(c, effort);

        BankChain chain = new BankChain();
        chain.steps.Add(Snapshot(c));
        for (int day = 1; day < STEP_COUNT; day++)
        {
            PlayOneDay(c);
            chain.steps.Add(Snapshot(c));
        }
        return chain;
    }

    // Một ngày chơi: ra thêm đồ (đôi khi được món hiếm hơn → power nhảy), nâng cánh, thỉnh thoảng nâng relic/áo/pet.
    private static void PlayOneDay(Character c)
    {
        if (Random.value < 0.4f && c.level < MAX_LEVEL)
        {
            c.level++;
        }
        Forge(c, Random.Range(20, 160));
        LevelUpWing(c, Random.Range(0, 4));
        if (Random.value < 0.1f)
        {
            RaiseEnchantment(c);
        }
        if (Random.value < 0.08f)
        {
            LevelUpCape(c, Random.Range(1, 4));
        }
        SummonCompanions(c, Random.Range(0, 60));
    }

    private static BankStep Snapshot(Character c)
    {
        return new BankStep
        {
            power = PlayerPower.ToLong(Power(c)),
            items = BossRushController.BuildItems(c.equipment),
            companions = BossRushController.BuildCompanions(c.companions),
            enchantmentTiers = BossRushController.BuildEnchantmentTiers(c.enchantments),
            showCloak = c.showCloak,
        };
    }

    private static double Power(Character c)
    {
        return PlayerPower.Calculate(c.equipment, c.enchantments, c.companions.owned);
    }

    // ===== Đồ (cùng cơ chế LootService.RollOne, nhưng trên bộ đồ của nhân vật ảo) =====

    private static void Forge(Character c, int rolls)
    {
        for (int i = 0; i < rolls; i++)
        {
            Rarity rarity = ForgeController.RollRarity(c.level - 1);
            List<GearItemData> gears = gearPool.FindAll(g => g.rarity == rarity);
            List<WeaponData> weapons = weaponPool.FindAll(w => w.rarity == rarity);
            while (gears.Count == 0 && weapons.Count == 0 && rarity > Rarity.Common)
            {
                rarity--;
                gears = gearPool.FindAll(g => g.rarity == rarity);
                weapons = weaponPool.FindAll(w => w.rarity == rarity);
            }
            if (gears.Count == 0 && weapons.Count == 0)
            {
                continue;
            }

            int index = Random.Range(0, gears.Count + weapons.Count);
            GearSlotType slot = index < gears.Count ? gears[index].slot : GearSlotType.WEAPON;
            string id = index < gears.Count ? gears[index].assetName : weapons[index - gears.Count].assetName;

            EquippedItemData worn = c.equipment.GetRecord(slot);
            int level = ForgeController.RollForgeLevel(worn == null ? ForgeController.ForgeMinLevel : worn.level,
                worn == null ? Rarity.Common : worn.rarity, rarity, worn == null);
            TryEquip(c, slot, id, rarity, level, GearStatCalculator.RollSubStats(statConfig, rarity));
        }
    }

    // Chỉ giữ món mới nếu Power tăng (người chơi thật cũng vậy); không thì trả lại món cũ.
    private static void TryEquip(Character c, GearSlotType slot, string id, Rarity rarity, int level, List<GearSubStat> subStats)
    {
        double before = Power(c);
        EquippedItemData old = c.equipment.GetRecord(slot);
        c.equipment.Equip(slot, id, rarity, level, subStats);
        if (Power(c) > before)
        {
            return;
        }
        if (old == null)
        {
            c.equipment.Unequip(slot);
        }
        else
        {
            c.equipment.Equip(slot, old.equipId, old.rarity, old.level, old.subStats);
        }
    }

    // ===== Cánh / áo choàng / relic / pet =====

    private static void SetupWing(Character c)
    {
        List<WingData> wings = GameData.staticData.wings.wings;
        if (c.level < WingService.UNLOCK_PLAYER_LEVEL || wings == null || wings.Count == 0)
        {
            return;
        }

        // Cánh hiếm dần theo level người chơi (chế bằng quặng đào được).
        int target = Mathf.Clamp(Mathf.RoundToInt(c.level / 12f + Random.Range(-1f, 1f)), 0, wings.Count - 1);
        WingData wing = wings[0];
        for (int i = 1; i < wings.Count; i++)
        {
            if (Mathf.Abs((int)wings[i].rarity - target) < Mathf.Abs((int)wing.rarity - target))
            {
                wing = wings[i];
            }
        }

        List<GearSubStat> subStats = new List<GearSubStat>();
        for (int i = 0; i < wing.subStats.Count; i++)
        {
            subStats.Add(new GearSubStat(wing.subStats[i].type, wing.subStats[i].value));
        }
        int level = Random.Range(1, Mathf.Clamp(10 + c.level, 2, Mathf.Max(2, wing.maxLevel)) + 1);
        c.equipment.Equip(GearSlotType.WING, wing.wingId.ToString(), wing.rarity, level, subStats);
    }

    private static void LevelUpWing(Character c, int levels)
    {
        EquippedItemData rec = c.equipment.GetRecord(GearSlotType.WING);
        if (rec == null || levels <= 0)
        {
            return;
        }
        WingData wing = GameData.staticData.wings.GetData(int.Parse(rec.equipId));
        int level = Mathf.Min(rec.level + levels, Mathf.Max(rec.level, wing != null ? wing.maxLevel : rec.level));
        c.equipment.Equip(GearSlotType.WING, rec.equipId, rec.rarity, level, rec.subStats);
    }

    private static void SetupCape(Character c)
    {
        List<CapeData> capes = GameData.staticData.capes.capes;
        if (c.level < CapeService.UNLOCK_PLAYER_LEVEL || capes == null || capes.Count == 0 || Random.value > 0.65f)
        {
            return;
        }

        // Summon vài chục lượt, giữ áo hiếm nhất (cùng bảng tỉ lệ CapeSummonConfig).
        CapeData best = null;
        int summons = Random.Range(3, 40) + c.level;
        for (int i = 0; i < summons; i++)
        {
            Rarity rarity = CapeSummonConfig.RollRarity(c.capeSummons++);
            List<CapeData> pool = capes.FindAll(x => x.rarity == rarity);
            if (pool.Count > 0 && (best == null || rarity > best.rarity))
            {
                best = pool[Random.Range(0, pool.Count)];
            }
        }
        if (best == null)
        {
            return;
        }
        c.equipment.Equip(GearSlotType.CAPE, best.capeId.ToString(), best.rarity, 1, GearStatCalculator.RollSubStats(statConfig, best.subStatCount));
        LevelUpCape(c, Random.Range(0, 6 + c.level / 5));
    }

    private static void LevelUpCape(Character c, int levels)
    {
        EquippedItemData rec = c.equipment.GetRecord(GearSlotType.CAPE);
        if (rec == null || levels <= 0)
        {
            return;
        }
        int maxLevel = GameData.staticData.capes.config != null ? GameData.staticData.capes.config.maxLevel : rec.level;
        c.equipment.Equip(GearSlotType.CAPE, rec.equipId, rec.rarity, Mathf.Min(rec.level + levels, Mathf.Max(rec.level, maxLevel)), rec.subStats);
    }

    // Relic: đa số slot trống, vài slot +1..+3 (người thật ở Iron: +1 đến +3 trên 2–4 món).
    // Người cày nặng thì relic lên cao (tới +11 = EnchantmentConfig.MAX_TIER).
    private static void SetupEnchantments(Character c, float effort)
    {
        if (c.level < ENCHANT_UNLOCK_LEVEL)
        {
            return;
        }
        int count = Random.Range(0, Mathf.Min(EnchantmentConfig.SLOT_ORDER.Length, 1 + c.level / 10) + 1);
        if (effort > 1.2f)
        {
            count = Mathf.RoundToInt(count * Random.Range(3f, 7f));
        }
        for (int i = 0; i < count; i++)
        {
            RaiseEnchantment(c);
        }
    }

    private static void RaiseEnchantment(Character c)
    {
        if (c.level < ENCHANT_UNLOCK_LEVEL)
        {
            return;
        }
        GearSlotType slot = EnchantmentConfig.SLOT_ORDER[Random.Range(0, EnchantmentConfig.SLOT_ORDER.Length)];
        if (c.equipment.GetRecord(slot) == null)
        {
            return;
        }
        c.enchantments.SetEquipped(slot, Mathf.Min(c.enchantments.GetEquipped(slot) + 1, EnchantmentConfig.MAX_TIER));
    }

    // Pet theo đúng cơ chế summon của game (CompanionService.Summon): rarity roll theo tổng số lượt đã summon, con
    // trùng thành thẻ, đủ thẻ thì lên cấp; mang ra trận 3 con tốt nhất (rarity rồi tới level, như QuickEquip).
    private static void SummonCompanions(Character c, int count)
    {
        UserCompanionData pets = c.companions;
        for (int i = 0; i < count; i++)
        {
            Rarity rarity = pets.totalSummons == CompanionSummonLevelConfig.GUARANTEED_UNCOMMON_AT
                ? Rarity.Uncommon
                : CompanionSummonLevelConfig.RollRarity(pets.totalSummons);
            CompanionData data = GameData.staticData.companions.GetRandom(rarity);
            pets.totalSummons++;
            if (data == null)
            {
                continue;
            }
            if (pets.IsOwned(data.assetName))
            {
                pets.AddCards(data.assetName, 1);
            }
            else
            {
                pets.Own(data.assetName).isNew = false;
            }
        }

        for (int i = 0; i < pets.owned.Count; i++)
        {
            while (pets.Upgrade(pets.owned[i].companionId))
            {
            }
        }

        List<CompanionModel> sorted = new List<CompanionModel>(pets.owned);
        sorted.Sort((a, b) =>
        {
            int ra = (int)GameData.staticData.companions.GetData(a.companionId).rarity;
            int rb = (int)GameData.staticData.companions.GetData(b.companionId).rarity;
            return ra != rb ? rb.CompareTo(ra) : b.level.CompareTo(a.level);
        });
        pets.equipped.Clear();
        for (int i = 0; i < sorted.Count && i < UserCompanionData.MAX_EQUIPPED; i++)
        {
            pets.equipped.Add(sorted[i].companionId);
        }
    }

    // ===== Data tĩnh =====

    private static bool LoadStatic()
    {
        if (GameData.staticData.gears == null || GameData.staticData.weapons == null || GameData.staticData.companions == null)
        {
            GameData.staticData.Load();
        }
        statConfig = EquipmentStatResolver.LoadConfig();
        gearPool = GameData.staticData.gears.all != null ? GameData.staticData.gears.all.FindAll(g => g.isMonsterGear == false) : new List<GearItemData>();
        weaponPool = GameData.staticData.weapons.weapons != null ? GameData.staticData.weapons.weapons.FindAll(w => w.isMonsterWeapon == false) : new List<WeaponData>();
        return statConfig != null && (gearPool.Count > 0 || weaponPool.Count > 0);
    }
}

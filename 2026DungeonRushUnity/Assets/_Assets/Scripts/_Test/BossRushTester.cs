using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

// Công cụ test (chỉ Editor): đánh thử TỪNG boss Boss Rush ngay trong Game view, KHÔNG cần server.
// Tự tạo khi Play (không phải đặt vào scene). Ở MainGame bấm nút "Boss" góc dưới-trái (cạnh nút "Đồ").
// Chọn boss 1..10, tier (máu/sát thương theo bảng gốc), số đồng đội (bot lấy từ server/functions/data/botBank.json),
// mốc bot (0 = yếu nhất .. 6 = mạnh nhất), bật/tắt giới hạn 30s → "Đánh". "Thoát" = nút Exit giữa trận.
// Trận dựng bằng BossRushFightSession với fightToken rỗng → hết giờ ReportDamage chỉ log lỗi, không lưu báo cáo giả.
public class BossRushTester : MonoBehaviour
{
    private const int BOSS_COUNT = 10;
    private const int MAX_ALLIES = 7;
    private const int UNLIMITED_TIME = 99999;

    public bool showPanel;
    public int bossNumber = 1;
    public int tier = 1;
    public int allyCount = MAX_ALLIES;
    public int botStep = 3;
    public bool isUnlimitedTime = true;

    private class BotStep
    {
        public double power;
        public List<BossRushItemModel> items;
        public List<BossRushCompanionModel> companions;
        public List<int> enchantmentTiers;
        public bool showCloak = true;
    }

    private class BotChain
    {
        public string id;
        public List<BotStep> steps;
    }

    private List<BotChain> botBank;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject("BossRushTester");
        go.AddComponent<BossRushTester>();
        DontDestroyOnLoad(go);
    }

    private void OnGUI()
    {
        // Chỉ ở MainGame: ở Login chạm GameController.Instance sẽ tự tạo Singleton giả.
        if (SceneManager.GetActiveScene().name != StaticValue.SCENE_MAINGAME)
            return;

        float scale = Screen.width / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float height = Screen.height / scale;

        if (GUI.Button(new Rect(58, height - 34, 50, 30), showPanel ? "X" : "Boss"))
            showPanel = !showPanel;

        BossRushMode mode = GameController.Instance.mode as BossRushMode;
        if (mode != null && isUnlimitedTime)
            mode.defaultBattleTime = UNLIMITED_TIME;

        if (showPanel == false)
            return;

        StaticBossRushData data = GameData.staticData.bossRush;
        GUILayout.BeginArea(new Rect(4, 60, 532, 250), GUI.skin.box);

        BossRushBossDefinition definition = data.GetBoss(bossNumber);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Boss", GUILayout.Width(80));
        if (GUILayout.Button("<", GUILayout.Width(30)))
            bossNumber = bossNumber <= 1 ? BOSS_COUNT : bossNumber - 1;
        if (GUILayout.Button(">", GUILayout.Width(30)))
            bossNumber = bossNumber >= BOSS_COUNT ? 1 : bossNumber + 1;
        GUILayout.Label($"{bossNumber}. {definition.bossName}  [{definition.skillType}]");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Tier", GUILayout.Width(80));
        if (GUILayout.Button("<", GUILayout.Width(30)))
            tier = Mathf.Max(1, tier - 1);
        if (GUILayout.Button(">", GUILayout.Width(30)))
            tier = Mathf.Min(StaticBossRushData.MAX_TIER, tier + 1);
        GUILayout.Label($"{tier} {data.GetTierName(tier)}  HP {data.GetBossHp(bossNumber, tier).ToLetter()}  Dmg {data.GetBossDamage(tier)}");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Đồng đội", GUILayout.Width(80));
        if (GUILayout.Button("<", GUILayout.Width(30)))
            allyCount = Mathf.Max(0, allyCount - 1);
        if (GUILayout.Button(">", GUILayout.Width(30)))
            allyCount = Mathf.Min(MAX_ALLIES, allyCount + 1);
        GUILayout.Label(allyCount.ToString(), GUILayout.Width(30));
        GUILayout.Label("Mốc bot", GUILayout.Width(60));
        if (GUILayout.Button("<", GUILayout.Width(30)))
            botStep = Mathf.Max(0, botStep - 1);
        if (GUILayout.Button(">", GUILayout.Width(30)))
            botStep = Mathf.Min(6, botStep + 1);
        GUILayout.Label(botStep.ToString());
        GUILayout.EndHorizontal();

        isUnlimitedTime = GUILayout.Toggle(isUnlimitedTime, " Không giới hạn giờ (tắt = 30s như thật, hết giờ hiện popup kết quả)");

        GUILayout.BeginHorizontal();
        GUI.enabled = mode == null && GameController.Instance.isChangingMode == false;
        if (GUILayout.Button("Đánh", GUILayout.Height(36)))
            StartFight();
        GUI.enabled = mode != null;
        if (GUILayout.Button("Thoát", GUILayout.Height(36)))
            mode.ExitMidFight();
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void StartFight()
    {
        StaticBossRushData data = GameData.staticData.bossRush;
        double hp = data.GetBossHp(bossNumber, tier);
        BossRushBossModel boss = new BossRushBossModel
        {
            BossNumber = bossNumber,
            CurrentHP = hp,
            MaxHP = hp,
            AttackPower = data.GetBossDamage(tier),
        };
        BossRushFightSession.Begin(BuildAllies(), boss, "test", null, tier);

        showPanel = false;
        GameController.Instance.uiLobby.CloseAllTabs();
        GameController.Instance.ChangeMode(ModeType.BossRush);
        DebugCustom.Log($"[BossRushTester] Boss {bossNumber} tier {tier}, {allyCount} đồng đội mốc {botStep}");
    }

    // Lấy ngẫu nhiên allyCount chuỗi bot khác nhau, mỗi người ở mốc botStep (kẹp theo số mốc của chuỗi).
    private List<BossRushPlayerModel> BuildAllies()
    {
        var allies = new List<BossRushPlayerModel>();
        List<BotChain> bank = LoadBotBank();
        if (bank.Count == 0)
            return allies;

        var used = new HashSet<int>();
        while (allies.Count < allyCount && used.Count < bank.Count)
        {
            int index = Random.Range(0, bank.Count);
            if (used.Add(index) == false)
                continue;

            BotChain chain = bank[index];
            BotStep step = chain.steps[Mathf.Min(botStep, chain.steps.Count - 1)];
            allies.Add(new BossRushPlayerModel
            {
                UserId = "test_" + chain.id,
                PlayerName = "Bot " + chain.id,
                Power = step.power,
                Items = step.items ?? new List<BossRushItemModel>(),
                Companions = step.companions ?? new List<BossRushCompanionModel>(),
                EnchantmentTiers = step.enchantmentTiers ?? new List<int>(),
                ShowCloak = step.showCloak,
                AvatarId = index % 22,
            });
        }
        return allies;
    }

    private List<BotChain> LoadBotBank()
    {
        if (botBank != null)
            return botBank;

        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../server/functions/data/botBank.json"));
        if (File.Exists(path) == false)
        {
            DebugCustom.LogWarning("[BossRushTester] Không thấy " + path + " → đánh không có đồng đội.");
            botBank = new List<BotChain>();
            return botBank;
        }

        botBank = JsonConvert.DeserializeObject<List<BotChain>>(File.ReadAllText(path)) ?? new List<BotChain>();
        botBank.RemoveAll(c => c == null || c.steps == null || c.steps.Count == 0);
        return botBank;
    }
#endif
}

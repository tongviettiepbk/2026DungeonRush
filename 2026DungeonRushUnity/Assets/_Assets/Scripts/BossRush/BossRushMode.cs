using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mode BOSS RUSH — vào từ UIBossRushPopup (Fight) qua BossRushController.StartFight → ChangeMode(ModeType.BossRush).
// Port GameController gốc phần Boss Rush (hie/hif/hih/hij + SpawnController.hvl/hvm):
//   • Đội nhà: hero + ≤7 GHOST (người chơi khác trong nhóm, mặc đồ snapshot). KHÔNG có pet — gốc
//     CompanionController.OnStateChanged return khi GameController.IsBossRush; SpawnController.hvl dựng ghost chỉ
//     gồm enchantment + đồ + cánh + áo choàng. Own Effect của pet sở hữu vẫn cộng vào chỉ số.
//   • Boss: máu CHUNG của nhóm (server), boss chết → "Boss Defeated", boss kế = BossHPTable[tier][boss+1] spawn ngay.
//   • Đồng hồ FightDuration = 30s; CHỈ hết giờ mới kết thúc (hero chết vẫn chờ) → báo server
//     damageDealt = damage của HERO CHÍNH (IsMainCharacter), totalDamageDealt = damage CẢ ĐỘI (IsHome).
//   • Popup "Fight Complete!" (Team Damage + Damage Dealt) → Continue về campaign + mở lại sảnh Boss Rush.
//   • Đấu trường (BossRushConfig gốc): lưới 12×12 không tường, không chướng ngại; hero ô giữa, ghost 8 ô quanh
//     (GridManager.ijq); boss ô ngẫu nhiên ngoài vùng 5×5 giữa (ijt); camera bám hero + CameraFollowOffset.
public class BossRushMode : BaseMode
{
    [Header("Boss Rush")]
    [Tooltip("Prefab boss theo BossDefinitions (index 0..9: Dark Lich, Black Dragon, ...). Trống → enemyPrefab.")]
    public GameObject[] bossPrefabs;
    public float delayShowResult = 1.5f;     // gốc hik(1.5f)
    [Tooltip("BossRushConfig.CameraFollowOffset gốc: camera = vị trí người được bám + offset (giữ z).")]
    public Vector3 cameraFollowOffset = new Vector3(0f, -5f, 0f);
    private const float CAMERA_LERP_SPEED = 4f;  // LateUpdate gốc: t = min(dt × 4, 1) khi đổi người bám

    private List<Vector2Int> teamCells = new List<Vector2Int>();
    private Transform cameraTransform;
    private Vector3 cameraHomePosition;
    private BaseUnit cameraTarget;

    private BossRushBossUnit boss;
    private BossRushBossModel bossModel;
    private int tier;
    private long ownDamage;      // wui gốc
    private long teamDamage;     // wuh gốc
    private Transform enemiesGroup;
    private Transform ghostsGroup;
    private UIBossRushHud hud;

    public BossRushBossUnit Boss => boss;
    public long OwnDamage => ownDamage;
    public long TeamDamage => teamDamage;

    public override void Init(GameController controller, ModeType typeModeInput = ModeType.DefaultLevel)
    {
        base.Init(controller, typeModeInput);
        Reset();

        if (!BossRushFightSession.IsActive || BossRushFightSession.Boss == null)
        {
            DebugCustom.LogError("[BossRushMode] Không có BossRushFightSession — về campaign.");
            StartCoroutine(RoutineBackToCampaign());
            return;
        }

        ownDamage = 0;
        teamDamage = 0;
        tier = BossRushFightSession.Tier;
        bossModel = BossRushFightSession.Boss;
        defaultBattleTime = Mathf.RoundToInt(StaticBossRushData.FIGHT_DURATION);

        hud = GameController.uiLobby != null && GameController.uiLobby.objBossRushUI != null
            ? GameController.uiLobby.objBossRushUI.GetComponent<UIBossRushHud>()
            : null;
        SetActiveHud(true);

        Initialize();
    }

    private IEnumerator RoutineBackToCampaign()
    {
        yield return null;
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
    }

    // ===== Dựng màn =====

    protected override bool AllowPets => false;

    // SpawnController.hvj gốc: ô đội = ijq(1 + số ghost) — hero ô đầu (giữa đấu trường), ghost các ô sau.
    protected override Vector2Int GetHeroSpawnCell()
    {
        MapController map = MapController.Instance;
        return teamCells.Count > 0 ? teamCells[0] : new Vector2Int(map.Rows / 2, map.Cols / 2);
    }

    protected override void CreateTeamA()
    {
        List<BossRushPlayerModel> allies = BossRushFightSession.Allies;
        teamCells = MapSpawnCells.GetArenaTeamCells(MapController.Instance.Grid, 1 + (allies != null ? allies.Count : 0));
        SpawnHeroAndPets();
        SpawnGhosts(allies);
        StartCameraFollow();
    }

    private void SpawnGhosts(List<BossRushPlayerModel> allies)
    {
        if (heroPrefab == null || allies == null || allies.Count == 0)
        {
            return;
        }

        MapController map = MapController.Instance;
        ghostsGroup = NewGroup("Ghosts");

        for (int i = 0; i < allies.Count && i + 1 < teamCells.Count; i++)
        {
            BossRushPlayerModel ally = allies[i];
            Vector3 pos = map.CellToWorld(teamCells[i + 1]);
            HeroUnit ghost = SpawnUnit<HeroUnit>(heroPrefab, pos, ghostsGroup);
            ghost.gameObject.name = "Ghost_" + ally.PlayerName;

            // Pet sở hữu chỉ để cộng Own Effect vào chỉ số ghost (không spawn).
            List<CompanionModel> owned = new List<CompanionModel>();
            if (ally.Companions != null)
            {
                for (int c = 0; c < ally.Companions.Count; c++)
                {
                    BossRushCompanionModel m = ally.Companions[c];
                    owned.Add(new CompanionModel(m.CompanionId) { level = m.CompanionLevel, isNew = false });
                }
            }

            ghost.SetupAsGhost(ally.PlayerName, BossRushController.ToEquipmentData(ally.Items), owned,
                               BossRushController.ToEnchantmentData(ally.EnchantmentTiers), ally.ShowCloak);
            ghost.SpawnInBattle(BuildHeroStats(), StaticValue.TAG_TEAM_A, pos);
        }
    }

    protected override void CreateTeamB()
    {
        enemiesGroup = NewGroup("Enemies");
        SpawnBoss();
    }

    // SpawnController.hvm gốc: boss theo mẫu (N-1)%10, sát thương theo tier, HP = máu chung hiện tại.
    private void SpawnBoss()
    {
        StaticBossRushData data = GameData.staticData.bossRush;
        BossRushBossDefinition definition = data.GetBoss(bossModel.BossNumber);
        int index = data.bosses.IndexOf(definition);
        GameObject prefab = bossPrefabs != null && index >= 0 && index < bossPrefabs.Length && bossPrefabs[index] != null
            ? bossPrefabs[index]
            : enemyPrefab;
        if (prefab == null)
        {
            DebugCustom.LogError("[BossRushMode] Chưa gán prefab boss / enemyPrefab.");
            return;
        }

        // GridManager.ijt: ô trống ngẫu nhiên ngoài vùng 5×5 giữa đấu trường (không đè lên quân đang đứng).
        MapController map = MapController.Instance;
        var occupied = new HashSet<Vector2Int>();
        for (int i = 0; i < teamA.Count; i++)
        {
            if (teamA[i] != null && teamA[i].isTargetable) occupied.Add(map.WorldToCell(teamA[i].Transform.position));
        }
        Vector2Int cell = MapSpawnCells.GetArenaBossCell(map.Grid, occupied);
        Vector3 pos = map.CellToWorld(cell);

        boss = SpawnUnit<BossRushBossUnit>(prefab, pos, enemiesGroup);
        boss.gameObject.name = "Boss_" + definition.bossName;
        boss.SpawnBoss(this, bossModel, definition, pos);
        RefreshHud();
    }

    // ===== Damage (GameController.hif gốc) =====

    public void OnBossDamaged(BaseUnit attacker, double damage)
    {
        if (isEndMode || attacker == null || !attacker.isTeamA)
        {
            return;
        }

        long amount = damage >= long.MaxValue ? long.MaxValue : (long)damage;
        teamDamage += amount;
        if (attacker == hero)
        {
            ownDamage += amount;
        }
        RefreshHud();
    }

    // ===== Boss chết → boss kế (GameController.hih gốc) =====

    protected override void CheckRemainingEnemies()
    {
        if (isEndMode || boss == null || boss.isTargetable)
        {
            return;
        }

        UIManager.Instance.ShowToastMessage("Boss Defeated!", isLocalize: false);

        BossRushBossUnit dead = boss;
        boss = null;
        StartCoroutine(RoutineRemoveBoss(dead));

        bossModel.BossNumber++;
        bossModel.MaxHP = GameData.staticData.bossRush.GetBossHp(bossModel.BossNumber, tier);
        bossModel.CurrentHP = bossModel.MaxHP;
        bossModel.AttackPower = GameData.staticData.bossRush.GetBossDamage(tier);
        SpawnBoss();
    }

    private IEnumerator RoutineRemoveBoss(BaseUnit dead)
    {
        yield return new WaitForSeconds(1f);
        if (dead != null)
        {
            dead.Deactive();
            Destroy(dead.gameObject);
        }
    }

    // Hero chính chết KHÔNG kết thúc trận: gốc chỉ gọi GameController.hij khi hết FightDuration (GameController.Update
    // là nơi gọi duy nhất) → ghost đánh tiếp tới hết giờ, damage của họ vẫn cộng vào teamDamage.
    protected override void OnAllyDie(int battleId)
    {
    }

    // ===== Đồng hồ / HUD =====

    protected override void UpdateBattleTime(float timer, int totalTime)
    {
        if (hud != null)
        {
            hud.SetRemainingTime(Mathf.Max(0f, totalTime - timer));
        }
    }

    private void RefreshHud()
    {
        if (hud == null || bossModel == null)
        {
            return;
        }
        string bossName = boss != null ? boss.DisplayName : GameData.staticData.bossRush.GetBoss(bossModel.BossNumber).bossName;
        hud.SetBoss(bossModel.BossNumber, bossName, bossModel.CurrentHP, bossModel.MaxHP);
    }

    // HUD boss + tấm phủ EventBlocker bật/tắt cùng nhau (GameplayUI.OnStateChanged gốc: epn + EventBlockerUI.Show).
    private void SetActiveHud(bool isOn)
    {
        if (GameController.uiLobby == null)
        {
            return;
        }
        if (GameController.uiLobby.objBossRushUI != null)
        {
            GameController.uiLobby.objBossRushUI.SetActive(isOn);
        }
        GameController.uiLobby.SetActiveEventBlocker(isOn);
    }

    // ===== Kết thúc (GameController.hij gốc) =====

    protected override void CalculateResult(bool isWin)
    {
        StartCoroutine(RoutineReport());
    }

    private IEnumerator RoutineReport()
    {
        bool done = false;
        BossRushFightResultResponseDTO result = null;
        bool success = false;
        BossRushController.Instance.ReportDamage(ownDamage, teamDamage, (ok, res) =>
        {
            success = ok;
            result = res;
            done = true;
        });

        yield return new WaitForSeconds(delayShowResult);
        while (!done)
        {
            yield return null;
        }

        SetActiveHud(false);
        UIBossRushEndPopup popup = UIManager.Instance.LoadUI(UIKey.BossRushEndPopup, isBackable: false) as UIBossRushEndPopup;
        if (popup != null)
        {
            popup.Show(teamDamage, ownDamage, success && result != null && !result.autoLoss, Exit);
        }
        else
        {
            Exit();
        }
    }

    // Về campaign rồi mở lại sảnh Boss Rush (nút Continue của popup kết quả).
    public void Exit()
    {
        Leave(true);
    }

    // Nút Exit trên EventBlocker (GameController.hii gốc): bỏ trận giữa chừng — KHÔNG báo damage (vé đã tiêu
    // lúc startbossrushfight, fight token tự hết hạn), về campaign, không mở lại sảnh.
    // Hết giờ rồi (đang báo kết quả) thì bỏ qua. Trả về true nếu đã thoát.
    public bool ExitMidFight()
    {
        if (isEndMode)
        {
            return false;
        }
        return Leave(false);
    }

    private bool Leave(bool reopenLobby)
    {
        if (GameController.Instance.isChangingMode)
        {
            return false;
        }

        isEndMode = true;
        isPause = true;
        BossRushFightSession.Clear();
        SetActiveHud(false);
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
        if (reopenLobby)
        {
            UIBossRushPopup.OpenAfterModeChange();
        }
        return true;
    }

    // ===== Camera bám người chơi (CameraController.gxt / LateUpdate gốc) =====

    private void StartCameraFollow()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            cameraHomePosition = cameraTransform.position;
        }
        cameraTarget = hero;
    }

    private void StopCameraFollow()
    {
        if (cameraTransform != null)
        {
            cameraTransform.position = cameraHomePosition;
            cameraTransform = null;
        }
        cameraTarget = null;
    }

    // Bám hero; hero gục thì chuyển sang đồng minh còn sống (lerp ×4/s), bám lại người cũ thì đặt thẳng.
    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        BaseUnit target = hero != null && hero.isTargetable ? hero : FindAliveAlly();
        if (target == null)
        {
            return;
        }

        Vector3 goal = target.Transform.position + cameraFollowOffset;
        goal.z = cameraTransform.position.z;
        if (target == cameraTarget)
        {
            cameraTransform.position = goal;
        }
        else
        {
            float t = Mathf.Min(Time.deltaTime * CAMERA_LERP_SPEED, 1f);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, goal, t);
        }
    }

    private BaseUnit FindAliveAlly()
    {
        for (int i = 0; i < teamA.Count; i++)
        {
            if (teamA[i] != null && teamA[i].isTargetable) return teamA[i];
        }
        return null;
    }

    public override void Reset()
    {
        StopCameraFollow();
        base.Reset();
        teamCells.Clear();
        boss = null;
        ghostsGroup = null;
        enemiesGroup = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopCameraFollow();
        SetActiveHud(false);
    }
}

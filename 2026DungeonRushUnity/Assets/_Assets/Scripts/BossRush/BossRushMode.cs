using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mode BOSS RUSH — vào từ UIBossRushPopup (Fight) qua BossRushController.StartFight → ChangeMode(ModeType.BossRush).
// Port GameController gốc phần Boss Rush (hie/hif/hih/hij + SpawnController.hvl/hvm):
//   • Đội nhà: hero + ≤7 GHOST (người chơi khác trong nhóm, mặc đồ snapshot). KHÔNG có pet — gốc
//     CompanionController.OnStateChanged return khi GameController.IsBossRush; SpawnController.hvl dựng ghost chỉ
//     gồm enchantment + đồ + cánh + áo choàng. Own Effect của pet sở hữu vẫn cộng vào chỉ số.
//   • Boss: máu CHUNG của nhóm (server), boss chết → "Boss Defeated", boss kế = BossHPTable[tier][boss+1] spawn ngay.
//   • Đồng hồ FightDuration = 30s; hết giờ (hoặc hero chết) → báo server
//     damageDealt = damage của HERO CHÍNH (IsMainCharacter), totalDamageDealt = damage CẢ ĐỘI (IsHome).
//   • Popup "Fight Complete!" (Team Damage + Damage Dealt) → Continue về campaign + mở lại sảnh Boss Rush.
public class BossRushMode : BaseMode
{
    [Header("Boss Rush")]
    [Tooltip("Prefab boss theo BossDefinitions (index 0..9: Dark Lich, Black Dragon, ...). Trống → enemyPrefab.")]
    public GameObject[] bossPrefabs;
    [Tooltip("Hàng spawn boss (0 = dưới cùng).")]
    public int bossRow = 9;
    public float delayShowResult = 1.5f;     // gốc hik(1.5f)

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

    protected override void CreateTeamA()
    {
        SpawnHeroAndPets();
        SpawnGhosts(BossRushFightSession.Allies);
    }

    private void SpawnGhosts(List<BossRushPlayerModel> allies)
    {
        if (heroPrefab == null || allies == null || allies.Count == 0)
        {
            return;
        }

        MapController map = MapController.Instance;
        ghostsGroup = NewGroup("Ghosts");
        List<Vector2Int> cells = PickGhostCells(allies.Count);

        for (int i = 0; i < allies.Count && i < cells.Count; i++)
        {
            BossRushPlayerModel ally = allies[i];
            Vector3 pos = map.CellToWorld(cells[i]);
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

            ghost.SetupAsGhost(ally.PlayerName, BossRushController.ToEquipmentData(ally.Items), owned);
            ghost.SpawnInBattle(BuildHeroStats(), StaticValue.TAG_TEAM_A, pos);
        }
    }

    // Ô trống ở 2 hàng dưới, trừ ô của hero, ưu tiên gần giữa.
    private List<Vector2Int> PickGhostCells(int count)
    {
        MapController map = MapController.Instance;
        Vector2Int heroCell = new Vector2Int(0, map.Cols / 2);
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int row = 0; row < StaticMapData.PLAYER_SPAWN_ROWS + 1 && row < map.Rows; row++)
        {
            for (int col = 0; col < map.Cols; col++)
            {
                Vector2Int cell = new Vector2Int(row, col);
                if (cell != heroCell && map.IsWalkable(cell))
                {
                    cells.Add(cell);
                }
            }
        }
        cells.Sort((a, b) => (a.x * 100 + Mathf.Abs(a.y - heroCell.y)).CompareTo(b.x * 100 + Mathf.Abs(b.y - heroCell.y)));
        if (cells.Count > count)
        {
            cells.RemoveRange(count, cells.Count - count);
        }
        return cells;
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

        MapController map = MapController.Instance;
        Vector2Int cell = new Vector2Int(Mathf.Clamp(bossRow, 0, map.Rows - 1), map.Cols / 2);
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

    // Thua khi hero chính chết → kết thúc trận sớm (vẫn báo damage).
    protected override void OnAllyDie(int battleId)
    {
        if (hero == null || !hero.isTargetable)
        {
            EndGame(false);
        }
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

    private void SetActiveHud(bool isOn)
    {
        if (GameController.uiLobby != null && GameController.uiLobby.objBossRushUI != null)
        {
            GameController.uiLobby.objBossRushUI.SetActive(isOn);
        }
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

    // Về campaign rồi mở lại sảnh Boss Rush.
    public void Exit()
    {
        if (GameController.Instance.isChangingMode)
        {
            return;
        }

        isEndMode = true;
        isPause = true;
        BossRushFightSession.Clear();
        SetActiveHud(false);
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
        UIBossRushPopup.OpenAfterModeChange();
    }

    public override void Reset()
    {
        base.Reset();
        boss = null;
        ghostsGroup = null;
        enemiesGroup = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SetActiveHud(false);
    }
}

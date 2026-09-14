using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mode CAMPAIGN (MainMap — màn thường). BaseMode đã lo dựng map/lưới/wall + vòng đời
// (RoutineTimer, EndGame, Pause/Resume, OnUnitDie→thắng/thua, cheat Editor W/L); ở đây chỉ
// còn phần riêng của campaign: spawn quân THẬT (Hero+Pet team A, Enemy team B) từ prefab có rig.
//
// YÊU CẦU PREFAB: heroPrefab/petPrefab/enemyPrefab (khai báo ở BaseMode) phải có rig BaseUnit
// (con "Body" Collider2D, "FlipPoints"/"CenterBody"/"FirePoint"/"health-bar", Rigidbody2D+
// AudioSource+AnimationController ở root) — PreviewCharacter là mẫu. YÊU CẦU TAG: "TeamA"/"TeamB".
public class CampaignMode : BaseMode
{


    private HeroUnit hero;
    public HeroUnit Hero => hero;

    public override void Init(GameController controller, ModeType typeModeInput = ModeType.DefaultLevel)
    {
        base.Init(controller, typeModeInput);
        Build();
    }

    // Dựng lại màn từ đầu (Reset dọn màn cũ → Initialize chạy vòng đời BaseMode).
    [ContextMenu("Rebuild")]
    public void Build()
    {
        Reset();
        Initialize();
    }

    // ===== VÒNG ĐỜI (override hook BaseMode) =====

    protected override void CreateTeamA()
    {
        SpawnHeroAndPets();
    }

    protected override void CreateTeamB()
    {
        SpawnEnemies(currentLevel.enemies);
    }

    // Dọn màn cũ trước khi dựng lại (BaseMode.Reset lo container; đây thêm phần riêng campaign).
    [ContextMenu("Clear")]
    public override void Reset()
    {
        base.Reset();

        GameController.Instance.ResetBattle();
        hero = null;
        isEndMode = false;
    }

    // ===== KẾT QUẢ TRẬN (Phase 1: chỉ vòng lặp win/lose + reload) =====
    // BaseMode.EndGame gọi CalculateResult sau khi phát EventID.EndGame.
    //   Win  → đánh dấu qua màn (PassStage: passedStageId + curStageId sang màn kế) rồi lưu.
    //   Lose → giữ nguyên curStageId, đánh lại chính màn đó.
    // Dù thắng hay thua đều dựng lại màn sau delayEndGame giây. Reload trễ (coroutine) để KHÔNG
    // hủy unit ngay giữa lúc BaseMode đang dispatch sự kiện UnitDie/EndGame.
    protected override void CalculateResult(bool isWin)
    {
        if (isWin)
        {
            int stageId = overrideStageId > 0 ? overrideStageId : GameData.userData.campaign.curStageId;

            // Exp thắng màn = round(49 + level) (clear sạch quái, xem DecodedData/EXP_MODEL.md).
            // Cộng vào playerExperience → có thể lên playerLevel (rarity table tự tốt lên).
            int level = GameData.staticData.campaign.GetLevel(stageId);
            int expReward = GameData.staticData.experience.GetStageExp(level);
            int oldPlayerLevel = GameData.userData.player.playerLevel;
            int levelsGained = GameData.userData.player.AddExperience(expReward);

            // Mỗi level vừa lên → thưởng gem theo bảng LevelPopup.ywc (level 1-16 = 5, sau tăng dần).
            if (levelsGained > 0)
            {
                StaticExperienceData exp = GameData.staticData.experience;
                int totalGem = 0;
                for (int lv = oldPlayerLevel + 1; lv <= GameData.userData.player.playerLevel; lv++)
                {
                    totalGem += exp.GetLevelUpGemReward(lv);
                }
                GameData.userData.items.Receive(ItemType.GEM, totalGem);
                DebugCustom.Log($"[Campaign] Lên {levelsGained} level -> playerLevel = {GameData.userData.player.playerLevel}, +{totalGem} gem");
                // TODO(Phase 3): bật UILevelPopup (bảng rarity + số gem) trước khi reload.
            }

            GameData.userData.campaign.PassStage(stageId);
            GameData.Save(true);
        }

        StartCoroutine(RoutineReloadAfterResult());
    }

    private IEnumerator RoutineReloadAfterResult()
    {
        yield return new WaitForSeconds(delayEndGame);
        Build();
    }

    // ===== SPAWN QUÂN THẬT =====

    private void SpawnHeroAndPets()
    {
        if (heroPrefab == null)
        {
            DebugCustom.LogWarning("[CampaignMode] Chưa gán heroPrefab — bỏ qua spawn Hero/Pet.");
            return;
        }

        // Hero đứng giữa hàng spawn DƯỚI cùng (row = 0).
        Vector2Int heroCell = new Vector2Int(0, MapController.Instance.Cols / 2);
        Vector3 heroPos = MapController.Instance.CellToWorld(heroCell);

        Transform parent = NewGroup("Allies");
        hero = SpawnUnit<HeroUnit>(heroPrefab, heroPos, parent);
        hero.SpawnInBattle(BuildHeroStats(), StaticValue.TAG_TEAM_A, heroPos);

        if (petPrefab == null || petCount <= 0)
        {
            return;
        }

        //BaseStats petStats = BuildPetStats();
        //for (int i = 0; i < petCount; i++)
        //{
        //    // Rải pet quanh hero.
        //    float angle = (360f / petCount) * i * Mathf.Deg2Rad;
        //    Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.8f;
        //    Vector3 petPos = MapController.Instance.ClampPointInMap(heroPos + offset);

        //    PetUnit pet = SpawnUnit<PetUnit>(petPrefab, petPos, parent);
        //    pet.owner = hero;
        //    pet.SpawnInBattle(petStats, StaticValue.TAG_TEAM_A, petPos);
        //}
    }

    private void SpawnEnemies(List<EnemySpawnGenerator.EnemySpawnInfo> enemies)
    {
        if (enemyPrefab == null)
        {
            DebugCustom.LogWarning("[CampaignMode] Chưa gán enemyPrefab — bỏ qua spawn Enemy.");
            return;
        }

        Transform parent = NewGroup("Enemies");
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemySpawnGenerator.EnemySpawnInfo info = enemies[i];
            Vector3 pos = MapController.Instance.CellToWorld(info.cell);

            EnemyUnit enemy = SpawnUnit<EnemyUnit>(enemyPrefab, pos, parent);
            if (info.isBoss)
            {
                enemy.transform.localScale *= 1.6f;
            }
            enemy.SpawnEnemy(info, pos);
        }
    }

    // ===== helpers =====

    private T SpawnUnit<T>(GameObject prefab, Vector3 pos, Transform parent) where T : BaseUnit
    {
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, parent);
        T unit = go.GetComponent<T>();
        if (unit == null)
        {
            unit = go.AddComponent<T>();
        }
        return unit;
    }

    // Chỉ số hero = tầng NỀN gốc (PlayerBase*) từ GearStatConfig. Hero trần (chưa đồ) = Damage 10/HP 50.
    // (Cộng dồn main stat đồ đang mặc để ở bước hệ trang bị đầy đủ.)
    private static GearStatConfigData gearStatConfig;

    private BaseStats BuildHeroStats()
    {
        if (gearStatConfig == null)
        {
            gearStatConfig = Resources.Load<GearStatConfigData>("Scriptable Objects/Gears/GearStatConfig");
        }

        if (gearStatConfig == null)
        {
            DebugCustom.LogError("[CampaignMode] Thiếu GearStatConfig — dùng BaseStats rỗng cho Hero.");
            return new BaseStats();
        }

        return gearStatConfig.GetPlayerBaseStats();
    }

    private BaseStats BuildPetStats()
    {
        return new BaseStats
        {
            attack = petAttack,
            attackPerSecond = petAttackSpeed,
            attackRange = petAttackRange,
            maxHp = petMaxHp,
            moveSpeed = petMoveSpeed,
        };
    }
}

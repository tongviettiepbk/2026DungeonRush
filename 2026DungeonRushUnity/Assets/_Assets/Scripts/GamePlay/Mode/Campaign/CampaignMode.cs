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

    // ===== SPAWN QUÂN THẬT =====

    private void SpawnHeroAndPets()
    {
        if (heroPrefab == null)
        {
            DebugCustom.LogWarning("[CampaignMode] Chưa gán heroPrefab — bỏ qua spawn Hero/Pet.");
            return;
        }

        // Hero đứng giữa hàng spawn dưới cùng.
        Vector2Int heroCell = new Vector2Int(MapController.Instance.Cols / 2, 0);
        Vector3 heroPos = MapController.Instance.CellToWorld(heroCell);

        Transform parent = NewGroup("Allies");
        hero = SpawnUnit<HeroUnit>(heroPrefab, heroPos, parent);
        hero.SpawnInBattle(BuildHeroStats(), StaticValue.TAG_TEAM_A, heroPos);

        if (petPrefab == null || petCount <= 0)
        {
            return;
        }

        BaseStats petStats = BuildPetStats();
        for (int i = 0; i < petCount; i++)
        {
            // Rải pet quanh hero.
            float angle = (360f / petCount) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.8f;
            Vector3 petPos = MapController.Instance.ClampPointInMap(heroPos + offset);

            PetUnit pet = SpawnUnit<PetUnit>(petPrefab, petPos, parent);
            pet.owner = hero;
            pet.SpawnInBattle(petStats, StaticValue.TAG_TEAM_A, petPos);
        }
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

    private BaseStats BuildHeroStats()
    {
        return new BaseStats
        {
            attack = heroAttack,
            attackPerSecond = heroAttackSpeed,
            attackRange = heroAttackRange,
            maxHp = heroMaxHp,
            moveSpeed = heroMoveSpeed,
        };
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

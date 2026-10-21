using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Mode CAMPAIGN (MainMap — màn thường). BaseMode đã lo dựng map/lưới/wall + vòng đời
// (RoutineTimer, EndGame, Pause/Resume, OnUnitDie→thắng/thua, cheat Editor W/L); ở đây chỉ
// còn phần riêng của campaign: spawn quân THẬT (Hero+Pet team A, Enemy team B) từ prefab có rig.
//
// YÊU CẦU PREFAB: heroPrefab/enemyPrefab (khai báo ở BaseMode) + prefab companion (load theo
// assetName từ Resources/Prefabs/Units/Companions) phải có rig BaseUnit
// (con "Body" Collider2D, "FlipPoints"/"CenterBody"/"FirePoint"/"health-bar", Rigidbody2D+
// AudioSource+AnimationController ở root) — PreviewCharacter là mẫu. YÊU CẦU TAG: "TeamA"/"TeamB".
public class CampaignMode : BaseMode
{
    [Header("Mảnh exp rơi khi quái chết / bay về hero khi thắng (FX)")]
    public LootDropController lootDrop;

    [Header("Chuyển màn qua cửa (LevelTransitionController gốc, giá trị GameplayScene)")]
    public float doorOpenDuration = 0.8f;
    public float fadeDuration = 0.5f;
    public AudioClip doorOpenSound;      // AudioConfig.DoorOpenSound gốc
    public float doorOpenVolume = 0.151f;

    private bool isOfferJustUnlocked;   // yda gốc: vừa bật Weapon Offer lần đầu → chào khi màn kế bắt đầu (jix)
    private bool isTransitionHoldingFade;   // màn hình đang giữ tối giữa chuyển màn

    private const float COMPLETE_DELAY = 0.5f;          // SpawnController.huj: quái cuối chết 0.5s mới Completed
    private const float DOOR_SLIDE = 0.8f;               // mỗi cánh cửa trượt ra ±0.8 (DOLocalMoveX)
    private const float MIN_TRANSITION_SPEED = 0.5f;     // max(MoveSpeed, 0.5)
    // Điểm hero đi tới sau cửa: LevelLoader cổng trên (0, H/2 + 0.5) + (0, 1.5) = ô (hàng trên, cột giữa) + (−0.5, 2.5).
    private static readonly Vector3 DOOR_EXIT_OFFSET = new Vector3(-0.5f, 2.5f, 0f);

    // Chuyển màn chỉ tối vùng map (tấm tối của lobby), hàng pet trở xuống vẫn sáng; thiếu lobby thì tối toàn màn.
    private static Image MapFadeImage
    {
        get
        {
            UIMainLobby lobby = GameController.Instance != null ? GameController.Instance.uiLobby : null;
            return lobby != null ? lobby.imgLevelProgressDark : null;
        }
    }

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

    // SpawnController.huu gốc: ô quái = GridManager.ijx (ô trống hàng ≥5 đi tới được, bốc ngẫu nhiên).
    protected override void CreateTeamB()
    {
        int stageId = currentLevel.stageId;
        List<Vector2Int> cells = MapSpawnCells.FindEnemySpawnCells(MapController.Instance.Grid, EnemySpawnGenerator.CampaignCount(stageId));
        SpawnEnemies(EnemySpawnGenerator.Generate(stageId, cells));
    }

    // Dọn màn cũ trước khi dựng lại (BaseMode.Reset lo container + hero/pet + ResetBattle).
    [ContextMenu("Clear")]
    public override void Reset()
    {
        base.Reset();
        if (isTransitionHoldingFade)
        {
            // Bị dọn giữa lúc đang tối (VD đổi mode) → trả màn hình sáng lại.
            isTransitionHoldingFade = false;
            UIManager.Instance.FadeIn(0f);
        }
        if (lootDrop != null)
        {
            lootDrop.Clear();
        }
    }

    // Quái chết → rơi mảnh exp. Rơi TRƯỚC base (base xét thắng) để mảnh của con cuối cũng kịp được thu.
    protected override void OnEnemyDie(int battleId)
    {
        BaseUnit unit = GameController.Instance.GetUnitByBattleId(battleId);
        if (lootDrop != null && unit != null && teamB.Contains(unit))
        {
            lootDrop.DropExp(unit.transform.position);
        }

        base.OnEnemyDie(battleId);
    }

    // ===== KẾT QUẢ TRẬN =====
    // BaseMode.EndGame gọi CalculateResult sau khi phát EventID.EndGame.
    //   Win  → đánh dấu qua màn (PassStage: cập nhật passedStageId + stageIdCurrent sang màn kế) rồi lưu,
    //          rồi hero đi ra cửa trên sang màn kế (RoutineLevelTransition, theo LevelTransitionController gốc).
    //   Lose → lùi stageIdCurrent 1 màn (StepBackStage), passedStageId giữ nguyên, rồi đánh lại màn dễ hơn
    //          sau delayEndGame giây.
    // Dựng lại màn trễ (coroutine) để KHÔNG hủy unit ngay giữa lúc BaseMode đang dispatch sự kiện UnitDie/EndGame.
    protected override void CalculateResult(bool isWin)
    {
        DebugCustom.ShowLog("Show EndGame");
        if (isWin)
        {
            int stageId = overrideStageId > 0 ? overrideStageId : GameData.userData.campaign.stageIdCurrent;

            // Exp thắng màn = round(49 + level) (clear sạch quái, xem DecodedData/EXP_MODEL.md).
            // Như mảnh exp gốc (LootDropController.ExperienceShardCollected → hdp): có boost exp, cộng dồn "(+N)",
            // đầy thanh thì phần dư bị bỏ và LevelPopup tự bật (bấm Complete mới lên level).
            int level = GameData.staticData.campaign.GetLevel(stageId);
            PlayerExpService.Grant(GameData.staticData.experience.GetStageExp(level), accumulateDisplay: true);

            GameData.userData.campaign.PassStage(stageId);
            // OfferController.jiw gốc ("main_combat" thắng): đủ level 3 lần đầu → bật Weapon Offer, chào ở màn kế.
            isOfferJustUnlocked = WeaponOfferService.OnCampaignWon();
            GameData.Save(true);

            StartCoroutine(RoutineLevelTransition());
            return;
        }
        else if (overrideStageId <= 0)
        {
            // Thua → tự động lùi campaign 1 màn rồi đánh lại màn dễ hơn.
            // (Chỉ áp dụng cho tiến trình thật; đang ép stage trong Editor thì giữ nguyên.)
            GameData.userData.campaign.StepBackStage();
            GameData.Save(true);
        }

        StartCoroutine(RoutineReloadAfterResult());
    }

    private IEnumerator RoutineReloadAfterResult()
    {
        yield return new WaitForSeconds(delayEndGame);

        // Chờ mảnh exp bay hết về hero rồi mới dựng màn mới.
        while (lootDrop != null && lootDrop.IsCollecting)
        {
            yield return null;
        }

        Build();
        OnLevelReloaded();
    }

    // Sau khi dựng lại màn (thắng qua cửa / thua đánh lại).
    private void OnLevelReloaded()
    {
        // Cập nhật lại lobby (level, thanh exp, gem, tên màn) sau khi data đổi vì thắng/thua.
        if (GameController.Instance.uiLobby != null)
        {
            GameController.Instance.uiLobby.Refresh();
        }

        if (isOfferJustUnlocked)
        {
            isOfferJustUnlocked = false;
            UIOfferPopup.Show();
        }
    }

    // ===== CHUYỂN MÀN QUA CỬA (thắng) =====
    // LevelTransitionController.hrq gốc (chỉ campaign, khi GameState → Completed):
    //   1. thu mảnh exp về hero (LootDropController.hsv) + mở 2 cánh cửa trên (hrz);
    //   2. hero quay về phía cửa (hru) rồi đi A* tới ô hàng trên cùng, cột giữa/trái giữa gần hơn (hrs);
    //   3. tối dần fadeDuration trong lúc hero đi tiếp ra sau cửa (hrt);
    //   4. chờ thu xong mảnh exp → dựng màn kế (hero đứng lại ô (hàng 2, cột giữa) — NextSpawnAtBottomGate
    //      gốc luôn TẮT, không vào từ cổng dưới) → sáng dần fadeDuration.
    private IEnumerator RoutineLevelTransition()
    {
        yield return new WaitForSeconds(COMPLETE_DELAY);

        if (lootDrop != null && hero != null)
        {
            lootDrop.CollectAll(hero.transform);
        }

        yield return RoutineOpenDoors();

        bool isFadeDone = false;
        if (hero != null)
        {
            yield return RoutineHeroWalkOut(() => isFadeDone = true);
        }
        else
        {
            UIManager.Instance.FadeOut(FadeColor.Black, fadeDuration, target: MapFadeImage, doneCallback: () => isFadeDone = true);
        }

        while (!isFadeDone)
        {
            yield return null;
        }
        isTransitionHoldingFade = true;

        while (lootDrop != null && lootDrop.IsCollecting)
        {
            yield return null;
        }

        // Build gọi Reset → StopAllCoroutines dừng chính coroutine này ở lần yield kế ⇒ phần sau Build
        // không yield nữa, fade-in chạy trên UIManager.
        isTransitionHoldingFade = false;
        Build();
        UIManager.Instance.FadeIn(fadeDuration);
        OnLevelReloaded();
    }

    // LevelTransitionController.hrz: 2 cánh cửa trượt sang 2 bên (Ease InSine) + tiếng mở cửa.
    private IEnumerator RoutineOpenDoors()
    {
        MapController map = MapController.Instance;
        StaticMapData.MapEnvironmentConfig envCfg = GameData.staticData.map.GetConfig(type);
        if (map == null || map.topDoor1 == null || map.topDoor2 == null || (envCfg != null && !envCfg.hasDoor))
        {
            yield break;
        }

        AudioManager.Instance.PlaySfx(doorOpenSound, doorOpenVolume);
        float duration = doorOpenDuration / GameController.Instance.gameSpeed;
        map.topDoor1.DOLocalMoveX(map.topDoor1.localPosition.x - DOOR_SLIDE, duration).SetEase(Ease.InSine);
        map.topDoor2.DOLocalMoveX(map.topDoor2.localPosition.x + DOOR_SLIDE, duration).SetEase(Ease.InSine);
        yield return new WaitForSeconds(duration);
    }

    // hru + hrs + hrt: quay về cửa, đi theo đường A* tới ô hàng trên cùng, rồi vừa tối dần vừa đi ra sau cửa.
    private IEnumerator RoutineHeroWalkOut(System.Action onFadeDone)
    {
        MapController map = MapController.Instance;
        Vector3 exitPoint = map.CellToWorld(map.Rows - 1, map.Cols / 2) + DOOR_EXIT_OFFSET;

        Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        hero.StopMove();
        hero.Face(exitPoint.x > hero.transform.position.x);
        hero.animationController.PlayAnimMove();

        // Ô đích: hàng trên cùng, cột giữa (x 0) hoặc cột ngay bên trái (x −1) — cột nào gần hero hơn.
        Vector2Int heroCell = map.WorldToCell(hero.transform.position);
        int x = heroCell.y - map.Cols / 2;
        int goalCol = Mathf.Abs(x + 1) > Mathf.Abs(x) ? map.Cols / 2 : map.Cols / 2 - 1;
        List<Vector2Int> path = map.FindPath(heroCell, new Vector2Int(map.Rows - 1, goalCol));

        float speed = Mathf.Max(hero.stats.moveSpeed, MIN_TRANSITION_SPEED) * GameController.Instance.gameSpeed;
        for (int i = 1; i < path.Count; i++)
        {
            yield return MoveHeroTo(map.CellToWorld(path[i]), speed);
        }

        UIManager.Instance.FadeOut(FadeColor.Black, fadeDuration, target: MapFadeImage, doneCallback: () => onFadeDone());
        yield return MoveHeroTo(exitPoint, speed);
    }

    private WaitForSeconds MoveHeroTo(Vector3 point, float speed)
    {
        point.z = hero.transform.position.z;
        float duration = Vector3.Distance(hero.transform.position, point) / speed;
        hero.Face(point.x > hero.transform.position.x);
        hero.transform.DOMove(point, duration).SetEase(Ease.Linear);
        return new WaitForSeconds(duration);
    }

    // ===== SPAWN QUÂN THẬT =====

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
}

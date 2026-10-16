using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Dọn màn cũ trước khi dựng lại (BaseMode.Reset lo container + hero/pet + ResetBattle).
    [ContextMenu("Clear")]
    public override void Reset()
    {
        base.Reset();
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

    // ===== KẾT QUẢ TRẬN (Phase 1: chỉ vòng lặp win/lose + reload) =====
    // BaseMode.EndGame gọi CalculateResult sau khi phát EventID.EndGame.
    //   Win  → đánh dấu qua màn (PassStage: cập nhật passedStageId + stageIdCurrent sang màn kế) rồi lưu.
    //   Lose → lùi stageIdCurrent 1 màn (StepBackStage), passedStageId giữ nguyên, rồi đánh lại màn dễ hơn.
    // Dù thắng hay thua đều dựng lại màn sau delayEndGame giây (thắng thì chờ thêm tới khi thu xong mảnh exp). Reload trễ (coroutine) để KHÔNG
    // hủy unit ngay giữa lúc BaseMode đang dispatch sự kiện UnitDie/EndGame.
    protected override void CalculateResult(bool isWin)
    {
        DebugCustom.ShowLog("Show EndGame");
        if (isWin)
        {
            int stageId = overrideStageId > 0 ? overrideStageId : GameData.userData.campaign.stageIdCurrent;

            // Exp thắng màn = round(49 + level) (clear sạch quái, xem DecodedData/EXP_MODEL.md).
            // Cộng vào playerExperience → có thể lên playerLevel (rarity table tự tốt lên).
            int level = GameData.staticData.campaign.GetLevel(stageId);
            // Boost exp mua ở Store (lifetime ×2, 20 phút ×1.5, 5 phút ×2) + thưởng gem mỗi level vừa lên.
            // TODO(Phase 3): lên level thì bật UILevelPopup (bảng rarity + số gem) trước khi reload.
            PlayerExpService.Grant(GameData.staticData.experience.GetStageExp(level));

            GameData.userData.campaign.PassStage(stageId);
            GameData.Save(true);

            // Mảnh exp trên map bay về hero (chỉ là hiệu ứng, exp đã cộng ở trên).
            if (lootDrop != null && hero != null)
            {
                lootDrop.CollectAll(hero.transform);
            }
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

        // Cập nhật lại lobby (level, thanh exp, gem, tên màn) sau khi data đổi vì thắng/thua.
        if (GameController.Instance.uiLobby != null)
        {
            GameController.Instance.uiLobby.Refresh();
        }
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

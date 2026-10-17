using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mode DUNGEON (Dragon's Hoard / Zombie Outbreak / Cultist Ritual) — vào từ UIDungeonPopupStart (Enter)
// qua GameController.ChangeMode(DungeonConfig.modeType). Phần chung mọi dungeon:
//   - level đang đánh = DungeonProgress.level của dungeonType,
//   - UI trong trận = objDungeonUI của UIMainLobby (nút Exit — thoát KHÔNG tiêu key; txtLevelMap = độ khó),
//   - kết quả: THẮNG mới gọi DungeonService.CompleteDungeon (tiêu key + thưởng + level+1), thua giữ nguyên,
//     rồi hiện UIDungeonEndPopup; bấm nút popup → về lại campaign.
// Mode con chỉ cần override CreateTeamB (sinh quái riêng của dungeon). Luật gốc: DecodedData/DUNGEON_MODEL.md.
public class DungeonMode : BaseMode
{
    [Header("Dungeon")]
    public DungeonType dungeonType;

    // Level dungeon của trận này (chốt lúc dựng màn, trước khi thắng làm level tăng).
    protected int dungeonLevel;
    public int DungeonLevel => dungeonLevel;

    public override void Init(GameController controller, ModeType typeModeInput = ModeType.DefaultLevel)
    {
        base.Init(controller, typeModeInput);
        Reset();
        Initialize();
    }

    protected override void CreateMap()
    {
        dungeonLevel = DungeonService.GetProgress(dungeonType).level;
        base.CreateMap();
    }

    // Gốc LevelController.hpv → LevelLoader.itp(level) → rw.itr: seed = level dungeon × 7912 (+1000/lần thử)
    // ⇒ mỗi level dungeon 1 layout cố định, KHÔNG phụ thuộc màn campaign.
    protected override int GetLayoutLevel(int stageId)
    {
        return dungeonLevel;
    }

    protected override void CreateTeamA()
    {
        SpawnHeroAndPets();
    }

    // GridManager.ijx(count) — chỗ đứng quái (dùng chung với campaign): ô trống từ hàng 5, đi tới được từ
    // hàng 0 cột giữa (flood-fill 8 hướng), bốc ngẫu nhiên không trùng. Thiếu ô → trả ít hơn count.
    protected List<Vector2Int> PickEnemyCells(int count)
    {
        return MapSpawnCells.FindEnemySpawnCells(MapController.Instance.Grid, count);
    }

    protected override void CalculateResult(bool isWin)
    {
        int reward = isWin ? DungeonService.CompleteDungeon(dungeonType) : 0;
        GameController.Instance.uiLobby.UpdateLootTicketText();
        StartCoroutine(RoutineShowResult(isWin, reward));
    }

    private IEnumerator RoutineShowResult(bool isWin, int reward)
    {
        yield return new WaitForSeconds(delayEndGame);

        // Ẩn nút Exit khi hiện kết quả (chỉ còn nút của popup để về campaign).
        GameController.Instance.uiLobby.SetActiveDungeonUI(false);
        UIDungeonEndPopup popup = UIManager.Instance.LoadUI(UIKey.DungeonEndPopup, isBackable: false) as UIDungeonEndPopup;
        if (popup != null)
        {
            popup.Show(isWin, dungeonType, reward, Exit);
        }
        else
        {
            Exit();
        }
    }

    // Về campaign (nút Exit trong trận hoặc nút popup kết quả).
    public void Exit()
    {
        // Đang fade đổi mode (VD vừa vào trận) → ChangeMode sẽ bỏ qua, để người chơi bấm lại.
        if (GameController.Instance.isChangingMode)
        {
            return;
        }

        // Chốt trận: trong lúc fade không được tính thắng/thua nữa (thoát không tiêu key).
        isEndMode = true;
        isPause = true;
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
    }
}

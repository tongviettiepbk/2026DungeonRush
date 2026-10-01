using System.Collections;
using UnityEngine;

// Mode DUNGEON (Dragon's Hoard / Zombie Outbreak / Cultist Ritual) — vào từ UIDungeonPopupStart (Enter)
// qua GameController.ChangeMode(DungeonConfig.modeType). Phần chung mọi dungeon:
//   - level đang đánh = DungeonProgress.level của dungeonType,
//   - HUD trong trận (UIDungeonHud: nút Exit — thoát KHÔNG tiêu key),
//   - kết quả: THẮNG mới gọi DungeonService.CompleteDungeon (tiêu key + thưởng + level+1), thua giữ nguyên,
//     rồi hiện UIDungeonEndPopup; bấm nút popup → về lại campaign.
// Mode con chỉ cần override CreateTeamB (sinh quái riêng của dungeon). Luật gốc: DecodedData/DUNGEON_MODEL.md.
public class DungeonMode : BaseMode
{
    [Header("Dungeon")]
    public DungeonType dungeonType;

    // Level dungeon của trận này (chốt lúc dựng màn, trước khi thắng làm level tăng).
    protected int dungeonLevel;

    private UIDungeonHud hud;

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

    protected override void CreateTeamA()
    {
        SpawnHeroAndPets();
    }

    public override void StartGame()
    {
        base.StartGame();

        hud = UIManager.Instance.LoadUI(UIKey.DungeonHud, isBackable: false) as UIDungeonHud;
        if (hud != null)
        {
            hud.Show(dungeonType, dungeonLevel, Exit);
        }
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

        CloseHud();
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
        // Đang fade đổi mode (VD vừa vào trận) → ChangeMode sẽ bỏ qua, giữ HUD để bấm lại.
        if (GameController.Instance.isChangingMode)
        {
            return;
        }

        // Chốt trận: trong lúc fade không được tính thắng/thua nữa (thoát không tiêu key).
        isEndMode = true;
        isPause = true;
        CloseHud();
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
    }

    private void CloseHud()
    {
        if (hud != null)
        {
            hud.Close();
            hud = null;
        }
    }
}

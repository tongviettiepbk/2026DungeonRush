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
    protected override int GetMapSeedKey()
    {
        return dungeonLevel;
    }

    protected override void CreateTeamA()
    {
        SpawnHeroAndPets();
    }

    // GridManager.ijx(count) — chỗ đứng quái dungeon (dùng chung): ô trống ở nửa trên (row ≥ 5),
    // nằm trong vùng liên thông với hero (ijw flood-fill — map có obstacle có thể tạo ô kín),
    // rồi bốc ngẫu nhiên từng ô không trùng (Random.Range + RemoveAt). Thiếu ô → trả ít hơn count.
    private const int ENEMY_MIN_ROW = 5;

    protected List<Vector2Int> PickEnemyCells(int count)
    {
        MapController map = MapController.Instance;
        HashSet<Vector2Int> reachable = FloodFill(map, new Vector2Int(0, map.Cols / 2));

        var candidates = new List<Vector2Int>();
        for (int row = ENEMY_MIN_ROW; row < map.Rows; row++)
        {
            for (int col = 0; col < map.Cols; col++)
            {
                var cell = new Vector2Int(row, col);
                if (reachable.Contains(cell))
                {
                    candidates.Add(cell);
                }
            }
        }

        var result = new List<Vector2Int>();
        while (result.Count < count && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return result;
    }

    // Các ô đi được nối liền với start (4 hướng).
    private static HashSet<Vector2Int> FloodFill(MapController map, Vector2Int start)
    {
        var visited = new HashSet<Vector2Int>();
        if (!map.IsWalkable(start))
        {
            return visited;
        }

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector2Int next = cell + dirs[i];
                if (map.IsWalkable(next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }
        return visited;
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

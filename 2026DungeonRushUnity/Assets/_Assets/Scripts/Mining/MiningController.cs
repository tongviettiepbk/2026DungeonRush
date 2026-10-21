using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Điều khiển 1 phiên đào trong MiningWorld (MiningController gốc, reverse il2cpp v41).
// Bấm 1 khối trong vùng nhìn thấy (trừ hàng đáy) → theo dụng cụ đang chọn:
//   - Cuốc (Pickaxe): tìm đường A* tới ô cạnh khối → trừ 1 cuốc → đi tới → quay mặt → đập 1 nhát;
//     vỡ thì + quặng (nếu nhặt được), độ sâu tăng khi đào tới hàng BaselineRow−1 của màn hình.
//   - Khoan (Drill): như trên nhưng trừ 1 khoan, rồi khoan thẳng xuống DrillDepth ô từ khối đã chọn
//     (phá + nhặt từng ô, nhân vật chạy theo với PowerUpMoveSpeed).
//   - Cuốc vàng (GoldenPickaxe): trừ 1, rồi đi xuống GoldenPickaxeRows hàng theo cột đã chọn, mỗi hàng
//     vung 1 nhát phá SẠCH cả hàng.
// Không có đường → toast "No path available!"; hết dụng cụ → toast tương ứng; bấm quá sâu → "Too deep!".
public class MiningController : MonoBehaviour
{
    public static Action<MineOreType> OreMined;
    public static Action<int> PickaxeCountChanged;
    public static Action ToolActionCompleted;
    public static Action<int> DepthChanged;

    public MiningGrid grid;
    public MiningCharacter character;
    public MiningCamera miningCamera;
    public Transform blockParent;
    public Transform backgroundPlane1;
    public Transform backgroundPlane2;

    private MiningConfig config;
    private readonly MiningPathfinder pathfinder = new MiningPathfinder();
    private UserMiningData data;
    private Coroutine toolRoutine;
    private Transform planeLower;
    private Transform planeUpper;
    private float planeHeight;
    private bool isInitialized;

    public MiningState State { get; private set; }
    public MiningToolType SelectedTool { get; private set; }

    // jbe: vào mỏ.
    public void Init(MiningConfig cfg)
    {
        config = cfg;
        data = GameData.userData.mining;
        MiningService.InitIfNeeded();
        TutorialService.Complete(FtueStep.EnterMine);   // jbp gốc: vào mỏ lần đầu (chỉ đánh dấu)
        MiningService.RegenPickaxes();
        MiningService.CheckDailyAdReset();
        AlignBlockParent();

        grid.Init(config, blockParent, data.seed, data.minedPositions, data.damagedBlocks, miningCamera.miningCam);
        data.mineLevel = MiningService.GetMineLevel(data.currentDepth);
        grid.GenerateRows(data.currentDepth, data.mineLevel);

        character.Init(grid, config.CharacterMoveSpeed, new Vector2Int(data.characterGridX, data.characterGridY));
        character.SetTool(config.GetToolSprite(SelectedTool), config.GetToolAnimator(SelectedTool));
        miningCamera.Init(config, data.currentDepth, blockParent);

        AdvanceDepthOnInit();
        EnsureCharacterWalkable();
        SetupBackgroundPlanes();
        State = MiningState.Idle;
        isInitialized = true;
        data.isDataChanged = true;
    }

    private void LateUpdate()
    {
        if (isInitialized)
        {
            UpdateBackgroundPlanes();
        }
    }

    // jbf: BlockParent đặt ở y = CellSize·depth để hàng `depth` luôn ở y = 0 của world mỏ.
    private void AlignBlockParent()
    {
        Vector3 pos = blockParent.position;
        blockParent.position = new Vector3(pos.x, config.CellSize * data.currentDepth, pos.z);
    }

    // jbg: 2 tấm nền xếp chồng theo chiều cao, căn theo camera.
    private void SetupBackgroundPlanes()
    {
        if (backgroundPlane1 == null || backgroundPlane2 == null)
        {
            return;
        }

        Renderer r = backgroundPlane1.GetComponent<Renderer>();
        if (r == null)
        {
            return;
        }

        Vector3 size = r.bounds.size;
        planeHeight = size.y > 0.01f ? size.y : size.z;
        if (planeHeight <= 0f)
        {
            return;
        }

        float camY = miningCamera.miningCam.transform.position.y;
        float baseY = (float)Math.Round(camY / planeHeight) * planeHeight;
        backgroundPlane1.position = new Vector3(backgroundPlane1.position.x, baseY, backgroundPlane1.position.z);
        backgroundPlane2.position = new Vector3(backgroundPlane2.position.x, baseY - planeHeight, backgroundPlane2.position.z);
        planeLower = backgroundPlane2;
        planeUpper = backgroundPlane1;
    }

    // jbh: camera xuống quá tấm dưới → đưa tấm trên xuống dưới (cuộn vô hạn).
    private void UpdateBackgroundPlanes()
    {
        if (planeLower == null || planeHeight <= 0f)
        {
            return;
        }

        float camY = miningCamera.miningCam.transform.position.y;
        if (camY < planeLower.position.y)
        {
            planeUpper.position = new Vector3(planeUpper.position.x, planeLower.position.y - planeHeight, planeUpper.position.z);
            Transform tmp = planeLower;
            planeLower = planeUpper;
            planeUpper = tmp;
        }
    }

    // jbl
    public void SelectTool(MiningToolType tool)
    {
        SelectedTool = tool;
        character.SetTool(config.GetToolSprite(tool), config.GetToolAnimator(tool));
    }

    // jbm: bấm vào ô lưới.
    public void OnCellClicked(Vector2Int cell)
    {
        if (State != MiningState.Idle)
        {
            return;
        }

        EnsureCharacterWalkable();
        if (cell.y >= data.currentDepth + config.VisibleRows - 1)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Mining.TooDeep"), isLocalize: false);
            return;
        }

        switch (SelectedTool)
        {
            case MiningToolType.Drill: UseDrill(cell); break;
            case MiningToolType.GoldenPickaxe: UseGoldenPickaxe(cell); break;
            default: UsePickaxe(cell); break;
        }
    }

    // ----- Cuốc (jbn → jbo → jbp) -----

    private void UsePickaxe(Vector2Int cell)
    {
        if (grid.HasBlock(cell) == false)
        {
            return;
        }

        if (MiningService.GetToolCount(MiningToolType.Pickaxe) <= 0)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Mining.NoPickaxes"), isLocalize: false);
            return;
        }

        List<Vector2Int> path = pathfinder.FindPathToNeighbor(character.GridPosition, cell, grid.IsWalkable);
        if (path == null)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Mining.NoPath"), isLocalize: false);
            return;
        }

        MiningService.ConsumeTool(MiningToolType.Pickaxe);
        PickaxeCountChanged?.Invoke(MiningService.GetToolCount(MiningToolType.Pickaxe));
        State = MiningState.Moving;
        character.MoveAlongPath(path, () => SwingPickaxe(cell));
    }

    // jbo
    private void SwingPickaxe(Vector2Int cell)
    {
        State = MiningState.Mining;
        character.Face(cell - character.GridPosition, () =>
        {
            character.PlayAttack(() => HitBlock(cell), () => State = MiningState.Idle);
        });
    }

    // jbp: nhát đập trúng khối.
    private void HitBlock(Vector2Int cell)
    {
        MiningBlock block = grid.GetBlock(cell);
        if (block == null)
        {
            return;
        }

        MineOreType ore = block.OreType;
        bool collectible = block.IsCollectible;
        PlayHitSound(ore);
        if (grid.HitBlock(cell))
        {
            SpawnBreakParticle(cell, ore);
            if (collectible)
            {
                CollectOre(ore);
            }
            UpdateDepth(cell.y, 0f);
        }
        SaveProgress();
    }

    private void CollectOre(MineOreType ore)
    {
        MiningService.AddOre(ore, 1);
        OreMined?.Invoke(ore);
    }

    // ----- Khoan (jbq → jbr) -----

    private void UseDrill(Vector2Int cell)
    {
        if (grid.HasBlock(cell) == false)
        {
            return;
        }

        if (MiningService.GetToolCount(MiningToolType.Drill) <= 0)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Mining.NoDrills"), isLocalize: false);
            return;
        }

        List<Vector2Int> path = pathfinder.FindPathToNeighbor(character.GridPosition, cell, grid.IsWalkable);
        if (path == null)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Mining.NoPath"), isLocalize: false);
            return;
        }

        MiningService.ConsumeTool(MiningToolType.Drill);
        State = MiningState.UsingTool;
        StopToolRoutine();
        character.MoveAlongPath(path, () =>
        {
            character.Face(cell - character.GridPosition, () => toolRoutine = StartCoroutine(DrillRoutine(cell.x, cell.y)));
        });
    }

    // jbr (sp): khoan thẳng xuống DrillDepth ô bắt đầu từ khối đã chọn.
    private IEnumerator DrillRoutine(int x, int startRow)
    {
        float speed = config.PowerUpMoveSpeed;
        for (int i = 0; i < config.DrillDepth; i++)
        {
            int row = startRow + i;
            Vector2Int cell = new Vector2Int(x, row);
            BreakBlockDirect(cell);
            yield return StartCoroutine(character.MoveToCell(cell, speed));
            UpdateDepth(row, speed);
        }
        FinishToolAction();
    }

    // ----- Cuốc vàng (jbs → jbt) -----

    private void UseGoldenPickaxe(Vector2Int cell)
    {
        if (grid.HasBlock(cell) == false)
        {
            return;
        }

        if (MiningService.GetToolCount(MiningToolType.GoldenPickaxe) <= 0)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Mining.NoGoldenPickaxes"), isLocalize: false);
            return;
        }

        List<Vector2Int> path = pathfinder.FindPathToNeighbor(character.GridPosition, cell, grid.IsWalkable);
        if (path == null)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Mining.NoPath"), isLocalize: false);
            return;
        }

        MiningService.ConsumeTool(MiningToolType.GoldenPickaxe);
        State = MiningState.UsingTool;
        StopToolRoutine();
        character.MoveAlongPath(path, () =>
        {
            character.Face(cell - character.GridPosition, () => toolRoutine = StartCoroutine(GoldenPickaxeRoutine(cell.x, cell.y)));
        });
    }

    // jbt (sq): GoldenPickaxeRows hàng; hàng nào còn khối thì quay về khối đó, vung 1 nhát phá cả hàng, rồi đi xuống ô (x, hàng).
    private IEnumerator GoldenPickaxeRoutine(int x, int startRow)
    {
        float speed = config.PowerUpMoveSpeed;
        for (int row = startRow; row < startRow + config.GoldenPickaxeRows; row++)
        {
            Vector2Int target = FindBlockInRow(row, x);
            if (target.x >= 0)
            {
                bool faced = false;
                character.Face(target - character.GridPosition, () => faced = true);
                while (faced == false)
                {
                    yield return null;
                }

                bool done = false;
                Vector2Int hitTarget = target;
                int hitRow = row;
                character.PlayAttack(() =>
                {
                    MiningBlock block = grid.GetBlock(hitTarget);
                    if (block != null)
                    {
                        PlayHitSound(block.OreType);
                    }
                    BreakRow(hitRow);
                }, () => done = true);
                while (done == false)
                {
                    yield return null;
                }
            }

            yield return StartCoroutine(character.MoveToCell(new Vector2Int(x, row), speed));
            UpdateDepth(row, speed);
        }
        FinishToolAction();
    }

    // jbu: ưu tiên khối tại (x, row), không thì khối đầu tiên trong hàng; không có → (−1, −1).
    private Vector2Int FindBlockInRow(int row, int x)
    {
        Vector2Int cell = new Vector2Int(x, row);
        if (grid.HasBlock(cell))
        {
            return cell;
        }

        for (int i = 0; i < config.GridWidth; i++)
        {
            cell = new Vector2Int(i, row);
            if (grid.HasBlock(cell))
            {
                return cell;
            }
        }
        return new Vector2Int(-1, -1);
    }

    // jbv: phá cả hàng.
    private void BreakRow(int row)
    {
        for (int i = 0; i < config.GridWidth; i++)
        {
            BreakBlockDirect(new Vector2Int(i, row));
        }
    }

    // Phá thẳng 1 khối (không tính nhát): tiếng + hạt + nhặt quặng.
    private void BreakBlockDirect(Vector2Int cell)
    {
        MiningBlock block = grid.GetBlock(cell);
        if (block == null)
        {
            return;
        }

        MineOreType ore = block.OreType;
        bool collectible = block.IsCollectible;
        PlayHitSound(ore);
        grid.RemoveBlock(cell);
        SpawnBreakParticle(cell, ore);
        if (collectible)
        {
            CollectOre(ore);
        }
    }

    private void FinishToolAction()
    {
        toolRoutine = null;
        SaveProgress();
        State = MiningState.Idle;
        ToolActionCompleted?.Invoke();
    }

    private void StopToolRoutine()
    {
        if (toolRoutine != null)
        {
            StopCoroutine(toolRoutine);
            toolRoutine = null;
        }
    }

    // ----- Độ sâu -----

    // jbw: đào tới hàng >= depth + BaselineRow − 1 → depth = row − BaselineRow + 2; rồi tự xuống tiếp khi hàng mốc đã sạch.
    private void UpdateDepth(int row, float scrollSpeed)
    {
        int oldDepth = data.currentDepth;
        int threshold = oldDepth + config.BaselineRow - 1;
        if (row >= threshold)
        {
            SetDepth(row - config.BaselineRow + 2);
        }

        AdvanceWhileCleared();
        int newDepth = data.currentDepth;
        if (newDepth != oldDepth)
        {
            miningCamera.ScrollToDepth(newDepth, () => grid.CleanupAbove(newDepth), scrollSpeed);
            DepthChanged?.Invoke(newDepth);
        }
    }

    private void SetDepth(int depth)
    {
        data.currentDepth = depth;
        data.mineLevel = MiningService.GetMineLevel(depth);
        grid.GenerateRows(depth, data.mineLevel);
        data.isDataChanged = true;
    }

    // jbx: hàng mốc (depth + BaselineRow − 1) không còn khối → xuống thêm 1 (tối đa 100 lần).
    private void AdvanceWhileCleared()
    {
        int before = data.currentDepth;
        for (int i = 0; i < 100; i++)
        {
            if (grid.IsRowCleared(data.currentDepth + config.BaselineRow - 1) == false)
            {
                break;
            }
            SetDepth(data.currentDepth + 1);
        }

        if (data.currentDepth != before)
        {
            EnsureCharacterWalkable();
        }
    }

    // jca: lúc vào mỏ — xuống hết các hàng đã sạch, đặt camera ngay.
    private void AdvanceDepthOnInit()
    {
        int before = data.currentDepth;
        AdvanceWhileCleared();
        if (data.currentDepth != before)
        {
            miningCamera.SnapToDepth(data.currentDepth);
            grid.CleanupAbove(data.currentDepth);
            DepthChanged?.Invoke(data.currentDepth);
        }
    }

    // jby/jbz: nhân vật đứng ô không đi được (bị trôi khỏi vùng) → dịch tới ô trống gần nhất.
    private void EnsureCharacterWalkable()
    {
        if (grid.IsWalkable(character.GridPosition))
        {
            return;
        }

        Vector2Int cell = FindWalkableCell();
        character.Teleport(cell);
        data.characterGridX = cell.x;
        data.characterGridY = cell.y;
        data.isDataChanged = true;
    }

    // jbz: ưu tiên cùng cột từ trên xuống, rồi quét cả vùng; không có → (cột, depth).
    private Vector2Int FindWalkableCell()
    {
        int depth = data.currentDepth;
        int col = Mathf.Clamp(character.GridPosition.x, 0, config.GridWidth - 1);
        for (int row = depth; row < depth + config.VisibleRows; row++)
        {
            if (grid.IsWalkable(new Vector2Int(col, row)))
            {
                return new Vector2Int(col, row);
            }
        }

        for (int row = depth; row < depth + config.VisibleRows; row++)
        {
            for (int x = 0; x < config.GridWidth; x++)
            {
                if (grid.IsWalkable(new Vector2Int(x, row)))
                {
                    return new Vector2Int(x, row);
                }
            }
        }
        return new Vector2Int(col, depth);
    }

    // ----- Hiệu ứng -----

    // jcc
    private void PlayHitSound(MineOreType ore)
    {
        AudioClip clip = config.GetHitSound(ore, out float volume);
        AudioManager.Instance.PlaySfx(clip, volume);
    }

    // jcd/jcf: hạt vỡ tại ô, tự huỷ sau khi chạy xong.
    private void SpawnBreakParticle(Vector2Int cell, MineOreType ore)
    {
        GameObject prefab = config.GetOreParticle(ore);
        if (prefab == null)
        {
            return;
        }

        GameObject go = Instantiate(prefab, grid.GridToWorld(cell), Quaternion.identity, blockParent);
        MiningGrid.SetLayerRecursive(go, blockParent.gameObject.layer);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        float life = 2f;
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
            life = ps.main.duration + ps.main.startLifetime.constantMax;
        }
        Destroy(go, life);
    }

    // ----- Lưu -----

    // jcg: lưu vị trí nhân vật + ô đã đào + khối đập dở.
    public void SaveProgress()
    {
        data.characterGridX = character.GridPosition.x;
        data.characterGridY = character.GridPosition.y;
        data.minedPositions = grid.GetMinedPositions();
        data.damagedBlocks = grid.GetDamagedBlocks();
        data.isDataChanged = true;
        GameData.Save();
    }

    // jch: rời mỏ (đóng popup) — dừng mọi thứ, dọn khối, lưu.
    public void Shutdown()
    {
        StopToolRoutine();
        character.StopMoving();
        character.KillRotate();
        miningCamera.Stop();
        grid.ClearAll();
        SaveProgress();
        isInitialized = false;
    }
}

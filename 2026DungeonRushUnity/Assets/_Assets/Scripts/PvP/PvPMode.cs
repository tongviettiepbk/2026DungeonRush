using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mode PVP — vào từ UIPvPFindOpponentPopup (chọn đối thủ) qua PvPController.StartBattle → ChangeMode(ModeType.PvP).
// Port GameController gốc phần PvP (hia/hib/hic/hhu/hio + SpawnController.hvg/hvh):
//   • Map PvpMap = MainMap không box (MapConfig gốc). Đội nhà: hero + pet đang mang (pet VẪN ra trận).
//   • Đối thủ = SNAPSHOT đồ/cánh/áo choàng/relic của người chơi khác, phe B (IsPvPOpponent), đứng giữa hàng trên cùng
//     lùi PvPEnemyYOffset = 2 ô; pet đang mang của đối thủ cũng spawn quanh nó (AI tự ra chiêu).
//   • Đồng hồ 30s (wtz = 30). Kết thúc:
//       – Hero đối thủ chết → gỡ pet đối thủ (hvi) → THẮNG;
//       – Hero mình chết → THUA;
//       – Hết giờ (hib): Σ(HP/MaxHP) hero còn sống mỗi phe → mình ≥ đối thủ ⇒ THẮNG (hoà = thắng);
//       – Exit giữa trận (hio) → THUA.
//   • Báo server reportPvPBattle(token, won) → UIPvPEndPopup (Victory/Defeated + trophy + thưởng) sau 1.5s (Exit: 0.5s).
public class PvPMode : BaseMode
{
    private HeroUnit opponent;
    private readonly List<PetUnit> opponentPets = new List<PetUnit>();
    private Transform opponentGroup;
    private bool isGiveUp;
    private Transform clockHand;

    public HeroUnit Opponent => opponent;

    public override void Init(GameController controller, ModeType typeModeInput = ModeType.DefaultLevel)
    {
        base.Init(controller, typeModeInput);
        Reset();

        if (!PvPBattleSession.IsActive || PvPBattleSession.Opponent == null)
        {
            DebugCustom.LogError("[PvPMode] Không có PvPBattleSession — về campaign.");
            StartCoroutine(RoutineBackToCampaign());
            return;
        }

        isGiveUp = false;
        defaultBattleTime = Mathf.RoundToInt(StaticPvPData.BATTLE_DURATION);
        SetActiveHud(true);
        UpdateBattleTime(0f, defaultBattleTime);

        Initialize();
    }

    private IEnumerator RoutineBackToCampaign()
    {
        yield return null;
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
    }

    // ===== Dựng màn =====

    protected override void CreateTeamA()
    {
        SpawnHeroAndPets();
    }

    // SpawnController.hvg gốc: đối thủ ở cột giữa, hàng trên cùng lùi PvPEnemyYOffset ô.
    protected override void CreateTeamB()
    {
        PvPPlayerModel model = PvPBattleSession.Opponent;
        if (heroPrefab == null || model == null)
        {
            DebugCustom.LogError("[PvPMode] Thiếu heroPrefab / đối thủ.");
            return;
        }

        MapController map = MapController.Instance;
        int row = Mathf.Clamp(map.Rows - 1 - StaticPvPData.ENEMY_Y_OFFSET, 0, map.Rows - 1);
        Vector2Int cell = new Vector2Int(row, map.Cols / 2);
        Vector3 pos = map.CellToWorld(cell);

        opponentGroup = NewGroup("Opponent");
        opponent = SpawnUnit<HeroUnit>(heroPrefab, pos, opponentGroup);
        opponent.gameObject.name = "PvPOpponent_" + model.PlayerName;

        // Pet sở hữu → Own Effect cộng chỉ số đối thủ (như ghost Boss Rush).
        List<CompanionModel> owned = new List<CompanionModel>();
        if (model.Companions != null)
        {
            for (int i = 0; i < model.Companions.Count; i++)
            {
                BossRushCompanionModel m = model.Companions[i];
                owned.Add(new CompanionModel(m.CompanionId) { level = m.CompanionLevel, isNew = false });
            }
        }

        opponent.SetupAsGhost(model.PlayerName, BossRushController.ToEquipmentData(model.Items), owned,
                              BossRushController.ToEnchantmentData(model.EnchantmentTiers), model.ShowCloak);
        opponent.SpawnInBattle(BuildHeroStats(), StaticValue.TAG_TEAM_B, pos);

        SpawnOpponentPets(model);
    }

    // SpawnController.hvh gốc: pet đang mang của đối thủ spawn quanh nó (phe đối thủ).
    private void SpawnOpponentPets(PvPPlayerModel model)
    {
        if (model.Companions == null || opponent == null)
        {
            return;
        }

        List<BossRushCompanionModel> equipped = model.Companions.FindAll(c => c != null && c.Equipped);
        Vector3 center = opponent.transform.position;
        for (int i = 0; i < equipped.Count; i++)
        {
            CompanionData data = GameData.staticData.companions.GetData(equipped[i].CompanionId);
            GameObject petPrefab = data != null ? data.LoadPrefab() : null;
            if (petPrefab == null)
            {
                DebugCustom.LogWarning("[PvPMode] Không có prefab companion đối thủ: " + equipped[i].CompanionId);
                continue;
            }

            float angle = (360f / equipped.Count) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.8f;
            Vector3 petPos = MapController.Instance.ClampPointInMap(center + offset);

            PetUnit pet = SpawnUnit<PetUnit>(petPrefab, petPos, opponentGroup);
            pet.gameObject.name = "PvPOpponentPet_" + data.assetName;
            pet.SetupCompanion(data, opponent, Mathf.Max(1, equipped[i].CompanionLevel), petPos);
            opponentPets.Add(pet);
        }
    }

    // ===== Kết thúc =====

    // Hero đối thủ chết → gỡ pet đối thủ (SpawnController.hvi) → THẮNG. Bản sao chết không kết thúc trận.
    protected override void CheckRemainingEnemies()
    {
        if (isEndMode || opponent == null || opponent.isTargetable)
        {
            return;
        }

        RemoveOpponentPets();
        EndGame(true);
    }

    private void RemoveOpponentPets()
    {
        for (int i = 0; i < opponentPets.Count; i++)
        {
            if (opponentPets[i] != null)
            {
                opponentPets[i].Deactive();
                opponentPets[i].gameObject.SetActive(false);
            }
        }
        opponentPets.Clear();
    }

    // Đồng hồ 30s: hết giờ → so tổng %HP (GameController.hib) thay vì xử thua như BaseMode.
    protected override IEnumerator RoutineTimer()
    {
        battleTimer = 0f;
        ActiveBattleTimer(true);

        while (isEndMode == false)
        {
            if (flagBattleTimer)
            {
                battleTimer += Time.deltaTime * GameController.Instance.gameSpeed;
                UpdateBattleTime(battleTimer, defaultBattleTime);
                if (battleTimer >= defaultBattleTime)
                {
                    EndGame(SumHealthRatio(teamA) >= SumHealthRatio(teamB));
                    yield break;
                }
            }

            yield return null;
        }
    }

    // Σ(HP/MaxHP) các nhân vật còn sống (hero + bản sao), không tính pet.
    private static double SumHealthRatio(List<BaseUnit> units)
    {
        double sum = 0d;
        for (int i = 0; i < units.Count; i++)
        {
            HeroUnit unit = units[i] as HeroUnit;
            if (unit != null && unit.isTargetable && unit.GetMaxHp() > 0d)
            {
                sum += unit.hp / unit.GetMaxHp();
            }
        }
        return sum;
    }

    // Nút Exit trên EventBlocker (GameController.hio gốc): bỏ trận = THUA, kết quả hiện sau 0.5s.
    public bool GiveUp()
    {
        if (isEndMode)
        {
            return false;
        }
        isGiveUp = true;
        EndGame(false);
        return true;
    }

    protected override void CalculateResult(bool isWin)
    {
        StartCoroutine(RoutineReport(isWin));
    }

    private IEnumerator RoutineReport(bool won)
    {
        if (PvPBattleSession.IsClanWar)
        {
            yield return RoutineReportClanWar(won);
            yield break;
        }

        bool done = false;
        bool success = false;
        PvPBattleResultResponseDTO result = null;
        PvPController.Instance.ReportBattle(won, (ok, res) =>
        {
            success = ok;
            result = res;
            done = true;
        });

        yield return new WaitForSeconds(isGiveUp ? StaticPvPData.GIVE_UP_DELAY : StaticPvPData.END_DELAY);
        while (!done)
        {
            yield return null;
        }

        SetActiveHud(false);
        UIPvPEndPopup popup = UIManager.Instance.LoadUI(UIKey.PvPEndPopup, isBackable: false) as UIPvPEndPopup;
        if (popup != null)
        {
            popup.Show(won, success ? GameData.userData.pvp.pendingSettlement : null, success ? null : (result != null ? result.message : null), Exit);
        }
        else
        {
            PvPController.Instance.ClaimSettlement();
            Exit();
        }
    }

    // Trận Clan War ngày 6 (ClanWarController.gaf gốc): báo kết quả → toast điểm đóng góp → về lobby, mở lại tab Clan > Battle.
    private IEnumerator RoutineReportClanWar(bool won)
    {
        bool done = false;
        ClanWarPvpReportResponseDTO result = null;
        bool success = false;
        ClanWarController.Instance.ReportPvp(won, (ok, res) =>
        {
            success = ok;
            result = res;
            done = true;
        });

        yield return new WaitForSeconds(isGiveUp ? StaticPvPData.GIVE_UP_DELAY : StaticPvPData.END_DELAY);
        float waited = 0f;
        while (!done && waited < 10f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        string message;
        if (!done || (!success && result == null)) message = "Result will be synced when the connection recovers.";
        else if (!success) message = ClanWarController.ErrorText(result.code, "Failed to report battle.");
        else if (result.alreadyDefeated) message = "This enemy was already defeated by your clan. No points awarded.";
        else if (result.pointsAwarded > 0) message = "+" + result.pointsAwarded + " Contribution Points earned!";
        else message = won ? "Victory!" : "Defeated";
        UIManager.Instance.ShowToastMessage(message, isLocalize: false);

        if (GameController.Instance.isChangingMode)
        {
            yield break;
        }
        isEndMode = true;
        isPause = true;
        PvPBattleSession.Clear();
        SetActiveHud(false);
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
        ClanTabPage.OpenWarTabAfterModeChange();
    }

    // Đóng popup kết quả → về campaign rồi mở lại sảnh PvP.
    public void Exit()
    {
        if (GameController.Instance.isChangingMode)
        {
            return;
        }

        isEndMode = true;
        isPause = true;
        PvPBattleSession.Clear();
        SetActiveHud(false);
        GameController.Instance.ChangeMode(ModeType.DefaultLevel);
        UIPvPPopup.OpenAfterModeChange();
    }

    // ===== HUD (PvPUI gốc: thời gian còn lại + ClockAnimation) =====

    protected override void UpdateBattleTime(float timer, int totalTime)
    {
        UIMainLobby lobby = GameController.uiLobby;
        if (lobby == null)
        {
            return;
        }

        float remain = Mathf.Max(0f, totalTime - timer);
        if (lobby.txtTimePvp != null)
        {
            lobby.txtTimePvp.text = Mathf.CeilToInt(remain) + "s";
        }
        if (clockHand != null && totalTime > 0)
        {
            clockHand.localEulerAngles = new Vector3(0f, 0f, -360f * timer / totalTime);
        }
    }

    // HUD PvP + tấm phủ EventBlocker (GameplayUI.OnStateChanged gốc nhánh BossRush/PvP).
    private void SetActiveHud(bool isOn)
    {
        UIMainLobby lobby = GameController != null ? GameController.uiLobby : null;
        if (lobby == null)
        {
            return;
        }
        if (lobby.objPvpUI != null)
        {
            lobby.objPvpUI.SetActive(isOn);
            if (clockHand == null)
            {
                clockHand = lobby.objPvpUI.transform.Find("ClockUI/MinuteHand");
            }
        }
        lobby.SetActiveEventBlocker(isOn);
    }

    public override void Reset()
    {
        base.Reset();
        opponent = null;
        opponentPets.Clear();
        opponentGroup = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SetActiveHud(false);
    }
}

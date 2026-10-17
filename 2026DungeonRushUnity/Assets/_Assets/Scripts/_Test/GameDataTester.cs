using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// Kéo vào 1 GameObject trống trong scene bất kỳ để test data layer.
// Chuột phải component để chạy các lệnh cheat.
public class GameDataTester : MonoBehaviour
{
    private void Awake()
    {
        //GameData.Init();

        DebugCustom.Log("[Tester] isNewUser=" + GameData.userData.isNewUser);
        DebugCustom.Log("[Tester] Profile=" + JsonConvert.SerializeObject(GameData.userData.profile));
        DebugCustom.Log("[Tester] Campaign=" + JsonConvert.SerializeObject(GameData.userData.campaign));
        DebugCustom.Log("[Tester] Items=" + JsonConvert.SerializeObject(GameData.userData.items));
        DebugCustom.Log("[Tester] Settings=" + JsonConvert.SerializeObject(GameData.userData.settings));
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            GameData.Save(true);
        }
    }

    private void OnApplicationQuit()
    {
        GameData.Save(true);
    }

    [ContextMenu("Cheat/Add 1000 Gold")]
    private void CheatAddGold()
    {
        GameData.userData.items.Receive(ItemType.GOLD, 1000);
        GameData.Save(true);
        DebugCustom.Log("[Tester] Gold=" + GameData.userData.items.GetQuantityHave(ItemType.GOLD));
    }

    [ContextMenu("Cheat/Pass Current Stage")]
    private void CheatPassStage()
    {
        var campaign = GameData.userData.campaign;
        campaign.PassStage(campaign.stageIdCurrent);
        GameData.Save(true);
        DebugCustom.Log("[Tester] stageIdCurrent=" + campaign.stageIdCurrent + ", passed=" + campaign.passedStageId);
    }

    [ContextMenu("Test/Generate Current Campaign Level")]
    private void TestGenerateLevel()
    {
        int stageId = GameData.userData.campaign.stageIdCurrent;
        int level = GameData.staticData.campaign.GetLevel(stageId);
        CampaignLevelBuilder.CampaignLevel map = CampaignLevelBuilder.Build(stageId, ModeType.DefaultLevel, level);

        int rows = map.grid.GetLength(0);
        int cols = map.grid.GetLength(1);
        Vector2Int heroCell = MapSpawnCells.GetHeroSpawnCell(map.grid);
        List<EnemySpawnGenerator.EnemySpawnInfo> enemies = EnemySpawnGenerator.Generate(stageId,
            MapSpawnCells.FindEnemySpawnCells(map.grid, EnemySpawnGenerator.CampaignCount(stageId)));
        var enemyCells = new HashSet<Vector2Int>();
        for (int i = 0; i < enemies.Count; i++) enemyCells.Add(enemies[i].cell);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[Map] stage={stageId} level={level} walls={map.obstacles.Count} enemies={enemies.Count}");
        // In từ hàng TRÊN (row rows-1) xuống row 0. # = chướng ngại, H = hero, E = quái, khoảng trắng = trống.
        for (int row = rows - 1; row >= 0; row--)
        {
            string line = "";
            for (int col = 0; col < cols; col++)
            {
                var c = new Vector2Int(row, col);
                if (map.grid[row, col] == 1) line += "#";
                else if (c == heroCell) line += "H";
                else if (enemyCells.Contains(c)) line += "E";
                else line += " ";
            }
            sb.AppendLine("|" + line + "|");
        }
        DebugCustom.Log(sb.ToString());

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemySpawnGenerator.EnemySpawnInfo e = enemies[i];
            DebugCustom.Log($"[Enemy {i}] cell={e.cell} lv={e.level} hp={e.health:0} atk={e.attackPower:0} ranged={e.isRanged}");
        }
    }

    // Companion DPS (Ember Fist) để test hệ save/level companion.
    private const string CHEAT_COMPANION_ID = "Companion_1_Common_DPS_Single";

    [ContextMenu("Cheat/Companion - Add 16 Cards (DPS)")]
    private void CheatCompanionAddCards()
    {
        GameData.userData.companions.AddCards(CHEAT_COMPANION_ID, 16);
        int levelsUp = 0;
        while (GameData.userData.companions.Upgrade(CHEAT_COMPANION_ID))
        {
            levelsUp++;
        }
        GameData.userData.companions.Equip(CHEAT_COMPANION_ID);
        GameData.Save(true);

        CompanionModel m = GameData.userData.companions.GetModel(CHEAT_COMPANION_ID);
        DebugCustom.Log($"[Tester] Companion {CHEAT_COMPANION_ID}: +{levelsUp} level -> lv={m.level} cards={m.cardCount} equipped={GameData.userData.companions.IsEquipped(CHEAT_COMPANION_ID)}");
    }

    [ContextMenu("Cheat/Companion - Log Owned")]
    private void CheatCompanionLog()
    {
        DebugCustom.Log("[Tester] Companions=" + JsonConvert.SerializeObject(GameData.userData.companions));
    }

    [ContextMenu("Cheat/Clear All Save")]
    private void CheatClearSave()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        GameData.Reset();
        DebugCustom.Log("[Tester] Cleared save. Restart play mode to re-init.");
    }
}

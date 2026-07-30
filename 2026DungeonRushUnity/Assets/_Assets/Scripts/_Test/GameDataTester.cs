using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// Kéo vào 1 GameObject trống trong scene bất kỳ để test data layer.
// Chuột phải component để chạy các lệnh cheat.
public class GameDataTester : MonoBehaviour
{
    private void Awake()
    {
        GameData.Init();

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
        campaign.PassStage(campaign.curStageId);
        GameData.Save(true);
        DebugCustom.Log("[Tester] curStageId=" + campaign.curStageId + ", passed=" + campaign.passedStageId);
    }

    [ContextMenu("Test/Generate Current Campaign Level")]
    private void TestGenerateLevel()
    {
        int stageId = GameData.userData.campaign.curStageId;
        CampaignLevelBuilder.CampaignLevel level = CampaignLevelBuilder.Build(stageId);

        MapConfig cfg = level.config;
        var playerSpawns = new HashSet<Vector2Int>(level.playerSpawnCells);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[Map] stage={stageId} env={level.environment} walls={level.obstacles.Count} enemies={level.enemies.Count}");
        // In từ hàng trên xuống. # = wall(1), . = spawn quân, D = cửa, khoảng trắng = trống(0).
        for (int y = cfg.rows - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < cfg.cols; x++)
            {
                var c = new Vector2Int(x, y);
                if (level.grid[x, y] == 1) row += "#";
                else if (c == level.doorCell) row += "D";
                else if (playerSpawns.Contains(c)) row += ".";
                else row += " ";
            }
            sb.AppendLine("|" + row + "|");
        }
        DebugCustom.Log(sb.ToString());

        for (int i = 0; i < level.enemies.Count; i++)
        {
            EnemySpawnGenerator.EnemySpawnInfo e = level.enemies[i];
            DebugCustom.Log($"[Enemy {i}] cell={e.cell} lv={e.level} hp={e.health:0} atk={e.attackPower:0} boss={e.isBoss}");
        }
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

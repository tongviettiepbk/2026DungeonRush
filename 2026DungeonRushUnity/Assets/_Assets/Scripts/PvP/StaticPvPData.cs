using System.Collections.Generic;
using UnityEngine;

// 1 league.
public class PvPLeagueDefinition
{
    public int leagueIndex;
    public string leagueName;      // Common.PvP.League.{index}
    public Color leagueColor;
}

// Config tĩnh PvP mùa + nhóm league (GDD/PVP_DESIGN.md). League, cúp, nhóm, thưởng do server giữ
// (server/functions/src/pvpConfig.ts); ở đây chỉ còn tên + màu league và các hằng số của trận đánh.
public class StaticPvPData
{
    public const int UNLOCK_PLAYER_LEVEL = 15;
    public const int DAILY_TICKETS = 5;                     // không có vé xem quảng cáo
    public const float BATTLE_DURATION = 30f;               // GameController.hia: wtz = 30
    public const int ENEMY_Y_OFFSET = 2;                    // SpawnController.PvPEnemyYOffset (GameplayScene)
    public const float END_DELAY = 1.5f;                    // GameController.hhu: Completed/Failed → hht(1.5f)
    public const float GIVE_UP_DELAY = 0.5f;                // GameController.hio → hht(0.5f)
    public const string CONTENT_VERSION = "1.0.0";          // PvPConfig.ContentVersion

    public List<PvPLeagueDefinition> leagues;

    public StaticPvPData()
    {
        leagues = new List<PvPLeagueDefinition>
        {
            League(1, 0.69f, 0.69f, 0.72f),
            League(2, 0.71f, 0.47f, 0.28f),
            League(3, 0.83f, 0.86f, 0.91f),
            League(4, 0.95f, 0.78f, 0.26f),
            League(5, 0.53f, 0.9f, 0.84f),
            League(6, 0.18f, 0.74f, 0.42f),
            League(7, 0.48f, 0.78f, 0.98f),
            League(8, 0.77f, 0.49f, 0.94f),
            League(9, 0.98f, 0.43f, 0.43f),
            League(10, 1f, 0.59f, 0.1f),
        };
    }

    private static PvPLeagueDefinition League(int index, float r, float g, float b)
    {
        return new PvPLeagueDefinition
        {
            leagueIndex = index, leagueName = "Common.PvP.League." + index, leagueColor = new Color(r, g, b, 1f),
        };
    }

    // League theo index (không có → league đầu).
    public PvPLeagueDefinition GetLeagueByIndex(int index)
    {
        for (int i = 0; i < leagues.Count; i++)
        {
            if (leagues[i].leagueIndex == index)
            {
                return leagues[i];
            }
        }
        return leagues[0];
    }

    public string GetLeagueName(int index) => Loc.L(GetLeagueByIndex(index).leagueName);
}

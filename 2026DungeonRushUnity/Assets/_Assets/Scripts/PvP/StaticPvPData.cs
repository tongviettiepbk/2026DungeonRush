using System;
using System.Collections.Generic;
using UnityEngine;

// 1 league (PvPLeagueDefinition gốc).
public class PvPLeagueDefinition
{
    public int leagueIndex;
    public string leagueName;      // Common.PvP.League.{index}
    public Color leagueColor;
    public int minTrophy;
    public int maxTrophy;
    public int kWin;
    public int kLoss;
    public int weeklyDecay;
}

// Config tĩnh PvP — REVERSE từ game gốc v41 (PvPConfig.asset + PvPConfig.jkp/jkx/jky/jlb, GameController.hia,
// SpawnController.PvPEnemyYOffset). Server (server/functions/src/pvpConfig.ts) giữ bản sao y hệt.
// Chi tiết: DecodedData/PVP_MODEL.md.
public class StaticPvPData
{
    public const int UNLOCK_PLAYER_LEVEL = 15;              // PvPController.yev
    public const int START_TROPHY = 1000;                   // save gốc PvPTrophy mặc định
    public const int MINIMUM_TROPHY = 0;                    // PvPConfig.MinimumTrophy
    public const int DAILY_FREE_TICKETS = 5;                // TicketConfig.DailyFreeTickets
    public const int MAX_AD_TICKETS_PER_DAY = 4;            // TicketConfig.MaxRewardedAdTicketsPerDay
    public const int SNAPSHOT_UPLOAD_MIN_INTERVAL = 1800;   // Matchmaking.SnapshotUploadMinIntervalSeconds
    public const float BATTLE_DURATION = 30f;               // GameController.hia: wtz = 30
    public const int ENEMY_Y_OFFSET = 2;                    // SpawnController.PvPEnemyYOffset (GameplayScene)
    public const float END_DELAY = 1.5f;                    // GameController.hhu: Completed/Failed → hht(1.5f)
    public const float GIVE_UP_DELAY = 0.5f;                // GameController.hio → hht(0.5f)
    public const string CONTENT_VERSION = "1.0.0";          // PvPConfig.ContentVersion
    public static readonly string[] LEADERBOARD_SCOPES = { "world", "country" };   // PvPLeaderboardPopup.ygt

    public List<PvPLeagueDefinition> leagues;

    public StaticPvPData()
    {
        leagues = new List<PvPLeagueDefinition>
        {
            League(1, "Iron League", 0.69f, 0.69f, 0.72f, 0, 1199, 40, -10),
            League(2, "Bronze League", 0.71f, 0.47f, 0.28f, 1200, 1399, 40, -10),
            League(3, "Silver League", 0.83f, 0.86f, 0.91f, 1400, 1599, 35, -15),
            League(4, "Gold League", 0.95f, 0.78f, 0.26f, 1600, 1799, 30, -15),
            League(5, "Platinum League", 0.53f, 0.9f, 0.84f, 1800, 1999, 30, -20),
            League(6, "Emerald League", 0.18f, 0.74f, 0.42f, 2000, 2199, 30, -20),
            League(7, "Diamond League", 0.48f, 0.78f, 0.98f, 2200, 2399, 25, -25),
            League(8, "Master", 0.77f, 0.49f, 0.94f, 2400, 2599, 25, -25),
            League(9, "Grandmaster", 0.98f, 0.43f, 0.43f, 2600, 2799, 20, -20),
            League(10, "Legend", 1f, 0.59f, 0.1f, 2800, int.MaxValue, 20, -20),
        };
    }

    private static PvPLeagueDefinition League(int index, string name, float r, float g, float b, int min, int max, int kWin, int kLoss)
    {
        return new PvPLeagueDefinition
        {
            leagueIndex = index, leagueName = name, leagueColor = new Color(r, g, b, 1f),
            minTrophy = min, maxTrophy = max, kWin = kWin, kLoss = kLoss, weeklyDecay = 0,
        };
    }

    // jkp: league chứa trophy (kẹp ≥ 0); không khớp → league đầu.
    public PvPLeagueDefinition GetLeague(int trophy)
    {
        int t = Math.Max(0, trophy);
        for (int i = 0; i < leagues.Count; i++)
        {
            if (t >= leagues[i].minTrophy && t <= leagues[i].maxTrophy)
            {
                return leagues[i];
            }
        }
        return leagues[0];
    }

    // jko: league theo index (không có → league đầu).
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

    public int GetLeagueIndex(int trophy) => GetLeague(trophy).leagueIndex;

    public string GetLeagueName(int index) => GetLeagueByIndex(index).leagueName;

    // jlb: điểm kỳ vọng Elo.
    public static double ExpectedScore(int my, int opp)
    {
        return 1d / (1d + Math.Pow(10d, (opp - my) / 400d));
    }

    // jkx: trophy cộng khi thắng (≥ 1). Làm tròn như Mathf.RoundToInt gốc (nửa về số chẵn).
    public int GetWinDelta(int my, int opp)
    {
        float v = (float)(GetLeague(my).kWin * (1d - ExpectedScore(my, opp)));
        return Math.Max(1, Mathf.RoundToInt(v));
    }

    // jky: trophy trừ khi thua (≤ −1).
    public int GetLossDelta(int my, int opp)
    {
        float v = (float)(-Math.Abs(GetLeague(my).kLoss) * ExpectedScore(my, opp));
        return Math.Min(-1, Mathf.RoundToInt(v));
    }

    // jkz / jla.
    public int GetProjectedWin(int my, int opp) => Math.Max(MINIMUM_TROPHY, my + GetWinDelta(my, opp));
    public int GetProjectedLoss(int my, int opp) => Math.Max(MINIMUM_TROPHY, my + GetLossDelta(my, opp));
}

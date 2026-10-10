using System.Collections.Generic;
using TMPro;
using UnityEngine;

// "Day {n} Actions" (ClanWarDayActionsSection gốc): liệt kê hành động có điểm trong ngày theo config.sourcesByDay
// + awards (Loot theo 10 rarity, Summon Companion/Cape 6 rarity, Level Up "New Level x 100 pts", Mine 7 quặng,
// Use Dungeon Key) và đếm ngược "Ends in {0}".
public class ClanWarDayActionsSection : MonoBehaviour
{
    public ClanWarActionCard CardPrefab;
    public Transform ListContent;
    public TMP_Text ActionsTitleText;
    public TMP_Text ActionsTimeLeftText;

    private readonly List<ClanWarActionCard> cards = new List<ClanWarActionCard>();
    private int drawnDay = -1;

    // gbo gốc.
    public void Set(ClanWarWireDTO war, ClanWarConfigDTO config)
    {
        int day = Mathf.Clamp(war.activeDay, 1, 5);
        if (ActionsTitleText != null) ActionsTitleText.text = "Day " + day + " Actions";
        if (drawnDay == day && cards.Count > 0) return;
        drawnDay = day;
        Clear();
        if (CardPrefab != null) CardPrefab.gameObject.SetActive(false);

        List<string> sources = null;
        if (config?.sourcesByDay != null) config.sourcesByDay.TryGetValue(day.ToString(), out sources);
        if (sources == null || sources.Count == 0)
        {
            sources = new List<string>(day % 2 == 1 ? StaticClanData.ODD_DAY_SOURCES : StaticClanData.EVEN_DAY_SOURCES);
        }
        ClanWarAwardsDTO aw = config?.awards;
        for (int i = 0; i < sources.Count; i++)
        {
            switch (sources[i])
            {
                case StaticClanData.SOURCE_LOOT:
                    foreach (string r in StaticClanData.LOOT_RARITIES) Add("Loot " + r + " Equipment", Points(aw?.lootEquipment, r));
                    break;
                case StaticClanData.SOURCE_SUMMON_COMPANION:
                    foreach (string r in StaticClanData.SUMMON_RARITIES) Add("Summon " + r + " Companion", Points(aw?.summonCompanion, r));
                    break;
                case StaticClanData.SOURCE_LEVEL_UP:
                    Add("Level Up", "New Level x " + (aw != null && aw.levelUpMultiplier > 0 ? aw.levelUpMultiplier : 100) + " pts");
                    break;
                case StaticClanData.SOURCE_MINING:
                    foreach (string o in StaticClanData.ORES) Add("Mine " + o, Points(aw?.mining, o));
                    break;
                case StaticClanData.SOURCE_DUNGEON_KEY:
                    int key = aw != null && aw.dungeonKey > 0 ? aw.dungeonKey : StaticClanData.DUNGEON_KEY_FALLBACK_POINTS;
                    Add("Use Dungeon Key", ClanWarController.FormatNumber(key) + " pts");
                    break;
                case StaticClanData.SOURCE_SUMMON_CAPE:
                    foreach (string r in StaticClanData.SUMMON_RARITIES) Add("Summon " + r + " Cape", Points(aw?.summonCape, r));
                    break;
            }
        }
    }

    // gbp gốc.
    public void SetTimeLeft(long seconds)
    {
        if (ActionsTimeLeftText != null) ActionsTimeLeftText.text = "Ends in " + ClanWarController.FormatCountdown(seconds);
    }

    // gbq gốc: "{pts} pts" hoặc "-".
    private static string Points(Dictionary<string, int> table, string key)
    {
        if (table != null && table.TryGetValue(key.ToLowerInvariant(), out int p)) return ClanWarController.FormatNumber(p) + " pts";
        return "-";
    }

    // gbr gốc.
    private void Add(string label, string points)
    {
        if (CardPrefab == null) return;
        ClanWarActionCard card = Instantiate(CardPrefab, ListContent);
        card.gameObject.SetActive(true);
        card.Set(label, points);
        cards.Add(card);
    }

    private void Clear()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
    }
}

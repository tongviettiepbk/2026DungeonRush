using System.Collections.Generic;
using TMPro;
using UnityEngine;

// "Day 6 PvP" (ClanWarPvpSection gốc): vé "{n}/{max}", "{d}/{t} players are defeated", dòng giải thích reset vòng,
// danh sách đối thủ (chưa hạ trước, Power giảm dần); trống → "No Enemy Clan Members".
public class ClanWarPvpSection : MonoBehaviour
{
    public GameObject HeaderRoot;
    public GameObject ListRoot;
    public GameObject NoEnemiesRoot;
    public ClanWarEnemyCard EnemyCardPrefab;
    public Transform ListContent;
    public TMP_Text PvpTitleText;
    public TMP_Text PvpTimeLeftText;
    public TMP_Text PvpTicketText;
    public TMP_Text PvpDefeatedProgressText;
    public TMP_Text PvpHelperText;
    public TMP_Text NoEnemiesText;

    private readonly List<ClanWarEnemyCard> cards = new List<ClanWarEnemyCard>();
    private bool isStarting;

    private void Awake()
    {
        if (PvpTitleText != null) PvpTitleText.text = "Day 6 PvP";
        if (NoEnemiesText != null) NoEnemiesText.text = "No Enemy Clan Members";
        if (EnemyCardPrefab != null) EnemyCardPrefab.gameObject.SetActive(false);
    }

    // gdd gốc.
    public void Set(ClanWarWireDTO war, ClanWarConfigDTO config)
    {
        Clear();
        ClanWarPvpDTO pvp = war.pvp;
        bool has = pvp != null && pvp.targets != null && pvp.targets.Count > 0;
        if (HeaderRoot != null) HeaderRoot.SetActive(true);
        if (ListRoot != null) ListRoot.SetActive(has);
        if (NoEnemiesRoot != null) NoEnemiesRoot.SetActive(!has);
        if (pvp == null) return;

        if (PvpTicketText != null) PvpTicketText.text = "<sprite=0>" + pvp.myTickets + "/" + pvp.ticketsMax;
        if (PvpDefeatedProgressText != null) PvpDefeatedProgressText.text = pvp.defeatedCount + "/" + pvp.targetCount + " players are defeated";
        if (PvpHelperText != null)
        {
            int minMembers = config?.pvp != null ? config.pvp.minMembersForReset : 30;
            if (pvp.resetEnabled) PvpHelperText.text = "Tickets refresh when " + pvp.resetThreshold + " players are defeated";
            else if (pvp.resetCount >= pvp.maxResets && pvp.maxResets > 0) PvpHelperText.text = "Maximum round resets reached for this war";
            else PvpHelperText.text = "Tickets and defeated status do not refresh for this round because one clan started the round below " + minMembers + " members";
        }
        if (!has) return;

        List<ClanWarPvpTargetDTO> targets = new List<ClanWarPvpTargetDTO>(pvp.targets);
        targets.Sort((a, b) => a.defeated != b.defeated ? a.defeated.CompareTo(b.defeated) : b.power.CompareTo(a.power));
        int points = config?.pvp != null && config.pvp.winPoints > 0 ? config.pvp.winPoints : (config?.awards != null ? config.awards.pvpWin : 0);
        for (int i = 0; i < targets.Count; i++)
        {
            ClanWarPvpTargetDTO t = targets[i];
            ClanWarEnemyCard card = Instantiate(EnemyCardPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Set(t, pvp.canAttack, pvp.myTickets > 0, points, () => OnBattle(t, card), null);
            cards.Add(card);
        }
    }

    // gde gốc.
    public void SetTimeLeft(long seconds)
    {
        if (PvpTimeLeftText != null) PvpTimeLeftText.text = "Ends in " + ClanWarController.FormatCountdown(seconds);
    }

    // gdf gốc.
    private void OnBattle(ClanWarPvpTargetDTO target, ClanWarEnemyCard card)
    {
        if (isStarting) return;
        isStarting = true;
        card.SetBusy(true);
        ClanWarController.Instance.StartPvp(target.userId, (ok, error) =>
        {
            isStarting = false;
            if (card != null) card.SetBusy(false);
            if (!ok) ClanUIUtil.Toast(error);
        });
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

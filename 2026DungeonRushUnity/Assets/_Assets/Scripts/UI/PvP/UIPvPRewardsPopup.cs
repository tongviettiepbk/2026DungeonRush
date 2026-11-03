using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bảng thưởng mùa (SpecialPvPRewardsPopup gốc, "Season Rewards"): lật trái/phải theo league (bắt đầu ở league hiện tại),
// mỗi league 7 khung hạng với 4 món, lấy từ rewardTable server trả trong openPvP.
public class UIPvPRewardsPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtLeague;
    public Button btNext;
    public Button btPrevious;
    public Button btClose;
    public Transform listContent;
    public ElementPvPRewardBandUI elementPrefab;

    private readonly List<GameObject> rows = new List<GameObject>();
    private int leagueIndex = 1;

    protected override void Awake()
    {
        base.Awake();
        if (btNext != null) btNext.onClick.AddListener(() => Shift(1));
        if (btPrevious != null) btPrevious.onClick.AddListener(() => Shift(-1));
        if (btClose != null) btClose.onClick.AddListener(Close);
        if (elementPrefab != null) elementPrefab.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (txtTitle != null) txtTitle.text = Loc.L("Popup.PvP.Rewards.Title");
        leagueIndex = PvPController.Instance.LeagueIndex;
        Draw();
    }

    private void Shift(int step)
    {
        int count = GameData.staticData.pvp.leagues.Count;
        leagueIndex = Mathf.Clamp(leagueIndex + step, 1, count);
        Draw();
    }

    private void Draw()
    {
        StaticPvPData data = GameData.staticData.pvp;
        PvPLeagueDefinition league = data.GetLeagueByIndex(leagueIndex);
        if (txtLeague != null)
        {
            txtLeague.text = data.GetLeagueName(leagueIndex);
            txtLeague.color = league.leagueColor;
        }
        if (btPrevious != null) btPrevious.gameObject.SetActive(leagueIndex > 1);
        if (btNext != null) btNext.gameObject.SetActive(leagueIndex < data.leagues.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null) Destroy(rows[i]);
        }
        rows.Clear();

        List<PvPRewardBandDTO> bands = PvPController.Instance.GetRewardBands(leagueIndex);
        for (int i = 0; i < bands.Count; i++)
        {
            ElementPvPRewardBandUI row = Instantiate(elementPrefab, listContent);
            row.gameObject.SetActive(true);
            row.Setup(bands[i]);
            rows.Add(row.gameObject);
        }
    }

    // Mỗi thưởng = 1 dòng TMP "<sprite=0>{số}" dùng sprite asset của loại thưởng (Boss Rush + Season Complete dùng chung).
    public static void FillRewards(List<RewardEntry> rewards, Transform parent, TMP_Text prefab, RewardIconSet icons, List<GameObject> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null) Destroy(rows[i]);
        }
        rows.Clear();
        if (rewards == null || parent == null || prefab == null)
        {
            return;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            TMP_Text row = Instantiate(prefab, parent);
            row.gameObject.SetActive(true);
            TMP_SpriteAsset asset = icons != null ? icons.Get(rewards[i].Type) : null;
            if (asset != null)
            {
                row.spriteAsset = asset;
                row.text = "<sprite=0>" + rewards[i].Amount.ToLetter();
            }
            else
            {
                row.text = rewards[i].Amount.ToLetter() + " " + rewards[i].Type;
            }
            rows.Add(row.gameObject);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Lưới ô thưởng (ClanWarRewardsGridView gốc, 3 cột): sinh ClanWarRewardCell cho từng loại > 0.
public class ClanWarRewardsGridView : MonoBehaviour
{
    private const int COLUMNS = 3;

    public GridLayoutGroup Grid;
    public ClanWarRewardCell CellPrefab;
    public Sprite LootBoxIcon;
    public Sprite BonesIcon;
    public Sprite PickaxeIcon;
    public Sprite ExperienceIcon;
    public Sprite CloakCurrencyIcon;
    public Sprite GoldenPickaxeIcon;
    public Sprite DrillIcon;
    public Sprite VialIcon;

    private readonly List<ClanWarRewardCell> cells = new List<ClanWarRewardCell>();

    // gds gốc.
    public void SetRewards(ClanWarRewardsDTO r)
    {
        Clear();
        if (CellPrefab != null) CellPrefab.gameObject.SetActive(false);
        if (Grid != null)
        {
            Grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            Grid.constraintCount = COLUMNS;
        }
        if (r == null) return;
        Add(LootBoxIcon, r.lootBox);
        Add(BonesIcon, r.bones);
        Add(PickaxeIcon, r.pickaxe);
        Add(ExperienceIcon, r.experience);
        Add(CloakCurrencyIcon, r.cloakCurrency);
        Add(GoldenPickaxeIcon, r.goldenPickaxe);
        Add(DrillIcon, r.drill);
        Add(VialIcon, r.vial);
    }

    // gdt gốc.
    private void Add(Sprite icon, int amount)
    {
        if (amount <= 0 || CellPrefab == null) return;
        Transform parent = Grid != null ? Grid.transform : transform;
        ClanWarRewardCell cell = Instantiate(CellPrefab, parent);
        cell.gameObject.SetActive(true);
        cell.Set(icon, amount);
        cells.Add(cell);
    }

    private void Clear()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null) Destroy(cells[i].gameObject);
        }
        cells.Clear();
    }
}

using TMPro;
using UnityEngine;

// Bảng thưởng dạng hàng cố định (ClanWarRewardsView gốc): mỗi loại 1 hàng, số 0 thì ẩn hàng.
public class ClanWarRewardsView : MonoBehaviour
{
    public GameObject LootBoxRow;
    public TMP_Text LootBoxAmountText;
    public GameObject BonesRow;
    public TMP_Text BonesAmountText;
    public GameObject PickaxeRow;
    public TMP_Text PickaxeAmountText;
    public GameObject ExperienceRow;
    public TMP_Text ExperienceAmountText;
    public GameObject CloakCurrencyRow;
    public TMP_Text CloakCurrencyAmountText;
    public GameObject GoldenPickaxeRow;
    public TMP_Text GoldenPickaxeAmountText;
    public GameObject DrillRow;
    public TMP_Text DrillAmountText;
    public GameObject VialRow;
    public TMP_Text VialAmountText;

    // gev gốc.
    public void SetRewards(ClanWarRewardsDTO r)
    {
        r = r ?? new ClanWarRewardsDTO();
        Row(LootBoxRow, LootBoxAmountText, r.lootBox);
        Row(BonesRow, BonesAmountText, r.bones);
        Row(PickaxeRow, PickaxeAmountText, r.pickaxe);
        Row(ExperienceRow, ExperienceAmountText, r.experience);
        Row(CloakCurrencyRow, CloakCurrencyAmountText, r.cloakCurrency);
        Row(GoldenPickaxeRow, GoldenPickaxeAmountText, r.goldenPickaxe);
        Row(DrillRow, DrillAmountText, r.drill);
        Row(VialRow, VialAmountText, r.vial);
    }

    // gew gốc.
    private static void Row(GameObject row, TMP_Text text, int amount)
    {
        if (row != null) row.SetActive(amount > 0);
        if (text != null) text.text = ClanWarController.FormatNumber(amount);
    }
}

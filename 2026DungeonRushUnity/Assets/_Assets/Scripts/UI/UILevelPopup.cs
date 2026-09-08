using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup "Rarity Table" — hiện khi thắng campaign làm playerLevel tăng.
//   - Vương miện "Level N" + thanh exp
//   - Bảng xác suất rarity: cột Lv N (hiện tại) vs Lv N+1 (kế) lấy từ StaticForgeData
//   - (TODO) phần thưởng mỗi lần lên level
// Prefab: Resources/Prefabs/UI/LevelPopup.prefab (controller gốc đã bị strip). Các field serialized
// dưới đây PHẢI gán trong Inspector — map theo node: Header.CurrentLevelText/NextLevelText,
// mỗi hàng rarity (Common..Divine) có CommonCurrentRate + CommonNextRate.
public class UILevelPopup : BaseUI
{
    // 1 hàng rarity trong bảng: 2 ô % (level hiện tại / level kế).
    [System.Serializable]
    public class RarityRow
    {
        public TMP_Text currentRate;   // node "CommonCurrentRate" của hàng
        public TMP_Text nextRate;      // node "CommonNextRate" của hàng
    }

    public TMP_Text titleText;         // "Level N" (vương miện)
    public TMP_Text currentLevelText;  // header cột trái "Lv N"
    public TMP_Text nextLevelText;     // header cột phải "Lv N+1"
    public Slider expSlider;           // thanh exp 0..1
    public TMP_Text expText;           // "curExp / xpRequired"
    public TMP_Text rewardText;        // số thưởng (gem...) — TODO nối data thưởng
    public Button closeButton;

    // 10 hàng theo THỨ TỰ Rarity 0..9 (Common..Divine). Ultimate(10) không có trong bảng forge.
    public List<RarityRow> rows = new List<RarityRow>();

    protected override void Awake()
    {
        base.Awake();
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    // level = playerLevel HIỆN TẠI (sau khi đã lên). So sánh Lv level vs Lv (level+1).
    // curExp/xpRequired = tiến trình exp trong level hiện tại (cho thanh exp).
    public void Show(int level, int curExp, int xpRequired)
    {
        if (titleText != null) titleText.text = "Level " + level;
        if (currentLevelText != null) currentLevelText.text = "Lv " + level;
        if (nextLevelText != null) nextLevelText.text = "Lv " + (level + 1);

        // Row bảng rarity = level - 1 (Level 1 -> dòng 0). Cột "kế" = level (clamp ở Max trong GetProbabilities).
        List<float> cur = GameData.staticData.forge.GetProbabilities(level - 1);
        List<float> next = GameData.staticData.forge.GetProbabilities(level);
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == null) continue;
            if (rows[i].currentRate != null)
                rows[i].currentRate.text = (i < cur.Count ? cur[i] : 0f).ToString("0.##") + "%";
            if (rows[i].nextRate != null)
                rows[i].nextRate.text = (i < next.Count ? next[i] : 0f).ToString("0.##") + "%";
        }

        if (expSlider != null)
            expSlider.value = xpRequired > 0 ? (float)curExp / xpRequired : 0f;
        if (expText != null)
            expText.text = curExp + " / " + xpRequired;

        gameObject.SetActive(true);
    }
}

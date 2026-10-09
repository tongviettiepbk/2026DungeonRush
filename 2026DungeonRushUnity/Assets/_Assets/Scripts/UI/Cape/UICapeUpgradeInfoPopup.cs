using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup tỉ lệ summon Cape theo level (CapeUpgradeInfoPopup gốc) — mở từ nút "i" của UICapePopup.
// "Level N" + trái/phải xem level khác (1..50); 6 dòng tỉ lệ Common..Mythic "{0:F1}%"; thanh tiến độ chỉ hiện ở level hiện tại.
public class UICapeUpgradeInfoPopup : BaseUI
{
    public List<TMP_Text> listTxtRate;   // index = Rarity 0..5
    public TMP_Text txtLevel;
    public Button btLeft;
    public Button btRight;
    public GameObject objProgress;
    public TMP_Text txtProgress;
    public RectTransform rectFill;
    public Button btClose;

    private int viewLevel;
    private int currentLevel;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btLeft.onClick.AddListener(() => SetViewLevel(viewLevel - 1));
        btRight.onClick.AddListener(() => SetViewLevel(viewLevel + 1));
    }

    // eip/evb
    public void Show()
    {
        int total = GameData.userData.capes.totalSummons;
        currentLevel = CapeSummonConfig.GetLevel(total);

        CapeSummonConfig.GetProgress(total, out int current, out int required, out bool isMax);
        if (isMax)
        {
            txtProgress.text = "Max Level";
            rectFill.localScale = Vector3.one;
        }
        else
        {
            txtProgress.text = current + "/" + required;
            rectFill.localScale = new Vector3(required > 0 ? Mathf.Clamp01((float)current / required) : 0f, 1f, 1f);
        }

        SetViewLevel(currentLevel);
        gameObject.SetActive(true);
    }

    // evc
    private void SetViewLevel(int level)
    {
        viewLevel = Mathf.Clamp(level, 1, CapeSummonConfig.MaxLevel);
        txtLevel.text = "Level " + viewLevel;
        btLeft.gameObject.SetActive(viewLevel > 1);
        btRight.gameObject.SetActive(viewLevel < CapeSummonConfig.MaxLevel);
        objProgress.SetActive(viewLevel == currentLevel);

        float[] rates = CapeSummonConfig.GetRates(viewLevel);
        for (int i = 0; i < listTxtRate.Count; i++)
        {
            listTxtRate[i].text = (i < rates.Length ? rates[i] : 0f).ToString("F1") + "%";
        }
    }
}

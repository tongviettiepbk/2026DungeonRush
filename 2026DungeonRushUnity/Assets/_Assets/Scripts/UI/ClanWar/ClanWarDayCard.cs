using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ngày trong "War Results" (ClanWarDayCard gốc): "Day {n}", War Score, clan thắng + MVP, màu Win/Lose/Not Concluded.
public class ClanWarDayCard : MonoBehaviour
{
    public TMP_Text WarScoreValueText;
    public TMP_Text WinnerNameText;
    public TMP_Text PlayerNameText;
    public Button PlayerButton;
    public TMP_Text DayLabelText;
    public TMP_Text StateLabelText;
    public Graphic StatusColorTarget;
    public Color WinColor = new Color(0.298f, 0.686f, 0.314f);
    public Color LoseColor = new Color(1f, 0.267f, 0.267f);
    public Color NotConcludedColor = Color.gray;
    // Reskin Đợt 8: đổi sprite thẻ theo kết quả (mình thắng xanh / địch thắng xám / chưa chốt tối), không tô màu đè.
    public Image StatusImage;
    public Sprite WinSprite;          // Clan/card_blue
    public Sprite LoseSprite;         // Clan/card_gray
    public Sprite NotConcludedSprite; // Clan/card_dark

    private Action onPlayer;

    private void Awake()
    {
        if (PlayerButton != null) PlayerButton.onClick.AddListener(() => onPlayer?.Invoke());
    }

    // gbt gốc: (day, warScore mặc định của ngày, kết quả hoặc null, onMvp).
    public void Set(int day, int warScore, ClanWarDailyResultDTO result, Action<ClanWarMvpDTO> onMvp)
    {
        if (DayLabelText != null) DayLabelText.text = Loc.L("Clan.War.Day", day);
        bool concluded = result != null;
        if (WarScoreValueText != null) WarScoreValueText.text = (concluded ? result.warScore : warScore).ToString();
        if (StateLabelText != null)
        {
            // Chưa chốt: "Not Concluded" nằm ở cột Winner (tên clan thắng ẩn).
            StateLabelText.gameObject.SetActive(!concluded);
            StateLabelText.text = Loc.L("Clan.War.DaysResults.NotConcluded");
        }
        if (WinnerNameText != null)
        {
            WinnerNameText.gameObject.SetActive(concluded);
            WinnerNameText.text = concluded ? (string.IsNullOrEmpty(result.winnerClanName) ? "-" : result.winnerClanName) : string.Empty;
        }
        ClanWarMvpDTO mvp = concluded ? result.winnerMvp : null;
        if (PlayerNameText != null) PlayerNameText.text = mvp != null ? mvp.playerName : string.Empty;
        if (PlayerButton != null) PlayerButton.gameObject.SetActive(mvp != null);
        onPlayer = mvp != null && onMvp != null ? () => onMvp(mvp) : (Action)null;
        if (StatusImage != null && WinSprite != null && LoseSprite != null && NotConcludedSprite != null)
            StatusImage.sprite = !concluded ? NotConcludedSprite : (result.myClanWon ? WinSprite : LoseSprite);
        else SetColor(!concluded ? NotConcludedColor : (result.myClanWon ? WinColor : LoseColor));
    }

    // gbu gốc.
    private void SetColor(Color c)
    {
        if (StatusColorTarget != null) StatusColorTarget.color = c;
    }
}

using System.Collections;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thanh exp đáy lobby (UIMainGame2/.../BottomUI/ExperienceUI) — port ExperienceUI gốc (reverse il2cpp kfe..kgb).
//  - Slider tween tới % exp (SliderAnimationDuration, SliderAnimationEase); lên level thì tween về 0.
//  - PercentageText "cur/need", vừa nhận exp thì thêm " <color=#00FF00>(+N)</color>" rồi ẩn sau EarnedExpHideDelay
//    giây; max level = "MAX".
//  - LevelText "Lvl N". Đầy thanh (chờ lên level) → nút Level màu ButtonCompletedColor + chấm thông báo;
//    bình thường màu ButtonActiveColor.
//  - BoostIndicatorText "x2" khi đang có boost exp (lifetime/quảng cáo/X2/X5).
//  - Bấm nút Level (cả ButtonInvisible phủ thanh) → âm click + mở LevelPopup.
//  - Tay chỉ LevelTutorialUI (kfl gốc): hiện khi thanh exp đầy mà chưa lên level lần nào (FTUE step 2).
public class ExperienceUI : MonoBehaviour
{
    public Slider Slider;
    public TextMeshProUGUI PercentageText;
    public Button LevelButton;
    public Button InvisibleButton;
    public TutorialUI LevelTutorialUI;
    public TextMeshProUGUI LevelText;
    public GameObject NotificationUI;
    public TextMeshProUGUI BoostIndicatorText;

    // Giá trị serialized của prefab gốc.
    public Color ButtonCompletedColor = new Color(0.0784f, 0.6235f, 0.9922f, 1f);
    public Color ButtonActiveColor = new Color(0.298f, 0.298f, 0.298f, 1f);
    public float SliderAnimationDuration = 0.2f;
    public Ease SliderAnimationEase = Ease.OutQuad;
    public float EarnedExpHideDelay = 3f;

    private Tween sliderTween;
    private int earnedExp;          // yrx: số "(+N)" đang hiện
    private Coroutine hideEarnedRoutine;
    private float lastBoostMultiplier = -1f;
    private bool isInitialized;

    private void Awake()
    {
        if (LevelButton != null) LevelButton.onClick.AddListener(OnLevelButtonClicked);
        if (InvisibleButton != null) InvisibleButton.onClick.AddListener(OnLevelButtonClicked);
    }

    private void Start()
    {
        PlayerExpService.ExperienceChanged += OnExperienceChanged;
        PlayerExpService.LevelChanged += OnLevelChanged;
        PlayerExpService.LevelUpReady += OnLevelUpReady;
        PlayerExpService.LevelUpCompleted += OnLevelUpCompleted;
        PlayerExpService.ExperienceEarned += OnExperienceEarned;
        TutorialService.StepCompleted += OnTutorialStepCompleted;
        isInitialized = true;
        Refresh();
    }

    private void OnDestroy()
    {
        PlayerExpService.ExperienceChanged -= OnExperienceChanged;
        PlayerExpService.LevelChanged -= OnLevelChanged;
        PlayerExpService.LevelUpReady -= OnLevelUpReady;
        PlayerExpService.LevelUpCompleted -= OnLevelUpCompleted;
        PlayerExpService.ExperienceEarned -= OnExperienceEarned;
        TutorialService.StepCompleted -= OnTutorialStepCompleted;
        sliderTween?.Kill();
    }

    // Update gốc: boost X2/X5 hết hạn theo thời gian → vẽ lại chỉ báo boost. Gộp luôn boost quảng cáo /
    // mastery / lifetime (gốc nghe BoostStateChanged, ExpBoostChanged, UpgradeLevelChanged(AdBoostWorth)).
    private void Update()
    {
        if (isInitialized && !Mathf.Approximately(ShopService.GetExpMultiplier(), lastBoostMultiplier))
            RefreshBoostIndicator();
    }

    // kft gốc: vẽ lại toàn bộ (vào game / sau khi data đổi ngoài luồng exp, vd đổi save).
    public void Refresh()
    {
        if (!isInitialized) return;
        RefreshLevelText();
        sliderTween?.Kill();
        if (Slider != null) Slider.value = PlayerExpService.GetProgress();
        RefreshPercentageText();
        RefreshLevelButton();
        RefreshBoostIndicator();
    }

    private void OnTutorialStepCompleted(FtueStep step)
    {
        RefreshTutorial();
    }

    // kfl gốc.
    private void RefreshTutorial()
    {
        if (LevelTutorialUI != null)
            LevelTutorialUI.SetVisible(PlayerExpService.IsLevelUpReady && TutorialService.IsCompleted(FtueStep.LevelUp) == false);
    }

    // kfh gốc (PlayerExperienceChanged).
    private void OnExperienceChanged()
    {
        if (PlayerExpService.IsMaxLevel) return;
        AnimateSlider(PlayerExpService.GetProgress());
        RefreshPercentageText();
    }

    // kfi gốc (PlayerLevelChanged).
    private void OnLevelChanged()
    {
        RefreshLevelText();
        AnimateSlider(0f);
        ClearEarnedExp();
        RefreshPercentageText();
    }

    // kfj gốc: đầy thanh.
    private void OnLevelUpReady()
    {
        SetLevelButtonState(true);
    }

    // kfk gốc: vừa lên level.
    private void OnLevelUpCompleted()
    {
        SetLevelButtonState(false);
        ClearEarnedExp();
        RefreshPercentageText();
    }

    // kfm gốc: hiện "(+N)" (cộng dồn nếu accumulate) rồi tự ẩn sau EarnedExpHideDelay.
    private void OnExperienceEarned(int amount, bool accumulate)
    {
        earnedExp = accumulate ? earnedExp + amount : amount;
        RefreshPercentageText();
        if (hideEarnedRoutine != null) StopCoroutine(hideEarnedRoutine);
        if (isActiveAndEnabled) hideEarnedRoutine = StartCoroutine(RoutineHideEarnedExp());
    }

    private IEnumerator RoutineHideEarnedExp()
    {
        yield return new WaitForSeconds(EarnedExpHideDelay);
        hideEarnedRoutine = null;
        ClearEarnedExp();
        RefreshPercentageText();
    }

    // kfq gốc.
    private void ClearEarnedExp()
    {
        earnedExp = 0;
        if (hideEarnedRoutine != null)
        {
            StopCoroutine(hideEarnedRoutine);
            hideEarnedRoutine = null;
        }
    }

    // OnLevelButtonClicked gốc → ExperienceController.hds (mở LevelPopup).
    public void OnLevelButtonClicked()
    {
        AudioManager.Instance.PlayButtonClick();
        if (LevelTutorialUI != null) LevelTutorialUI.Hide();
        PlayerExpService.OpenLevelPopup();
    }

    // kfn gốc.
    private void AnimateSlider(float target)
    {
        if (Slider == null) return;
        sliderTween?.Kill();
        sliderTween = DOTween.To(() => Slider.value, v => Slider.value = v, target, SliderAnimationDuration)
            .SetEase(SliderAnimationEase).SetTarget(this);
    }

    // kfr gốc: Common.Level.Abbrev "Lvl {0}".
    private void RefreshLevelText()
    {
        if (LevelText != null) LevelText.text = Loc.L("Common.Level.Abbrev", GameData.userData.player.playerLevel);
    }

    // kfo gốc.
    private void RefreshPercentageText()
    {
        if (PercentageText == null) return;
        if (PlayerExpService.IsMaxLevel)
        {
            PercentageText.text = Loc.L("Common.Max");
            return;
        }

        string text = FormatNumber(GameData.userData.player.playerExperience) + "/" + FormatNumber(PlayerExpService.GetXpRequired());
        if (earnedExp >= 1)
            text += " <color=#00FF00>(+" + FormatNumber(earnedExp) + ")</color>";
        PercentageText.text = text;
    }

    // kft/kfj/kfk gốc: màu nút Level + chấm thông báo; max level = màu thường, không chấm.
    private void RefreshLevelButton()
    {
        SetLevelButtonState(PlayerExpService.IsLevelUpReady);
    }

    private void SetLevelButtonState(bool isLevelUpReady)
    {
        if (LevelButton != null && LevelButton.image != null)
            LevelButton.image.color = isLevelUpReady ? ButtonCompletedColor : ButtonActiveColor;
        // kfs gốc: NotificationUI bật/tắt chấm.
        if (NotificationUI != null) NotificationUI.SetActive(isLevelUpReady);
        RefreshTutorial();
    }

    // kfy gốc: hiện "x{hệ số}" khi hệ số boost exp > 1.
    private void RefreshBoostIndicator()
    {
        float multiplier = ShopService.GetExpMultiplier();
        lastBoostMultiplier = multiplier;
        if (BoostIndicatorText == null) return;
        BoostIndicatorText.gameObject.SetActive(multiplier > 1f);
        if (multiplier > 1f) BoostIndicatorText.text = "x" + FormatMultiplier(multiplier);
    }

    // kfz gốc: số nguyên thì in số nguyên, lẻ thì "0.##".
    private static string FormatMultiplier(float value)
    {
        float rounded = Mathf.Round(value);
        if (Mathf.Approximately(value, rounded)) return ((int)rounded).ToString();
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    // rn.iqt(value, 2) gốc: < 1000 in nguyên, lớn hơn rút gọn K/M/B/T 2 chữ số lẻ.
    public static string FormatNumber(double value)
    {
        if (value < 1000) return value.ToString("N0", CultureInfo.InvariantCulture);
        string[] suffixes = { "K", "M", "B", "T" };
        int index = -1;
        while (value >= 1000 && index < suffixes.Length - 1)
        {
            value /= 1000;
            index++;
        }
        return value.ToString("F2", CultureInfo.InvariantCulture) + suffixes[index];
    }
}

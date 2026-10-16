using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup "Rarity Table" — port LevelPopup gốc (reverse il2cpp eip/kmq/kmr/kms/kmt/kmx/kmy/kmz/kna).
// Mở bằng nút Level ở thanh exp, hoặc tự bật khi thanh exp đầy (PlayerExpService).
//  - Tiêu đề "Level N", bảng % rarity cột "Lv N" vs "Lv N+1" (bảng forge, "{0:F2}%").
//  - Chưa đầy: thanh exp "cur/need". Đầy: ẩn thanh, hiện nút "Complete!" (không đóng được bằng bấm nền tối).
//  - Bấm Complete: âm levelup → +gem thưởng (bảng LevelPopup.ywc) → lên level (+exp dư để dành) → hàng rarity
//    nảy lần lượt từ dưới lên, nháy nền và đổi số sang level mới → gem bay về lobby.
//  - Max level: nút xám "MAXED" không bấm được, ẩn thanh exp.
// Prefab Resources/Prefabs/UI/UILevelPopup do LevelPopupPrefabBuilder dựng từ LevelPopup.prefab rip.
public class UILevelPopup : BaseUI
{
    public TMP_Text LevelText;
    public Button CompleteButton;
    public Image CompleteButtonImage;
    public TMP_Text CompleteButtonText;
    public TMP_Text CurrentLevelText;
    public TMP_Text NextLevelText;
    // 10 hàng theo thứ tự Rarity 0..9 (Common..Divine).
    public List<TMP_Text> CurrentLevelRarityProbabilityTexts = new List<TMP_Text>();
    public List<TMP_Text> NextLevelRarityProbabilityTexts = new List<TMP_Text>();
    public List<RectTransform> RarityRowElements = new List<RectTransform>();
    public List<Image> RarityRowBackgrounds = new List<Image>();
    public Slider ExperienceSlider;
    public TMP_Text PercentageText;
    public Button CloseButton;
    public Button DarkButton;
    public TMP_Text GemCountText;
    public AudioClip LevelUpSound;

    // Giá trị serialized của prefab gốc / GameResources.
    public Color DisabledButtonColor = new Color(0.6706f, 0.6706f, 0.6706f, 1f);
    public Color DefaultButtonColor = new Color(0.2078f, 0.6f, 0.8667f, 1f);
    public float RowScaleDuration = 0.2f;
    public float RowScaleTarget = 1.07f;
    public float RowOutshoot = 0.1f;
    public Ease RowScaleEase = Ease.OutBack;
    public float FlashDuration = 0.2f;
    public float RowStaggerDelay = 0.1f;

    private bool isAnimating;       // ywi
    private bool isCloseOnClickDark = true;
    private Sequence levelUpSequence;

    protected override void Awake()
    {
        base.Awake();
        CompleteButton.onClick.AddListener(OnCompleteClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        if (DarkButton != null) DarkButton.onClick.AddListener(OnDarkClicked);
    }

    // eip gốc.
    public void Show()
    {
        PlayerExpService.ExperienceChanged -= OnExperienceChanged;
        PlayerExpService.LevelUpReady -= OnExperienceChanged;
        PlayerExpService.ExperienceChanged += OnExperienceChanged;
        PlayerExpService.LevelUpReady += OnExperienceChanged;
        Refresh();
        SetGemReward(GameData.userData.player.playerLevel);
    }

    // ein gốc.
    public override void Close()
    {
        PlayerExpService.ExperienceChanged -= OnExperienceChanged;
        PlayerExpService.LevelUpReady -= OnExperienceChanged;
        levelUpSequence?.Kill();
        isAnimating = false;
        base.Close();
    }

    private void OnDestroy()
    {
        PlayerExpService.ExperienceChanged -= OnExperienceChanged;
        PlayerExpService.LevelUpReady -= OnExperienceChanged;
        levelUpSequence?.Kill();
    }

    // kmo / kmp gốc: đang chạy hiệu ứng lên level thì để hiệu ứng vẽ.
    private void OnExperienceChanged()
    {
        if (!isAnimating) Refresh();
    }

    // Popup.CloseOnClickDark gốc: đang chờ Complete thì bấm nền tối không đóng.
    private void OnDarkClicked()
    {
        if (isCloseOnClickDark) Close();
    }

    // kmq gốc.
    private void Refresh()
    {
        int level = GameData.userData.player.playerLevel;
        LevelText.text = "Level " + level;

        if (PlayerExpService.IsMaxLevel)
        {
            SetRarityTexts(level, level);
            CompleteButton.gameObject.SetActive(true);
            CompleteButton.interactable = false;
            CompleteButtonText.text = "MAXED";
            CompleteButtonImage.color = DisabledButtonColor;
            ExperienceSlider.gameObject.SetActive(false);
            return;
        }

        bool isReady = PlayerExpService.IsLevelUpReady;
        SetRarityTexts(level, level + 1);
        CompleteButton.gameObject.SetActive(isReady);
        ExperienceSlider.gameObject.SetActive(!isReady);
        isCloseOnClickDark = !isReady;
        if (isReady)
        {
            CompleteButtonText.text = "Complete!";
            CompleteButtonImage.color = DefaultButtonColor;
            CompleteButton.interactable = !isAnimating;
            return;
        }

        ExperienceSlider.value = PlayerExpService.GetProgress();
        PercentageText.text = ExperienceUI.FormatNumber(GameData.userData.player.playerExperience) + "/"
            + ExperienceUI.FormatNumber(PlayerExpService.GetXpRequired());
    }

    // kmr gốc: header "Lv N" + % từng rarity của 2 level.
    private void SetRarityTexts(int currentLevel, int nextLevel)
    {
        CurrentLevelText.text = "Lv " + currentLevel;
        NextLevelText.text = "Lv " + nextLevel;
        SetRates(CurrentLevelRarityProbabilityTexts, GetProbabilities(currentLevel));
        SetRates(NextLevelRarityProbabilityTexts, GetProbabilities(nextLevel));
    }

    private static void SetRates(List<TMP_Text> texts, List<float> rates)
    {
        for (int i = 0; i < texts.Count && i < rates.Count; i++)
        {
            SetRate(texts[i], rates[i]);
        }
    }

    private static void SetRate(TMP_Text text, float rate)
    {
        if (text != null) text.text = rate.ToString("F2") + "%";
    }

    // EconomyController.hcq(level) gốc: dòng bảng forge = level - 1.
    private static List<float> GetProbabilities(int level)
    {
        return GameData.staticData.forge.GetProbabilities(level - 1);
    }

    // kmx / kmy gốc: số gem nhận khi lên level kế.
    private void SetGemReward(int level)
    {
        if (GemCountText != null)
            GemCountText.text = "<sprite=0> " + GameData.staticData.experience.GetLevelUpGemReward(level + 1);
    }

    // kms gốc.
    private void OnCompleteClicked()
    {
        if (PlayerExpService.IsMaxLevel || isAnimating || !PlayerExpService.IsLevelUpReady) return;
        PlayLevelUp();
    }

    // kmt gốc.
    private void PlayLevelUp()
    {
        isAnimating = true;
        CompleteButton.gameObject.SetActive(false);
        isCloseOnClickDark = false;
        AudioManager.Instance.PlaySfx(LevelUpSound);

        // kmz: cộng gem trước khi lên level (bay về lobby lúc hiệu ứng xong).
        int gem = GameData.staticData.experience.GetLevelUpGemReward(GameData.userData.player.playerLevel + 1);
        GameData.userData.items.Receive(ItemType.GEM, gem);
        PlayerExpService.CompleteLevelUp();

        int newLevel = GameData.userData.player.playerLevel;
        int nextLevel = newLevel + (PlayerExpService.IsMaxLevel ? 0 : 1);
        LevelText.text = "Level " + newLevel;
        ExperienceSlider.value = 0f;
        List<float> currentRates = GetProbabilities(newLevel);
        List<float> nextRates = GetProbabilities(nextLevel);

        levelUpSequence?.Kill();
        levelUpSequence = DOTween.Sequence().SetTarget(this);
        int count = RarityRowElements.Count;
        float halfScale = RowScaleDuration * 0.5f;
        float halfFlash = FlashDuration * 0.5f;
        for (int i = 0; i < count; i++)
        {
            int index = i;
            RectTransform row = RarityRowElements[i];
            // Hàng dưới cùng (Divine) chạy trước, lên dần tới Common.
            float delay = RowStaggerDelay * (count - 1 - i);
            levelUpSequence.Insert(delay, row.DOScale(Vector3.one * RowScaleTarget, halfScale).SetEase(RowScaleEase, RowOutshoot));
            levelUpSequence.Insert(delay + halfScale, row.DOScale(Vector3.one, halfScale).SetEase(Ease.InBack, RowOutshoot));

            if (i < RarityRowBackgrounds.Count && RarityRowBackgrounds[i] != null)
            {
                Image background = RarityRowBackgrounds[i];
                Color color = background.color;
                color.a = 0f;
                background.color = color;
                levelUpSequence.Insert(delay, background.DOFade(1f, halfFlash).SetEase(Ease.Linear));
                levelUpSequence.Insert(delay + halfFlash, background.DOFade(0f, halfFlash).SetEase(Ease.Linear));
            }

            // us.kmk gốc: đỉnh nháy → đổi số hàng này sang level mới (hàng đầu tiên chạy thì đổi luôn header).
            levelUpSequence.InsertCallback(delay + halfFlash, () =>
            {
                if (index == count - 1)
                {
                    CurrentLevelText.text = "Lv " + newLevel;
                    NextLevelText.text = "Lv " + nextLevel;
                }
                if (index < CurrentLevelRarityProbabilityTexts.Count && index < currentRates.Count)
                    SetRate(CurrentLevelRarityProbabilityTexts[index], currentRates[index]);
                if (index < NextLevelRarityProbabilityTexts.Count && index < nextRates.Count)
                    SetRate(NextLevelRarityProbabilityTexts[index], nextRates[index]);
            });
        }

        // ur.kmj gốc: xong hiệu ứng → gem bay về lobby (kna), số gem cho level kế (kmy), vẽ lại.
        levelUpSequence.InsertCallback(RowScaleDuration + RowStaggerDelay * count, () =>
        {
            isAnimating = false;
            UIMainLobby lobby = GameController.Instance != null ? GameController.Instance.uiLobby : null;
            if (lobby != null)
            {
                // Tính năng mở theo level (pet, wing, boost...) + gem bay về icon gem lobby.
                lobby.Refresh();
                if (gem > 0 && GemCountText != null)
                    lobby.PlayGemFly(GemCountText.transform, gem);
            }
            SetGemReward(newLevel);
            Refresh();
        });
    }
}

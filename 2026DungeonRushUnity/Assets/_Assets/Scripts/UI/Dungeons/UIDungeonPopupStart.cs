using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup vào dungeon (DungeonPopup gốc): độ khó, thưởng màn hiện tại, số key, Sweep Last / Enter / Ads (+1 key).
public class UIDungeonPopupStart : BaseUI
{
    public Button btClose;
    public TMP_Text txtTitle;
    public TMP_Text txtDifficulty;
    public TMP_Text txtLvl;
    public TMP_Text txtQuantityReward;
    public Image imgIconReward;
    public Image imgIconRibbon;   // icon trên ruy băng tiêu đề (reskin): cùng icon thưởng của dungeon

    public TMP_Text txtQuantityKeyProcess;
    public Image imgIconKey;

    [Space(20)]
    public Button btSweepLast;
    public Button btEnter;
    public Button btEnterByAds;
    public TMP_Text txtQuantityProcessAds;

    [Space(20)]
    // Icon theo DungeonType (index 0 DragonBoss, 1 ZombieHorde, 2 Cultist).
    public Sprite[] rewardIcons;
    public Sprite[] keyIcons;

    public TutorialUI enterTutorial;   // DungeonPopup.EnterTutorialUI gốc

    private DungeonType type;
    private Action onChanged;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btSweepLast.onClick.AddListener(OnClickSweep);
        btEnter.onClick.AddListener(OnClickEnter);
        btEnterByAds.onClick.AddListener(OnClickAds);
    }

    // onChanged: báo tab Dungeon vẽ lại khi key đổi (sweep / ads).
    public void Show(DungeonType type, Action onChanged)
    {
        this.type = type;
        this.onChanged = onChanged;
        Refresh();
    }

    private void Refresh()
    {
        DungeonConfig data = GameData.staticData.dungeons.GetData(type);
        DungeonProgress progress = DungeonService.GetProgress(type);

        txtTitle.text = data.LocalizedName;
        txtLvl.text = StaticDungeonData.GetDifficultyText(progress.level);
        txtQuantityReward.text = GameData.staticData.dungeons.GetReward(type, progress.level).ToString();
        txtQuantityKeyProcess.text = progress.TotalKeys + "/" + StaticDungeonData.DAILY_KEYS;
        SetIcon(imgIconReward, rewardIcons);
        if (imgIconRibbon != null)
            SetIcon(imgIconRibbon, rewardIcons);
        SetIcon(imgIconKey, keyIcons);

        btSweepLast.interactable = DungeonService.CanSweep(type);

        // Enter và Ads chung 1 chỗ (DungeonPopup.kch): còn key → Enter; hết key + còn lượt ads → Ads;
        // hết cả hai → Enter bị khoá.
        bool hasKey = progress.TotalKeys > 0;
        bool showAd = !hasKey && DungeonService.CanWatchAd(type);
        btEnter.gameObject.SetActive(!showAd);
        btEnter.interactable = hasKey;
        btEnterByAds.gameObject.SetActive(showAd);
        txtQuantityProcessAds.text = (StaticDungeonData.MAX_AD_PER_DAY - progress.adWatchCount) + "/" + StaticDungeonData.MAX_AD_PER_DAY;
        if (enterTutorial != null)
            enterTutorial.SetVisible(GetEnterTutorialStep() != null);
    }

    // kcg / kck gốc: Dragon chưa vào lần nào → step 6; Zombie khi đã xong 6 → step 7.
    private FtueStep? GetEnterTutorialStep()
    {
        bool dragonDone = TutorialService.IsCompleted(FtueStep.EnterDragonDungeon);
        if (type == DungeonType.DragonBoss && dragonDone == false)
            return FtueStep.EnterDragonDungeon;
        if (type == DungeonType.ZombieHorde && dragonDone && TutorialService.IsCompleted(FtueStep.EnterZombieDungeon) == false)
            return FtueStep.EnterZombieDungeon;
        return null;
    }

    private void SetIcon(Image image, Sprite[] icons)
    {
        int index = (int)type;
        if (icons != null && index < icons.Length && icons[index] != null)
            image.sprite = icons[index];
    }

    private void OnClickSweep()
    {
        int amount = DungeonService.Sweep(type);
        if (amount <= 0)
            return;

        UIManager.Instance.ShowToastMessage("+" + amount, isLocalize: false);
        OnKeysChanged();
    }

    private void OnClickEnter()
    {
        // Đang trong 1 dungeon khác (lobby vẫn bấm được trong trận) → chặn (UI.Dungeon.InProgress gốc).
        if (GameController.Instance.mode != null && GameController.Instance.mode.type != ModeType.DefaultLevel)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("UI.Dungeon.InProgress"), isLocalize: false);
            return;
        }

        if (DungeonService.GetProgress(type).TotalKeys <= 0)
        {
            UIManager.Instance.ShowToastMessage("Hết key", isLocalize: false);
            return;
        }

        FtueStep? step = GetEnterTutorialStep();
        if (step != null)
            TutorialService.Complete(step.Value);

        // Vào trận: DungeonMode lo phần thắng (CompleteDungeon tiêu key) / thua / thoát (không tiêu key).
        Close();
        GameController.Instance.uiLobby.CloseAllTabs();
        GameController.Instance.ChangeMode(GameData.staticData.dungeons.GetData(type).modeType);
    }

    private void OnClickAds()
    {
        if (!DungeonService.CanWatchAd(type))
            return;

        MediationAds.Instance.ShowRewardedVideoAd("dungeon_key", (result) =>
        {
            if (result != ShowResultADS.Finished)
                return;

            DungeonService.OnAdWatched(type);
            OnKeysChanged();
        });
    }

    private void OnKeysChanged()
    {
        Refresh();
        GameController.Instance.uiLobby.UpdateLootTicketText();
        onChanged?.Invoke();
    }
}

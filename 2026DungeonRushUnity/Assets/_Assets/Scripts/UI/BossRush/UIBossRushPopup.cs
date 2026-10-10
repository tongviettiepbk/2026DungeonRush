using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sảnh nhóm Boss Rush (BossRushPopup gốc): tier, đếm ngược đợt, boss chung "Boss #{0}: {1}" + HP, danh sách tới 100 người
// theo damage (dòng mình highlight), nút Fight "{0}/{1}\nFight", bảng thưởng. Tự làm mới định kỳ từ server.
public class UIBossRushPopup : BaseUI
{
    private const float POLL_INTERVAL = 15f;

    public TMP_Text txtTier;
    public TMP_Text txtTimer;
    public TMP_Text txtStatus;
    public TMP_Text txtBossTitle;
    public Slider sliderBossHp;
    public TMP_Text txtBossHp;
    public Transform playerListContent;
    public ElementBossRushPlayerUI playerElementPrefab;
    public Button btFight;
    public TMP_Text txtFightButton;
    public Button btRewards;
    public Button btClose;
    public UIBossRushRewardsPanel rewardsPanel;
    public GameObject objContent;
    public GameObject objLoading;

    private readonly List<ElementBossRushPlayerUI> elements = new List<ElementBossRushPlayerUI>();
    private float tick;
    private float pollTimer;
    private bool isRequesting;

    private static BossRushController Controller => BossRushController.Instance;

    protected override void Awake()
    {
        base.Awake();
        btFight.onClick.AddListener(OnClickFight);
        if (btRewards != null) btRewards.onClick.AddListener(OnClickRewards);
        if (btClose != null) btClose.onClick.AddListener(Close);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        BossRushController.PoolUpdated += OnPoolUpdated;
        if (rewardsPanel != null) rewardsPanel.Hide();
        Draw();
        RequestPool();
    }

    private void OnDisable()
    {
        BossRushController.PoolUpdated -= OnPoolUpdated;
    }

    private void Update()
    {
        tick -= Time.unscaledDeltaTime;
        if (tick <= 0f)
        {
            tick = 1f;
            DrawTimers();
        }

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer <= 0f)
        {
            RequestPool();
        }
    }

    private void OnPoolUpdated(BossRushPoolModel pool)
    {
        Draw();
    }

    private void RequestPool()
    {
        pollTimer = POLL_INTERVAL;
        if (isRequesting) return;
        isRequesting = true;

        Controller.RefreshPool((ok, res) =>
        {
            isRequesting = false;
            if (this == null || !gameObject.activeInHierarchy) return;

            if (res != null && !string.IsNullOrEmpty(res.unclaimedPoolId))
            {
                Close();
                UIBossRushClaimPopup.Open(res.unclaimedPoolId);
                return;
            }
            if (!ok && Controller.Pool == null)
            {
                UIManager.Instance.ShowToastMessage("Couldn't load Boss Rush. Please try again.", isLocalize: false);
                Close();
                return;
            }
            Draw();
        });
    }

    // ===== Vẽ =====

    private void Draw()
    {
        BossRushPoolModel pool = Controller.Pool;
        bool hasPool = pool != null;
        if (objLoading != null) objLoading.SetActive(!hasPool);
        if (objContent != null) objContent.SetActive(hasPool);
        if (!hasPool) return;

        StaticBossRushData data = GameData.staticData.bossRush;
        txtTier.text = data.GetTierName(pool.Tier);

        string bossName = data.GetBoss(pool.CurrentBossNumber).bossName;
        txtBossTitle.text = "Boss #" + pool.CurrentBossNumber + ": " + bossName;
        sliderBossHp.value = pool.MaxBossHP > 0d ? (float)(pool.CurrentBossHP / pool.MaxBossHP) : 0f;
        txtBossHp.text = pool.CurrentBossHP.ToLetter() + " / " + pool.MaxBossHP.ToLetter();

        DrawPlayers(pool.Players);
        DrawTimers();
    }

    private void DrawPlayers(List<BossRushPlayerModel> players)
    {
        string myId = FirebaseManager.Instance.Uid;
        for (int i = 0; i < players.Count; i++)
        {
            if (i >= elements.Count)
            {
                ElementBossRushPlayerUI element = Instantiate(playerElementPrefab, playerListContent);
                elements.Add(element);
            }
            elements[i].gameObject.SetActive(true);
            elements[i].Show(players[i], players[i].UserId == myId, null);
        }
        for (int i = players.Count; i < elements.Count; i++)
        {
            elements[i].gameObject.SetActive(false);
        }
    }

    private void DrawTimers()
    {
        BossRushPoolModel pool = Controller.Pool;
        DateTime now = BossRushSchedule.UtcNow;
        bool ended = pool != null && pool.EventKey != BossRushSchedule.GetEventKey(now);

        if (txtTimer != null)
        {
            txtTimer.text = ended ? "Event Ended" : BossRushSchedule.FormatRemain(BossRushSchedule.GetEventEnd(now) - now);
        }

        BossRushTicketsDTO tickets = GameData.userData.bossRush.tickets;
        int remaining = tickets.freeRemaining + tickets.adRemaining;
        int daily = tickets.dailyFreeTickets > 0 ? tickets.dailyFreeTickets : GameData.staticData.events.GetData(EventModeType.BossRush).dailyFreeTickets;
        // "{0}/{1}\nFight": vé free còn lại / vé free mỗi ngày; hết free thì đánh bằng vé ads (xem ads trước).
        txtFightButton.text = tickets.freeRemaining > 0 || tickets.adRemaining <= 0
            ? tickets.freeRemaining + "/" + daily + "\nFight"
            : "Ad " + tickets.adRemaining + "/" + GameData.staticData.events.GetData(EventModeType.BossRush).maxAdTicketsPerDay + "\nFight";
        btFight.interactable = !ended && remaining > 0 && !Controller.IsBusy;

        if (txtStatus != null)
        {
            if (ended)
            {
                txtStatus.text = "Results are being prepared";
            }
            else if (remaining <= 0)
            {
                txtStatus.text = "No fights remaining today!\nNext in\n" + BossRushSchedule.FormatRemain(now.Date.AddDays(1) - now);
            }
            else
            {
                txtStatus.text = string.Empty;
            }
        }
    }

    // ===== Nút =====

    private void OnClickFight()
    {
        if (Controller.IsBusy) return;

        if (Controller.HasFreeFight)
        {
            StartFight();
            return;
        }

        if (GameData.userData.bossRush.tickets.adRemaining > 0)
        {
            // Hết vé free → xem ads (RewardedType.BossRushEntry gốc, placement boss_rush_ad) rồi dùng vé ads.
            MediationAds.Instance.ShowRewardedVideoAd("boss_rush_ad", result =>
            {
                if (result == ShowResultADS.Finished) StartFight();
            });
            return;
        }

        UIManager.Instance.ShowToastMessage("No fights remaining today!", isLocalize: false);
    }

    private void StartFight()
    {
        btFight.interactable = false;
        Controller.StartFight((ok, message) =>
        {
            if (ok)
            {
                Close();
                return;
            }
            UIManager.Instance.ShowToastMessage(message, isLocalize: false);
            DrawTimers();
        });
    }

    private void OnClickRewards()
    {
        BossRushPoolModel pool = Controller.Pool;
        if (rewardsPanel == null || pool == null) return;

        int position = 0;
        string myId = FirebaseManager.Instance.Uid;
        for (int i = 0; i < pool.Players.Count; i++)
        {
            if (pool.Players[i].UserId == myId) position = pool.Players[i].Position;
        }
        rewardsPanel.Show(position, Mathf.Max(0, pool.CurrentBossNumber - 1), Controller.RewardTable);
    }

    // Sau trận: chờ đổi mode xong (fade) rồi mở lại sảnh.
    public static void OpenAfterModeChange()
    {
        BossRushController.Instance.StartCoroutine(RoutineOpenAfterModeChange());
    }

    private static IEnumerator RoutineOpenAfterModeChange()
    {
        yield return null;
        while (GameController.Instance.isChangingMode)
        {
            yield return null;
        }
        UIManager.Instance.LoadUI(UIKey.BossRushPopup);
    }
}

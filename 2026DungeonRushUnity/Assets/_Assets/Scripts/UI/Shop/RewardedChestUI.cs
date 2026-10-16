using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Rương có cánh bay ngang màn lobby (RewardedChestUI gốc, reverse il2cpp ksi..ksq):
//  - Vào game 100s thì bay lần đầu; nhận / đóng popup / bay hết đường → 60s sau bay tiếp.
//  - Chỉ bay khi đang chơi campaign (gốc chặn lúc Dungeon / Boss Rush...), không thì thử lại sau 15s; trần 800 lượt/ngày.
//  - Đường bay: Start → biên phải (3s) → biên trái (8s) → biên phải (8s) → End (3s), Linear; cánh vỗ ±20° (0.12s),
//    rương nhún Y +30 (0.6s). Bấm rương → popup nhận Gem / hộp loot bằng quảng cáo.
public class RewardedChestUI : MonoBehaviour
{
    private const float WING_START_ANGLE = 10f;
    private const float WING_FLAP_ANGLE = 20f;
    private const float WING_FLAP_DURATION = 0.12f;
    private const float BOB_HEIGHT = 30f;
    private const float BOB_DURATION = 0.6f;
    private const float EDGE_DURATION = 3f;
    private const float CROSS_DURATION = 8f;

    public RectTransform WingLeftRectTransform;
    public RectTransform WingRightRectTransform;
    public RectTransform ChestParentRectTransform;
    public RectTransform ChestMoveYParentRectTransform;
    public RectTransform StartPosition;
    public RectTransform EndPosition;
    public RectTransform LeftBorderPosition;
    public RectTransform RightBorderPosition;
    public ParticleSystem particle;    // UIParticle gốc (project không có plugin) → ParticleSystem vẽ trên canvas
    public Button chestButton;

    private static long nextSpawnTime;  // zan gốc (static, không lưu save)
    private Sequence pathSequence;
    private RewardedChestType chestType;
    private bool isFlying;
    private bool isInitialized;
    private Vector3 moveYOrigin;

    private void Awake()
    {
        if (chestButton != null)
            chestButton.onClick.AddListener(OnChestClicked);
        if (ChestMoveYParentRectTransform != null)
            moveYOrigin = ChestMoveYParentRectTransform.localPosition;
        ChestParentRectTransform.gameObject.SetActive(false);
    }

    // ksi gốc.
    private void Start()
    {
        ShopService.CheckRewardedChestReset();
        nextSpawnTime = ShopService.Now() + ShopService.REWARDED_CHEST_FIRST_DELAY;
        isInitialized = true;
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    // Update gốc.
    private void Update()
    {
        if (isInitialized == false || isFlying)
            return;
        if (ShopService.Now() >= nextSpawnTime)
            TrySpawn();
    }

    // ksn gốc.
    private void TrySpawn()
    {
        if (ShopService.GetRewardedChestRemaining() <= 0)
            return;

        if (IsCampaignPlaying() == false)
        {
            nextSpawnTime = ShopService.Now() + ShopService.REWARDED_CHEST_RETRY_DELAY;
            return;
        }

        Spawn();
    }

    private static bool IsCampaignPlaying()
    {
        BaseMode mode = GameController.Instance != null ? GameController.Instance.mode : null;
        return mode != null && mode.type == ModeType.DefaultLevel && mode.isPause == false && mode.isEndMode == false;
    }

    // kso gốc.
    private void Spawn()
    {
        chestType = ShopService.RollRewardedChestType();
        isFlying = true;
        KillTweens();

        ChestParentRectTransform.gameObject.SetActive(true);
        ChestParentRectTransform.localScale = Vector3.one;
        ChestParentRectTransform.localPosition = StartPosition.localPosition;
        if (particle != null)
        {
            // Như ForgeUIController.PlayParticle: canvas lobby Screen Space Camera → particle vẽ trên canvas.
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas = canvas.rootCanvas;
                foreach (ParticleSystemRenderer r in particle.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    r.sortingLayerID = canvas.sortingLayerID;
                    r.sortingOrder = canvas.sortingOrder + 1;
                }
            }
            particle.Clear(true);
            particle.Play(true);
        }

        WingLeftRectTransform.localRotation = Quaternion.Euler(0f, 0f, -WING_START_ANGLE);
        WingRightRectTransform.localRotation = Quaternion.Euler(0f, 0f, WING_START_ANGLE);
        ChestMoveYParentRectTransform.localPosition = moveYOrigin;
        WingLeftRectTransform.DOLocalRotate(new Vector3(0f, 0f, WING_FLAP_ANGLE), WING_FLAP_DURATION).SetLoops(-1, LoopType.Yoyo);
        WingRightRectTransform.DOLocalRotate(new Vector3(0f, 0f, -WING_FLAP_ANGLE), WING_FLAP_DURATION).SetLoops(-1, LoopType.Yoyo);
        ChestMoveYParentRectTransform.DOLocalMoveY(moveYOrigin.y + BOB_HEIGHT, BOB_DURATION).SetLoops(-1, LoopType.Yoyo);

        pathSequence = DOTween.Sequence();
        pathSequence.Insert(0f, ChestParentRectTransform.DOLocalMove(RightBorderPosition.localPosition, EDGE_DURATION).SetEase(Ease.Linear));
        pathSequence.Append(ChestParentRectTransform.DOLocalMove(LeftBorderPosition.localPosition, CROSS_DURATION).SetEase(Ease.Linear));
        pathSequence.Append(ChestParentRectTransform.DOLocalMove(RightBorderPosition.localPosition, CROSS_DURATION).SetEase(Ease.Linear));
        pathSequence.Append(ChestParentRectTransform.DOLocalMove(EndPosition.localPosition, EDGE_DURATION).SetEase(Ease.Linear));
        pathSequence.OnComplete(Hide);
    }

    // OnChestClicked gốc: dừng bay, mở popup; đóng popup (nhận hay không) → ẩn rương, hẹn lượt sau.
    private void OnChestClicked()
    {
        if (isFlying == false)
            return;

        pathSequence?.Pause();
        UIRewardedChestPopup.Show(chestType, ShopService.GetRewardedChestAmount(chestType), Hide);
    }

    // ksp gốc.
    private void Hide()
    {
        isFlying = false;
        KillTweens();
        if (particle != null)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        ChestParentRectTransform.gameObject.SetActive(false);
        nextSpawnTime = ShopService.Now() + ShopService.REWARDED_CHEST_AFTER_DELAY;
    }

    private void KillTweens()
    {
        pathSequence?.Kill();
        pathSequence = null;
        if (WingLeftRectTransform != null) WingLeftRectTransform.DOKill();
        if (WingRightRectTransform != null) WingRightRectTransform.DOKill();
        if (ChestMoveYParentRectTransform != null) ChestMoveYParentRectTransform.DOKill();
        if (ChestParentRectTransform != null) ChestParentRectTransform.DOKill();
    }
}

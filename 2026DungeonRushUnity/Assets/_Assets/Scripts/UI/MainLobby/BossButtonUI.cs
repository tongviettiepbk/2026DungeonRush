using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Nút Boss ở TopBarUI (BossButtonUI gốc, reverse il2cpp v41 — xem DecodedData/BOSS_BUTTON_MODEL.md).
// Thuộc tính năng Boss Gate gốc: cứ level chia hết 10 là cổng boss; thua thì bị lùi level và nút này hiện để đánh lại boss.
//  - Show: hiện bằng CanvasGroup (alpha 1, bấm được), nút + vầng sáng nhún scale vô hạn (InOutSine, Yoyo); tay chỉ step 22.
//  - Hide (jwr/jws): alpha 0, khoá bấm, trả scale 1, kill tween, ẩn tay chỉ. Awake luôn ẩn trước.
//  - Bấm (jwu): lần đầu đánh dấu FtueStep.BossButton (22) + ẩn tay chỉ, rồi BossGateController.eju (đánh lại cổng boss).
// Node luôn bật, ẩn/hiện chỉ qua CanvasGroup như gốc.
public class BossButtonUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _button;
    [SerializeField] private RectTransform _buttonRect;
    [SerializeField] private RectTransform _glowRect;
    [SerializeField] private TutorialUI _tutorialUI;
    // Giá trị serialized gốc (GameplayScene).
    [SerializeField] private float _buttonPulseScale = 1.1f;
    [SerializeField] private float _buttonPulseDuration = 0.5f;
    [SerializeField] private float _glowPulseScale = 2.1581f;
    [SerializeField] private float _glowPulseDuration = 1.5f;

    private Tween buttonTween;   // ylj gốc
    private Tween glowTween;     // ylk gốc
    private bool isShowing;      // yll gốc

    // fc.ejy gốc: Boss Gate bật (remote "boss_gate_enabled") && LastFailedBossGateLevel > 0
    // && LastFailedBossGateLevel > HighestBossGateCleared && đang ở campaign thường && Level < LastFailedBossGateLevel.
    // Live v41 boss_gate_enabled = false và project CHƯA dựng Boss Gate → luôn false (nút không bao giờ hiện, như live).
    public static bool CanRetryBossGate()
    {
        return false;
    }

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(OnClickBoss);
        HideImmediate();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void Show()
    {
        if (isShowing)
            return;

        isShowing = true;
        KillTweens();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        if (_buttonRect != null)
        {
            _buttonRect.localScale = Vector3.one;
            buttonTween = _buttonRect.DOScale(_buttonPulseScale, _buttonPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        if (_glowRect != null)
        {
            _glowRect.localScale = Vector3.one;
            glowTween = _glowRect.DOScale(_glowPulseScale, _glowPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        RefreshTutorial();
    }

    // jwr gốc: chỉ ẩn khi đang hiện.
    public void Hide()
    {
        if (isShowing)
            HideImmediate();
    }

    // jws gốc.
    private void HideImmediate()
    {
        isShowing = false;
        KillTweens();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_buttonRect != null)
            _buttonRect.localScale = Vector3.one;
        if (_glowRect != null)
            _glowRect.localScale = Vector3.one;
        if (_tutorialUI != null)
            _tutorialUI.Hide();
    }

    // jwq gốc: chưa xong step 22 → hiện tay chỉ.
    private void RefreshTutorial()
    {
        if (_tutorialUI == null)
            return;

        if (TutorialService.IsCompleted(FtueStep.BossButton))
            _tutorialUI.Hide();
        else
            _tutorialUI.Show();
    }

    // jwu gốc.
    private void OnClickBoss()
    {
        // Gốc kèm analytics "boss_button_clicked" (co.csm) — project chưa có analytics tutorial.
        TutorialService.Complete(FtueStep.BossButton);
        if (_tutorialUI != null)
            _tutorialUI.Hide();

        // Gốc: GameController.BossGateController.eju() — dừng chuyển màn, ẩn nút, nhảy về level cổng boss đã thua.
        // Project chưa có Boss Gate.
        DebugCustom.Log("[BossButton] click — Boss Gate chưa dựng");
    }

    // jwt gốc.
    private void KillTweens()
    {
        buttonTween?.Kill();
        glowTween?.Kill();
        buttonTween = null;
        glowTween = null;
    }
}

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Khu Loot ở lobby (ForgeUI + ForgeController gốc). Xem DecodedData/FORGE_MODEL.md.
//  - Nút Loot: tiêu 1 hộp (LOOT_TICKET) → 1 món.
//  - Nút Auto: chưa chạy → mở UIAutoForgePopup; đang chạy → dừng. Mỗi lượt auto tiêu
//    ForgeService.GetSelectedHammerCount() hộp, món không đạt lọc tự bán lấy EXP, món đạt lọc hiện popup.
//    Hết hộp thì tự tắt.
//  - Món ra nằm trên bục (itemList) rồi xử lý lần lượt: slot trống → UINewItemPopup (mặc vào hero);
//    có đồ → UINewForgePopup (Equip: mặc món mới, 2 thẻ đổi chỗ; Sell: bán món còn lại lấy EXP).
//  - Anim/FX theo ForgeUI gốc (ForgeUI.idn/ied/iee/ids/idt/iei/iem): bấm Loot → hộp phồng (OutBack) → lắc
//    (PunchRotation) → hộp vỡ + particle hộp → món nảy ra trên bục + glow màu rarity; bán món → particle exp;
//    rảnh 3s thì vệt sáng chạy qua nút Loot. Nút giữ trạng thái lún suốt lượt loot / khi auto (ForgeUI.iep).
//  - Chạm nền popup khi chưa chọn (CloseOnClickDark gốc) = bỏ qua: món quay lại đầu bục, dừng xử lý (cả auto)
//    tới khi người chơi bấm món đó để mở lại popup (ForgeController.het / hen gốc).
//  - Rời màn chính (mở tab / Enchantment / Wing) khi đang auto → tự tắt auto; lượt đang dở vẫn tự bán theo lọc,
//    món cần chọn thì chờ, về màn chính mới hiện popup (user yêu cầu 2026-10-22, gốc không có).
//  - Tay chỉ tutorial (FTUE): nút Loot tới khi loot lần đầu (step 1); nút Auto khi đã loot mà chưa Start auto
//    (step 12) hoặc đã nâng Mastery mà chưa chọn số búa (step 20). Xem DecodedData/TUTORIAL_MODEL.md.
public class ForgeUIController : MonoBehaviour
{
    private enum State { Idle, PlayingAnimation, ShowingItems, ShowingPopup }

    private class PendingItem
    {
        public LootResult item;
        public ItemElementView view;
        public bool isDismissed;
    }

    public UIMainLobby owner;
    public Button btLoot;
    public Button btAuto;
    public GameObject objAutoButton;     // cả nút Auto — ẩn khi chưa tới level hiện
    public GameObject objAutoRunning;    // vòng xoay khi auto đang chạy
    public GameObject objAutoIdle;       // chữ "AUTO" khi auto đang tắt
    public Image imgAutoTop;             // reskin: nắp nút Auto — chưa tới level dùng được thì đổi sang ảnh khoá (xám)
    public Sprite sprAutoLocked;
    public Transform itemList;
    public ItemElementView itemPrefab;
    public float autoRotateSpeed = 360f;
    public float processDelay = 0.35f;   // món hiện trên bục rồi mới xử lý
    public float autoNextDelay = 0.5f;   // nghỉ giữa 2 lượt auto

    [Header("Tutorial")]
    public TutorialUI lootTutorial;      // ForgeUI.LootTutorialUI gốc
    public TutorialUI autoForgeTutorial; // AutoForgeButton.AutoForgeTutorialUI gốc

    [Header("Anim / FX (ForgeUI gốc)")]
    public ForgeButtonView forgeButton;
    public Image boxSprite;              // hộp loot hiện lên rồi vỡ
    public Image itemGlowImage;          // quầng sáng sau bục, màu rarity
    public Image movingImage;            // vệt sáng chạy qua nút Loot
    public ParticleSystem lootBoxParticle;
    public ParticleSystem coinParticle;  // mảnh exp bay ra khi bán
    public float movementDistance = 500f;
    public float movementDuration = 0.4f;
    public float waitDuration = 2f;
    public float glowFadeDuration = 0.2f;
    public float idleTimeBeforeAnimation = 3f;
    public float forgeScaleDuration = 0.15f;
    public float forgePrePunchInterval = 0.4f;
    public float forgePunchDuration = 0.4f;
    public float forgePostPunchInterval = 0.2f;
    public Vector3 forgePunchRotation = new Vector3(0f, 0f, 15f);
    public int forgePunchVibrato = 10;
    public float forgePunchElasticity = 1f;

    private const float BOX_START_SCALE = 0.8f;
    private const float ITEM_SCALE_DURATION = 0.05f;
    private const float GLOW_ALPHA = 0.24f;
    private const float COIN_PARTICLE_COOLDOWN = 0.1f;

#if UNITY_EDITOR
    [Header("DEBUG - chỉ dùng test")]
    // Nhập level để test bảng rarity. -1 = dùng giá trị thật (playerLevel).
    [SerializeField] private int debugForgeLevel = -1;
#endif

    private readonly List<PendingItem> queue = new List<PendingItem>();
    private State state = State.Idle;
    private bool isAuto;
    private bool isAutoBatch;      // lượt đang chạy bắt đầu lúc auto → vẫn lọc bán dù auto đã tắt giữa chừng
    private bool isHome = true;
    private bool isWaitingHome;    // món cần chọn đang chờ người chơi về màn chính

    private Sequence boxSequence;
    private Sequence shineSequence;
    private Tween glowTween;
    private Coroutine idleRoutine;
    private Vector2 movingImageStartPos;
    private float lastCoinTime = -1f;

    public bool IsAuto => isAuto;

    private void Awake()
    {
        if (movingImage != null)
            movingImageStartPos = movingImage.rectTransform.anchoredPosition;
    }

    private void Start()
    {
        // ForgeButton gốc bắt OnPointerUp thay cho Button.onClick.
        if (forgeButton != null)
            forgeButton.onReleased += OnClickLoot;
        else
            btLoot.onClick.AddListener(OnClickLoot);
        btAuto.onClick.AddListener(OnClickAuto);
        TutorialService.StepCompleted += OnTutorialStepCompleted;
        RefreshAutoVisual();
        RefreshButtonState();
    }

    private void OnEnable()
    {
        RefreshAutoVisual();
        RestartIdleTimer();
    }

    private void OnDisable()
    {
        StopShine();
    }

    private void OnDestroy()
    {
        TutorialService.StepCompleted -= OnTutorialStepCompleted;
        boxSequence?.Kill();
        shineSequence?.Kill();
        glowTween?.Kill();
    }

    private void Update()
    {
        if (isAuto && objAutoRunning != null)
            objAutoRunning.transform.Rotate(0f, 0f, -autoRotateSpeed * Time.deltaTime);
        RefreshHomeScreen();

#if UNITY_EDITOR
        // chỉnh nhanh debugForgeLevel khi đang chơi: mũi tên lên/xuống
        if (Input.GetKeyDown(KeyCode.UpArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {++debugForgeLevel}");
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {--debugForgeLevel}");
#endif
    }

    // Rời màn chính → tắt auto; quay về → hiện popup món đang chờ.
    private void RefreshHomeScreen()
    {
        bool home = owner == null || owner.IsHomeScreen();
        if (home == isHome)
            return;

        isHome = home;
        if (home == false)
        {
            if (isAuto)
                SetAuto(false);
            return;
        }

        if (isWaitingHome)
        {
            isWaitingHome = false;
            ProcessNext();
        }
    }

    // ----- Nút -----

    // ForgeUI.jwj gốc: bấm là kêu click trước mọi kiểm tra.
    private void OnClickLoot()
    {
        AudioManager.Instance.PlayButtonClick();
        if (state != State.Idle || isAuto)
            return;

        // ForgeUI.idz gốc: bấm Loot lần đầu là xong step 1 (kể cả khi hết hộp).
        if (TutorialService.Complete(FtueStep.Loot) && lootTutorial != null)
            lootTutorial.Hide();

        if (ForgeService.HammerCount < 1)
        {
            ShowNotEnough();
            ReleaseLootButton();
            return;
        }

        StartForge(1);
    }

    // ForgeUI.idz gốc: không loot được thì nhả nút + đếm lại thời gian chờ vệt sáng.
    private void ReleaseLootButton()
    {
        if (forgeButton != null)
            forgeButton.Release();
        RestartIdleTimer();
    }

    private void OnClickAuto()
    {
        AudioManager.Instance.PlayButtonClick();   // AutoForgeButton.OnPointerUp gốc
        if (GameData.userData.player.playerLevel < ForgeService.AUTO_USABLE_LEVEL)
        {
            UIManager.Instance.ShowToastMessage("Mở ở level " + ForgeService.AUTO_USABLE_LEVEL, isLocalize: false);
            return;
        }

        // AutoForgeButton.OnPointerUp gốc: bấm Auto lần đầu (đã mở khoá) → step 3, cất tay chỉ.
        TutorialService.Complete(FtueStep.AutoForgeClicked);
        if (autoForgeTutorial != null)
            autoForgeTutorial.Hide();

        if (isAuto)
        {
            SetAuto(false);
            return;
        }

        UIAutoForgePopup popup = UIManager.Instance.LoadUI(UIKey.AutoForgePopup) as UIAutoForgePopup;
        if (popup != null)
            popup.Show(StartAuto);
    }

    private void StartAuto()
    {
        if (ForgeService.HammerCount < 1)
        {
            ShowNotEnough();
            return;
        }

        SetAuto(true);
        if (state == State.Idle)
            StartForge(ForgeService.GetSelectedHammerCount());
    }

    private void SetAuto(bool value)
    {
        isAuto = value;
        RefreshAutoVisual();
        RefreshButtonState();
    }

    private void SetState(State value)
    {
        state = value;
        RefreshButtonState();
    }

    // ForgeUI.iep gốc: auto → nút lún + khoá; đang loot/còn món trên bục → khoá; rảnh → mở + nảy lên.
    private void RefreshButtonState()
    {
        if (forgeButton == null)
            return;

        if (isAuto)
        {
            StopShine();
            forgeButton.Interactable = false;
            forgeButton.Press();
            return;
        }

        if (state != State.Idle || queue.Count > 0)
        {
            forgeButton.Interactable = false;
            return;
        }

        if (forgeButton.Interactable == false)
        {
            forgeButton.Interactable = true;
            forgeButton.Release();
        }
    }

    // Gọi khi lobby refresh (đổi level): nút Auto hiện từ level 2.
    public void RefreshAutoVisual()
    {
        if (objAutoButton != null)
            objAutoButton.SetActive(GameData.userData != null && GameData.userData.player.playerLevel >= ForgeService.AUTO_VISIBLE_LEVEL);
        if (objAutoRunning != null)
            objAutoRunning.SetActive(isAuto);
        if (objAutoIdle != null)
            objAutoIdle.SetActive(isAuto == false);
        RefreshAutoLockVisual();
        RefreshTutorials();
    }

    private Sprite sprAutoNormal;

    // Reskin: Auto hiện từ AUTO_VISIBLE_LEVEL nhưng chỉ bấm được từ AUTO_USABLE_LEVEL → khoảng giữa hiện nắp xám.
    private void RefreshAutoLockVisual()
    {
        if (imgAutoTop == null || sprAutoLocked == null || GameData.userData == null)
            return;
        if (sprAutoNormal == null)
            sprAutoNormal = imgAutoTop.sprite;
        bool isLocked = GameData.userData.player.playerLevel < ForgeService.AUTO_USABLE_LEVEL;
        imgAutoTop.sprite = isLocked ? sprAutoLocked : sprAutoNormal;
    }

    private void OnTutorialStepCompleted(FtueStep step)
    {
        RefreshTutorials();
    }

    // ForgeUI.idl + AutoForgeButton.iay gốc.
    private void RefreshTutorials()
    {
        if (lootTutorial != null)
            lootTutorial.SetVisible(TutorialService.IsCompleted(FtueStep.Loot) == false);

        if (autoForgeTutorial != null)
        {
            bool wantStart = TutorialService.IsCompleted(FtueStep.Loot) && TutorialService.IsCompleted(FtueStep.StartAutoForge) == false;
            bool wantHammer = TutorialService.IsCompleted(FtueStep.UpgradeMastery) && TutorialService.IsCompleted(FtueStep.SelectHammerCount) == false;
            bool usable = GameData.userData.player.playerLevel >= ForgeService.AUTO_USABLE_LEVEL;
            autoForgeTutorial.SetVisible((wantStart || wantHammer) && usable && autoForgeTutorial.gameObject.activeInHierarchy);
        }
    }

    private static void ShowNotEnough()
    {
        UIManager.Instance.ShowNotice(
            content: "Không đủ hộp loot.",
            isLocalizeContent: false,
            popupType: PopupNoticeType.Yes,
            title: "LOOT",
            labelYes: "OK");
    }

    // ----- 1 lượt forge (hej + hek gốc) -----

    private void StartForge(int count)
    {
        count = Mathf.Min(count, ForgeService.HammerCount);
        if (count < 1)
        {
            SetAuto(false);
            return;
        }

        int forgeLevel = GameData.userData.player.playerLevel - 1;
#if UNITY_EDITOR
        if (debugForgeLevel >= 0)
            forgeLevel = debugForgeLevel;
#endif

        // Trừ hộp trước, mỗi hộp 1 món.
        isAutoBatch = isAuto;
        GameData.userData.items.Consume(ItemType.LOOT_TICKET, count);
        owner.UpdateLootTicketText();

        for (int i = 0; i < count; i++)
        {
            LootResult item = LootService.RollOne(forgeLevel);
            if (item == null)
                continue;   // LootService đã log lỗi.

            // Clan War: điểm "Loot {rarity} Equipment" (ClanWarController.fzk gốc).
            ClanWarController.Instance.RecordLoot(item.rarity);
            queue.Add(new PendingItem { item = item });
        }

        StopShine();
        SetState(State.PlayingAnimation);
        PlayBoxAnimation(OnBoxOpened);
    }

    // ForgeController.hek gốc (sau OnForgeAnimationCompleted): món nảy ra trên bục rồi mới xử lý.
    private void OnBoxOpened()
    {
        foreach (PendingItem pending in queue)
        {
            pending.view = SpawnView(pending.item);
            PendingItem item = pending;
            if (pending.view != null && pending.view.button != null)
                pending.view.button.onClick.AddListener(() => OnClickPodiumItem(item));
        }

        ShowItemList();
        SetState(State.ShowingItems);
        StartCoroutine(ProcessAfter(processDelay));
    }

    // ForgeUI.idn gốc: hộp phồng 0.8→1 (OutBack) → chờ → tiếng đập + lắc → chờ → tiếng mở, ẩn hộp, nổ particle.
    private void PlayBoxAnimation(System.Action onComplete)
    {
        boxSequence?.Kill();
        if (boxSprite == null)
        {
            onComplete();
            return;
        }

        RectTransform box = boxSprite.rectTransform;
        boxSequence = DOTween.Sequence();
        boxSequence.AppendCallback(() =>
        {
            box.localScale = Vector3.one * BOX_START_SCALE;
            box.localRotation = Quaternion.identity;
            boxSprite.gameObject.SetActive(true);
        });
        boxSequence.Append(box.DOScale(Vector3.one, forgeScaleDuration).SetEase(Ease.OutBack));
        boxSequence.AppendInterval(forgePrePunchInterval);
        boxSequence.AppendCallback(() => AudioManager.Instance.PlayBoxPunch());
        boxSequence.Append(box.DOPunchRotation(forgePunchRotation, forgePunchDuration, forgePunchVibrato, forgePunchElasticity));
        boxSequence.AppendInterval(forgePostPunchInterval);
        boxSequence.AppendCallback(() =>
        {
            AudioManager.Instance.PlayBoxOpen();
            boxSprite.gameObject.SetActive(false);
            PlayParticle(lootBoxParticle);
            onComplete();
        });
    }

    private ItemElementView SpawnView(LootResult item)
    {
        if (itemPrefab == null || itemList == null)
            return null;

        // ForgeUI.ied gốc: món nảy từ 0 → 1 (OutBack 0.05s).
        ItemElementView view = Instantiate(itemPrefab, itemList);
        view.SetItem(item);
        view.transform.localScale = Vector3.zero;
        view.transform.DOScale(Vector3.one, ITEM_SCALE_DURATION).SetEase(Ease.OutBack);
        return view;
    }

    // ForgeUI.ids/idv gốc: hiện bục + quầng sáng màu rarity món đang chờ, alpha 0 → 0.24.
    private void ShowItemList()
    {
        if (itemList != null)
            itemList.localScale = Vector3.one;
        if (itemGlowImage == null || queue.Count == 0)
            return;

        glowTween?.Kill();
        Color color = UITabWing.GetRarityColor(queue[0].item.rarity);
        color.a = 0f;
        itemGlowImage.color = color;
        itemGlowImage.gameObject.SetActive(true);
        glowTween = itemGlowImage.DOFade(GLOW_ALPHA, glowFadeDuration);
    }

    // ForgeUI.idt/idw gốc: hết món thì ẩn bục + quầng sáng.
    private void HideItemList()
    {
        glowTween?.Kill();
        if (itemGlowImage != null)
            itemGlowImage.gameObject.SetActive(false);
    }

    private IEnumerator ProcessAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessNext();
    }

    // hem gốc: lấy món đầu hàng chờ, auto bán hoặc hiện popup; hết món thì kết thúc lượt.
    private void ProcessNext()
    {
        while (queue.Count > 0)
        {
            PendingItem pending = queue[0];
            queue.RemoveAt(0);

            if (isAutoBatch && ForgeService.ShouldDiscard(pending.item))
            {
                Sell(pending);
                continue;
            }

            if (isHome == false)
            {
                queue.Insert(0, pending);
                isWaitingHome = true;
                return;
            }

            ShowPopup(pending);
            return;
        }

        OnBatchDone();
    }

    private void OnBatchDone()
    {
        HideItemList();
        SetState(State.Idle);
        RestartIdleTimer();
        GameData.Save();

        if (isAuto == false)
            return;

        if (ForgeService.HammerCount < 1)
        {
            SetAuto(false);
            return;
        }

        StartCoroutine(NextAutoAfter(autoNextDelay));
    }

    private IEnumerator NextAutoAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isAuto && state == State.Idle)
            StartForge(ForgeService.GetSelectedHammerCount());
    }

    // heq gốc: slot trống → UINewItemPopup (chỉ để mặc); có đồ → UINewForgePopup (mặc món mới hoặc bán).
    private void ShowPopup(PendingItem pending)
    {
        SetState(State.ShowingPopup);
        LootResult item = pending.item;
        GearSlotType slot = item.EquipSlot;
        LootResult equipped = LootService.BuildFromEquipId(slot, GameData.userData.equipment.GetEquipped(slot));

        if (equipped == null)
        {
            UINewItemPopup newItem = UIManager.Instance.LoadUI(UIKey.NewItemPopup) as UINewItemPopup;
            if (newItem == null)
            {
                OnItemEquipped(pending, item);
                return;
            }
            newItem.Show(item, () => OnItemEquipped(pending, item), () => OnItemDismissed(pending));
            return;
        }

        UINewForgePopup forge = UIManager.Instance.LoadUI(UIKey.NewForgePopup) as UINewForgePopup;
        if (forge == null)
        {
            // Không mở được popup thì coi như bán để vòng forge không kẹt.
            OnItemSold(pending, item);
            return;
        }
        forge.Show(equipped, item, Equip, sold => OnItemSold(pending, sold), () => OnItemDismissed(pending));
    }

    // het gốc: món bị bỏ qua quay lại đầu bục và chờ; lần đầu thì chỉ tay vào nó (FTUE step 21).
    private void OnItemDismissed(PendingItem pending)
    {
        pending.isDismissed = true;
        queue.Insert(0, pending);
        SetState(State.ShowingItems);
        if (pending.view != null && pending.view.dismissedTutorial != null && TutorialService.IsCompleted(FtueStep.ReopenDismissedItem) == false)
            pending.view.dismissedTutorial.Show();
    }

    // hen + ItemElementUI.ButtonClicked gốc: bấm món đang bị bỏ qua → mở lại popup của nó.
    private void OnClickPodiumItem(PendingItem pending)
    {
        if (state != State.ShowingItems || pending.isDismissed == false)
            return;

        TutorialService.Complete(FtueStep.ReopenDismissedItem);
        if (pending.view != null && pending.view.dismissedTutorial != null)
            pending.view.dismissedTutorial.Hide();
        pending.isDismissed = false;
        queue.Remove(pending);
        ShowPopup(pending);
    }

    private void OnItemEquipped(PendingItem pending, LootResult item)
    {
        Equip(item);
        RemoveView(pending);
        ProcessNext();
    }

    private void OnItemSold(PendingItem pending, LootResult item)
    {
        ForgeService.Sell(item);
        PlayCoinParticle();
        owner.RefreshExp();
        RemoveView(pending);
        ProcessNext();
    }

    private void Equip(LootResult item)
    {
        GameData.userData.equipment.Equip(item.EquipSlot, item.equipId, item.rarity, item.level, item.subStats);
        GameData.Save();
        this.PostEvent(EventID.EquipmentChanged, item.EquipSlot);
        owner.RefreshEquipment();
        RateUsService.OnItemEquipped(item.rarity);
    }

    // hep/hey gốc: bán lấy EXP.
    private void Sell(PendingItem pending)
    {
        ForgeService.Sell(pending.item);
        PlayCoinParticle();
        RemoveView(pending);
        owner.RefreshExp();
    }

    // ForgeUI.iee gốc: tách món khỏi bục (để bục không giật layout), thu nhỏ về 0 (InBack 0.05s) rồi huỷ.
    private void RemoveView(PendingItem pending)
    {
        if (pending.view == null)
            return;

        Transform view = pending.view.transform;
        if (itemList != null)
            view.SetParent(itemList.parent, true);
        view.DOScale(Vector3.zero, ITEM_SCALE_DURATION).SetEase(Ease.InBack)
            .OnComplete(() => Destroy(view.gameObject));
        pending.view = null;
    }

    // ----- FX phụ -----

    // ForgeUI.iei gốc: particle mảnh exp, chặn spam 0.1s.
    private void PlayCoinParticle()
    {
        if (Time.unscaledTime - lastCoinTime < COIN_PARTICLE_COOLDOWN)
            return;
        lastCoinTime = Time.unscaledTime;
        PlayParticle(coinParticle);
    }

    // Gốc vẽ particle qua UIParticle (plugin không có): canvas lobby là Screen Space Camera nên cho
    // ParticleSystem vẽ ngay trên canvas, sorting = canvas gốc + 1.
    private void PlayParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

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
        particle.Play(true);
    }

    // ForgeUI.ien/ieo/iem gốc: rảnh idleTimeBeforeAnimation giây thì vệt sáng chạy ngang nút Loot
    // (+movementDistance trong movementDuration, Linear) rồi nghỉ waitDuration, lặp vô hạn.
    private void RestartIdleTimer()
    {
        StopShine();
        if (movingImage == null || isActiveAndEnabled == false)
            return;
        idleRoutine = StartCoroutine(IdleThenShine());
    }

    private IEnumerator IdleThenShine()
    {
        yield return new WaitForSeconds(idleTimeBeforeAnimation);
        idleRoutine = null;
        if (state != State.Idle || isAuto)
            yield break;

        RectTransform rt = movingImage.rectTransform;
        shineSequence?.Kill();
        shineSequence = DOTween.Sequence();
        shineSequence.Append(rt.DOAnchorPosX(movingImageStartPos.x + movementDistance, movementDuration).SetEase(Ease.Linear));
        shineSequence.AppendInterval(waitDuration);
        shineSequence.AppendCallback(() => rt.anchoredPosition = movingImageStartPos);
        shineSequence.SetLoops(-1);
    }

    // ForgeUI.iea gốc.
    private void StopShine()
    {
        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }
        shineSequence?.Kill();
        shineSequence = null;
        if (movingImage != null)
            movingImage.rectTransform.anchoredPosition = movingImageStartPos;
    }
}

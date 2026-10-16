using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Popup Đào mỏ (MiningPopup gốc) — mở từ nút Mining ở trang Wing.
// Sinh MiningWorld ở toạ độ xa (1000, 1000), camera mỏ render vào RenderTexture hiện trên RawImage;
// bấm lên RawImage → đổi điểm chạm ra ô lưới (ViewportToWorldPoint) → MiningController.OnCellClicked.
// 3 nút dụng cụ: còn hàng → chọn dụng cụ (nút sáng màu Default, nút khác Passive); hết → xem ads nhận
// (PickaxeAdRewardCount/…; giới hạn Daily*AdLimit/ngày UTC; đã mua No Ads thì nhận luôn).
public class UIMiningPopup : BaseUI, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    // GameResources gốc.
    public static readonly Color DEFAULT_BUTTON_COLOR = new Color(0.2078431f, 0.6f, 0.8666667f, 1f);
    public static readonly Color PASSIVE_BUTTON_COLOR = new Color(0.1698113f, 0.1698113f, 0.1698113f, 1f);

    private const string MINING_LAYER = "Mining";
    private static readonly Vector3 WORLD_POSITION = new Vector3(1000f, 1000f, 0f);

    public RawImage rawMining;
    public GameObject prefabMiningWorld;

    [Header("Dụng cụ")]
    public Button btPickaxe;
    public Button btDrill;
    public Button btGoldenPickaxe;
    public Image imgPickaxeButton;
    public Image imgDrillButton;
    public Image imgGoldenPickaxeButton;
    public GameObject objPickaxeDefault;
    public GameObject objPickaxeAd;
    public GameObject objDrillDefault;
    public GameObject objDrillAd;
    public GameObject objGoldenPickaxeDefault;
    public GameObject objGoldenPickaxeAd;
    public TMP_Text txtPickaxeCount;
    public TMP_Text txtDrillCount;
    public TMP_Text txtGoldenPickaxeCount;
    public TMP_Text txtPickaxeAdCount;
    public TMP_Text txtDrillAdCount;
    public TMP_Text txtGoldenPickaxeAdCount;
    public TMP_Text txtPickaxeDailyLimit;
    public TMP_Text txtDrillDailyLimit;
    public TMP_Text txtGoldenPickaxeDailyLimit;

    [Header("Quặng")]
    public TMP_Text txtCoal;
    public TMP_Text txtDiamond;
    public TMP_Text txtEmerald;
    public TMP_Text txtGold;
    public TMP_Text txtIron;
    public TMP_Text txtRuby;
    public TMP_Text txtStone;

    [Header("Độ sâu / level")]
    public Slider sliderDepth;
    public TMP_Text txtDepthSlider;
    public TMP_Text txtPickaxeTimer;
    public TMP_Text txtDepth;
    public TMP_Text txtMineLevel;

    [Header("Nút khác")]
    public Button btRarity;
    public Button btClose;
    public Button btOffer;

    private const float TIMER_INTERVAL = 1f;

    private MiningController controller;
    private GameObject world;
    private Camera mainCamera;
    private int miningLayerMask;
    private RenderTexture renderTexture;
    private float timer;
    private MiningBlock pressedBlock;

    private MiningConfig Config => MiningService.Config;

    protected override void Awake()
    {
        base.Awake();
        btPickaxe.onClick.AddListener(() => OnClickTool(MiningToolType.Pickaxe));
        btDrill.onClick.AddListener(() => OnClickTool(MiningToolType.Drill));
        btGoldenPickaxe.onClick.AddListener(() => OnClickTool(MiningToolType.GoldenPickaxe));
        btClose.onClick.AddListener(Close);
        if (btRarity != null) btRarity.onClick.AddListener(OnClickRarity);
        if (btOffer != null) btOffer.onClick.AddListener(OnClickOffer);
    }

    // eip gốc: dựng world + camera + nối sự kiện.
    public void Open()
    {
        if (world != null)
        {
            CloseWorld();
        }

        world = Instantiate(prefabMiningWorld);
        world.transform.position = WORLD_POSITION;
        controller = world.GetComponent<MiningController>();

        int layer = LayerMask.NameToLayer(MINING_LAYER);
        if (layer >= 0)
        {
            miningLayerMask = 1 << layer;
            MiningGrid.SetLayerRecursive(world, layer);
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.cullingMask &= ~miningLayerMask;
            }
        }

        Camera cam = controller.miningCamera.miningCam;
        cam.targetTexture = GetRenderTexture();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 1f);
        if (layer >= 0)
        {
            cam.cullingMask = miningLayerMask;
        }
        rawMining.texture = renderTexture;

        controller.Init(Config);

        if (sliderDepth != null)
        {
            sliderDepth.interactable = false;
        }

        MiningController.OreMined += OnOreMined;
        MiningController.PickaxeCountChanged += OnPickaxeCountChanged;
        MiningController.ToolActionCompleted += OnToolActionCompleted;
        MiningController.DepthChanged += OnDepthChanged;
        PurchaseController.PurchaseSuccessful += OnPurchaseSuccessful;

        timer = 0f;
        RefreshAll();
        RefreshOfferButton();
    }

    // RenderTexture cùng tỉ lệ với RawImage (camera tự lấy aspect theo texture).
    private RenderTexture GetRenderTexture()
    {
        if (renderTexture != null)
        {
            return renderTexture;
        }

        Rect rect = rawMining.rectTransform.rect;
        int w = Mathf.Clamp(Mathf.RoundToInt(rect.width), 256, 2048);
        int h = Mathf.Clamp(Mathf.RoundToInt(rect.height), 256, 2048);
        renderTexture = new RenderTexture(w, h, 16);
        renderTexture.name = "MiningRenderTexture";
        return renderTexture;
    }

    public override void Close()
    {
        CloseWorld();
        GameData.Save(true);
        this.PostEvent(EventID.MiningChanged);
        base.Close();
    }

    // ein gốc: gỡ sự kiện, dừng controller, huỷ world, trả cullingMask camera chính.
    private void CloseWorld()
    {
        MiningController.OreMined -= OnOreMined;
        MiningController.PickaxeCountChanged -= OnPickaxeCountChanged;
        MiningController.ToolActionCompleted -= OnToolActionCompleted;
        MiningController.DepthChanged -= OnDepthChanged;
        PurchaseController.PurchaseSuccessful -= OnPurchaseSuccessful;

        if (controller != null)
        {
            controller.Shutdown();
            controller = null;
        }
        if (world != null)
        {
            Destroy(world);
            world = null;
        }
        if (mainCamera != null && miningLayerMask != 0)
        {
            mainCamera.cullingMask |= miningLayerMask;
        }
        pressedBlock = null;
    }

    private void OnDestroy()
    {
        CloseWorld();
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= TIMER_INTERVAL)
        {
            timer = 0f;
            RefreshTimer();
        }
    }

    // ----- Chạm lưới (OnPointerClick/Down/Up + jee) -----

    public void OnPointerClick(PointerEventData eventData)
    {
        if (TryGetCell(eventData, out Vector2Int cell))
        {
            controller.OnCellClicked(cell);
            RefreshAll();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (TryGetCell(eventData, out Vector2Int cell))
        {
            pressedBlock = controller.grid.GetBlock(cell);
            if (pressedBlock != null)
            {
                pressedBlock.ShowClickOverlay();
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pressedBlock != null)
        {
            pressedBlock.HideClickOverlay();
            pressedBlock = null;
        }
    }

    // jee: điểm chạm → toạ độ trong RawImage → viewport (0..1) → world mỏ → ô lưới.
    private bool TryGetCell(PointerEventData eventData, out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        if (controller == null)
        {
            return false;
        }

        RectTransform rt = rawMining.rectTransform;
        Camera eventCam = eventData.pressEventCamera;
        if (RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.position, eventCam) == false)
        {
            return false;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventCam, out Vector2 local);
        Rect rect = rt.rect;
        Vector3 viewport = new Vector3((local.x - rect.x) / rect.width, (local.y - rect.y) / rect.height, 0f);
        Vector3 worldPoint = controller.miningCamera.miningCam.ViewportToWorldPoint(viewport);
        cell = controller.grid.WorldToGrid(worldPoint);
        return true;
    }

    // ----- Dụng cụ (jef/jeq/jer) -----

    // jef: còn dụng cụ → chọn; hết → xem ads.
    private void OnClickTool(MiningToolType tool)
    {
        if (MiningService.GetToolCount(tool) > 0)
        {
            controller.SelectTool(tool);
            RefreshTools();
        }
        else
        {
            WatchAdForTool(tool);
        }
    }

    // jeq: còn lượt ads hôm nay mới xem. Đã mua No Ads → nhận luôn (dus gốc).
    private void WatchAdForTool(MiningToolType tool)
    {
        MiningService.CheckDailyAdReset();
        if (MiningService.GetAdRemaining(tool) < 1)
        {
            return;
        }

        if (GameData.userData.shop.isNoAdsBought)
        {
            OnAdRewarded(tool);
            return;
        }

        MediationAds.Instance.ShowRewardedVideoAd("mining_tool", (result) =>
        {
            if (result == ShowResultADS.Finished)
            {
                OnAdRewarded(tool);
            }
        });
    }

    // jer
    private void OnAdRewarded(MiningToolType tool)
    {
        MiningService.OnAdWatched(tool);
        if (controller != null)
        {
            controller.SelectTool(tool);
        }
        RefreshAll();
    }

    // ----- Hiển thị -----

    // jej
    private void RefreshAll()
    {
        if (controller == null)
        {
            return;
        }

        UserMiningData data = GameData.userData.mining;
        SetText(txtCoal, MiningService.GetOre(MineOreType.CoalMine).ToString());
        SetText(txtDiamond, MiningService.GetOre(MineOreType.DiamondMine).ToString());
        SetText(txtEmerald, MiningService.GetOre(MineOreType.EmeraldMine).ToString());
        SetText(txtGold, MiningService.GetOre(MineOreType.GoldMine).ToString());
        SetText(txtIron, MiningService.GetOre(MineOreType.IronMine).ToString());
        SetText(txtRuby, MiningService.GetOre(MineOreType.RubyMine).ToString());
        SetText(txtStone, MiningService.GetOre(MineOreType.Stone).ToString());
        SetText(txtDepth, string.Format("Depth: {0}", data.currentDepth));
        SetText(txtMineLevel, "Lvl " + data.mineLevel);
        RefreshDepthSlider();
        RefreshTools();
    }

    // jeg: 3 nút dụng cụ + số cuốc "có/trần" + đồng hồ.
    private void RefreshTools()
    {
        MiningToolType selected = controller.SelectedTool;
        RefreshToolButton(MiningToolType.Pickaxe, selected, imgPickaxeButton, objPickaxeDefault, objPickaxeAd,
            txtPickaxeCount, txtPickaxeAdCount, txtPickaxeDailyLimit);
        RefreshToolButton(MiningToolType.Drill, selected, imgDrillButton, objDrillDefault, objDrillAd,
            txtDrillCount, txtDrillAdCount, txtDrillDailyLimit);
        RefreshToolButton(MiningToolType.GoldenPickaxe, selected, imgGoldenPickaxeButton, objGoldenPickaxeDefault, objGoldenPickaxeAd,
            txtGoldenPickaxeCount, txtGoldenPickaxeAdCount, txtGoldenPickaxeDailyLimit);
        SetText(txtPickaxeCount, string.Format("{0}/{1}", MiningService.GetToolCount(MiningToolType.Pickaxe), MiningService.GetMaxPickaxe()));
        RefreshTimer();
    }

    // jeh: còn hàng → nhóm số lượng; hết → nhóm ads "x<thưởng>" + "<icon ads> còn/giới hạn".
    private void RefreshToolButton(MiningToolType tool, MiningToolType selected, Image img, GameObject objDefault, GameObject objAd,
        TMP_Text txtCount, TMP_Text txtAdCount, TMP_Text txtDailyLimit)
    {
        int count = MiningService.GetToolCount(tool);
        if (objDefault != null) objDefault.SetActive(count > 0);
        if (objAd != null) objAd.SetActive(count < 1);
        if (img != null)
        {
            img.color = count > 0 && tool == selected ? DEFAULT_BUTTON_COLOR : PASSIVE_BUTTON_COLOR;
        }

        if (count > 0)
        {
            if (tool != MiningToolType.Pickaxe)
            {
                SetText(txtCount, count.ToString());
            }
            return;
        }

        int limit = MiningService.GetAdLimit(tool);
        int remaining = MiningService.GetAdRemaining(tool);
        SetText(txtAdCount, remaining >= 1 ? "x" + MiningService.GetAdRewardCount(tool) : string.Empty);
        SetText(txtDailyLimit, string.Format("<sprite=0>{0}/{1}", remaining, limit));
    }

    // jek: hồi cuốc + "Next in: mm:ss".
    private void RefreshTimer()
    {
        if (MiningService.RegenPickaxes())
        {
            RefreshTools();
            return;
        }

        int seconds = MiningService.GetSecondsToNextPickaxe();
        SetText(txtPickaxeTimer, seconds > 0 ? "Next in: " + FormatTime(seconds) : string.Empty);
    }

    private static string FormatTime(int seconds)
    {
        int m = seconds / 60;
        int s = seconds % 60;
        return m > 0 ? string.Format("{0}m {1:00}s", m, s) : string.Format("{0}s", s);
    }

    // jel: thanh tiến độ level mỏ "độ sâu / mốc level kế".
    private void RefreshDepthSlider()
    {
        if (sliderDepth == null)
        {
            return;
        }

        if (MiningService.IsMaxMineLevel())
        {
            sliderDepth.value = 1f;
            SetText(txtDepthSlider, "Max");
            return;
        }

        UserMiningData data = GameData.userData.mining;
        int start = Config.GetDepthForLevel(data.mineLevel);
        int perLevel = Config.GetDepthPerLevel(data.mineLevel);
        float progress = perLevel >= 1 ? (float)(data.currentDepth - start) / perLevel : 0f;
        sliderDepth.value = Mathf.Max(progress, 0.01f);
        SetText(txtDepthSlider, string.Format("{0}/{1}", data.currentDepth, start + perLevel));
    }

    // jeo: nút offer chỉ hiện khi còn gói chưa mua.
    private void RefreshOfferButton()
    {
        if (btOffer != null)
        {
            btOffer.gameObject.SetActive(MiningService.GetNextOffer() != null);
        }
    }

    private static void SetText(TMP_Text txt, string value)
    {
        if (txt != null)
        {
            txt.text = value;
        }
    }

    // ----- Sự kiện controller -----

    private void OnOreMined(MineOreType ore)
    {
        RefreshAll();
    }

    private void OnPickaxeCountChanged(int count)
    {
        RefreshTools();
    }

    private void OnToolActionCompleted()
    {
        RefreshAll();
    }

    // jfc
    private void OnDepthChanged(int depth)
    {
        RefreshAll();
    }

    // jep: mua gói offer xong.
    private void OnPurchaseSuccessful(string productId)
    {
        if (MiningService.IsOfferProduct(productId))
        {
            RefreshOfferButton();
            RefreshAll();
        }
    }

    // ----- Nút -----

    // jem
    private void OnClickRarity()
    {
        UIManager.Instance.LoadUI(UIKey.MiningRarityPopup);
    }

    // jen
    private void OnClickOffer()
    {
        if (MiningService.GetNextOffer() == null)
        {
            return;
        }

        UIMiningOfferPopup popup = UIManager.Instance.LoadUI(UIKey.MiningOfferPopup) as UIMiningOfferPopup;
        if (popup != null)
        {
            popup.Setup();
        }
    }
}

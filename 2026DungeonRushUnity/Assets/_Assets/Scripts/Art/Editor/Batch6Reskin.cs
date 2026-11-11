using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 6 – PvP & Boss Rush theo concept tools/concept/batch6/concept.png (kit chung PopupStyle).
// GIỮ art dòng / thanh máu / đầu boss / khiên của mockup cũ (1_UIKit/PvP, 1_UIKit/BossRush); chỉ đồng bộ khung, X, nút, chữ.
// UIPlayerPopup: bố cục user chỉnh tay → chỉ X + tách cột chỉ số. KHÔNG gọi ProfilePrefabBuilder.
// Toạ độ = đơn vị canvas (concept × 2), đo từ MÉP TRÊN panel tới TÂM (PopupStyle.PlaceTop). Chạy lại ra cùng kết quả.
public static class Batch6Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const float W = PopupStyle.WIDTH_MEDIUM;                        // 816
    private const float INNER = W - 2f * PopupStyle.SIDE_INSET;             // 708
    private const float TOP = PopupStyle.CONTENT_TOP;                       // 176
    private const float BTN = PopupStyle.BUTTON_HEIGHT;                     // 150
    private const float BOTTOM = PopupStyle.BOTTOM_INSET;                   // 58

    [MenuItem("Tools/DungeonRush/Reskin/Batch 6 (PvP, Boss Rush)")]
    public static string ApplyAll()
    {
        ApplyPvPLobby();
        ApplyPvPFind();
        ApplyPvPEnd();
        ApplyPvPRewards();
        ApplyPvPSeasonClaim();
        ApplyBossRushJoin();
        ApplyBossRushLobby();
        ApplyBossRushEnd();
        ApplyBossRushClaim();
        ApplyPlayer();
        return "ok";
    }

    private delegate void Edit(GameObject root);

    private static void EditPrefab(string name, Edit edit)
    {
        string path = UI + name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("[Batch6Reskin] " + name + " done");
    }

    #region PvP
    private const float PVP_LOBBY_HEIGHT = 1686f;

    public static void ApplyPvPLobby()
    {
        EditPrefab("UIPvPPopup", root =>
        {
            UIPvPPopup ui = root.GetComponent<UIPvPPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", PVP_LOBBY_HEIGHT, true);
            PopupStyle.Title(ui.txtLeagueName, W);
            Close(ui.btClose);
            HeaderButton(ui.btReward);

            PopupStyle.PlaceTop(Find(panel, "TopImage"), 0f, 266f, 192f, 180f);
            PopupStyle.PlaceTop(ui.txtSeasonTimer.transform, 0f, 421f, 640f, 100f);
            PopupStyle.Plain(ui.txtSeasonTimer, PopupStyle.BROWN, 36f, 24f);
            ui.txtSeasonTimer.alignment = TextAlignmentOptions.Center;

            // Danh sách giữ art dòng; dòng của mình ghim đè đáy danh sách.
            Transform loaded = ui.objLoaded.transform;
            PopupStyle.Stretch(loaded);
            PopupStyle.PlaceTop(ui.leaderboardScroll.transform, 0f, 972f, 746f, 972f);
            PopupStyle.PlaceTop(ui.stickyRow.transform, 0f, 1397f, 746f, 122f);
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, 972f, 76f, 76f);

            PopupStyle.Button(ui.btBattle.transform, PopupStyle.ButtonColor.Orange, 320f);
            PopupStyle.PlaceTop(ui.btBattle.transform, 0f, PVP_LOBBY_HEIGHT - BOTTOM - BTN / 2f, 320f, BTN);
            Button(ui.btBattle);
            ui.sprBattle = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.sprBattleDisabled = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private const float PVP_FIND_HEIGHT = 1400f;

    public static void ApplyPvPFind()
    {
        EditPrefab("UIPvPFindOpponentPopup", root =>
        {
            UIPvPFindOpponentPopup ui = root.GetComponent<UIPvPFindOpponentPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", PVP_FIND_HEIGHT, true);
            PopupStyle.Title(ui.txtTitle, W);
            Close(ui.btClose);

            PopupStyle.PlaceTop(Find(panel, "TopImage"), 0f, 261f, 180f, 170f);
            PopupStyle.PlaceTop(ui.txtResetTime.transform, 0f, 389f, INNER, 50f);
            PopupStyle.Plain(ui.txtResetTime, PopupStyle.BROWN, 36f, 24f);
            ui.txtResetTime.alignment = TextAlignmentOptions.Center;
            Transform ticketBox = ui.txtTickets.transform.parent;
            PopupStyle.PlaceTop(ticketBox, 0f, 468f, 212f, 72f);
            PopupStyle.Stretch(ui.txtTickets.transform, 6f, 4f, 6f, 4f);
            PopupStyle.Outline(ui.txtTickets, 38f, 26f);
            ui.txtTickets.alignment = TextAlignmentOptions.Center;

            const float listCenter = 933f;
            PopupStyle.PlaceTop(ui.txtError.transform, 0f, listCenter, 700f, 130f);
            PopupStyle.Outline(ui.txtError, 44f, 28f);
            ui.txtError.color = PopupStyle.DOWN;
            ui.txtError.alignment = TextAlignmentOptions.Center;
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, listCenter, 76f, 76f);

            Transform loaded = ui.objLoaded.transform;
            PopupStyle.Stretch(loaded);
            Transform list = ui.listContent;
            PopupStyle.PlaceTop(list, 0f, listCenter, 746f, 818f);
            VerticalLayoutGroup layout = list.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;

            // Dòng đối thủ: "+N 🏆" trên nút Challenge cam.
            ElementPvPPlayerUI row = ui.elementPrefab;
            PopupStyle.Button(row.btBattle.transform, PopupStyle.ButtonColor.Orange, 212f, 88f, 26f);
            PopupStyle.Center(row.btBattle.transform, 249f, -22f, 212f, 88f);
            Button(row.btBattle);
            row.txtBattle.lineSpacing = -18f;
            PopupStyle.Center(row.txtWinTrophy.transform, 249f, 46f, 212f, 40f);
            PopupStyle.Outline(row.txtWinTrophy, 30f, 22f);
            row.txtWinTrophy.alignment = TextAlignmentOptions.Center;
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private const float PVP_END_HEIGHT = 500f;

    public static void ApplyPvPEnd()
    {
        EditPrefab("UIPvPEndPopup", root =>
        {
            UIPvPEndPopup ui = root.GetComponent<UIPvPEndPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", PVP_END_HEIGHT, true);
            PopupStyle.Title(ui.txtTitle, W);
            Close(ui.btClose);

            PopupStyle.Stretch(ui.objResult.transform);
            PopupStyle.PlaceTop(ui.txtTrophy.transform, 0f, 214f, 700f, 90f);
            PopupStyle.Outline(ui.txtTrophy, 60f, 36f);
            ui.txtTrophy.alignment = TextAlignmentOptions.Center;
            PopupStyle.PlaceTop(ui.txtError.transform, 0f, 214f, 700f, 100f);
            PopupStyle.Outline(ui.txtError, 40f, 26f);
            ui.txtError.color = PopupStyle.DOWN;
            ui.txtError.alignment = TextAlignmentOptions.Center;
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, 214f, 76f, 76f);

            const float buttonY = 359f;
            MainButton(ui.btClaim, PopupStyle.ButtonColor.Green, buttonY);
            MainButton(ui.btRetry, PopupStyle.ButtonColor.Orange, buttonY);
            ui.sprContinueWin = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprContinueLose = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private const float PVP_REWARDS_HEIGHT = 1098f;

    public static void ApplyPvPRewards()
    {
        EditPrefab("UIPvPRewardsPopup", root =>
        {
            UIPvPRewardsPopup ui = root.GetComponent<UIPvPRewardsPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "NewItem", PVP_REWARDS_HEIGHT, true);
            PopupStyle.Title(ui.txtTitle, W);
            Close(ui.btClose);

            Transform mid = ui.txtLeague.transform.parent;
            PopupStyle.PlaceTop(mid, 0f, 222f, INNER, 100f);
            PopupStyle.Center(ui.txtLeague.transform, 0f, 0f, 400f, 90f);
            PopupStyle.Outline(ui.txtLeague, 48f, 30f);
            ui.txtLeague.alignment = TextAlignmentOptions.Center;
            ArrowButton(ui.btPrevious, -274f);
            ArrowButton(ui.btNext, 274f);

            PopupStyle.PlaceTop(ui.listContent, 0f, 666f, 746f, 748f);
            VerticalLayoutGroup layout = ui.listContent.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private const float SEASON_CLAIM_HEIGHT = 732f;

    public static void ApplyPvPSeasonClaim()
    {
        EditPrefab("UIPvPSeasonClaimPopup", root =>
        {
            UIPvPSeasonClaimPopup ui = root.GetComponent<UIPvPSeasonClaimPopup>();
            ((RectTransform)Find(root.transform, "PvPSeasonClaimPopup")).sizeDelta = new Vector2(W, 0f);
            RectTransform panel = Panel(root, "ContentTransform", "Background", SEASON_CLAIM_HEIGHT, false);
            PopupStyle.Title(ui.txtTitle, W);
            Decor(Find(panel, "Icon"), 168f, 160f, -30f);

            Transform tier = ui.txtRank.transform.parent.parent;
            PopupStyle.PlaceTop(tier, 0f, 222f, 440f, 92f);
            PopupStyle.Stretch(ui.txtRank.transform.parent, 6f, 6f, 6f, 6f);
            PopupStyle.Stretch(ui.txtRank.transform);
            PopupStyle.Outline(ui.txtRank, 40f, 26f);
            ui.txtRank.color = Color.white;
            ui.txtRank.alignment = TextAlignmentOptions.Center;

            Transform rewards = ui.txtLeagueResult.transform.parent;
            PopupStyle.PlaceTop(rewards, 0f, 396f, INNER, 208f);
            PopupStyle.Center(ui.txtLeagueResult.transform, 0f, 66f, 680f, 50f);
            PopupStyle.Outline(ui.txtLeagueResult, 38f, 24f);
            ui.txtLeagueResult.alignment = TextAlignmentOptions.Center;
            PopupStyle.Center(ui.txtTrophy.transform, 0f, 10f, 300f, 50f);
            PopupStyle.Outline(ui.txtTrophy, 38f, 24f);
            ui.txtTrophy.alignment = TextAlignmentOptions.Center;
            RewardGrid(ui.rewardsContent, ui.rewardTextPrefab, -55f, 4, 160f, 50f);

            const float buttonY = 599f;
            MainButton(ui.btClaim, PopupStyle.ButtonColor.Green, buttonY);
            PopupStyle.PlaceTop(ui.txtError.transform, 0f, 396f, 700f, 60f);
            PopupStyle.Outline(ui.txtError, 34f, 22f);
            ui.txtError.color = PopupStyle.DOWN;
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, buttonY, 76f, 76f);
        });
    }
    #endregion

    #region Boss Rush
    private const float JOIN_HEIGHT = 680f;

    public static void ApplyBossRushJoin()
    {
        EditPrefab("UIBossRushJoinPopup", root =>
        {
            UIBossRushJoinPopup ui = root.GetComponent<UIBossRushJoinPopup>();
            ((RectTransform)Find(root.transform, "BossRushJoinPopup")).sizeDelta = new Vector2(W, 0f);
            RectTransform panel = Panel(root, "ContentTransform", "Background", JOIN_HEIGHT, false);
            PopupStyle.Title(ui.txtTitle, W);
            Close(ui.btClose);
            Decor(Find(panel, "BossImage"), 190f, 190f, -65f);

            PopupStyle.PlaceTop(ui.txtTimer.transform, 0f, 199f, INNER, 50f);
            PopupStyle.Plain(ui.txtTimer, PopupStyle.BROWN, 36f, 24f);
            ui.txtTimer.alignment = TextAlignmentOptions.Center;
            TanLabel(ui.txtTierName, 290f, 376f, 88f);
            PopupStyle.PlaceTop(ui.txtDescription.transform, 0f, 403f, 600f, 90f);
            PopupStyle.Plain(ui.txtDescription, PopupStyle.BROWN, 36f, 24f);
            ui.txtDescription.alignment = TextAlignmentOptions.Center;

            MainButton(ui.btJoin, PopupStyle.ButtonColor.Orange, 547f);
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, 547f, 76f, 76f);
            ui.objLoading.transform.SetAsLastSibling();
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private const float LOBBY_HEIGHT = 1647f;
    private const float REWARDS_HEIGHT = 1358f;

    public static void ApplyBossRushLobby()
    {
        EditPrefab("UIBossRushPopup", root =>
        {
            UIBossRushPopup ui = root.GetComponent<UIBossRushPopup>();
            ((RectTransform)Find(root.transform, "BossRushPopup")).sizeDelta = new Vector2(W, 0f);
            RectTransform panel = Panel(root, "ContentTransform", "Background", LOBBY_HEIGHT, false);
            Close(ui.btClose);
            HeaderButton(ui.btRewards);
            Decor(Find(panel, "BossImage"), 190f, 190f, -65f);

            // Header 2 dòng: "Boss Rush" + tên league.
            TMP_Text title = Text(panel, "TitleText");
            HeaderLine(title, 46f);
            HeaderLine(ui.txtTier, 94f);

            Transform clock = ui.txtTimer.transform.parent;
            PopupStyle.PlaceTop(clock, 0f, 216f, 274f, 80f);
            PopupStyle.Outline(ui.txtTimer, 36f, 24f);

            Transform boss = ui.txtBossTitle.transform.parent;
            PopupStyle.PlaceTop(boss, 0f, 364f, INNER, 176f);
            PopupStyle.Stretch(boss.Find("Background"));
            PopupStyle.Center(ui.txtBossTitle.transform, 0f, 44f, 680f, 50f);
            PopupStyle.Outline(ui.txtBossTitle, 34f, 24f);
            ui.txtBossTitle.alignment = TextAlignmentOptions.Center;
            PopupStyle.Center(ui.sliderBossHp.transform, 0f, -24f, 600f, 46f);

            // Danh sách: mask không sprite + lề 8 (viền đen dòng không bị cắt).
            const float fightY = LOBBY_HEIGHT - BOTTOM - BTN / 2f;               // 1514
            const float listTop = 472f, listBottom = fightY - BTN / 2f - 20f;    // 1419
            PopupStyle.Stretch(ui.objContent.transform);
            Transform scroll = Find(ui.objContent.transform, "Scroll View");
            PopupStyle.PlaceTop(scroll, 0f, (listTop + listBottom) / 2f, 760f, listBottom - listTop);
            Transform viewport = Find(scroll, "Viewport");
            PopupStyle.Stretch(viewport);
            Image mask = viewport.GetComponent<Image>();
            mask.sprite = null;
            mask.color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, (listTop + listBottom) / 2f, 76f, 76f);
            VerticalLayoutGroup listLayout = ui.playerListContent.GetComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(8, 8, listLayout.padding.top, listLayout.padding.bottom);

            // Dòng "hết lượt đánh" nằm trên panel tối đè đáy danh sách (chỉ hiện khi có chữ).
            Image status = PopupStyle.NewImage(panel, "StatusPanel", PopupStyle.Kit("Mastery/mastery_panel"));
            status.transform.SetSiblingIndex(ui.objContent.transform.GetSiblingIndex() + 1);
            PopupStyle.PlaceTop(status.transform, 0f, listBottom - 90f, 620f, 170f);
            Reparent(ui.txtStatus.transform, status.transform);
            ui.txtStatus.gameObject.SetActive(true);
            PopupStyle.Stretch(ui.txtStatus.transform, 20f, 10f, 20f, 10f);
            PopupStyle.Outline(ui.txtStatus, 32f, 22f);
            ui.txtStatus.alignment = TextAlignmentOptions.Center;
            ui.objStatusPanel = status.gameObject;
            status.gameObject.SetActive(false);

            // Fight: cam / hồng (vé ads, chữ có icon IsAd) / xám.
            PopupStyle.Button(ui.btFight.transform, PopupStyle.ButtonColor.Orange, 300f, BTN, 44f);
            PopupStyle.PlaceTop(ui.btFight.transform, 0f, fightY, 300f, BTN);
            Button(ui.btFight);
            ui.txtFightButton.lineSpacing = -20f;
            ui.txtFightButton.spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>("Assets/_Assets/Art/2_Icons/Currency/IsAd.asset");
            ui.sprFight = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.sprFightAd = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Pink);
            ui.sprFightDisabled = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);

            ApplyRewardsPanel(ui.rewardsPanel);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Bảng thưởng (con của UIBossRushPopup): nhãn mục nâu, ô đảm bảo tan, mỗi mốc hạng 1 dòng tan.
    private static void ApplyRewardsPanel(UIBossRushRewardsPanel rewards)
    {
        Transform root = rewards.transform;
        PopupStyle.Center(root, 0f, 0f, W, REWARDS_HEIGHT);
        RectTransform panel = (RectTransform)Find(root, "Background");
        PopupStyle.Shell(panel, W, REWARDS_HEIGHT, panel, panel.Find("Header"));
        PopupStyle.Title(Text(panel, "TitleText"), W);
        Close(Find(panel, "CloseButton").GetComponent<Button>());

        Transform prize = Find(panel, "GuaranteedPrize");
        PopupStyle.PlaceTop(prize, 0f, 338f, INNER, 200f);
        SectionLabel(Text(prize, "PrizePoolInfoText"), 139f);
        TMP_Text guaranteed = Text(prize, "GuaranteedRewardsText");
        PopupStyle.Center(guaranteed.transform, 0f, 0f, 680f, 180f);
        PopupStyle.Outline(guaranteed, 32f, 22f);
        guaranteed.alignment = TextAlignmentOptions.Center;

        Transform scroll = Find(panel, "Scroll View");
        PopupStyle.PlaceTop(scroll, 0f, 879f, 656f, 710f);
        Transform viewport = Find(scroll, "Viewport");
        PopupStyle.Stretch(viewport);
        Image mask = viewport.GetComponent<Image>();
        mask.sprite = null;
        mask.color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        SectionLabel(Text(scroll, "PlacementRewardsText"), 394f);
        SectionLabel(Text(scroll, "PlacementText"), -396f);

        Transform row = Find(viewport, "PlacementRow");
        foreach (TMP_Text text in row.GetComponentsInChildren<TMP_Text>(true))
            PopupStyle.Outline(text, text.name == "RankText" ? 32f : 28f, 20f);
        Find(panel, "CloseButton").SetAsLastSibling();
    }

    private const float END_HEIGHT = 552f;

    public static void ApplyBossRushEnd()
    {
        EditPrefab("UIBossRushEndPopup", root =>
        {
            UIBossRushEndPopup ui = root.GetComponent<UIBossRushEndPopup>();
            ((RectTransform)Find(root.transform, "BossRushEndPopup")).sizeDelta = new Vector2(W, 0f);
            RectTransform panel = Panel(root, "ContentTransform", "Background", END_HEIGHT, false);
            PopupStyle.Title(ui.txtTitle, W);
            Close(ui.btClose);

            Transform loaded = ui.txtInfo.transform.parent;
            PopupStyle.Stretch(loaded);
            Image box = PopupStyle.NewImage(loaded, "InfoBox", PopupStyle.Kit("Mastery/box_tan"));
            box.transform.SetAsFirstSibling();
            PopupStyle.PlaceTop(box.transform, 0f, 246f, 600f, 140f);
            PopupStyle.PlaceTop(ui.txtInfo.transform, 0f, 246f, 580f, 130f);
            PopupStyle.Outline(ui.txtInfo, 40f, 26f);
            ui.txtInfo.alignment = TextAlignmentOptions.Center;
            PopupStyle.PlaceTop(ui.txtError.transform, 0f, 246f, 700f, 130f);
            PopupStyle.Outline(ui.txtError, 40f, 26f);
            ui.txtError.color = PopupStyle.DOWN;
            ui.txtError.alignment = TextAlignmentOptions.Center;

            const float buttonY = 419f;
            MainButton(ui.btContinue, PopupStyle.ButtonColor.Green, buttonY);
            MainButton(ui.btRetry, PopupStyle.ButtonColor.Orange, buttonY);
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, buttonY, 76f, 76f);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private const float CLAIM_HEIGHT = 720f;

    public static void ApplyBossRushClaim()
    {
        EditPrefab("UIBossRushClaimPopup", root =>
        {
            UIBossRushClaimPopup ui = root.GetComponent<UIBossRushClaimPopup>();
            ((RectTransform)Find(root.transform, "BossRushClaimPopup")).sizeDelta = new Vector2(W, 0f);
            RectTransform panel = Panel(root, "ContentTransform", "Background", CLAIM_HEIGHT, false);
            PopupStyle.Title(ui.txtTitle, W);
            Close(ui.btClose);
            Decor(Find(panel, "BossImage"), 190f, 190f, -65f);

            // Ô league: giữ toạ độ chữ con (hiệu ứng trượt đọc vị trí lúc Awake), chỉ đổi khung + chữ.
            Transform tier = ui.txtCurrentTier.transform.parent.parent;
            PopupStyle.PlaceTop(tier, 0f, 220f, 376f, 88f);
            PopupStyle.Outline(ui.txtCurrentTier, 38f, 24f);
            PopupStyle.Outline(ui.txtNextTier, 38f, 24f);
            TMP_Text oldName = Text(tier, "RankNameText");
            if (oldName != null) PopupStyle.Outline(oldName, 38f, 24f);

            Transform rewards = ui.txtRank.transform.parent;
            PopupStyle.PlaceTop(rewards, 0f, 388f, INNER, 200f);
            PopupStyle.Center(ui.txtRank.transform, 0f, 64f, 680f, 50f);
            PopupStyle.Outline(ui.txtRank, 36f, 24f);
            ui.txtRank.alignment = TextAlignmentOptions.Center;
            RewardGrid(ui.rewardsContent, ui.rewardTextPrefab, -30f, 3, 200f, 104f);

            const float buttonY = 587f;
            MainButton(ui.btClaim, PopupStyle.ButtonColor.Green, buttonY);
            MainButton(ui.btRetry, PopupStyle.ButtonColor.Orange, buttonY);
            PopupStyle.PlaceTop(ui.txtError.transform, 0f, 388f, 700f, 200f);
            PopupStyle.Outline(ui.txtError, 40f, 26f);
            ui.txtError.color = PopupStyle.DOWN;
            PopupStyle.PlaceTop(ui.objLoading.transform, 0f, buttonY, 76f, 76f);
            ui.btClose.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Hồ sơ người khác (giữ bố cục user chỉnh tay)
    public static void ApplyPlayer()
    {
        EditPrefab("UIPlayerPopup", root =>
        {
            UIPlayerPopup ui = root.GetComponent<UIPlayerPopup>();
            Close(ui.btClose);

            // Chỉ số phụ: cột nhãn nâu (txtSubStats) + cột số viền canh phải (txtSubStatValues, con phủ cùng ô).
            TMP_Text labels = ui.txtSubStats;
            RectTransform rt = labels.rectTransform;
            rt.sizeDelta = new Vector2(720f, rt.sizeDelta.y);
            LayoutElement element = GetOrAdd<LayoutElement>(labels.gameObject);
            element.minWidth = element.preferredWidth = 720f;
            float size = labels.fontSize;
            PopupStyle.Plain(labels, PopupStyle.BROWN, size);
            labels.alignment = TextAlignmentOptions.TopLeft;
            TMP_Text values = PopupStyle.GetOrCreateText(labels.transform, "BonusValues");
            PopupStyle.Stretch(values.transform);
            PopupStyle.Outline(values, size);
            values.alignment = TextAlignmentOptions.TopRight;
            values.text = "+3%";
            ui.txtSubStatValues = values;
        });
    }
    #endregion

    #region Helper
    // Popup chuẩn: khung (frame) cố định W×height căn giữa màn; container giữ đúng cỡ khung.
    private static RectTransform Panel(GameObject root, string containerName, string frameName, float height, bool frameStretched)
    {
        Transform container = Find(root.transform, containerName);
        PopupStyle.Center(container, 0f, 0f, W, height);
        RectTransform frame = (RectTransform)Find(container, frameName);
        PopupStyle.Shell(frame, W, height, frame, frame.Find("Header"));
        if (frameStretched) PopupStyle.Stretch(frame);
        return frame;
    }

    // Đầu boss / khiên nhô trên header, nằm dưới tiêu đề + nút.
    private static void Decor(Transform image, float width, float height, float topToCenter)
    {
        PopupStyle.PlaceTop(image, 0f, topToCenter, width, height);
        Transform header = image.parent.Find("Header");
        if (header != null) image.SetSiblingIndex(header.GetSiblingIndex() + 1);
    }

    // Nút vuông xanh lơ góc trái header (Rewards), icon 64.
    private static void HeaderButton(Button button)
    {
        ReskinPrefabUtil.Img(button.transform, PopupStyle.Kit("Lobby/btn_blue"));
        PopupStyle.PlaceTop(button.transform, -W / 2f + 71f, PopupStyle.HEADER_HEIGHT / 2f, 104f, 104f);
        Transform content = button.transform.Find("Content");
        if (content != null)
        {
            PopupStyle.Stretch(content);
            foreach (Transform child in content)
                if (child.GetComponent<Image>() != null) PopupStyle.Center(child, 0f, 4f, 64f, 64f);
        }
        Button(button);
        button.transform.SetAsLastSibling();
    }

    private static void HeaderLine(TMP_Text text, float topToCenter)
    {
        PopupStyle.PlaceTop(text.transform, 0f, topToCenter, W - 2f * (PopupStyle.CLOSE_SIZE + 40f), 52f);
        PopupStyle.Outline(text, 46f, 28f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
    }

    private static void TanLabel(TMP_Text text, float topToCenter, float width, float height)
    {
        Transform box = text.transform.parent;
        ReskinPrefabUtil.Img(box, PopupStyle.Kit("Mastery/box_tan"));
        PopupStyle.PlaceTop(box, 0f, topToCenter, width, height);
        PopupStyle.Stretch(text.transform, 8f, 4f, 8f, 4f);
        PopupStyle.Outline(text, 38f, 24f);
        text.alignment = TextAlignmentOptions.Center;
    }

    private static void SectionLabel(TMP_Text text, float y)
    {
        PopupStyle.Center(text.transform, 0f, y, 600f, 50f);
        PopupStyle.Plain(text, PopupStyle.BROWN, 36f, 24f);
        text.alignment = TextAlignmentOptions.Center;
    }

    // Lưới chữ thưởng "<icon>số" (GridLayoutGroup có sẵn) căn giữa ô tan.
    private static void RewardGrid(Transform grid, TMP_Text template, float y, int columns, float cellWidth, float height)
    {
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(cellWidth, 50f);
        layout.spacing = new Vector2(0f, 4f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.childAlignment = TextAnchor.UpperCenter;
        PopupStyle.Center(grid, 0f, y, columns * cellWidth, height);
        PopupStyle.Outline(template, 36f, 24f);
        template.alignment = TextAlignmentOptions.Center;
        template.enableWordWrapping = false;
    }

    private static void ArrowButton(Button button, float x)
    {
        ReskinPrefabUtil.Img(button.transform, PopupStyle.Kit("Lobby/btn_blue"));
        PopupStyle.Center(button.transform, x, 0f, 92f, 92f);
        Transform content = button.transform.Find("Content");
        if (content != null) PopupStyle.Stretch(content);
        Button(button);
    }

    private static void MainButton(Button button, PopupStyle.ButtonColor color, float topToCenter)
    {
        PopupStyle.Button(button.transform, color, 320f);
        PopupStyle.PlaceTop(button.transform, 0f, topToCenter, 320f, BTN);
        Button(button);
    }

    // Nút X kit; giữ trạng thái bật/tắt cũ (script tự bật khi cần).
    private static void Close(Button button)
    {
        if (button == null) return;
        bool active = button.gameObject.activeSelf;
        PopupStyle.CloseButton(button.transform);
        button.gameObject.SetActive(active);
        Button(button);
    }

    // Kit: trạng thái nút bằng sprite, không tô màu khi tắt (PressButtonUI lo hiệu ứng nhấn).
    private static void Button(Button button)
    {
        if (button != null) button.transition = Selectable.Transition.None;
    }

    private static void Reparent(Transform t, Transform parent)
    {
        if (t.parent != parent) t.SetParent(parent, false);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    // Tìm theo đường dẫn, không có thì tìm theo tên trong cả cây.
    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t != null) return t;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == path) return child;
        throw new System.Exception("[Batch6Reskin] thiếu node " + root.name + "/" + path);
    }

    private static TMP_Text Text(Transform root, string path)
    {
        return Find(root, path).GetComponent<TMP_Text>();
    }
    #endregion
}

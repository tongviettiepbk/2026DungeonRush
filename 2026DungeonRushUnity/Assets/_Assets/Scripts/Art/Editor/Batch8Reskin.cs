using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 8 – Clan War & Chat theo concept tools/concept/batch8/concept.png (kit chung PopupStyle + helper Batch7Reskin).
// User duyệt 2026-11-12: tên clan lên header bảng đóng góp, thẻ ngày đổi sprite theo kết quả (xanh/xám/tối) + 2 key mới
// Popup.ClanWarResults.Day/.Points, nút ? mở Championship Ranking, mốc thưởng có nút Claim + chấm ! / tick, cooldown ẩn Win/Lose,
// Report nút đỏ + key dịch. Toạ độ = concept × 2, đo từ MÉP TRÊN panel tới TÂM. Chạy lại ra cùng kết quả.
public static class Batch8Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string CARDS = "Assets/_Assets/Prefabs/UINew/Clan/";
    private const float W = PopupStyle.WIDTH_MEDIUM;                        // 816
    private const float INNER = W - 2f * PopupStyle.SIDE_INSET;             // 708
    private const float BOTTOM = PopupStyle.BOTTOM_INSET;                   // 58

    private static readonly Color DARK_TEXT = new Color32(90, 63, 34, 255);   // #5A3F22 chữ lý do report
    private static readonly Color LINE = new Color32(227, 207, 168, 255);     // #E3CFA8 vạch kẻ dòng
    private static readonly Color DIVIDER = new Color32(216, 191, 149, 255);  // #D8BF95 vạch dưới Champion

    [MenuItem("Tools/DungeonRush/Reskin/Batch 8 (Clan War & Chat)")]
    public static string ApplyAll()
    {
        ApplyContribution();
        ApplyResults();
        ApplyLeaderboard();
        ApplyRewards();
        ApplyReport();
        return "ok";
    }

    private delegate void Edit(GameObject root);

    private static void EditPrefab(string path, Edit edit)
    {
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
        Debug.Log("[Batch8Reskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Contribution leaderboard
    private const float CONTRIB_HEIGHT = 1446f;

    public static void ApplyContribution()
    {
        EditPrefab(UI + "UIClanContributionLeaderboardPopup.prefab", root =>
        {
            UIClanContributionLeaderboardPopup ui = root.GetComponent<UIClanContributionLeaderboardPopup>();
            RectTransform panel = Batch7Reskin.Panel(root, "ContentTransform", "Background", CONTRIB_HEIGHT);

            // Tên clan lên header, banner nhỏ cạnh dòng "Day n Leaderboard" (bỏ khối banner to đè header).
            Batch7Reskin.Reparent(ui.ClanNameText.transform, panel);
            PopupStyle.Title(ui.ClanNameText, W);
            Batch7Reskin.Reparent(ui.Banner.transform, panel);
            PopupStyle.PlaceTop(ui.Banner.transform, -INNER / 2f + 64f, 230f, 104f, 124f);
            Transform oldHeader = panel.Find("Header");
            if (oldHeader != null) oldHeader.gameObject.SetActive(false);

            PopupStyle.PlaceTopLeft(ui.SubheaderText.transform, -INNER / 2f + 140f, 230f, INNER - 140f, 80f);
            PopupStyle.Outline(ui.SubheaderText, 48f, 30f);
            Batch7Reskin.Normal(ui.SubheaderText);
            ui.SubheaderText.alignment = TextAlignmentOptions.MidlineLeft;
            ui.SubheaderText.text = "Day 3 Leaderboard";

            PopupStyle.PlaceTop(ui.ModeSelector.transform, 0f, 356f, 600f, 88f);
            RectTransform selector = (RectTransform)ui.ModeSelector.transform.Find("Selector");
            PopupStyle.Center(selector, -150f, 0f, 300f, 88f);

            Batch7Reskin.ScrollList(ui.ListContent.parent.parent.GetComponent<ScrollRect>(), 872f, 904f);

            PopupStyle.PlaceTop(ui.DelayInfoText.transform, 0f, 1366f, INNER, 44f);
            PopupStyle.Plain(ui.DelayInfoText, PopupStyle.MUTE, 34f, 24f);
            Batch7Reskin.Normal(ui.DelayInfoText);
            ui.DelayInfoText.alignment = TextAlignmentOptions.Center;
            ui.DelayInfoText.text = "Updates every minute.";

            Batch7Reskin.Loading(ui.LoadingView, true);
            Batch7Reskin.Close(ui.CloseButton);
            ui.CloseButton.transform.SetAsLastSibling();
        });
        EditPrefab(CARDS + "ClanWarContributionCard.prefab", ContributionRow);
    }

    // Thẻ xám 124, chính mình = thẻ xanh (đổi sprite): hạng, avatar ô tối, tên, điểm vàng canh phải.
    private static void ContributionRow(GameObject root)
    {
        ClanWarContributionCard card = root.GetComponent<ClanWarContributionCard>();
        Batch7Reskin.CardHeight(root, PopupStyle.Kit("Clan/card_gray"), 124f);
        card.NormalSprite = PopupStyle.Kit("Clan/card_gray");
        card.SelfSprite = PopupStyle.Kit("Clan/card_blue");
        card.NormalColor = card.SelfColor = Color.white;
        root.GetComponent<Image>().color = Color.white;

        Batch7Reskin.Anchored(card.RankText.transform, 0f, 64f, 0f, 80f, 70f);
        PopupStyle.Outline(card.RankText, 44f, 26f);
        Batch7Reskin.Normal(card.RankText);
        card.RankText.alignment = TextAlignmentOptions.Center;
        card.RankText.enableWordWrapping = false;
        Batch7Reskin.AvatarSlot(card.ProfileImage, 161f, 86f, 72f);
        Batch7Reskin.LeftText(card.NameText, 220f, 0f, 240f, 50f, 34f);
        Batch7Reskin.Anchored(card.PointsText.transform, 1f, -26f - 110f, 0f, 220f, 50f);
        PopupStyle.Outline(card.PointsText, 36f, 24f);
        Batch7Reskin.Normal(card.PointsText);
        card.PointsText.color = PopupStyle.ACCENT;
        card.PointsText.alignment = TextAlignmentOptions.MidlineRight;
        card.PointsText.enableWordWrapping = false;
        card.PointsText.text = "12,500 pts";
    }
    #endregion

    #region War results
    private const float RESULTS_HEIGHT = 1354f;
    private const float VERSUS_HEIGHT = 314f;
    // Cột bảng ngày (tính từ mép trái thẻ 708): Day 26/104, Pts tâm 176, Winner 222/224, MVP 458/224.
    private const float COL_DAY = 26f, COL_DAY_W = 104f, COL_PTS = 176f, COL_PTS_W = 68f;
    private const float COL_WIN = 222f, COL_WIN_W = 224f, COL_MVP = 458f, COL_MVP_W = 224f;

    public static void ApplyResults()
    {
        EditPrefab(UI + "UIClanWarDaysAndResultsPopup.prefab", root =>
        {
            UIClanWarDaysAndResultsPopup ui = root.GetComponent<UIClanWarDaysAndResultsPopup>();
            RectTransform panel = Batch7Reskin.Panel(root, "ContentTransform", "Background", RESULTS_HEIGHT);
            Transform content = Batch7Reskin.Find(panel, "ContentRoot");
            PopupStyle.Stretch(content);
            PopupStyle.Title(ui.TitleText, W);

            PopupStyle.PlaceTop(ui.SubtitleText.transform, 0f, 186f, INNER, 44f);
            PopupStyle.Plain(ui.SubtitleText, PopupStyle.BROWN, 34f, 22f);
            Batch7Reskin.Normal(ui.SubtitleText);
            ui.SubtitleText.alignment = TextAlignmentOptions.Center;

            // Ô tan 2 clan: banner + tên + "Tier | server", tỉ số giữa.
            Image box = PopupStyle.NewImage(content, "VersusBox", PopupStyle.Kit("Mastery/box_tan"));
            box.type = Image.Type.Sliced;
            PopupStyle.PlaceTop(box.transform, 0f, 228f + VERSUS_HEIGHT / 2f, INNER, VERSUS_HEIGHT);
            Side(box.transform, -212f, ui.MyBanner, ui.MyClanNameText, ui.MyTierText, ui.MyServerText);
            Side(box.transform, 212f, ui.EnemyBanner, ui.EnemyClanNameText, ui.EnemyTierText, ui.EnemyServerText);
            Batch7Reskin.Reparent(ui.ScoreboardText.transform, box.transform);
            PopupStyle.Center(ui.ScoreboardText.transform, 0f, 0f, 180f, 110f);
            PopupStyle.Outline(ui.ScoreboardText, 80f, 50f);
            Batch7Reskin.Normal(ui.ScoreboardText);
            ui.ScoreboardText.alignment = TextAlignmentOptions.Center;
            ui.ScoreboardText.enableWordWrapping = false;
            ui.ScoreboardText.text = "<color=#5EC8FF>8</color> : 5";
            Transform clansRow = Batch7Reskin.Find(content, "ClansRow");
            clansRow.gameObject.SetActive(false);

            PopupStyle.PlaceTop(ui.DailyScoresHeaderText.transform, 0f, 598f, INNER, 56f);
            PopupStyle.Outline(ui.DailyScoresHeaderText, 44f, 28f);
            Batch7Reskin.Normal(ui.DailyScoresHeaderText);
            ui.DailyScoresHeaderText.alignment = TextAlignmentOptions.Center;

            // Tiêu đề cột nâu (key dịch, 2 key mới).
            Transform headers = Batch7Reskin.Find(content, "Headers");
            PopupStyle.PlaceTop(headers, 0f, 665f, INNER, 38f);
            HeaderText(headers, "DayLabelText", COL_DAY, COL_DAY_W, TextAlignmentOptions.MidlineLeft, "Popup.ClanWarResults.Day", "Day");
            HeaderText(headers, "WarScoreValueText", COL_PTS - COL_PTS_W / 2f, COL_PTS_W, TextAlignmentOptions.Center, "Popup.ClanWarResults.Points", "Pts");
            HeaderText(headers, "WinnerClanNameText", COL_WIN, COL_WIN_W, TextAlignmentOptions.MidlineLeft, "Clan.War.DaysResults.Winner", "Winner");
            HeaderText(headers, "MVPNameText", COL_MVP, COL_MVP_W, TextAlignmentOptions.MidlineLeft, "Clan.War.Mvp.Label", "MVP");

            // 6 thẻ ngày 92, cách 10.
            Transform days = ui.DaysContent;
            PopupStyle.PlaceTop(days, 0f, 694f + 301f, INNER, 602f);
            VerticalLayoutGroup layout = days.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Batch7Reskin.Loading(ui.LoadingView, true);
            Batch7Reskin.Close(ui.CloseButton);
            ui.CloseButton.transform.SetAsLastSibling();
        });
        EditPrefab(CARDS + "ClanWarDayCard.prefab", DayRow);
    }

    // 1 bên trong ô tan (toạ độ so với tâm ô cao 314): banner tâm +41, tên -68, "Tier | server" -111.
    private static void Side(Transform box, float x, ClanBannerView banner, TMP_Text name, TMP_Text tier, TMP_Text server)
    {
        Batch7Reskin.Reparent(banner.transform, box);
        PopupStyle.Center(banner.transform, x, 41f, 140f, 168f);
        Batch7Reskin.Reparent(name.transform, box);
        PopupStyle.Center(name.transform, x, -68f, 236f, 44f);
        PopupStyle.Outline(name, 34f, 18f);
        Batch7Reskin.Normal(name);
        name.alignment = TextAlignmentOptions.Center;
        name.enableWordWrapping = false;
        Batch7Reskin.Reparent(tier.transform, box);
        PopupStyle.Center(tier.transform, x - 60f, -111f, 112f, 36f);
        PopupStyle.Plain(tier, PopupStyle.BROWN, 28f, 18f);
        Batch7Reskin.Normal(tier);
        tier.alignment = TextAlignmentOptions.MidlineRight;
        tier.enableWordWrapping = false;
        Batch7Reskin.Reparent(server.transform, box);
        PopupStyle.Center(server.transform, x + 60f, -111f, 112f, 36f);
        PopupStyle.Plain(server, PopupStyle.BROWN, 28f, 18f);
        Batch7Reskin.Normal(server);
        server.alignment = TextAlignmentOptions.MidlineLeft;
        server.enableWordWrapping = false;
    }

    private static void HeaderText(Transform headers, string name, float left, float width, TextAlignmentOptions align, string key, string source)
    {
        TMP_Text text = Batch7Reskin.Find(headers, name).GetComponent<TMP_Text>();
        Batch7Reskin.Anchored(text.transform, 0f, left + width / 2f, 0f, width, 38f);
        PopupStyle.Plain(text, PopupStyle.BROWN, 30f, 20f);
        Batch7Reskin.Normal(text);
        text.alignment = align;
        text.enableWordWrapping = false;
        PopupStyle.SetKey(text, key, source);
    }

    // Thẻ ngày 92: mình thắng xanh / địch thắng xám / chưa chốt tối (đổi sprite). Chưa chốt: "Not Concluded" phủ cột Winner+MVP.
    private static void DayRow(GameObject root)
    {
        ClanWarDayCard card = root.GetComponent<ClanWarDayCard>();
        Image image = root.GetComponent<Image>();
        image.color = Color.white;
        LayoutElement element = Batch7Reskin.GetOrAdd<LayoutElement>(root);
        element.preferredHeight = element.minHeight = 92f;
        Batch7Reskin.CardHeight(root, PopupStyle.Kit("Clan/card_dark"), 92f);
        card.StatusImage = image;
        card.StatusColorTarget = null;
        card.WinSprite = PopupStyle.Kit("Clan/card_blue");
        card.LoseSprite = PopupStyle.Kit("Clan/card_gray");
        card.NotConcludedSprite = PopupStyle.Kit("Clan/card_dark");

        DayText(card.DayLabelText, COL_DAY, COL_DAY_W, 32f, TextAlignmentOptions.MidlineLeft);
        DayText(card.WarScoreValueText, COL_PTS - COL_PTS_W / 2f, COL_PTS_W, 32f, TextAlignmentOptions.Center);
        DayText(card.WinnerNameText, COL_WIN, COL_WIN_W, 32f, TextAlignmentOptions.MidlineLeft);
        DayText(card.PlayerNameText, COL_MVP, COL_MVP_W, 30f, TextAlignmentOptions.MidlineLeft);
        // Tên 15 ký tự rộng (WWW…) phải co nhỏ hơn mức chung mới vừa cột 224.
        card.WinnerNameText.fontSizeMin = card.PlayerNameText.fontSizeMin = 14f;
        card.DayLabelText.text = "Day 1";
        card.WarScoreValueText.text = "1";
        card.WinnerNameText.text = "DuckLords";
        card.PlayerNameText.text = "PondKing";

        TMP_Text state = PopupStyle.GetOrCreateText(root.transform, "StateLabelText");
        Batch7Reskin.Anchored(state.transform, 0f, COL_WIN + (COL_MVP + COL_MVP_W - COL_WIN) / 2f, 0f, COL_MVP + COL_MVP_W - COL_WIN, 50f);
        PopupStyle.Plain(state, PopupStyle.MUTE, 32f, 20f);
        Batch7Reskin.Normal(state);
        state.alignment = TextAlignmentOptions.MidlineLeft;
        state.enableWordWrapping = false;
        state.text = "Not Concluded";
        state.gameObject.SetActive(false);
        card.StateLabelText = state;
    }

    private static void DayText(TMP_Text text, float left, float width, float size, TextAlignmentOptions align)
    {
        Batch7Reskin.Anchored(text.transform, 0f, left + width / 2f, 0f, width, 50f);
        PopupStyle.Outline(text, size, 18f);
        Batch7Reskin.Normal(text);
        text.alignment = align;
        text.enableWordWrapping = false;
    }
    #endregion

    #region Clan leaderboard
    private const float LEADER_HEIGHT = 1378f;

    public static void ApplyLeaderboard()
    {
        EditPrefab(UI + "UIClanWarLeaderboardPopup.prefab", root =>
        {
            UIClanWarLeaderboardPopup ui = root.GetComponent<UIClanWarLeaderboardPopup>();
            RectTransform panel = Batch7Reskin.Panel(root, "ContentTransform", "Background", LEADER_HEIGHT);
            Transform content = Batch7Reskin.Find(panel, "Content");
            PopupStyle.Stretch(content);
            PopupStyle.Title(ui.TitleText, W);

            // Khung chứa nhãn + dòng Champion phủ kín panel → toạ độ con = toạ độ panel.
            Transform slot = Batch7Reskin.Find(content, "LeaderSlotArea");
            PopupStyle.Stretch(slot);

            // Nhãn "Champion Clan" nâu + vương miện + nút ? (luôn hiện, kể cả khi chưa có Champion).
            Transform crown = Batch7Reskin.Find(slot, "CrownIcon");
            Batch7Reskin.Reparent(crown, slot);
            PopupStyle.PlaceTop(crown, -175f, 198f, 56f, 41f);
            crown.GetComponent<Image>().preserveAspect = true;
            TMP_Text label = Batch7Reskin.Find(slot, "NameText_1").GetComponent<TMP_Text>();
            Batch7Reskin.Reparent(label.transform, slot);
            PopupStyle.PlaceTop(label.transform, 0f, 198f, 260f, 56f);
            PopupStyle.Plain(label, PopupStyle.BROWN, 38f, 24f);
            Batch7Reskin.Normal(label);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            Transform info = ui.ChampionClanInfoButton.transform;
            Image infoImage = ReskinPrefabUtil.Img(info, PopupStyle.Kit("Common/question"));
            infoImage.color = Color.white;
            infoImage.preserveAspect = true;
            PopupStyle.PlaceTop(info, 175f, 198f, 60f, 60f);
            Transform infoText = info.Find("InfoText");
            if (infoText != null) infoText.gameObject.SetActive(false);
            Batch7Reskin.Button(ui.ChampionClanInfoButton);

            // Dòng Champion = dòng thường cao 180 (chừa chỗ vương miện trên banner).
            PopupStyle.PlaceTop(ui.LeaderRowRoot.transform, 0f, 318f, INNER, 180f);
            Batch7Reskin.ClanRow(ui.LeaderRow.gameObject);
            RectTransform row = (RectTransform)ui.LeaderRow.transform;
            row.anchorMin = new Vector2(0f, 0.5f);
            row.anchorMax = new Vector2(1f, 0.5f);
            row.sizeDelta = new Vector2(0f, 180f);
            row.anchoredPosition = Vector2.zero;
            Batch7Reskin.Anchored(ui.LeaderRow.Banner.transform, 0f, 158f, -12f, 104f, 124f);

            Transform noLeader = ui.NoLeaderRoot.transform;
            ReskinPrefabUtil.Img(noLeader, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(noLeader, 0f, 318f, INNER, 180f);
            PopupStyle.Stretch(ui.NoLeaderText.transform, 24f, 10f, 24f, 10f);
            PopupStyle.Plain(ui.NoLeaderText, PopupStyle.BROWN, 36f, 24f);
            Batch7Reskin.Normal(ui.NoLeaderText);
            ui.NoLeaderText.alignment = TextAlignmentOptions.Center;

            Image divider = PopupStyle.NewImage(slot, "Divider", PopupStyle.Kit("Common/White1px"));
            divider.color = DIVIDER;
            PopupStyle.PlaceTop(divider.transform, 0f, 438f, INNER, 4f);

            Batch7Reskin.ScrollList(ui.ListContent.parent.parent.GetComponent<ScrollRect>(), 890f, 860f);
            Batch7Reskin.Loading(ui.LoadingView, true);
            Batch7Reskin.Close(ui.CloseButton);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region War rewards
    private const float REWARDS_HEIGHT = 1334f;
    private const float SUMMARY_TOP = 164f, SUMMARY_HEIGHT = 288f;
    private const float COLLECT_CARD = 140f;

    public static void ApplyRewards()
    {
        EditPrefab(UI + "UIClanWarRewardsPopup.prefab", root =>
        {
            UIClanWarRewardsPopup ui = root.GetComponent<UIClanWarRewardsPopup>();
            RectTransform panel = Batch7Reskin.Panel(root, "ContentTransform", "Background", REWARDS_HEIGHT);
            PopupStyle.Title(ui.TitleText, W);
            ui.TitleText.text = "War Rewards";

            // Thưởng clan theo tier: nhãn nâu + 2 ô tan Win xanh / Lose đỏ.
            Transform summary = Batch7Reskin.Find(panel, "TierSummary");
            PopupStyle.PlaceTop(summary, 0f, SUMMARY_TOP + SUMMARY_HEIGHT / 2f, INNER, SUMMARY_HEIGHT);
            ui.TierSummaryRoot = summary.gameObject;
            PopupStyle.Center(ui.TierHeaderText.transform, 0f, 120f, INNER, 48f);
            PopupStyle.Plain(ui.TierHeaderText, PopupStyle.BROWN, 38f, 24f);
            Batch7Reskin.Normal(ui.TierHeaderText);
            ui.TierHeaderText.alignment = TextAlignmentOptions.Center;
            ui.TierHeaderText.text = "Tier S Rewards";
            TierCard(Batch7Reskin.Find(summary, "WinCard"), ui.WinCardLabelText, ui.WinRewardsView, 28f, PopupStyle.UP);
            TierCard(Batch7Reskin.Find(summary, "LoseCard"), ui.LoseCardLabelText, ui.LoseRewardsView, -92f, PopupStyle.DOWN);

            // Cooldown: 2 thẻ nhận thưởng thế chỗ (tiêu đề + Claim hàng trên, thưởng hàng dưới).
            Transform collect = Batch7Reskin.Find(panel, "CooldownCollect");
            PopupStyle.PlaceTop(collect, 0f, SUMMARY_TOP + SUMMARY_HEIGHT / 2f, INNER, SUMMARY_HEIGHT);
            CollectCard(ui.CollectClanCardRoot.transform, 74f, ui.CollectClanTitleText, ui.CollectClanRewardsView, ui.CollectClanButton, ui.CollectClanClaimedRoot, ui.CollectClanClaimedText);
            CollectCard(ui.CollectPersonalCardRoot.transform, -74f, ui.CollectPersonalTitleText, ui.CollectPersonalRewardsView, ui.CollectPersonalButton, ui.CollectPersonalClaimedRoot, ui.CollectPersonalClaimedText);
            ui.CollectClanCardText.text = ui.CollectPersonalCardText.text = "Claim";
            ui.CollectClanTitleText.text = "Clan Rewards";
            ui.CollectPersonalTitleText.text = "Personal Rewards";
            collect.gameObject.SetActive(true);
            ui.CollectClanCardRoot.SetActive(false);
            ui.CollectPersonalCardRoot.SetActive(false);

            PopupStyle.PlaceTop(ui.MyScoreText.transform, 0f, 510f, INNER, 60f);
            PopupStyle.Outline(ui.MyScoreText, 48f, 30f);
            Batch7Reskin.Normal(ui.MyScoreText);
            ui.MyScoreText.alignment = TextAlignmentOptions.Center;
            ui.MyScoreText.text = "Your Score: 12,500";

            Track(ui);

            PopupStyle.PlaceTop(ui.DelayInfoText.transform, 0f, 1254f, INNER, 44f);
            PopupStyle.Plain(ui.DelayInfoText, PopupStyle.MUTE, 34f, 24f);
            Batch7Reskin.Normal(ui.DelayInfoText);
            ui.DelayInfoText.alignment = TextAlignmentOptions.Center;
            ui.DelayInfoText.text = "Updates every minute.";

            Batch7Reskin.Loading(ui.ClaimLoadingView, true);
            Batch7Reskin.Close(ui.CloseButton);
            ui.CloseButton.transform.SetAsLastSibling();
        });
        EditPrefab(CARDS + "ClanWarMilestoneCard.prefab", MilestoneRow);
    }

    // Ô tan 104: nhãn Win/Lose màu (viền) bên trái, thưởng icon + số viền.
    private static void TierCard(Transform card, TMP_Text label, ClanWarRewardsView view, float y, Color color)
    {
        PopupStyle.Center(card, 0f, y, INNER, 104f);
        Transform bg = card.Find("BackgroundImage");
        Image bgImage = ReskinPrefabUtil.Img(bg, PopupStyle.Kit("Mastery/box_tan"));
        bgImage.color = Color.white;
        PopupStyle.Stretch(bg);
        Batch7Reskin.Anchored(label.transform, 0f, 24f + 64f, 0f, 128f, 60f);
        PopupStyle.Outline(label, 40f, 24f);
        Batch7Reskin.Normal(label);
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
        RewardsRow(view, 0f, 172f, 0f, 500f, 32f, 44f, 20f);
    }

    // Thẻ nhận thưởng cooldown 140: tiêu đề nâu + nút Claim xanh lá (hết → xám) hàng trên, thưởng hàng dưới; đã nhận = tick + "Claimed".
    private static void CollectCard(Transform card, float y, TMP_Text title, ClanWarRewardsView view, Button button, GameObject claimed, TMP_Text claimedText)
    {
        PopupStyle.Center(card, 0f, y, INNER, COLLECT_CARD);
        Transform bg = card.Find("BackgroundImage");
        Image bgImage = ReskinPrefabUtil.Img(bg, PopupStyle.Kit("Mastery/box_tan"));
        bgImage.color = Color.white;
        PopupStyle.Stretch(bg);

        Batch7Reskin.Anchored(title.transform, 0f, 24f + 200f, 20f, 400f, 56f);
        PopupStyle.Plain(title, PopupStyle.BROWN, 38f, 24f);
        Batch7Reskin.Normal(title);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.enableWordWrapping = false;

        RewardsRow(view, 0f, 24f, -38f, INNER - 48f, 30f, 40f, 18f);

        PopupStyle.Button(button.transform, PopupStyle.ButtonColor.Green, 180f, 72f, 36f);
        Batch7Reskin.Anchored(button.transform, 1f, -20f - 90f, 20f, 180f, 72f);
        Batch7Reskin.Swap(button);

        Batch7Reskin.Anchored(claimed.transform, 1f, -20f - 100f, 20f, 200f, 60f);
        Image check = PopupStyle.NewImage(claimed.transform, "CheckIcon", PopupStyle.Kit("BattlePass/icon_check"));
        check.preserveAspect = true;
        Batch7Reskin.Anchored(check.transform, 0f, 24f, 0f, 48f, 48f);
        Batch7Reskin.Anchored(claimedText.transform, 0f, 52f + 74f, 0f, 148f, 50f);
        PopupStyle.Outline(claimedText, 34f, 22f);
        Batch7Reskin.Normal(claimedText);
        claimedText.color = PopupStyle.UP;
        claimedText.alignment = TextAlignmentOptions.MidlineLeft;
        claimedText.enableWordWrapping = false;
    }

    // Hàng thưởng ngang (ClanWarRewardsView): mỗi loại = icon + số trắng viền, xếp trái sang phải.
    private static void RewardsRow(ClanWarRewardsView view, float anchorX, float left, float y, float width, float size, float icon, float spacing)
    {
        Batch7Reskin.Anchored(view.transform, anchorX, left + width / 2f, y, width, icon + 8f);
        HorizontalLayoutGroup row = view.GetComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleLeft;
        row.spacing = spacing;
        row.padding = new RectOffset(0, 0, 0, 0);
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        foreach (Transform item in view.transform)
        {
            HorizontalLayoutGroup itemRow = item.GetComponent<HorizontalLayoutGroup>();
            if (itemRow == null) continue;
            itemRow.childAlignment = TextAnchor.MiddleLeft;
            itemRow.spacing = 4f;
            itemRow.childControlWidth = true;
            itemRow.childControlHeight = true;
            itemRow.childForceExpandWidth = false;
            itemRow.childForceExpandHeight = false;
            Transform iconT = item.Find("Icon");
            if (iconT != null)
            {
                LayoutElement e = Batch7Reskin.GetOrAdd<LayoutElement>(iconT.gameObject);
                e.preferredWidth = e.minWidth = icon;
                e.preferredHeight = e.minHeight = icon;
                iconT.GetComponent<Image>().preserveAspect = true;
            }
            Transform amount = item.Find("AmountText");
            if (amount != null)
            {
                TMP_Text text = amount.GetComponent<TMP_Text>();
                PopupStyle.Outline(text, size);
                Batch7Reskin.Normal(text);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.enableWordWrapping = false;
                LayoutElement e = Batch7Reskin.GetOrAdd<LayoutElement>(amount.gameObject);
                e.preferredWidth = -1f;
                e.minWidth = -1f;
                e.preferredHeight = icon;
            }
        }
    }

    // Track mốc: thanh dọc kit Clan 52 bên trái, thẻ mốc cuộn bên phải (mask chữ nhật, lề 8).
    private static void Track(UIClanWarRewardsPopup ui)
    {
        Transform track = Batch7Reskin.Find(ui.TrackScrollRect.transform.parent, "TrackScroll");
        PopupStyle.PlaceTop(track, 0f, 886f, INNER, 660f);
        ScrollRect scroll = ui.TrackScrollRect;
        PopupStyle.Stretch(scroll.transform);
        Image bg = scroll.GetComponent<Image>();
        if (bg != null) bg.enabled = false;
        RectTransform viewport = scroll.viewport;
        PopupStyle.Stretch(viewport);
        PopupStyle.RemoveComponent<Mask>(viewport);
        Batch7Reskin.GetOrAdd<RectMask2D>(viewport.gameObject);
        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null) PopupStyle.Invisible(viewportImage);

        RectTransform content = (RectTransform)ui.TrackContent;
        content.offsetMin = new Vector2(68f, content.offsetMin.y);
        content.offsetMax = new Vector2(0f, content.offsetMax.y);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 8, 8, 8);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;

        RectTransform bar = ui.TrackProgressBackgroundRect;
        ReskinPrefabUtil.Img(bar, PopupStyle.Kit("Clan/vbar_track"));
        Batch7Reskin.GetOrAdd<LayoutElement>(bar.gameObject).ignoreLayout = true;
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(0f, 1f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.sizeDelta = new Vector2(52f, -16f);
        bar.anchoredPosition = new Vector2(-42f, 0f);
        RectTransform fill = ui.TrackProgressFillRect;
        ReskinPrefabUtil.Img(fill, PopupStyle.Kit("Clan/vbar_fill"));
        fill.sizeDelta = new Vector2(40f, fill.sizeDelta.y);
    }

    // Thẻ mốc 124: xám chưa đạt / xanh đạt; nhận được = nút Claim xanh lá + chấm !; đã nhận = tick.
    private static void MilestoneRow(GameObject root)
    {
        ClanWarMilestoneCard card = root.GetComponent<ClanWarMilestoneCard>();
        Batch7Reskin.CardHeight(root, PopupStyle.Kit("Clan/card_gray"), 124f);
        card.ReachedSprite = PopupStyle.Kit("Clan/card_blue");
        card.UnreachedSprite = PopupStyle.Kit("Clan/card_gray");

        Batch7Reskin.LeftText(card.ThresholdText, 26f, 0f, 132f, 50f, 32f);
        card.ThresholdText.text = "10,000";
        RewardsRow(card.RewardsView, 0f, 166f, 0f, 296f, 28f, 38f, 10f);
        RectTransform viewRt = (RectTransform)card.RewardsView.transform;
        viewRt.anchorMin = viewRt.anchorMax = new Vector2(0f, 0.5f);

        // Nút Claim (hình, cả thẻ vẫn là nút bấm) + chấm "!" ở góc thẻ.
        Image tag = PopupStyle.NewImage(root.transform, "ClaimTag", PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green));
        tag.type = Image.Type.Sliced;
        Batch7Reskin.Anchored(tag.transform, 1f, -26f - 62f, 0f, 124f, 84f);
        TMP_Text tagText = PopupStyle.GetOrCreateText(tag.transform, "Text");
        PopupStyle.Center(tagText.transform, 0f, 5f, 110f, 60f);
        PopupStyle.Outline(tagText, 30f, 20f);
        Batch7Reskin.Normal(tagText);
        tagText.alignment = TextAlignmentOptions.Center;
        tagText.enableWordWrapping = false;
        PopupStyle.SetKey(tagText, "Clan.War.RewardCard.Claim", "Claim");
        Image alert = PopupStyle.NewImage(tag.transform, "Alert", PopupStyle.Kit("BattlePass/icon_alert"));
        alert.preserveAspect = true;
        PopupStyle.Center(alert.transform, 80f, 58f, 48f, 48f);
        tag.gameObject.SetActive(false);
        card.ClaimTag = tag.gameObject;

        Image check = PopupStyle.NewImage(root.transform, "CollectedTag", PopupStyle.Kit("BattlePass/icon_check"));
        check.preserveAspect = true;
        Batch7Reskin.Anchored(check.transform, 1f, -60f, 0f, 60f, 60f);
        check.gameObject.SetActive(false);
        card.CollectedTag = check.gameObject;

        Batch7Reskin.Loading(card.ClaimLoading, false);
        if (card.ClaimLoading != null) card.ClaimLoading.transform.SetAsLastSibling();
        Batch7Reskin.Button(card.ClaimButton);
    }
    #endregion

    #region Report
    private const float REPORT_HEIGHT = 1200f;
    private static readonly string[] REASON_KEYS =
    {
        "Popup.Report.Reason.Harassment", "Popup.Report.Reason.Spam", "Popup.Report.Reason.InappropriateLanguage",
        "Popup.Report.Reason.OffensiveName", "Popup.Report.Reason.Cheating", "Popup.Report.Reason.HateSpeech", "Popup.Report.Reason.Other",
    };
    private static readonly string[] REASON_TEXTS =
    {
        "Harassment", "Spam", "Inappropriate Language", "Offensive Player Name", "Cheating", "Hate Speech", "Other",
    };

    public static void ApplyReport()
    {
        EditPrefab(UI + "UIReportPopup.prefab", root =>
        {
            UIReportPopup ui = root.GetComponent<UIReportPopup>();
            // Khung = ContentTransform (vừa là panel vừa là ảnh khung) → header mới chèn dưới cùng thứ tự vẽ.
            RectTransform panel = Batch7Reskin.Panel(root, "ReportPopup", "ContentTransform", REPORT_HEIGHT);
            RectTransform frame = (RectTransform)panel.Find("ContentTransform");
            Transform header = frame.Find("Header");
            header.SetSiblingIndex(0);

            TMP_Text title = Batch7Reskin.Find(frame, "TitleText").GetComponent<TMP_Text>();
            PopupStyle.Title(title, W);
            PopupStyle.SetKey(title, "Popup.Report.Title", "Report");

            // Danh sách: hàng nhãn "Reason" 60 + 7 dòng 104 (ô tick tan + chữ nâu đậm, vạch kẻ mảnh).
            Transform options = Batch7Reskin.Find(frame, "ReportOptions");
            PopupStyle.PlaceTop(options, 0f, 168f + (60f + 7f * 104f) / 2f, INNER, 60f + 7f * 104f);
            VerticalLayoutGroup layout = options.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Transform labelRow = Batch7Reskin.Find(options, "Header");
            Height(labelRow, 60f);
            PopupStyle.Invisible(labelRow.GetComponent<Image>());
            TMP_Text label = Batch7Reskin.Find(labelRow, "NextLevelText").GetComponent<TMP_Text>();
            PopupStyle.Stretch(label.transform, 20f, 0f, 0f, 0f);
            PopupStyle.Plain(label, PopupStyle.BROWN, 38f, 24f);
            Batch7Reskin.Normal(label);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            PopupStyle.SetKey(label, "Popup.Report.Reason.Label", "Reason");

            for (int i = 0; i < ui.ReasonCheckButtons.Count; i++)
                ReasonRow(ui.ReasonCheckButtons[i], i, i == ui.ReasonCheckButtons.Count - 1);

            Button submit = ui.SubmitButton;
            PopupStyle.Button(submit.transform, PopupStyle.ButtonColor.Red, 340f, 150f, 52f);
            PopupStyle.PlaceTop(submit.transform, 0f, 1067f, 340f, 150f);
            Batch7Reskin.Button(submit);
            TMP_Text submitText = submit.GetComponentInChildren<TMP_Text>(true);
            PopupStyle.SetKey(submitText, "Common.Report", "Report");
            ui.SubmitText = submitText;

            // Icon xoay trắng thế chỗ chữ nút khi đang gửi.
            PopupStyle.PlaceTop(ui.LoadingImage.transform, 0f, 1061f, 68f, 68f);
            ui.LoadingImage.color = Color.white;
            ui.LoadingImage.transform.SetAsLastSibling();

            Batch7Reskin.Close(ui.CloseButton);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }

    // 1 dòng lý do: ô tick tan 80 + tick xanh, chữ nâu đậm canh trái (key dịch), vạch kẻ dưới (dòng cuối không vạch).
    private static void ReasonRow(CheckButton check, int index, bool last)
    {
        Transform row = check.transform.parent;
        Height(row, 104f);

        Image line = row.Find("Background").GetComponent<Image>();
        ReskinPrefabUtil.Img(line.transform, PopupStyle.Kit("Common/White1px"));
        line.type = Image.Type.Simple;
        line.color = LINE;
        RectTransform lineRt = line.rectTransform;
        lineRt.anchorMin = new Vector2(0f, 0f);
        lineRt.anchorMax = new Vector2(1f, 0f);
        lineRt.pivot = new Vector2(0.5f, 0f);
        lineRt.sizeDelta = new Vector2(0f, 4f);
        lineRt.anchoredPosition = Vector2.zero;
        line.gameObject.SetActive(!last);

        Transform box = row.Find("CheckButtonImg");
        Image boxImage = ReskinPrefabUtil.Img(box, PopupStyle.Kit("Mastery/box_tan"));
        boxImage.color = Color.white;
        Batch7Reskin.Anchored(box, 0f, 20f + 40f, 0f, 80f, 80f);
        Image mark = check.CheckImage;
        ReskinPrefabUtil.Img(mark.transform, PopupStyle.Kit("ClanSearch/check_green"));
        mark.color = Color.white;
        mark.preserveAspect = true;
        PopupStyle.Center(mark.transform, -2f, 8f, 84f, 84f);

        TMP_Text text = row.Find("Reason").GetComponent<TMP_Text>();
        Batch7Reskin.Anchored(text.transform, 0f, 124f + 280f, 0f, 560f, 60f);
        PopupStyle.Plain(text, DARK_TEXT, 38f, 24f);
        Batch7Reskin.Normal(text);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        PopupStyle.SetKey(text, REASON_KEYS[index], REASON_TEXTS[index]);

        PopupStyle.Invisible(check.GetComponent<Image>());
        PopupStyle.Stretch(check.transform);
        check.transform.SetAsLastSibling();
        Batch7Reskin.Button(check.GetComponent<Button>());
    }

    private static void Height(Transform t, float height)
    {
        LayoutElement e = Batch7Reskin.GetOrAdd<LayoutElement>(t.gameObject);
        e.preferredHeight = e.minHeight = height;
        RectTransform rt = (RectTransform)t;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }
    #endregion
}

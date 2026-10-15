using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Khu Loot ở lobby (ForgeUI + ForgeController gốc). Xem DecodedData/FORGE_MODEL.md.
//  - Nút Loot: tiêu 1 hộp (LOOT_TICKET) → 1 món.
//  - Nút Auto: chưa chạy → mở UIAutoForgePopup; đang chạy → dừng. Mỗi lượt auto tiêu
//    ForgeService.GetSelectedHammerCount() hộp, món không đạt lọc tự bán lấy EXP, món đạt lọc hiện popup.
//    Hết hộp thì tự tắt.
//  - Món ra nằm trên bục (itemList) rồi xử lý lần lượt: slot trống → UINewItemPopup (mặc vào hero);
//    có đồ → UINewForgePopup (Equip: mặc món mới, 2 thẻ đổi chỗ; Sell: bán món còn lại lấy EXP).
public class ForgeUIController : MonoBehaviour
{
    private enum State { Idle, ShowingItems, ShowingPopup }

    private class PendingItem
    {
        public LootResult item;
        public ItemElementView view;
    }

    public UIMainLobby owner;
    public Button btLoot;
    public Button btAuto;
    public GameObject objAutoButton;     // cả nút Auto — ẩn khi chưa tới level hiện
    public GameObject objAutoRunning;    // vòng xoay khi auto đang chạy
    public GameObject objAutoIdle;       // chữ "AUTO" khi auto đang tắt
    public Transform itemList;
    public ItemElementView itemPrefab;
    public float autoRotateSpeed = 360f;
    public float processDelay = 0.35f;   // món hiện trên bục rồi mới xử lý
    public float autoNextDelay = 0.5f;   // nghỉ giữa 2 lượt auto

#if UNITY_EDITOR
    [Header("DEBUG - chỉ dùng test")]
    // Nhập level để test bảng rarity. -1 = dùng giá trị thật (playerLevel).
    [SerializeField] private int debugForgeLevel = -1;
#endif

    private readonly List<PendingItem> queue = new List<PendingItem>();
    private State state = State.Idle;
    private bool isAuto;

    public bool IsAuto => isAuto;

    private void Start()
    {
        btLoot.onClick.AddListener(OnClickLoot);
        btAuto.onClick.AddListener(OnClickAuto);
        RefreshAutoVisual();
    }

    private void OnEnable()
    {
        RefreshAutoVisual();
    }

    private void Update()
    {
        if (isAuto && objAutoRunning != null)
            objAutoRunning.transform.Rotate(0f, 0f, -autoRotateSpeed * Time.deltaTime);

#if UNITY_EDITOR
        // chỉnh nhanh debugForgeLevel khi đang chơi: mũi tên lên/xuống
        if (Input.GetKeyDown(KeyCode.UpArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {++debugForgeLevel}");
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            DebugCustom.Log($"[Forge] debugForgeLevel = {--debugForgeLevel}");
#endif
    }

    // ----- Nút -----

    private void OnClickLoot()
    {
        if (state != State.Idle || isAuto)
            return;

        if (ForgeService.HammerCount < 1)
        {
            ShowNotEnough();
            return;
        }

        StartForge(1);
    }

    private void OnClickAuto()
    {
        if (GameData.userData.player.playerLevel < ForgeService.AUTO_USABLE_LEVEL)
        {
            UIManager.Instance.ShowToastMessage("Mở ở level " + ForgeService.AUTO_USABLE_LEVEL, isLocalize: false);
            return;
        }

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
        GameData.userData.items.Consume(ItemType.LOOT_TICKET, count);
        owner.UpdateLootTicketText();

        for (int i = 0; i < count; i++)
        {
            LootResult item = LootService.RollOne(forgeLevel);
            if (item == null)
                continue;   // LootService đã log lỗi.

            // Clan War: điểm "Loot {rarity} Equipment" (ClanWarController.fzk gốc).
            ClanWarController.Instance.RecordLoot(item.rarity);
            queue.Add(new PendingItem { item = item, view = SpawnView(item) });
        }

        state = State.ShowingItems;
        StartCoroutine(ProcessAfter(processDelay));
    }

    private ItemElementView SpawnView(LootResult item)
    {
        if (itemPrefab == null || itemList == null)
            return null;

        ItemElementView view = Instantiate(itemPrefab, itemList);
        view.SetItem(item);
        return view;
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

            if (isAuto && ForgeService.ShouldDiscard(pending.item))
            {
                Sell(pending);
                continue;
            }

            ShowPopup(pending);
            return;
        }

        OnBatchDone();
    }

    private void OnBatchDone()
    {
        state = State.Idle;
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
        state = State.ShowingPopup;
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
            newItem.Show(item, () => OnItemEquipped(pending, item));
            return;
        }

        UINewForgePopup forge = UIManager.Instance.LoadUI(UIKey.NewForgePopup) as UINewForgePopup;
        if (forge == null)
        {
            // Không mở được popup thì coi như bán để vòng forge không kẹt.
            OnItemSold(pending, item);
            return;
        }
        forge.Show(equipped, item, Equip, sold => OnItemSold(pending, sold));
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
    }

    // hep/hey gốc: bán lấy EXP.
    private void Sell(PendingItem pending)
    {
        ForgeService.Sell(pending.item);
        RemoveView(pending);
        owner.RefreshExp();
    }

    private static void RemoveView(PendingItem pending)
    {
        if (pending.view != null)
            Destroy(pending.view.gameObject);
    }
}

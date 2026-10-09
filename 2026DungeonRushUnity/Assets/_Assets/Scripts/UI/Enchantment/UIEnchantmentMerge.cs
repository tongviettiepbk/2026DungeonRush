using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Popup Merge (EnchantmentMergePopup gốc): 3 ô nguyên liệu cùng tier → ô kết quả tier+1.
//   • Mở từ popup Info: đặt sẵn 1 relic (relic vừa bấm). Relic ĐANG ĐEO thì nhớ sourceSlot → merge bằng
//     UserController.edx (relic ở slot + 2 relic kho), còn lại edr (3 relic kho).
//   • Kho: chưa chọn tier thì hiện mọi relic; đã chọn thì chỉ hiện relic cùng tier còn lại.
//   • Bấm relic kho: thêm vào ô (tier > 10 hoặc khác tier đang chọn thì bỏ qua). Bấm ô nguyên liệu: bỏ ra.
//   • Ô kết quả chỉ hiện khi đủ 3; merge xong reset popup về trống.
public class UIEnchantmentMerge : BaseUI
{
    public Button btClose;

    public List<ElementEnchantmentMergeUI> listResourceNeed;   // 3 ô nguyên liệu
    public ElementEnchantmentMergeUI mergeItemDone;             // ô kết quả (tier+1)
    // Template ô kho (nằm sẵn trong Content) — clone ra mỗi relic 1 ô, bản gốc ẩn đi.
    public GameObject objPrefabElementInventory;
    public Transform transParrentInventory;

    public Button btMerge;

    private readonly List<ElementEnchantmentMergeUI> listInventory = new List<ElementEnchantmentMergeUI>();
    private int selectedTier;       // xli: 0 = chưa chọn
    private int selectedCount;      // xlj: số ô đã đặt
    private GearSlotType? source;   // xlk: slot của relic ĐANG ĐEO dùng làm ô đầu (null = không có)

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        btMerge.onClick.AddListener(OnClickMerge);
        objPrefabElementInventory.SetActive(false);

        for (int i = 0; i < listResourceNeed.Count; i++)
        {
            int index = i;
            listResourceNeed[i].Init(_ => OnClickResource(index));
        }
    }

    // initialTier/sourceSlot: GamePopupController.hlv gốc. Mở trống: Show(0, null).
    public void Show(int initialTier, GearSlotType? equippedSlot)
    {
        selectedTier = 0;
        selectedCount = 0;
        source = null;
        if (initialTier >= 1)
        {
            selectedTier = initialTier;
            selectedCount = 1;
            source = equippedSlot;
        }

        Refresh();
        gameObject.SetActive(true);
    }

    // Số relic lấy từ KHO = số ô đã đặt trừ ô của relic đang đeo (hxs gốc).
    private int CountFromInventory()
    {
        return selectedCount - (source.HasValue ? 1 : 0);
    }

    // hxt gốc.
    private void Refresh()
    {
        int need = listResourceNeed.Count;
        for (int i = 0; i < need; i++)
        {
            listResourceNeed[i].SetData(i < selectedCount ? selectedTier : 0);
            listResourceNeed[i].SetInteractable(i < selectedCount);
        }

        bool isFull = selectedCount >= need;
        mergeItemDone.SetData(isFull ? selectedTier + 1 : 0);
        mergeItemDone.SetInteractable(isFull);
        btMerge.interactable = isFull;

        RefreshInventory();
    }

    // hxu gốc: tier 1..11, đã chọn tier thì chỉ hiện tier đó; mỗi relic 1 ô.
    private void RefreshInventory()
    {
        UserEnchantmentData user = GameData.userData.enchantments;
        int index = 0;
        for (int tier = 1; tier <= EnchantmentConfig.MAX_TIER; tier++)
        {
            if (selectedTier >= 1 && tier != selectedTier)
            {
                continue;
            }

            int count = user.GetOwned(tier) - (tier == selectedTier ? CountFromInventory() : 0);
            for (int k = 0; k < count; k++)
            {
                GetElement(index).SetData(tier);
                index++;
            }
        }

        for (int i = index; i < listInventory.Count; i++)
        {
            listInventory[i].gameObject.SetActive(false);
        }
    }

    private ElementEnchantmentMergeUI GetElement(int index)
    {
        if (index >= listInventory.Count)
        {
            GameObject obj = Instantiate(objPrefabElementInventory, transParrentInventory);
            ElementEnchantmentMergeUI element = obj.GetComponent<ElementEnchantmentMergeUI>();
            element.Init(OnClickInventory);
            listInventory.Add(element);
        }

        ElementEnchantmentMergeUI result = listInventory[index];
        result.gameObject.SetActive(true);
        return result;
    }

    // hxw gốc.
    private void OnClickInventory(ElementEchantmentUI element)
    {
        if (selectedCount >= listResourceNeed.Count || element.Tier >= EnchantmentConfig.MAX_TIER)
        {
            return;
        }
        if (selectedTier != 0 && element.Tier != selectedTier)
        {
            return;
        }

        int tier = element.Tier;
        int remaining = GameData.userData.enchantments.GetOwned(tier) - CountFromInventory();
        if (remaining < 1)
        {
            return;
        }

        selectedTier = tier;
        selectedCount++;
        Refresh();
    }

    // hxx gốc: bỏ 1 relic khỏi ô (ô đầu là relic đang đeo thì bỏ luôn sourceSlot).
    private void OnClickResource(int index)
    {
        if (index >= selectedCount)
        {
            return;
        }

        if (index == 0 && source.HasValue)
        {
            source = null;
        }
        selectedCount--;
        if (selectedCount <= 0)
        {
            selectedTier = 0;
            selectedCount = 0;
            source = null;
        }
        Refresh();
    }

    // hxy gốc.
    private void OnClickMerge()
    {
        if (selectedCount < listResourceNeed.Count)
        {
            return;
        }

        bool isEquipped = source.HasValue;
        double powerBefore = PlayerPower.GetCurrent();
        bool done = isEquipped
            ? EnchantmentService.MergeEquipped(source.Value)
            : EnchantmentService.Merge(selectedTier);

        selectedTier = 0;
        selectedCount = 0;
        source = null;
        Refresh();

        if (done)
        {
            this.PostEvent(EventID.EnchantmentChanged);
            if (isEquipped)
            {
                PlayerPower.NotifyChange(powerBefore);   // gốc hxy: chỉ nhánh relic đang đeo (edx) báo Power
            }
        }
    }
}

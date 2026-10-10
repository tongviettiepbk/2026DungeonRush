using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Mục rương trong Store (ChestStoreSection gốc). Rương Legendary trở lên dùng thẻ ngang (FeaturedCardPrefab) đặt ngay
// trên lưới; rương thường dùng thẻ dọc (CardPrefab) trong CardContainer.
public class ChestStoreSection : MonoBehaviour
{
    public ChestStoreCard CardPrefab;
    public ChestStoreCard FeaturedCardPrefab;
    public Transform CardContainer;

    private readonly List<ChestStoreCard> cards = new List<ChestStoreCard>();
    private readonly List<ChestStoreCard> featuredCards = new List<ChestStoreCard>();
    private readonly List<ChestData> shownChests = new List<ChestData>();
    private bool isLayoutDirty;

    public void Refresh(bool gateOpen)
    {
        List<ChestData> chests = ShopService.GetAvailableChests();
        bool isShow = gateOpen && chests.Count > 0;
        gameObject.SetActive(isShow);
        if (!isShow) return;

        if (IsSameAsShown(chests)) RefreshCards();
        else Rebuild(chests);
    }

    // foj gốc: danh sách rương không đổi → chỉ cập nhật dòng pity.
    private void RefreshCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) cards[i].RefreshPity();
        }
        for (int i = 0; i < featuredCards.Count; i++)
        {
            if (featuredCards[i] != null) featuredCards[i].RefreshPity();
        }
    }

    // fol gốc.
    private bool IsSameAsShown(List<ChestData> chests)
    {
        if (chests.Count != shownChests.Count) return false;
        for (int i = 0; i < chests.Count; i++)
        {
            if (chests[i] != shownChests[i]) return false;
        }
        return true;
    }

    // fom gốc.
    private void Rebuild(List<ChestData> chests)
    {
        Clear();
        shownChests.AddRange(chests);

        List<ChestData> featured = new List<ChestData>();
        for (int i = 0; i < chests.Count; i++)
        {
            if (chests[i].Rarity >= Rarity.Legendary) featured.Add(chests[i]);
        }
        // [SUY] gốc sort bằng 1 lambda chưa đọc — xếp rarity cao lên trên.
        featured.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));

        for (int i = 0; i < chests.Count; i++)
        {
            if (featured.Contains(chests[i])) continue;
            ChestStoreCard card = Instantiate(CardPrefab, CardContainer);
            card.Setup(chests[i]);
            cards.Add(card);
        }

        int containerIndex = CardContainer.GetSiblingIndex();
        for (int i = 0; i < featured.Count; i++)
        {
            ChestStoreCard card = Instantiate(FeaturedCardPrefab, transform);
            card.transform.SetSiblingIndex(containerIndex + i);
            card.Setup(featured[i]);
            featuredCards.Add(card);
        }

        isLayoutDirty = true;
    }

    // Xếp lại ở LateUpdate: lúc vừa Instantiate, layout group của thẻ chưa sẵn sàng nên đo chiều cao ra 0.
    private void LateUpdate()
    {
        if (!isLayoutDirty) return;
        isLayoutDirty = false;
        RebuildLayout();
    }

    // fon gốc.
    private void Clear()
    {
        for (int i = CardContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = CardContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        for (int i = 0; i < featuredCards.Count; i++)
        {
            if (featuredCards[i] == null) continue;
            featuredCards[i].transform.SetParent(null);
            Destroy(featuredCards[i].gameObject);
        }
        cards.Clear();
        featuredCards.Clear();
        shownChests.Clear();
    }

    // foo gốc: lưới thẻ + cả mục tự giãn theo số thẻ rồi báo layout cha xếp lại.
    private void RebuildLayout()
    {
        FitHeight(CardContainer as RectTransform);
        FitHeight(transform as RectTransform);
        RectTransform parent = transform.parent as RectTransform;
        if (parent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
    }

    private static void FitHeight(RectTransform rect)
    {
        if (rect == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, LayoutUtility.GetPreferredHeight(rect));
    }
}

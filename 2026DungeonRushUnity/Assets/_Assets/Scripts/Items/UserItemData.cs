using System.Collections.Generic;

// Tiền tệ / vật phẩm tiêu hao của người chơi.
// Key dict là (int)ItemType dạng string để serialize JSON ổn định.
public class UserItemData : BaseUserData
{
    public Dictionary<string, double> consumables { get; set; } = new Dictionary<string, double>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_ITEMS;
    }

    public void SetQuantity(ItemType type, double quantity)
    {
        string id = ((int)type).ToString();
        consumables[id] = quantity;

        if (quantity <= 0)
        {
            consumables.Remove(id);
        }

        isDataChanged = true;
    }

    public double GetQuantityHave(ItemType type)
    {
        string id = ((int)type).ToString();

        if (consumables.ContainsKey(id))
        {
            return consumables[id];
        }

        return 0;
    }

    public bool IsEnough(ItemType type, double quantity)
    {
        return GetQuantityHave(type) >= quantity;
    }

    public void Receive(ItemType type, double quantity)
    {
        if (quantity > 0)
        {
            Adjust(type, quantity);
        }
    }

    public bool Consume(ItemType type, double quantity)
    {
        if (quantity <= 0 || IsEnough(type, quantity) == false)
        {
            return false;
        }

        Adjust(type, -quantity);
        return true;
    }

    private void Adjust(ItemType type, double quantity)
    {
        double curQuantity = GetQuantityHave(type);
        SetQuantity(type, curQuantity + quantity);

        // TODO: khi có EventDispatcher thì post EventID.ItemChanged để UI tự cập nhật
    }
}

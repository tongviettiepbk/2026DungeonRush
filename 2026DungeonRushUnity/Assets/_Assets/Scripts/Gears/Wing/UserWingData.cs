using System.Collections.Generic;

// Save Wing người chơi đã chế tạo. Schema gốc (il2cpp v41): User.OwnedWings : List<WingModel>
//   WingModel { WingId, Level, CardCount, IsNew, SubStats }  — mỗi wingId tối đa 1 bản.
// Wing ĐANG MẶC không lưu ở đây mà ở UserEquipmentData slot WING (equipId = wingId) — bản ghi đó copy
// level/subStats nên WingService đồng bộ lại mỗi khi lên cấp / reroll wing đang mặc.
public class UserWingData : BaseUserData
{
    public List<WingModel> owned { get; set; } = new List<WingModel>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_WING;
    }

    public override void InitData()
    {
        base.InitData();
        owned = new List<WingModel>();
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (owned == null)
        {
            owned = new List<WingModel>();
            isDataChanged = true;
        }

        for (int i = owned.Count - 1; i >= 0; i--)
        {
            if (owned[i] == null)
            {
                owned.RemoveAt(i);
                isDataChanged = true;
                continue;
            }
            if (owned[i].subStats == null)
            {
                owned[i].subStats = new List<GearSubStat>();
                isDataChanged = true;
            }
            if (owned[i].level < 1)
            {
                owned[i].level = 1;
                isDataChanged = true;
            }
        }
    }

    // UserController.eec: wing đã sở hữu theo id (null = chưa chế tạo).
    public WingModel Get(int wingId)
    {
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i].wingId == wingId)
            {
                return owned[i];
            }
        }
        return null;
    }

    // UserController.eed: thêm wing mới chế tạo.
    public void Add(WingModel model)
    {
        owned.Add(model);
        isDataChanged = true;
    }
}

// 1 wing đã sở hữu (WingModel gốc). CardCount gốc không dùng ở luồng craft nên bỏ.
[System.Serializable]
public class WingModel
{
    public int wingId;
    public int level = 1;
    public bool isNew;
    public List<GearSubStat> subStats = new List<GearSubStat>();
}

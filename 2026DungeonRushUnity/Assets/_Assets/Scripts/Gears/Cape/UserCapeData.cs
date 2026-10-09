using System.Collections.Generic;

// Save Cape người chơi. Schema gốc (il2cpp v41, class User):
//   OwnedCapes : List<CapeModel{InstanceId, CapeId, Level, CurrentXP, CardCount, IsNew, SubStats}> — mỗi lần summon
//                1 bản RIÊNG (InstanceId = Guid), cùng mẫu có thể có nhiều bản.
//   EquippedCapeId (= InstanceId), TotalCapeSummons, ShowCloak.
// Cape đang mặc còn được copy sang UserEquipmentData slot CAPE (equipId = capeId) cho chỉ số/hình —
// CapeService đồng bộ lại khi lên cấp. CloakCurrency nằm ở UserItemData (ItemType.CLOAK).
public class UserCapeData : BaseUserData
{
    public List<CapeModel> owned { get; set; } = new List<CapeModel>();
    public string equippedInstanceId { get; set; }
    public int totalSummons { get; set; }
    public bool showCloak { get; set; } = true;

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_CAPE;
    }

    public override void InitData()
    {
        base.InitData();
        owned = new List<CapeModel>();
        equippedInstanceId = null;
        totalSummons = 0;
        showCloak = true;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (owned == null)
        {
            owned = new List<CapeModel>();
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
            // UserController.eek: cape thiếu InstanceId thì cấp Guid mới.
            if (string.IsNullOrEmpty(owned[i].instanceId))
            {
                owned[i].instanceId = System.Guid.NewGuid().ToString();
                isDataChanged = true;
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

        if (string.IsNullOrEmpty(equippedInstanceId) == false && Get(equippedInstanceId) == null)
        {
            equippedInstanceId = null;
            isDataChanged = true;
        }

        if (totalSummons < 0)
        {
            totalSummons = 0;
            isDataChanged = true;
        }
    }

    // UserController.een
    public CapeModel Get(string instanceId)
    {
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i].instanceId == instanceId)
            {
                return owned[i];
            }
        }
        return null;
    }

    // UserController.efc: bỏ 1 cape khỏi kho (salvage).
    public void Remove(string instanceId)
    {
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i].instanceId == instanceId)
            {
                owned.RemoveAt(i);
                isDataChanged = true;
                return;
            }
        }
    }
}

// 1 cape đã sở hữu (CapeModel gốc). CardCount gốc không dùng ở luồng XP nên bỏ.
[System.Serializable]
public class CapeModel
{
    public string instanceId;
    public int capeId;
    public int level = 1;
    public int currentXP;
    public bool isNew;
    public List<GearSubStat> subStats = new List<GearSubStat>();
}

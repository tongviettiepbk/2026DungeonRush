using System.Collections.Generic;

// Save companion người chơi (module BaseUserData → 1 key PlayerPrefs JSON, xem UserData).
// Schema gốc (il2cpp v41): OwnedCompanions: List<CompanionModel> ; EquippedCompanions: List<string>
// (khoá = CompanionData.assetName, trang bị tối đa 3 con). Companion lên cấp bằng THẺ — xem
// [[dungonrush-companion-battle-classes]] / CompanionUpgradeConfig. Đăng ký ở UserData (key + Load + list).
public class UserCompanionData : BaseUserData
{
    public const int MAX_EQUIPPED = 3;

    public List<CompanionModel> owned { get; set; } = new List<CompanionModel>();
    public List<string> equipped { get; set; } = new List<string>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_COMPANION;
    }

    public override void InitData()
    {
        base.InitData();
        owned = new List<CompanionModel>();
        equipped = new List<string>();
        isDataChanged = true;
    }

    // Sửa dữ liệu sai/thiếu sau load: đảm bảo list khác null, clamp level, bỏ trang bị con không sở hữu.
    public override void ValidateData()
    {
        if (owned == null)
        {
            owned = new List<CompanionModel>();
            isDataChanged = true;
        }
        if (equipped == null)
        {
            equipped = new List<string>();
            isDataChanged = true;
        }

        for (int i = 0; i < owned.Count; i++)
        {
            CompanionModel m = owned[i];
            if (m == null)
            {
                continue;
            }

            if (m.level < 1)
            {
                m.level = 1;
                isDataChanged = true;
            }
            else if (m.level > CompanionUpgradeConfig.MAX_LEVEL)
            {
                m.level = CompanionUpgradeConfig.MAX_LEVEL;
                isDataChanged = true;
            }
        }

        // Bỏ trang bị con chưa sở hữu + cắt phần vượt số slot.
        for (int i = equipped.Count - 1; i >= 0; i--)
        {
            if (IsOwned(equipped[i]) == false)
            {
                equipped.RemoveAt(i);
                isDataChanged = true;
            }
        }
        if (equipped.Count > MAX_EQUIPPED)
        {
            equipped.RemoveRange(MAX_EQUIPPED, equipped.Count - MAX_EQUIPPED);
            isDataChanged = true;
        }
    }

    // ===== Truy vấn =====

    public CompanionModel GetModel(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i] != null && owned[i].companionId == id)
            {
                return owned[i];
            }
        }

        return null;
    }

    public bool IsOwned(string id)
    {
        return GetModel(id) != null;
    }

    // Level companion đang sở hữu; CHƯA sở hữu → 1 (giá trị nền để pet vẫn ra trận đúng công thức).
    public int GetLevel(string id)
    {
        CompanionModel m = GetModel(id);
        return m != null ? m.level : 1;
    }

    // ===== Nâng cấp / sở hữu (thẻ) =====

    // Thêm 1 companion vào kho nếu chưa có; trả về model (mới hoặc đang có).
    public CompanionModel Own(string id)
    {
        CompanionModel m = GetModel(id);
        if (m == null)
        {
            m = new CompanionModel(id);
            owned.Add(m);
            isDataChanged = true;
        }
        return m;
    }

    // Cộng `count` thẻ cho companion + TỰ lên cấp khi đủ (tiêu thẻ theo CompanionUpgradeConfig).
    // Chưa sở hữu thì tự sở hữu. Trả về SỐ CẤP vừa lên (0 nếu không lên).
    public int AddCards(string id, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        CompanionModel m = Own(id);
        m.cardCount += count;
        isDataChanged = true;

        int levelsGained = 0;
        while (m.level < CompanionUpgradeConfig.MAX_LEVEL)
        {
            int need = CompanionUpgradeConfig.GetCardsRequired(m.level);
            if (m.cardCount < need)
            {
                break;
            }

            m.cardCount -= need;
            m.level++;
            levelsGained++;
        }

        return levelsGained;
    }

    // ===== Trang bị (tối đa 3, khoá = assetName) =====

    public List<string> GetEquipped()
    {
        return equipped;
    }

    public bool IsEquipped(string id)
    {
        return equipped.Contains(id);
    }

    public bool Equip(string id)
    {
        if (IsOwned(id) == false || IsEquipped(id) || equipped.Count >= MAX_EQUIPPED)
        {
            return false;
        }

        equipped.Add(id);
        isDataChanged = true;
        return true;
    }

    public void Unequip(string id)
    {
        if (equipped.Remove(id))
        {
            isDataChanged = true;
        }
    }
}

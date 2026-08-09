using Newtonsoft.Json;
using UnityEngine;

// Class cha cho mọi module save data (UserXxxData).
// Mỗi module tự serialize thành 1 key PlayerPrefs riêng, chỉ ghi khi isDataChanged.
public class BaseUserData
{
    [JsonIgnore] public bool isDataChanged;

    protected virtual string GetDataKey()
    {
        return string.Empty;
    }

    public virtual bool Save(bool forceSave)
    {
        if (forceSave)
        {
            isDataChanged = true;
        }

        if (isDataChanged)
        {
            PlayerPrefs.SetString(GetDataKey(), JsonConvert.SerializeObject(this));
            isDataChanged = false;

            DebugCustom.Log("save_" + GetDataKey());

            return true;
        }
        else
        {
            return false;
        }
    }

    // Gọi 1 lần khi user mới (chưa có save) để set dữ liệu khởi đầu
    public virtual void InitData() { }

    // Gọi sau mỗi lần load để sửa dữ liệu sai/thiếu (đổi version, hack, migrate...)
    public virtual void ValidateData() { }

    public virtual void RefreshNewDay() { isDataChanged = true; }

    public virtual void RefreshNewWeek() { isDataChanged = true; }

    public virtual void RefreshNewMonth() { isDataChanged = true; }

    public virtual void RefreshNewYear() { isDataChanged = true; }

    // Dùng cho chấm đỏ notify trên UI
    public virtual bool IsNotify() { return false; }
}

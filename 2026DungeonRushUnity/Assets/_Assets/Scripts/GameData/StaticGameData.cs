// Config tĩnh của game (balance/thiết kế), load 1 lần lúc mở game.
//
// Thêm 1 nhóm config mới gồm 2 bước:
//   1. Tạo class StaticXxxData (load từ ScriptableObject trong Resources, hoặc formula)
//   2. Thêm field + dòng khởi tạo trong Load() ở đây
public class StaticGameData
{
    public StaticItemData items;
    public StaticCampaignData campaign;
    public StaticWeaponData weapons;

    public void Load()
    {
        if (items == null) items = new StaticItemData();
        if (campaign == null) campaign = new StaticCampaignData();
        if (weapons == null) weapons = new StaticWeaponData();
    }
}

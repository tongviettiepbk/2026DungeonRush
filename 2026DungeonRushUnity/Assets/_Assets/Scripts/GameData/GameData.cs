// Hub dữ liệu trung tâm: mọi nơi trong game truy cập qua
//   GameData.staticData.xxx  — config tĩnh
//   GameData.userData.xxx    — save người chơi
public class GameData
{
    public static StaticGameData staticData = new StaticGameData();
    public static UserData userData = new UserData();

    // Gọi 1 lần duy nhất lúc mở game (scene Login/Root)
    public static void Init()
    {
        staticData.Load();
        userData.Load();
    }

    public static void Reset()
    {
        staticData = new StaticGameData();
        userData = new UserData();
    }

    public static void Save(bool forceSave = false)
    {
        userData.Save(forceSave);
    }
}

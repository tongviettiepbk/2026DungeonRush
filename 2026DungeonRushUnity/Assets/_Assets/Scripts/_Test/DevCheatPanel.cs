using UnityEngine;
using UnityEngine.SceneManagement;

// Bảng cheat bấm trên màn hình (Editor + bản Dev): trên điện thoại không có phím tắt L/V/O/K/F/P/W nên gom ra nút.
// Tự tạo khi chạy (không phải đặt vào scene). Ở MainGame bấm nút "Cheat" góc dưới-trái (cạnh "Đồ", "Boss").
// Mỗi nút gọi đúng hàm cheat của UIMainLobby / BaseMode như phím tắt, rồi lưu save ngay (máy thật hay bị tắt app ngang).
public class DevCheatPanel : MonoBehaviour
{
    public bool showPanel;

#if UNITY_EDITOR || DEV_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject("DevCheatPanel");
        go.AddComponent<DevCheatPanel>();
        DontDestroyOnLoad(go);
    }

    private void OnGUI()
    {
        // Chỉ ở MainGame: ở Login chạm GameController.Instance sẽ tự tạo Singleton giả.
        if (SceneManager.GetActiveScene().name != StaticValue.SCENE_MAINGAME)
            return;
        if (GameData.userData == null)
            return;

        float scale = Screen.width / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float height = Screen.height / scale;

        if (GUI.Button(new Rect(112, height - 34, 60, 30), showPanel ? "X" : "Cheat"))
            showPanel = !showPanel;

        if (showPanel == false)
            return;

        UIMainLobby lobby = FindAnyObjectByType<UIMainLobby>();
        GUILayout.BeginArea(new Rect(4, 60, 532, 200), GUI.skin.box);

        if (lobby == null)
        {
            GUILayout.Label("Không thấy UIMainLobby");
        }
        else
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100 Loot")) Run(lobby.CheatAddLootTicket);
            if (GUILayout.Button("+2000 Vial")) Run(lobby.CheatAddVial);
            if (GUILayout.Button("+1000 Quặng")) Run(lobby.CheatAddOres);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1000 Cloak")) Run(lobby.CheatAddCloak);
            if (GUILayout.Button("Offline 2h")) Run(lobby.CheatOfflineEarning);
            if (GUILayout.Button("Mở hết pet")) Run(lobby.CheatUnlockAllPets);
            GUILayout.EndHorizontal();
        }

        BaseMode mode = GameController.Instance.mode;
        GUILayout.BeginHorizontal();
        GUI.enabled = mode != null;
        if (GUILayout.Button("Thắng trận")) mode.EndGame(true);
        if (GUILayout.Button("Thua trận")) mode.EndGame(false);
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private static void Run(System.Action cheat)
    {
        cheat();
        GameData.Save(true);
    }
#endif
}

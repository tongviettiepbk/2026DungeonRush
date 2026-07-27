using UnityEngine;
using UnityEngine.UI;

// Lobby tối giản: xác nhận scene flow chạy được và data người chơi đã nạp.
// Chỉ hiển thị tên + vàng để kiểm chứng luồng Root -> Login -> Lobby.
// Sẽ thay bằng LobbyManager đầy đủ (header, tabs, buttons) ở phần UI.
public class LobbyManager : MonoBehaviour
{
    public Text txWelcome;
    public Text txGold;

    private void Start()
    {
        // Phòng khi bấm Play thẳng ở scene Lobby trong editor (chưa qua Login).
        if (string.IsNullOrEmpty(GameData.userData.profile.userName))
        {
            GameData.Init();
        }

        if (txWelcome != null)
        {
            txWelcome.text = "Welcome, " + GameData.userData.profile.userName;
        }

        if (txGold != null)
        {
            txGold.text = "Gold: " + GameData.userData.items.GetQuantityHave(ItemType.GOLD);
        }
    }
}

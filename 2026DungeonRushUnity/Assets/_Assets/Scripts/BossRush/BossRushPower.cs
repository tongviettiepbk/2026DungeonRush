using System;

// Power gửi lên Boss Rush — GỐC BossRushController.ell → rm.iqm: Power người chơi tính từ save (PlayerPower),
// làm tròn rm.iov. Không phụ thuộc Hero đang có trong scene.
public static class BossRushPower
{
    public static double GetPlayerPower()
    {
        return PlayerPower.ToLong(PlayerPower.GetCurrent());
    }
}

using UnityEngine;

// Bản rút gọn so với StickIdle (Camera/CameraController.cs). Đủ bề mặt cho BaseUnit
// (biên trái/phải màn hình dùng để tính IsInBattleScreen).
// TODO(follow-stick): nối camera combat thật khi port.
public class CameraController : Singleton<CameraController>
{
    public Transform left;
    public Transform right;
}
